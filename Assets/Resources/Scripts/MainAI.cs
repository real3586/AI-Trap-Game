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
    struct State
    {
        /// <summary>
        /// How many sides are blocked? Uses a Quadrant system.
        /// </summary>
        public List<States> status;
        /// <summary>
        /// Holds a list booleans with indexes of the Actions enum, indicating whether or not that action is possible. 
        /// </summary>
        public List<bool> possibleActions;
        /// <summary>
        /// What did the AI decide to do in this situation?
        /// </summary>
        public Directions decidedAction;
        /// <summary>
        /// -1, 0, and 1, with -1 being bad, 0 being okay, and 1 being good.
        /// </summary>
        public float decisionOutcome;
    }
    List<State> QTable = new();

    struct GridItem
    {
        /// <summary>
        /// Does this square have a block?
        /// </summary>
        public bool isBlocked;
        public int visited;
        public int x, z;
    }
    /// <summary>
    /// The MainGrid holds a grid of the GridItem struct.
    /// </summary>
    readonly GridItem[,] MainGrid = new GridItem[9, 9];

    public bool IsLerping { get; private set; }

    [SerializeField] TextMeshProUGUI decisionText, outcomeText, dataPointsText, randomText;
    [SerializeField] GameObject arrow, userModeStuff;
    [SerializeField] Button getBlockButton;
    bool wasRandomAction;

    enum Directions { North, South, West, East, NorthEast, SouthEast, NorthWest, SouthWest }
    enum States { NortheastBlocked, NorthwestBlocked, SoutheastBlocked, SouthwestBlocked }

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

    public void ClearAI()
    {
        QTable.Clear();
    }

    public IEnumerator AISequence()
    {
        int xPos = (int)transform.position.x;
        int zPos = (int)transform.position.z;

        // first check all possible moves, if any
        List<bool> possibleMoves = PossibleDirections(xPos, zPos);

        // decide the current state
        List<int> blockLocations = DetectBlocks(xPos, zPos);

        // take the highest value in the list
        int maxValue = 0;
        for (int i = 0; i < blockLocations.Count; i++)
        {
            if (maxValue < blockLocations[i])
            {
                maxValue = blockLocations[i];
            }
        }
        // if there is more than one highest or a tie, take them both
        List<States> mostBlockedDirections = new();
        for (int j = 0; j < blockLocations.Count; j++)
        {
            if (blockLocations[j] == maxValue)
            {
                mostBlockedDirections.Add((States)j);
            }
        }

        State newState = new()
        {
            status = mostBlockedDirections,
            possibleActions = possibleMoves
        };

        // terminate the function and end the game if there are no possible moves, or the whole list is false
        if (possibleMoves.All(value => value == false))
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
            Quaternion.Euler(0, directionToRotation[newState.decidedAction], 0));

        // shift the arrow a little forward
        arrow.transform.position += arrow.transform.forward * MathF.Sqrt(2) / 2;

        Vector3 hypotheticalPos = SimulateMove(newState.decidedAction, currentPosition);
        if (!GameManager.Instance.isUserMode)
        {
            // determine whether it was a good choice
            newState.decisionOutcome = DetermineChoiceOutcome(currentPosition, hypotheticalPos);
        }
        else
        {
            // disable new blocks
            getBlockButton.gameObject.SetActive(false);
            userModeStuff.SetActive(true);

            // wait for the user to provide feedback
            yield return new WaitUntil(() => GameManager.Instance.UserProvidedFeedback);
            GameManager.Instance.UserProvidedFeedback = false;

            // then reset everything
            getBlockButton.gameObject.SetActive(true);
            newState.decisionOutcome = GameManager.Instance.DecisionOutcome;
            userModeStuff.SetActive(false);
        }
        // add the new state to the Q table
        QTable.Add(newState);

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

    void UIUpdate(Directions decision, float outcome)
    {
        // display the decided action and the outcome
        decisionText.text = "Latest Decision: Moved " + decision.ToString();
        outcomeText.text = "Outcome: " + outcome.ToString("F3"); // 3 decimal places

        // display the amount of data points it has
        dataPointsText.text = "Current Data Points: " + QTable.Count;

        // display whether it was a random action
        string randomAction = wasRandomAction ? "Yes" : "No";
        randomText.text = "Was Random: " + randomAction;
    }

    public void ResetTextFields()
    {
        decisionText.text = "Latest Decision: ";
        randomText.text = "Was Random: ";
        outcomeText.text = "Outcome: ";
    }

    #region Decision Making and Feedback
    Directions DecideAction(State state, int xPos, int zPos)
    {
        // Take a list of all possible actions
        List<Directions> potentialActions = new();

        // Check if the AI has made decisions in this state before
        if (QTable.Any(entry => entry.status.SequenceEqual(state.status)))
        {
            // If decisions are available, choose the action based on past outcomes
            foreach (var qEntry in QTable)
            {
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
                    }
                }
            }
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

    float GetWeight(float decisionOutcome)
    {
        if (decisionOutcome >= 0.5f)
        {
            return decisionOutcome * 3; // Weight for good decisions
        }
        else if (decisionOutcome > -0.5f)
        {
            return decisionOutcome * 1.5f; // Weight for okay decisions
        }
        else
        {
            return decisionOutcome; // Weight for bad decisions
        }
    }

    Directions ChooseRandomAction(List<bool> possibleActions)
    {
        // Choose a random action from the list of possible actions
        List<Directions> validActions = new();

        for (int i = 0; i < possibleActions.Count; i++)
        {
            if (possibleActions[i])
            {
                validActions.Add((Directions)i);
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
            return Directions.North;
        }
    }

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
        }

        // pathfind to all of them, then store the distance to all the points
        // the indexes will match
        List<int> pathLength = new();
        for (int i = 0; i < previousEdges.Count; i++)
        {
            int endX = (int)previousEdges[i].x;
            int endZ = (int)previousEdges[i].z;
            pathLength.Add(TargetPath(currentX, currentZ, endX, endZ));
        }

        // find the smallest endpoint distance
        int minDistance = 1000;
        for (int j = 0; j < previousEdges.Count; j++)
        {
            if (pathLength[j] < minDistance)
            {
                minDistance = pathLength[j];
            }
        }

        // from the smallest endpoint distance find the endpoints with that distance
        List<Vector3> closestEndpoints = new();
        List<int> previousEndpointDistance = new();
        for (int k = 0; k < previousEdges.Count; k++)
        {
            if (pathLength[k] == minDistance)
            {
                // add the endpoint that corresponds to this index
                closestEndpoints.Add(previousEdges[k]);
                previousEndpointDistance.Add(pathLength[k]);
            }
        }

        // then pathfind to the closest points again using the new simulated position
        // keep track of the change in distance
        List<int> newEndpointDistance = new();
        for (int m = 0; m < closestEndpoints.Count; m++)
        {
            int endX = (int)closestEndpoints[m].x;
            int endZ = (int)closestEndpoints[m].z;
            newEndpointDistance.Add(TargetPath(newX, newZ, endX, endZ));
        }

        // then store the changes in distance to all points
        List<int> changeInDistance = new();
        for (int n = 0; n < newEndpointDistance.Count; n++)
        {
            // make sure to do previous - new, so that a good value is indicated by positive
            int delta = previousEndpointDistance[n] - newEndpointDistance[n];
            changeInDistance.Add(delta);
        }

        // finally take the average of the change in distance list
        choiceOutcome = (float)changeInDistance.Average();
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
                if (TestDirection(x, z, step, (Directions)i))
                {
                    int tempX = x + (int)directionToVector[(Directions)i].x;
                    int tempZ = z + (int)directionToVector[(Directions)i].z;
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

    bool TestDirection(int xPos, int zPos, int step, Directions dir)
    {
        int rows = MainGrid.GetLength(0);
        int columns = MainGrid.GetLength(1);
        return dir switch
        {
            Directions.North => zPos + 1 < columns && !MainGrid[xPos, zPos + 1].isBlocked && MainGrid[xPos, zPos + 1].visited == step,
            Directions.South => zPos - 1 > -1 && !MainGrid[xPos, zPos - 1].isBlocked && MainGrid[xPos, zPos - 1].visited == step,
            Directions.West => xPos - 1 > -1 && !MainGrid[xPos - 1, zPos].isBlocked && MainGrid[xPos - 1, zPos].visited == step,
            Directions.East => xPos + 1 < rows && !MainGrid[xPos + 1, zPos].isBlocked && MainGrid[xPos + 1, zPos].visited == step,

            Directions.NorthEast => zPos + 1 < columns && xPos + 1 < rows && !MainGrid[xPos + 1, zPos + 1].isBlocked && MainGrid[xPos + 1, zPos + 1].visited == step,
            Directions.SouthEast => zPos - 1 > -1 && xPos + 1 < rows && !MainGrid[xPos + 1, zPos - 1].isBlocked && MainGrid[xPos + 1, zPos - 1].visited == step,
            Directions.NorthWest => zPos + 1 < columns && xPos - 1 > -1 && !MainGrid[xPos - 1, zPos + 1].isBlocked && MainGrid[xPos - 1, zPos + 1].visited == step,
            Directions.SouthWest => zPos - 1 > -1 && xPos - 1 > -1 && !MainGrid[xPos - 1, zPos - 1].isBlocked && MainGrid[xPos - 1, zPos - 1].visited == step,
            _ => false,
        };
    }

    void TestEightDirections(int x, int z, int step)
    {
        for (int i = 0; i < 8; i++)
        {
            if (TestDirection(x, z, -1, (Directions)i))
            {
                int tempX = x + (int)directionToVector[(Directions)i].x;
                int tempZ = z + (int)directionToVector[(Directions)i].z;
                SetVisited(tempX, tempZ, step);
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
    Vector3 SimulateMove(Directions dir, Vector3 currentPos)
    {
        // uses the same code as MoveAI, but just returns a Vector3 instead of moving anything
        int xPos = (int)currentPos.x;
        int zPos = (int)currentPos.z;

        if (CanMoveInDirection(dir, xPos, zPos))
        {
            return currentPos + directionToVector[dir];
        }
        return Vector3.zero;
    }

    public void AddBlock(int x, int y)
    {
        // if there is already a block don't do anything
        if (MainGrid[x, y].isBlocked == true)
        {
            return;
        }
        MainGrid[x, y].isBlocked = true;
    }

    List<int> DetectBlocks(float xPos, float zPos)
    {
        List<int> ints = new();
        int blockCountNE = 0, blockCountNW = 0, blockCountSE = 0, blockCountSW = 0;

        for (int x = 0; x < MainGrid.GetLength(0); x++)
        {
            for (int y = 0; y < MainGrid.GetLength(1); y++)
            {
                // if true, a square is there (blocked)
                // if not, continue
                if (MainGrid[x, y].isBlocked)
                {
                    // check where the block is relative to the agent
                    // agent will learn to avoid directions with large amounts of blocks
                    if (x >= xPos && y >= zPos)
                    {
                        blockCountNE++;
                    }
                    if (x >= xPos && y <= zPos)
                    {
                        blockCountSE++;
                    }                    
                    if (x <= xPos && y >= zPos)
                    {
                        blockCountNW++;
                    }
                    if (x <= xPos && y <= zPos)
                    {
                        blockCountSW++;
                    }
                }
            }
        }

        // append all items to a list
        ints.Add(blockCountNE);
        ints.Add(blockCountNW);
        ints.Add(blockCountSE);
        ints.Add(blockCountSW);

        return ints;
    }
    #endregion
    #region Moving
    /// <summary>
    /// Use this function to check whether or not AI can move in this direction.
    /// </summary>
    /// <param name="dir">The Direction to be checked.</param>
    /// <returns>Whether or not the direction is blocked.</returns>
    bool CanMoveInDirection(Directions dir, int xPos, int zPos)
    {
        try
        {
            return dir switch
            {
                // "straight" directions
                Directions.North => !MainGrid[xPos, zPos + 1].isBlocked,
                Directions.South => !MainGrid[xPos, zPos - 1].isBlocked,
                Directions.West => !MainGrid[xPos - 1, zPos].isBlocked,
                Directions.East => !MainGrid[xPos + 1, zPos].isBlocked,

                // diagonal directions
                Directions.NorthEast => !MainGrid[xPos + 1, zPos + 1].isBlocked && (!MainGrid[xPos + 1, zPos].isBlocked || !MainGrid[xPos, zPos + 1].isBlocked),
                Directions.SouthEast => !MainGrid[xPos + 1, zPos - 1].isBlocked && (!MainGrid[xPos + 1, zPos].isBlocked || !MainGrid[xPos, zPos - 1].isBlocked),
                Directions.NorthWest => !MainGrid[xPos - 1, zPos + 1].isBlocked && (!MainGrid[xPos - 1, zPos].isBlocked || !MainGrid[xPos, zPos + 1].isBlocked),
                Directions.SouthWest => !MainGrid[xPos - 1, zPos - 1].isBlocked && (!MainGrid[xPos - 1, zPos].isBlocked || !MainGrid[xPos, zPos - 1].isBlocked),
                _ => false,
            };
        }
        catch
        {
            return false;
        }
    }

    List<bool> PossibleDirections(int posX, int posZ)
    {
        List<bool> possibleMoves = new();

        for (int i = 0; i < Enum.GetNames(typeof(Directions)).Length; i++)
        {
            Directions direction = (Directions)i;

            possibleMoves.Add(CanMoveInDirection(direction, posX, posZ));
        }

        return possibleMoves;
    }

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

    void MoveAI(Directions dir)
    {
        Vector3 currentPos = transform.position;
        int xPos = (int)currentPos.x;
        int zPos = (int)currentPos.z;

        if (CanMoveInDirection(dir, xPos, zPos))
        {
            StartCoroutine(LerpFunction(currentPos, currentPos + directionToVector[dir]));
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
    readonly Dictionary<Directions, Vector3> directionToVector = new()
    {
        {Directions.North, Vector3.forward},
        {Directions.South, Vector3.back },
        {Directions.West, Vector3.left },
        {Directions.East, Vector3.right },
        {Directions.NorthEast, Vector3.forward + Vector3.right },
        {Directions.SouthEast, Vector3.back + Vector3.right },
        {Directions.NorthWest, Vector3.forward + Vector3.left },
        {Directions.SouthWest, Vector3.back + Vector3.left }
    };
    readonly Dictionary<Directions, float> directionToRotation = new()
    {
        {Directions.North, 0},
        {Directions.South, 180 },
        {Directions.West, 270 },
        {Directions.East, 90 },
        {Directions.NorthEast, 45 },
        {Directions.SouthEast, 135 },
        {Directions.NorthWest, 315 },
        {Directions.SouthWest, 225 }
    };
}