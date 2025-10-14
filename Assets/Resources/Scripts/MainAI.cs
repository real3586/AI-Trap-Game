using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rand = UnityEngine.Random;


public class MainAI : MonoBehaviour
{
    public static MainAI Instance { get; private set; }
    public struct State
    {
        /// <summary>
        /// How many sides are blocked? Uses a Quadrant system.
        /// </summary>
        public List<Enums.BlockedDirections> status;
        /// <summary>
        /// Where is the AI at this point?
        /// </summary>
        public int x, z;
        /// <summary>
        /// Holds a list booleans with indexes of the Actions enum, indicating whether or not that action is possible. 
        /// </summary>
        public List<bool> possibleActions;
        /// <summary>
        /// What did the AI decide to do in this situation?
        /// </summary>
        public Enums.Directions decidedAction;
        /// <summary>
        /// [-1, 1] with -1 being bad, 0 being okay, and 1 being good.
        /// </summary>
        public float decisionOutcome;
    }
    List<State> QTable = new();
    public struct GridItem
    {
        /// <summary>
        /// Does this square have a block?
        /// </summary>
        public bool isBlocked;
        public int visited;
        public int x, z;
        /// <summary>
        /// From 0 to 1, or (0, 1].
        /// </summary>
        public float blockWeight;

    }
    /// <summary>
    /// The MainGrid holds a grid of the GridItem struct.
    /// </summary>
    public readonly GridItem[,] MainGrid = new GridItem[9, 9];

    public bool IsLerping { get; private set; }

    [Header("UI Stuff")]
    [SerializeField] TextMeshProUGUI decisionText; // weird because Unity is
    [SerializeField] TextMeshProUGUI outcomeText, dataPointsText, randomText, similarityText;
    [SerializeField] TextMeshProUGUI pastExact, pastSimilar;
    [SerializeField] GameObject arrow, userModeStuff;
    [SerializeField] Button getBlockButton, analysisButton;

    [Header("Hyperparameters")]
    [SerializeField, Tooltip("Minimum value for a decision to be considered good")] 
    float goodDecisionThreshold = 0.5f;
    [SerializeField, Tooltip("Weight given to a good decision")]
    float goodDecisionWeight = 6f;
    [SerializeField, Tooltip("Minimum value for a decision to be considered okay")] 
    float okayDecisionThreshold = -0.5f;
    [SerializeField, Tooltip("Weight given to an okay decision")] 
    float okayDecisionWeight = 1.5f;
    [SerializeField, Tooltip("Weight given to a bad decision. Bad threshhold is based on not good and not okay")]
    float badDecisionWeight = 1.0f;
    [SerializeField, Tooltip("How similar 2 states need to be for the AI to consider it")] 
    float similarityThreshold = 0.8f;
    [SerializeField, Tooltip("Similarity weight of 2 states based on their shared blockedDirections. Higher = shared directions will have less impact on similarity")] 
    float similarityDirections = 10.0f;
    [SerializeField, Tooltip("Similarity weight of 2 states based on how close they are. Higher = distance will have more impact on similarity")] 
    float similarityDistance = 3.0f;
    bool wasRandomAction;
    List<float> averageSimilarity = new();

    public bool pauseAILearning = false;

    // for algorithm mode
    List<GridItem> algoPath = new();

    private void Awake()
    {
        Instance = this;
        ResetGrid();
    }

    public void ResetGrid()
    {
        // reset the grid
        for (int x = 0; x < MainGrid.GetLength(0); x++)
        {
            for (int y = 0; y < MainGrid.GetLength(1); y++)
            {
                MainGrid[x, y].isBlocked = false;
                MainGrid[x, y].visited = -1;
                MainGrid[x, y].x = x;
                MainGrid[x, y].z = y;
            }
        }
    }

