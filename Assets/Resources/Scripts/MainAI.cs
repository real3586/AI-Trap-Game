using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
        public int decisionOutcome;
    }

    /// <summary>
    /// The MainGrid holds true values if there is a block present on these coords, and false otherwise.
    /// </summary>
    public bool[,] MainGrid = new bool[9, 9];
    List<State> QTable = new();

    public bool IsLerping { get; private set; }

    enum Directions { North, South, West, East, NorthEast, SouthEast, NorthWest, SouthWest }
    enum States { NortheastBlocked, NorthwestBlocked, SoutheastBlocked, SouthwestBlocked }

    private void Awake()
    {
        Instance = this;
    }

    public void ResetGrid()
    {
        // reset the grid
        for (int x = 0; x < MainGrid.GetLength(0); x++)
        {
            for (int y = 0; y < MainGrid.GetLength(1); y++)
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

        State newState = new()
        {
            status = mostBlockedDirections,
            possibleActions = possibleMoves
        };

        // terminate the function and end the game if there are no possible moves, or the whole list is false
        if (possibleMoves.All(value => value == false))
        {
            GameManager.Instance.GameEnd(false);
            return;
        }

        // decide the action based on the state and given actions
        newState.decidedAction = DecideAction(newState, xPos, zPos);
        Vector3 currentPosition = transform.position;
        MoveAI(newState.decidedAction);
        Debug.Log(newState.decidedAction);

        // determine whether it was a good choice
        Vector3 hypotheticalPos = SimulateMove(newState.decidedAction, currentPosition);
        newState.decisionOutcome = DetermineChoiceOutcome(currentPosition, hypotheticalPos);

        // add the new state to the Q table
        QTable.Add(newState);

        // finally check if the AI is on a winning square
        if (CheckWin((int)hypotheticalPos.x, (int)hypotheticalPos.z))
        {
            GameManager.Instance.GameEnd(true);
            return;
        }
    }

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
                    if (qEntry.decisionOutcome == 0)
                    {
                        // Make sure the action is possible before adding it
                        if (CanMoveInDirection((Directions)qEntry.decidedAction, xPos, zPos))
                        {
                            potentialActions.Add(qEntry.decidedAction);
                        }
                    }
                    else if (qEntry.decisionOutcome == 1)
                    {
                        // Again, make sure the action is possible.
                        // This time, the action is worth double, so add it twice.
                        if (CanMoveInDirection((Directions)qEntry.decidedAction, xPos, zPos))
                        {
                            potentialActions.Add(qEntry.decidedAction);
                            potentialActions.Add(qEntry.decidedAction);
                        }
                    }
                }
            }
            if (potentialActions.Count > 0)
            {
                int randomIndex = Rand.Range(0, potentialActions.Count - 1);
                return potentialActions[randomIndex];
            }
        }

        // If no past decisions, choose a random action from the possible actions
        Debug.Log("random action chosen");
        return ChooseRandomAction(state.possibleActions);
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

    int DetermineChoiceOutcome(Vector3 currentPos, Vector3 newPos)
    {
        int currentX = (int)currentPos.x;
        int currentZ = (int)currentPos.z;

        int newX = (int)newPos.x;
        int newZ = (int)newPos.z;

        int choiceOutcome;

        // Calculate distances to each edge before and after the move
        float distanceBefore = CalculateDistanceToClosestEdge(currentX, currentZ);
        float distanceAfter = CalculateDistanceToClosestEdge(newX, newZ);

        // Calculate how the move affects proximity to edges
        float proximityChange = distanceBefore - distanceAfter;

        // Assign outcome based on proximity change
        if (proximityChange > 0)
        {
            // Good move: moved closer to closest edge
            choiceOutcome = 1;
        }
        else if (proximityChange < 0)
        {
            // Bad move: moved away from closest edge
            choiceOutcome = -1;
        }
        else
        {
            // Okay move: neither closer nor farther from closest edge
            choiceOutcome = 0;
        }

        return choiceOutcome;
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
            StartCoroutine(LerpFunction(currentPos, directionToVector[dir]));
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

    Dictionary<Directions, Vector3> directionToVector = new()
{
        {Directions.North, Vector3.forward},
        {Directions.South, Vector3.back },
        {Directions.West, Vector3.left },
        {Directions.East, Vector3.right },
        {Directions.NorthEast, Vector3.forward + Vector3.right },
        {Directions.SouthEast, Vector3.back + Vector3.right },
        {Directions.NorthEast, Vector3.forward + Vector3.left },
        {Directions.SouthWest, Vector3.back + Vector3.left }
};
}