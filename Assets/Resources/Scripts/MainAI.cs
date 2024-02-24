using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MainAI : MonoBehaviour
{
    public static MainAI Instance { get; private set; }

    struct State
    {
        public List<States> status;
        public List<bool> possibleActions;
        public Actions decidedAction;
        public bool decisionWasGood;
    }

    /// <summary>
    /// The MainGrid holds true values if there is a block present on these coords, and false otherwise.
    /// </summary>
    public bool[,] MainGrid = new bool[9, 9];
    List<State> QTable = new();

    public bool IsLerping { get; private set; }

    enum Directions { North, South, West, East, NorthEast, SouthEast, NorthWest, SouthWest };
    enum Actions { MoveNorth, MoveSouth, MoveWest, MoveEast, MoveNorthEast, MoveSouthEast, MoveNorthWest, MoveSouthWest };
    enum States { NortheastBlocked, NorthwestBlocked, SoutheastBlocked, SouthwestBlocked }

    private void Awake()
    {
        Instance = this;

        // reset the grid
        for (int x = 0; x < MainGrid.GetLength(0); x++)
        {
            for (int y = 0;  y < MainGrid.GetLength(1); y++)
            {
                MainGrid[x, y] = false;
            }
        }
    }

    public void AISequence()
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

        for (int k = 0;  k < mostBlockedDirections.Count; k++)
        {
            Debug.Log(mostBlockedDirections[k]);
        }

        State newState = new()
        {
            status = mostBlockedDirections,
            possibleActions = possibleMoves
        };

        // terminate the function and end the game if there are no possible moves, or the whole list is false
        if (possibleMoves.All(value => value == false))
        {
            Debug.Log("game over");
            return;
        }

        // decide the action based on the state and given actions
        newState.decidedAction = DecideAction(newState);
        Vector3 currentPosition = transform.position;
        MoveAI(newState.decidedAction);

        // determine whether it was a good choice
        Vector3 previousPosition = SimulateMove(newState.decidedAction, currentPosition);
        newState.decisionWasGood = DetermineChoiceOutcome(currentPosition, previousPosition);

        // add the new state to the Q table
        QTable.Add(newState);
    }

    Actions DecideAction(State state)
    {
        // Check if the AI has made decisions in this state before
        if (QTable.Any(entry => entry.status.SequenceEqual(state.status)))
        {
            // If decisions are available, choose the action based on past outcomes
            foreach (var qEntry in QTable)
            {
                if (qEntry.status.SequenceEqual(state.status))
                {
                    if (qEntry.decisionWasGood)
                    {
                        // If past decisions were good, choose the action with the highest frequency
                        Actions mostFrequentGoodAction = GetMostFrequentGoodAction(qEntry);
                        return mostFrequentGoodAction;
                    }
                    else
                    {
                        // If past decisions were not good, choose a random action from the possible actions
                        Debug.Log("random action chosen, all bad actions");
                        return ChooseRandomAction(state.possibleActions);
                    }
                }
            }
        }
        else
        {
            // If no past decisions, choose a random action from the possible actions
            Debug.Log("random action chosen, no past decisions");
            return ChooseRandomAction(state.possibleActions);
        }

        // Default action in case of issues
        return Actions.MoveNorth;
    }

    Actions GetMostFrequentGoodAction(State qEntry)
    {
        // Assuming that possibleActions and decisionWasGood are parallel lists
        int maxFrequency = 0;
        Actions mostFrequentGoodAction = Actions.MoveNorth; // Default action in case of issues

        for (int i = 0; i < qEntry.possibleActions.Count; i++)
        {
            if (qEntry.possibleActions[i] && qEntry.decisionWasGood)
            {
                // Count the frequency of each good action
                int frequency = CountActionFrequency(qEntry, (Actions)i);
                if (frequency > maxFrequency)
                {
                    maxFrequency = frequency;
                    mostFrequentGoodAction = (Actions)i;
                }
            }
        }

        return mostFrequentGoodAction;
    }

    int CountActionFrequency(State qEntry, Actions action)
    {
        // Count the frequency of the given action in the past decisions
        int frequency = 0;

        for (int i = 0; i < qEntry.possibleActions.Count; i++)
        {
            if (qEntry.possibleActions[i] && qEntry.decisionWasGood && (Actions)i == action)
            {
                frequency++;
            }
        }

        return frequency;
    }

    Actions ChooseRandomAction(List<bool> possibleActions)
    {
        // Choose a random action from the list of possible actions
        List<Actions> validActions = new List<Actions>();

        for (int i = 0; i < possibleActions.Count; i++)
        {
            if (possibleActions[i])
            {
                validActions.Add((Actions)i);
            }
        }

        if (validActions.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, validActions.Count);
            return validActions[randomIndex];
        }
        else
        {
            // If no valid actions, return a default action or handle it as needed
            return Actions.MoveNorth;
        }
    }

    bool DetermineChoiceOutcome(Vector3 previousPos, Vector3 currentPos)
    {
        // Calculate the distance to the closest edge before taking the action
        float distanceBefore = CalculateDistanceToClosestEdge((int)previousPos.x, (int)previousPos.z);

        // Calculate the distance to the closest edge after taking the action
        float distanceAfter = CalculateDistanceToClosestEdge((int)currentPos.x, (int)currentPos.z);

        // Determine if the decision was good or bad
        return distanceAfter < distanceBefore;
    }

    float CalculateDistanceToClosestEdge(int xPos, int zPos)
    {
        // the edges are at (0, 0) and (MainGrid.GetLength(0), MainGrid.GetLength(1))
        float distanceToTopEdge = zPos;
        float distanceToBottomEdge = MainGrid.GetLength(1) - zPos;
        float distanceToLeftEdge = xPos;
        float distanceToRightEdge = MainGrid.GetLength(0) - xPos;

        // Find the minimum distance to any edge
        float minDistance = Mathf.Min(distanceToTopEdge, distanceToBottomEdge, distanceToLeftEdge, distanceToRightEdge);

        return minDistance;
    }

    Vector3 SimulateMove(Actions action, Vector3 currentPos)
    {
        // uses the same code as MoveAI, but just returns a Vector3 instead of moving anything
        Directions dir = (Directions)action;
        int xPos = (int)currentPos.x;
        int zPos = (int)currentPos.z;

        if (CanMoveInDirection(dir, xPos, zPos))
        {
            switch (dir)
            {
                case Directions.North:
                    return currentPos + Vector3.forward;
                case Directions.South:
                    return currentPos + Vector3.back;
                case Directions.West:
                    return currentPos + Vector3.left;
                case Directions.East:
                    return currentPos + Vector3.right;
                case Directions.NorthEast:
                    return currentPos + new Vector3(1, 0, 1);
                case Directions.SouthEast:
                    return currentPos + new Vector3(1, 0, -1);
                case Directions.NorthWest:
                    return currentPos + new Vector3(-1, 0, 1);
                case Directions.SouthWest:
                    return currentPos + new Vector3(-1, 0, -1);
            }
        }
        return Vector3.zero;
    }

    public void AddBlock(int x, int y)
    {
        // if there is already a block don't do anything
        if (MainGrid[x, y] == true)
        {
            return;
        }
        MainGrid[x, y] = true;
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
                if (MainGrid[x, y])
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
                Directions.North => !MainGrid[xPos, zPos + 1],
                Directions.South => !MainGrid[xPos, zPos - 1],
                Directions.West => !MainGrid[xPos - 1, zPos],
                Directions.East => !MainGrid[xPos + 1, zPos],

                // diagonal directions
                Directions.NorthEast => !MainGrid[xPos + 1, zPos + 1] && (!MainGrid[xPos + 1, zPos] || !MainGrid[xPos, zPos + 1]),
                Directions.SouthEast => !MainGrid[xPos + 1, zPos - 1] && (!MainGrid[xPos + 1, zPos] || !MainGrid[xPos, zPos - 1]),
                Directions.NorthWest => !MainGrid[xPos - 1, zPos + 1] && (!MainGrid[xPos - 1, zPos] || !MainGrid[xPos, zPos + 1]),
                Directions.SouthWest => !MainGrid[xPos - 1, zPos - 1] && (!MainGrid[xPos - 1, zPos] || !MainGrid[xPos, zPos - 1]),
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

    void MoveAI(Actions action)
    {
        Directions dir = (Directions)action;
        Vector3 currentPos = transform.position;
        int xPos = (int)currentPos.x;
        int zPos = (int)currentPos.z;

        if (CanMoveInDirection(dir, xPos, zPos))
        {
            switch (dir)
            {
                case Directions.North:
                    StartCoroutine(LerpFunction(currentPos, currentPos + Vector3.forward));
                    break;
                case Directions.South:
                    StartCoroutine(LerpFunction(currentPos, currentPos + Vector3.back));
                    break;
                case Directions.West:
                    StartCoroutine(LerpFunction(currentPos, currentPos + Vector3.left));
                    break;
                case Directions.East:
                    StartCoroutine(LerpFunction(currentPos, currentPos + Vector3.right));
                    break;
                case Directions.NorthEast:
                    StartCoroutine(LerpFunction(currentPos, currentPos + new Vector3(1, 0, 1)));
                    break;
                case Directions.SouthEast:
                    StartCoroutine(LerpFunction(currentPos, currentPos + new Vector3(1, 0, -1)));
                    break;
                case Directions.NorthWest:
                    StartCoroutine(LerpFunction(currentPos, currentPos + new Vector3(-1, 0, 1)));
                    break;
                case Directions.SouthWest:
                    StartCoroutine(LerpFunction(currentPos, currentPos + new Vector3(-1, 0, -1)));
                    break;
            }
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
}