    public IEnumerator AISequence()
    {
        int xPos = (int)transform.position.x;
        int zPos = (int)transform.position.z;

        State newState = GetState(xPos, zPos);

        // check if the game is over, and AI cannot make it to the end
        // 1/3: if there are no possible moves, or the whole list is false
        if (newState.possibleActions.All(value => value == false))
        {
            GameManager.Instance.GameEnd(false);
            yield break;
        }

        // 2/3: if there are any possible end points, ends the game a little faster
        List<Vector3> endpoints = GetValidEndpoints();
        if (endpoints.Count == 0)
        {
            GameManager.Instance.GameEnd(false);
            yield break;
        }

        // 3/3: pathfind to all the endpoints, and if they all are 1000, the game is over
        List<int> pathLengths = new();
        for (int i = 0; i < endpoints.Count; i++)
        {
            int x = (int)endpoints[i].x;
            int z = (int)endpoints[i].z;

            pathLengths.Add(TargetPath(xPos, zPos, x, z));
        }
        if (pathLengths.All(value => value == 1000))
        {
            GameManager.Instance.GameEnd(false);
            yield break;
        }

        // decide the action based on the state and given actions
        newState.decidedAction = DecideAction(newState, xPos, zPos);
        Vector3 currentPosition = transform.position;
        MoveAI(newState.decidedAction);

        // set the arrow position
        arrow.SetActive(true);
        arrow.transform.SetPositionAndRotation(transform.position + Vector3.up,
            Quaternion.Euler(0, Enums.directionToRotation[newState.decidedAction], 0));

        // shift the arrow a little forward
        arrow.transform.position += arrow.transform.forward * MathF.Sqrt(2) / 2;

        Vector3 hypotheticalPos = SimulateMove(newState.decidedAction, currentPosition);
        if (GameManager.Instance.mode == Enums.Modes.Classic)
        {
            // determine whether it was a good choice
            newState.decisionOutcome = DetermineChoiceOutcome(currentPosition, hypotheticalPos);
        }
        else
        {
            // disable new blocks
            getBlockButton.gameObject.SetActive(false);
            analysisButton.gameObject.SetActive(false);
            userModeStuff.SetActive(true);

            // wait for the user to provide feedback
            yield return new WaitUntil(() => GameManager.Instance.UserProvidedFeedback);
            GameManager.Instance.UserProvidedFeedback = false;

            // then reset everything
            getBlockButton.gameObject.SetActive(true);
            analysisButton.gameObject.SetActive(true);
            newState.decisionOutcome = GameManager.Instance.DecisionOutcome;
            userModeStuff.SetActive(false);
        }
        // add the new state to the Q table if we are allowed to
        if (!pauseAILearning) QTable.Add(newState);

        // update what the user sees
        UIUpdate(newState.decidedAction, newState.decisionOutcome);
        
        // finally check if the AI is on a winning square
        if (CheckWin((int)hypotheticalPos.x, (int)hypotheticalPos.z))
        {
            GameManager.Instance.GameEnd(true);
            yield break;
        }

        yield return null;
    }

    public State GetState(int xPos, int zPos)
    {
        // first check all possible moves, if any
        List<bool> possibleMoves = PossibleDirections(xPos, zPos);

        // decide the current state
        List<float> blockLocations = DetectBlocks(xPos, zPos);

        // take the highest value in the list
        float maxValue = 0;
        for (int i = 0; i < blockLocations.Count; i++)
        {
            if (maxValue < blockLocations[i])
            {
                maxValue = blockLocations[i];
            }
        }
        // if there is more than one highest or a tie, take them both
        List<Enums.BlockedDirections> mostBlockedDirections = new();
        for (int j = 0; j < blockLocations.Count; j++)
        {
            if (blockLocations[j] == maxValue)
            {
                mostBlockedDirections.Add((Enums.BlockedDirections)j);
            }
        }

        // create a new state with these details
        State newState = new()
        {
            status = mostBlockedDirections,
            possibleActions = possibleMoves,
            x = xPos,
            z = zPos
        };

        return newState;
    }

    #region UI Stuff
    void UIUpdate(Enums.Directions decision, float outcome)
    {
        // display the decided action and the outcome
        decisionText.text = "Latest Decision: Moved " + decision.ToString();
        outcomeText.text = "Outcome: " + outcome.ToString("F3"); // 3 decimal places

        // display the amount of data points it has
        dataPointsText.text = "Current Data Points: " + QTable.Count;

        // display whether it was a random action
        string randomAction = wasRandomAction ? "Yes" : "No";
        randomText.text = "Was Random: " + randomAction;

        // display the current average similarity scores
        float x = GetAverage(averageSimilarity);
        similarityText.text = "Average Similarity: " + x.ToString("F3");
    }

    public void ClearAI()
    {
        QTable.Clear();
    }

    public void ResetTextFields()
    {
        decisionText.text = "Latest Decision: ";
        randomText.text = "Was Random: ";
        outcomeText.text = "Outcome: ";
        similarityText.text = "";
    }

    public void AnalysisModeUpdate()
    {
        try
        {
            int[] pastStates = GetPreviousStates(QTable.Last());
            try
            {
                pastExact.text = "Previous Exact States: " + pastStates[0];
            }
            catch
            {
                pastExact.text = "Previous Exact States: 0";
            }
            try
            {
                pastSimilar.text = "Previous Similar States: " + pastStates[1];
            }
            catch
            {
                pastSimilar.text = "Previous Similar States: 0";
            }
        }
        catch
        {
            pastExact.text = "No data in table!";
            pastSimilar.text = "";
        }
    }

    int[] GetPreviousStates(State state)
    {
        // iterate through each qtable element
        // count how many times the ai has been in a given state
        int exactCount = 0;
        int similarCount = 0;
        for (int i = 0; i < QTable.Count; i++)
        {
            if (QTable[i].status.SequenceEqual(state.status))
            {
                exactCount++;
            }
            else if (SimilarityScore(QTable[i], state) >= 0.5f)
            {
                similarCount++;
            }
        }

        // add them to an array
        int[] counts =
        {
            exactCount, similarCount
        };
        return counts;
    }

    public float GetBlockWeight(Vector2 blockPosition)
    {
        float x = transform.position.x;
        float z = transform.position.z;
        Vector2 position = new(x, z);
        float distance = Vector2.Distance(position, blockPosition);

        return 1 / Mathf.Pow(distance, 2);
    }

    public float GetAverage(List<float> list)
    {
        float total = 0;
        foreach (float f in list)
        {
            total += f;
        }
        return total / list.Count;
    }
    #endregion
    #region Decision Making and Feedback
    /// <summary>
    /// Decides an action given a current state by pulling from past states
    /// </summary>
    /// <param name="state">The current state of the AI</param>
    /// <param name="xPos">X position of the AI</param>
    /// <param name="zPos">Z position of the AI</param>
    /// <returns></returns>
    Enums.Directions DecideAction(State state, int xPos, int zPos)
    {
        averageSimilarity.Clear();

        // Take a list of all possible actions
        List<Enums.Directions> potentialActions = new();

        // Check if the AI has made decisions in this state (or similar) before
        if (QTable.Any(entry => entry.status.SequenceEqual(state.status)) || 
            QTable.Any(entry => SimilarityScore(state, entry) >= similarityThreshold))
        {
            // If decisions are available, choose the action based on past outcomes
            foreach (var qEntry in QTable)
            {
                // this if checks for exact equal (100% similar)
                if (qEntry.status.SequenceEqual(state.status))
                {
                    // Make sure the action is possible before adding it
                    if (CanMoveInDirection(qEntry.decidedAction, xPos, zPos))
                    {
                        // Introduce a weight factor based on the decision outcome
                        float weight = GetWeight(qEntry.decisionOutcome);

                        // Add the action to the potential actions list based on its weight
                        for (int i = 0; i < Mathf.CeilToInt(weight); i++)
                        {
                            potentialActions.Add(qEntry.decidedAction);
                        }

                        // since the state is 100% similar, add 1 to the similarity tracker
                        averageSimilarity.Add(1);
                    }
                }
                // this else checks if it is at least somewhat similar (above 50%)
                else if (SimilarityScore(qEntry, state) >= similarityThreshold)
                {
                    // copy paste of code above
                    if (CanMoveInDirection(qEntry.decidedAction, xPos, zPos))
                    {
                        // this time reduce the weight by a little bit, 40%
                        float weight = GetWeight(qEntry.decisionOutcome) * 0.6f;

                        for (int i = 0; i < Mathf.CeilToInt(weight); i++)
                        {
                            potentialActions.Add(qEntry.decidedAction);
                        }

                        averageSimilarity.Add(SimilarityScore(qEntry, state));
                    }
                }
            }
            // not 100% guaranteed to do moves that are good
            // but they are much more likely to happen
            if (potentialActions.Count > 0)
            {
                int randomIndex = Rand.Range(0, potentialActions.Count - 1);
                wasRandomAction = false;
                return potentialActions[randomIndex];
            }
        }

        // If no past decisions, choose a random action from the possible actions
        wasRandomAction = true;
        return ChooseRandomAction(state.possibleActions);
    }

    /// <summary>
    /// Gives how similar history.status and toCompare are [0, 1].
    /// </summary>
    /// <param name="history">The state in the past</param>
    /// <param name="toCompare">The state to compare to history</param>
    /// <returns>[0, 1]</returns>
    float SimilarityScore(State history, State toCompare)
    {
        float similarity;

        // first check directions blocked
        // Note: there are 4 directions total
        // new list to store directions that both states share
        List<Enums.BlockedDirections> totalStates = new();

        // check if there are any more in the toCompare state
        for (int i = 0; i < 4; i++)
        {
            if (IsPresent(history.status, (Enums.BlockedDirections)i) && IsPresent(toCompare.status, (Enums.BlockedDirections)i))
            {
                totalStates.Add((Enums.BlockedDirections)i);
            }
        }
        // the length of the list is how many states they have in common
        similarity = totalStates.Count / similarityDirections;

        // next check position
        // use distance to check how close the ai was to the history point
        float distance = Vector2.Distance(new Vector2(history.x, history.z), new Vector2());

        // take the reciprocal
        distance = similarityDistance / distance;

        // add to similarity
        similarity += distance;
        return similarity;
    }

    bool IsPresent(List<Enums.BlockedDirections> blockedDirectionsList, Enums.BlockedDirections target)
    {
        for (int i = 0; i < blockedDirectionsList.Count; i++)
        {
            if (target == blockedDirectionsList[i])
            {
                return true;
            }
        }
        return false;
    }

    float GetWeight(float decisionOutcome)
    {
        if (decisionOutcome >= goodDecisionThreshold)
        {
            return decisionOutcome * goodDecisionWeight; // Weight for good decisions
        }
        else if (decisionOutcome > okayDecisionThreshold)
        {
            return decisionOutcome * okayDecisionWeight; // Weight for okay decisions
        }
        else
        {
            return decisionOutcome * badDecisionWeight; // Weight for bad decisions
        }
    }

    /// <summary>
    /// Returns a random direction from a list representing possible directions
    /// </summary>
    /// <param name="possibleActions">A boolean list with values corresponding to whether or not an action is valid</param>
    /// <returns>Any random direction that is possible</returns>
    Enums.Directions ChooseRandomAction(List<bool> possibleActions)
    {
        // Choose a random action from the list of possible actions
        List<Enums.Directions> validActions = new();

        for (int i = 0; i < possibleActions.Count; i++)
        {
            if (possibleActions[i])
            {
                validActions.Add((Enums.Directions)i);
            }
        }

        if (validActions.Count > 0)
        {
            int randomIndex = Rand.Range(0, validActions.Count);
            return validActions[randomIndex];
        }
        else
        {
            // If no valid actions, return a default action or handle it as needed
            return Enums.Directions.North;
        }
    }

    /// <summary>
    /// Gives the outcome for a specified choice by measuring how close it got us to the nearest endpoints
    /// </summary>
    /// <param name="currentPos">The current position as a Vector3</param>
    /// <param name="newPos">Where the AI ended up moving</param>
    /// <returns>[-1, 1] with -1 being a terrible move, and 1 being an amazing move. Returns 0 if currentPos is (4, 4)</returns>
    float DetermineChoiceOutcome(Vector3 currentPos, Vector3 newPos)
    {
        float choiceOutcome;
        int currentX = (int)currentPos.x;
        int currentZ = (int)currentPos.z;
        int newX = (int)newPos.x;
        int newZ = (int)newPos.z;

        // if we are at (4, 4) or the middle, return 0 immediately
        // it's hard to tell if moving from the middle is a good move sometimes
        if (currentX == 4 && currentZ == 4)
        {
            return 0;
        }

        // get all the valid endpoints from the current position
        List<Vector3> previousEdges = GetValidEndpoints();

        // by default if the result is an endpoint it was a good move
        for (int i = 0; i < previousEdges.Count; i++)
        {
            if (newPos == previousEdges[i])
            {
                return 1;
            }
        }

        // if there are no valid endpoints, terminate and end the game
        if (previousEdges.Count == 0)
        {
            GameManager.Instance.GameEnd(false);
            return -1;
        }

        // pathfind to all of the previous endpoints, then store the distance to all the points
        // the indexes will match
        List<int> previousPathLength = new();
        for (int i = 0; i < previousEdges.Count; i++)
        {
            int endX = (int)previousEdges[i].x;
            int endZ = (int)previousEdges[i].z;
            previousPathLength.Add(TargetPath(currentX, currentZ, endX, endZ));
        }

        // find the smallest endpoint distance
        int minDistance = 1000;
        for (int i = 0; i < previousEdges.Count; i++)
        {
            if (previousPathLength[i] < minDistance)
            {
                minDistance = previousPathLength[i];
            }
        }

        // from the smallest endpoint distance, find the endpoints with that distance
        List<Vector3> closestEndpoints = new();
        List<int> previousEndpointDistance = new();
        for (int i = 0; i < previousEdges.Count; i++)
        {
            if (previousPathLength[i] == minDistance)
            {
                // add the endpoint that corresponds to this index
                closestEndpoints.Add(previousEdges[i]);
                previousEndpointDistance.Add(previousPathLength[i]);
            }
        }

        // then pathfind to the closest points again using the new simulated position
        List<int> newEndpointDistance = new();
        for (int i = 0; i < closestEndpoints.Count; i++)
        {
            int endX = (int)closestEndpoints[i].x;
            int endZ = (int)closestEndpoints[i].z;
            newEndpointDistance.Add(TargetPath(newX, newZ, endX, endZ));
        }

        // then store the changes in distance to all points
        List <float> changeInDistance = new();
        for (int i = 0; i < newEndpointDistance.Count; i++)
        {
            // make sure to do previous - new, so that a good value is indicated by positive
            float delta = previousEndpointDistance[i] - newEndpointDistance[i];
            changeInDistance.Add(delta);
        }

        // finally take the average of the change in distance list
        choiceOutcome = changeInDistance.Average();
        choiceOutcome = Mathf.Clamp(choiceOutcome, -1, 1);
        return choiceOutcome;
    }
    #endregion
    #region Navigation to points
    void InitialSetup(int x, int z)
    {
        for (int i = 0; i < MainGrid.GetLength(0); i++)
        {
            for (int j = 0; j < MainGrid.GetLength(1); j++)
            {
                MainGrid[i, j].visited = -1;
            }
        }
        MainGrid[x, z].visited = 0;
    }

    void SetDistance(int x, int z)
    {
        InitialSetup(x, z);

        int rows = MainGrid.GetLength(0);
        int columns = MainGrid.GetLength(1);

        for (int step = 1; step < rows * columns; step++)
        {
            foreach (GridItem obj in MainGrid)
            {
                if (!obj.isBlocked && obj.visited == step - 1)
                {
                    TestEightDirections(obj.x, obj.z, step);
                }
            }
        }
    }
    /// <summary>
    /// Gets all the edgepoints that aren't blocked
    /// </summary>
    /// <returns>A list of non-blocked edgepoints</returns>
    List<Vector3> GetValidEndpoints()
    {
        List<Vector3> endpoints = new();

        // along the horizontal sides
        for (int x = 0; x < 9; x++)
        {
            if (!MainGrid[x, 8].isBlocked)
            {
                endpoints.Add(new Vector3(x, 1, 8));
            }
            if (!MainGrid[x, 0].isBlocked)
            {
                endpoints.Add(new Vector3(x, 1, 0));
            }
        }
        // along the vertical sides
        // from 1 to 7 to skip the corners (they got checked already)
        for (int y = 1; y < 8; y++)
        {
            if (!MainGrid[0, y].isBlocked)
            {
                endpoints.Add(new Vector3(0, 1, y));
            }
            if (!MainGrid[8, y].isBlocked)
            {
                endpoints.Add(new Vector3(8, 1, y));
            }
        }

        return endpoints;
    }

    /// <summary>
    /// Tries to pathfind to a specified point
    /// </summary>
    /// <param name="startX">X starting position</param>
    /// <param name="startZ">Z starting position</param>
    /// <param name="endX">X ending position</param>
    /// <param name="endZ">Z ending position</param>
    /// <returns>The length of the path to get from start to end, or 1000 if none</returns>
    int TargetPath(int startX, int startZ, int endX, int endZ)
    {
        SetDistance(startX, startZ);

        int step;
        int x = endX;
        int z = endZ;
        List<GridItem> path = new();
        List<GridItem> tempList = new();

        if (!MainGrid[endX, endZ].isBlocked && MainGrid[endX, endZ].visited > 0)
        {
            path.Add(MainGrid[x, z]);
            step = MainGrid[x, z].visited - 1;
        }
        else
        { 
            return 1000;
        }

        for (; step > -1; step--)
        {
            for (int i = 0; i < 8; i++)
            {
                if (TestDirection(x, z, step, (Enums.Directions)i))
                {
                    int tempX = x + (int)Enums.directionToVector[(Enums.Directions)i].x;
                    int tempZ = z + (int)Enums.directionToVector[(Enums.Directions)i].z;
                    tempList.Add(MainGrid[tempX, tempZ]);
                }
            }

            Vector3 target = new(endX, 0, endZ);
            GridItem tempObj = FindClosest(target, tempList);
            path.Add(tempObj);
            x = tempObj.x;
            z = tempObj.z;
            tempList.Clear();
        }

        return path.Count;
    }

    GridItem FindClosest(Vector3 targetLocation, List<GridItem> list)
    {
        float currentDistance = 1000;
        int indexNumber = 0;
        for (int i = 0; i < list.Count; i++)
        {
            Vector3 listItem = new(list[i].x, list[i].z);

            if (Vector3.Distance(targetLocation, listItem) < currentDistance)
            {
                currentDistance = Vector3.Distance(targetLocation, listItem);
                indexNumber = i;
            }
        }
        return list[indexNumber];
    }

    bool TestDirection(int xPos, int zPos, int step, Enums.Directions dir)
    {
        int rows = MainGrid.GetLength(0);
        int columns = MainGrid.GetLength(1);
        return dir switch
        {
            Enums.Directions.North => zPos + 1 < columns && !MainGrid[xPos, zPos + 1].isBlocked && MainGrid[xPos, zPos + 1].visited == step,
            Enums.Directions.South => zPos - 1 > -1 && !MainGrid[xPos, zPos - 1].isBlocked && MainGrid[xPos, zPos - 1].visited == step,
            Enums.Directions.West => xPos - 1 > -1 && !MainGrid[xPos - 1, zPos].isBlocked && MainGrid[xPos - 1, zPos].visited == step,
            Enums.Directions.East => xPos + 1 < rows && !MainGrid[xPos + 1, zPos].isBlocked && MainGrid[xPos + 1, zPos].visited == step,

            Enums.Directions.NorthEast => zPos + 1 < columns && xPos + 1 < rows && !MainGrid[xPos + 1, zPos + 1].isBlocked && MainGrid[xPos + 1, zPos + 1].visited == step,
            Enums.Directions.SouthEast => zPos - 1 > -1 && xPos + 1 < rows && !MainGrid[xPos + 1, zPos - 1].isBlocked && MainGrid[xPos + 1, zPos - 1].visited == step,
            Enums.Directions.NorthWest => zPos + 1 < columns && xPos - 1 > -1 && !MainGrid[xPos - 1, zPos + 1].isBlocked && MainGrid[xPos - 1, zPos + 1].visited == step,
            Enums.Directions.SouthWest => zPos - 1 > -1 && xPos - 1 > -1 && !MainGrid[xPos - 1, zPos - 1].isBlocked && MainGrid[xPos - 1, zPos - 1].visited == step,
            _ => false,
        };
    }

    void TestEightDirections(int x, int z, int step)
    {
        for (int i = 0; i < 8; i++)
        {
            if (TestDirection(x, z, -1, (Enums.Directions)i))
            {
                int tempX = x + (int)Enums.directionToVector[(Enums.Directions)i].x;
                int tempZ = z + (int)Enums.directionToVector[(Enums.Directions)i].z;

                // Check if the adjacent positions in the diagonal direction are not blocked
                if (!MainGrid[x, tempZ].isBlocked && !MainGrid[tempX, z].isBlocked)
                {
                    SetVisited(tempX, tempZ, step);
                }
            }
        }
    }

    void SetVisited(int x, int y, int step)
    {
        if (!MainGrid[x, y].isBlocked)
        {
            MainGrid[x, y].visited = step;
        }
    }
    #endregion
    #region Block Analysis
    /// <summary>
    /// Simulates moving the AI, if possible
    /// </summary>
    /// <param name="dir">The direction to move it in</param>
    /// <param name="currentPos">Current position of the AI</param>
    /// <returns>The resulting position as a Vector3 if valid, Vector3.zero if invalid</returns>
    Vector3 SimulateMove(Enums.Directions dir, Vector3 currentPos)
    {
        // uses the same code as MoveAI, but just returns a Vector3 instead of moving anything
        int xPos = (int)currentPos.x;
        int zPos = (int)currentPos.z;

        if (CanMoveInDirection(dir, xPos, zPos))
        {
            return currentPos + Enums.directionToVector[dir];
        }
        return Vector3.zero;
    }

    /// <summary>
    /// Called by the GameManager to update the local grid
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void AddBlock(int x, int y)
    {
        // if there is already a block don't do anything
        if (MainGrid[x, y].isBlocked == true)
        {
            return;
        }
        MainGrid[x, y].isBlocked = true;
    }

    /// <summary>
    /// Counts the number of blocks in quadrants around the AI
    /// </summary>
    /// <param name="xPos">X position of the AI</param>
    /// <param name="zPos">Z position of the AI</param>
    /// <returns>A list of floats corresponding to how many blocks are in each quadrant (weighted)</returns>
    List<float> DetectBlocks(float xPos, float zPos)
    {
        Vector2 aiPos = new(xPos, zPos);
        List<float> floats = new();
        float blockCountNE = 0, blockCountNW = 0, blockCountSE = 0, blockCountSW = 0;

        for (int x = 0; x < MainGrid.GetLength(0); x++)
        {
            for (int y = 0; y < MainGrid.GetLength(1); y++)
            {
                // if true, a square is there (blocked)
                // if not, continue
                if (MainGrid[x, y].isBlocked)
                {                       
                    // the closer the block is, the more it will be accounted for
                    // diagonals: the ai will not care as much
                    Vector2 blockPosition = new(x, y);
                    float distance = 1 / Mathf.Pow(Vector2.Distance(aiPos, blockPosition), 2);

                    // check where the block is relative to the agent
                    // agent will learn to avoid directions with large amounts of blocks
                    if (x >= xPos && y >= zPos)
                    {
                        blockCountNE += distance;
                    }
                    if (x >= xPos && y <= zPos)
                    {
                        blockCountSE += distance;
                    }                    
                    if (x <= xPos && y >= zPos)
                    {
                        blockCountNW += distance;
                    }
                    if (x <= xPos && y <= zPos)
                    {
                        blockCountSW += distance;
                    }
                }
            }
        }

        // append all items to a list
        floats.Add(blockCountNE);
        floats.Add(blockCountNW);
        floats.Add(blockCountSE);
        floats.Add(blockCountSW);

        return floats;
    }
    #endregion
    #region Moving
    /// <summary>
    /// Returns whether or not AI can move in this direction.
    /// </summary>
    /// <param name="dir">The Direction to be checked.</param>
    /// <returns>Whether or not the direction is blocked.</returns>
    bool CanMoveInDirection(Enums.Directions dir, int xPos, int zPos)
    {
        try
        {
            return dir switch
            {
                // "straight" directions
                Enums.Directions.North => !MainGrid[xPos, zPos + 1].isBlocked,
                Enums.Directions.South => !MainGrid[xPos, zPos - 1].isBlocked,
                Enums.Directions.West => !MainGrid[xPos - 1, zPos].isBlocked,
                Enums.Directions.East => !MainGrid[xPos + 1, zPos].isBlocked,

                // diagonal directions
                Enums.Directions.NorthEast => !MainGrid[xPos + 1, zPos + 1].isBlocked && (!MainGrid[xPos + 1, zPos].isBlocked || !MainGrid[xPos, zPos + 1].isBlocked),
                Enums.Directions.SouthEast => !MainGrid[xPos + 1, zPos - 1].isBlocked && (!MainGrid[xPos + 1, zPos].isBlocked || !MainGrid[xPos, zPos - 1].isBlocked),
                Enums.Directions.NorthWest => !MainGrid[xPos - 1, zPos + 1].isBlocked && (!MainGrid[xPos - 1, zPos].isBlocked || !MainGrid[xPos, zPos + 1].isBlocked),
                Enums.Directions.SouthWest => !MainGrid[xPos - 1, zPos - 1].isBlocked && (!MainGrid[xPos - 1, zPos].isBlocked || !MainGrid[xPos, zPos - 1].isBlocked),
                _ => false,
            };
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets all the possible moves for the AI at its current position
    /// </summary>
    /// <param name="posX">X position of the AI</param>
    /// <param name="posZ">Y position of the AI</param>
    /// <returns></returns>
    List<bool> PossibleDirections(int posX, int posZ)
    {
        List<bool> possibleMoves = new();

        for (int i = 0; i < Enum.GetNames(typeof(Enums.Directions)).Length; i++)
        {
            Enums.Directions direction = (Enums.Directions)i;

            possibleMoves.Add(CanMoveInDirection(direction, posX, posZ));
        }

        return possibleMoves;
    }

    /// <summary>
    /// Checks if the current position is on the edge of the square
    /// </summary>
    /// <param name="posX">X position of the AI</param>
    /// <param name="posZ">Z position of the AI</param>
    /// <returns>True if the AI is on the edge of the square, and false otherwise</returns>
    bool CheckWin(int posX, int posZ)
    {
        // a win is considered to be each side of the square
        if (posX == 0 || posZ == 0)
        {
            return true;
        }
        else if (posX == MainGrid.GetLength(0) - 1 || posZ == MainGrid.GetLength(0) - 1)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Moves the AI in a specified direction dir
    /// </summary>
    /// <param name="dir">The direction to move the AI</param>
    void MoveAI(Enums.Directions dir)
    {
        Vector3 currentPos = transform.position;
        int xPos = (int)currentPos.x;
        int zPos = (int)currentPos.z;

        if (CanMoveInDirection(dir, xPos, zPos))
        {
            StartCoroutine(LerpFunction(currentPos, currentPos + Enums.directionToVector[dir]));
        }
    }

    IEnumerator LerpFunction(Vector3 start, Vector3 end)
    {
        IsLerping = true;

        // lerps between the two start and end points in speed seconds
        float time = 0;

        while (time < 0.5)
        {
            time += Time.deltaTime;

            transform.position = Vector3.Lerp(start, end, time * 2);
            yield return new WaitForEndOfFrame();
        }
        transform.position = end;

        IsLerping = false;
        yield return null;
    }
    #endregion
    #region Algorithm Stuff
    public IEnumerator AlgoSequence()
    {
        int xPos = (int)transform.position.x;
        int zPos = (int)transform.position.z;
        Vector3 hypotheticalPos = Vector3.zero;

        // first check the current position and possible moves
        List<bool> possibleMoves = PossibleDirections(xPos, zPos);

        // code stolen from main ai sequence
        // run some calculations to check if the game is even possible to win
        // check if there are any possible end points, ends the game a little faster
        List<Vector3> endpoints = GetValidEndpoints();
        if (endpoints.Count == 0)
        {
            GameManager.Instance.GameEnd(false);
            yield break;
        }

        // pathfind to all the endpoints, and if they all are 1000, the game is over
        List<int> pathLengths = new();
        for (int i = 0; i < endpoints.Count; i++)
        {
            int x = (int)endpoints[i].x;
            int z = (int)endpoints[i].z;

            pathLengths.Add(TargetPath(xPos, zPos, x, z));
        }
        if (pathLengths.All(value => value == 1000))
        {
            GameManager.Instance.GameEnd(false);
            yield break;
        }

        // iterate through all possible moves and check for instant wins
        bool foundInstantWin = false;
        Enums.Directions travelDir = 0;
        for (int i = 0; i < possibleMoves.Count; i++)
        {
            if (possibleMoves[i])
            {
                Enums.Directions dir = (Enums.Directions)i;
                hypotheticalPos = SimulateMove(dir, transform.position);
                if (CheckWin((int)hypotheticalPos.x, (int)hypotheticalPos.z))
                {
                    // if it is an instant win via the simulated move, instantly move there
                    ExecuteMoveAlgo(dir);
                    foundInstantWin = true;
                    travelDir = (Enums.Directions)i;
                    break;
                }
            }
        }

        if (!foundInstantWin)
        {
            // if no instant wins, use the existing weight function code to find the closest points
            List<Vector3> bestMoves = FindBestMoveAlgo(xPos, zPos);

            // pick a random point
            int random = Rand.Range(0, bestMoves.Count);

            // pathfind to that point
            // we don't need the whole path, just the first move
            // so use a modified target path function
            Vector3 point = bestMoves[random];
            if (AlgoPath(xPos, zPos, (int)point.x, (int)point.z))
            {
                Vector3 nextStep = new(algoPath[1].x, 1, algoPath[1].z);
                Vector3 currentPos = new(xPos, 1, zPos);
                Vector3 directionVector = nextStep - currentPos;
                Debug.Log(directionVector);
                travelDir = Enums.vectorToDirection[directionVector];
                hypotheticalPos = SimulateMove(travelDir, currentPos);
                ExecuteMoveAlgo(travelDir);
            }
        }

        // set the arrow position
        arrow.SetActive(true);
        arrow.transform.SetPositionAndRotation(transform.position + Vector3.up,
            Quaternion.Euler(0, Enums.directionToRotation[travelDir], 0));

        // shift the arrow a little forward
        arrow.transform.position += arrow.transform.forward * MathF.Sqrt(2) / 2;

        // finally check if the AI is on a winning square
        if (CheckWin((int)hypotheticalPos.x, (int)hypotheticalPos.z))
        {
            GameManager.Instance.GameEnd(true);
            yield break;
        }
        yield return null;
    }

    void ExecuteMoveAlgo(Enums.Directions dir)
    {
        Vector3 currentPos = transform.position;
        int xPos = (int)currentPos.x;
        int zPos = (int)currentPos.z;

        if (CanMoveInDirection(dir, xPos, zPos))
        {
            StartCoroutine(LerpFunction(currentPos, currentPos + Enums.directionToVector[dir]));
        }
    }

    List<Vector3> FindBestMoveAlgo(int xPos, int zPos)
    {
        // get all the valid endpoints from the current position
        List<Vector3> previousEdges = GetValidEndpoints();

        // pathfind to all of them, then store the distance to all the points
        // the indexes will match
        List<int> pathLength = new();
        for (int i = 0; i < previousEdges.Count; i++)
        {
            int endX = (int)previousEdges[i].x;
            int endZ = (int)previousEdges[i].z;
            pathLength.Add(TargetPath(xPos, zPos, endX, endZ));
        }

        // find the smallest endpoint distance
        int minDistance = 1000;
        for (int i = 0; i < previousEdges.Count; i++)
        {
            if (pathLength[i] < minDistance)
            {
                minDistance = pathLength[i];
            }
        }

        // from the smallest endpoint distance find the endpoints with that distance or less
        List<Vector3> closestEndpoints = new();
        List<int> previousEndpointDistance = new();
        for (int k = 0; k < previousEdges.Count; k++)
        {
            if (pathLength[k] <= minDistance)
            {
                // add the endpoint that corresponds to this index
                closestEndpoints.Add(previousEdges[k]);
                previousEndpointDistance.Add(pathLength[k]);
            }
        }

        // return the endpoints with that distance
        return closestEndpoints;
    }

    bool AlgoPath(int startX, int startZ, int endX, int endZ)
    {
        SetDistance(startX, startZ);

        int step;
        int x = endX;
        int z = endZ;
        List<GridItem> tempList = new();

        algoPath.Clear();

        if (!MainGrid[endX, endZ].isBlocked && MainGrid[endX, endZ].visited > 0)
        {
            algoPath.Add(MainGrid[x, z]);
            step = MainGrid[x, z].visited - 1;
        }
        else
        {
            return false;
        }

        for (; step > -1; step--)
        {
            for (int i = 0; i < 8; i++)
            {
                if (TestDirection(x, z, step, (Enums.Directions)i))
                {
                    int tempX = x + (int)Enums.directionToVector[(Enums.Directions)i].x;
                    int tempZ = z + (int)Enums.directionToVector[(Enums.Directions)i].z;
                    tempList.Add(MainGrid[tempX, tempZ]);
                }
            }

            Vector3 target = new(endX, 0, endZ);
            GridItem tempObj = FindClosest(target, tempList);
            algoPath.Add(tempObj);
            x = tempObj.x;
            z = tempObj.z;
            tempList.Clear();
        }
        algoPath.Reverse();
        return true;
    }
    #endregion
}