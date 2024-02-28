using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
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
    GridItem[,] MainGrid = new GridItem[9, 9];

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
                MainGrid[x, y].isBlocked = false;
                MainGrid[x, y].visited = -1;
                MainGrid[x, y].x = x;
                MainGrid[x, y].z = y;
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
        Debug.Log(TargetPath(0, 0));

        // add the new state to the Q table
        QTable.Add(newState);

        // finally check if the AI is on a winning square
        if (CheckWin((int)hypotheticalPos.x, (int)hypotheticalPos.z))
        {
            GameManager.Instance.GameEnd(true);
            return;
        }
    }

    #region Decision Making
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
    #endregion
    #region Feedback
    int DetermineChoiceOutcome(Vector3 currentPos, Vector3 newPos)
    {
        int choiceOutcome = 0;
        int currentX = (int)currentPos.x;
        int currentZ = (int)currentPos.z;

        // get all the valid endpoints
        List<Vector3> closestPreviousEdges = GetValidEndpoints();

        // pathfind to all of them, then take the shortest path
        // if there are multiple, consider all of them
        List<int> pathLength = new();
        for (int i = 0; i < closestPreviousEdges.Count; i++)
        {
            int endX = (int)closestPreviousEdges[i].x;
            int endZ = (int)closestPreviousEdges[i].z;
            pathLength.Add(TargetPath(endX, endZ));
        }

        // then pathfind to the closest points again
        // keep track of the change in distance
        List<int> changeInDistance = new();

        return choiceOutcome;
    }
    #endregion
    #region Navigation to points
    void InitialSetup()
    {
        for (int i = 0; i < MainGrid.GetLength(0); i++)
        {
            for (int j = 0; j < MainGrid.GetLength(1); j++)
            {
                MainGrid[i, j].visited = -1;
            }
        }
        MainGrid[(int)transform.position.x, (int)transform.position.z].visited = 0;
    }

    void SetDistance()
    {
        InitialSetup();

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
                endpoints.Add(new Vector3(x, 0, 8));
            }
            if (!MainGrid[x, 0].isBlocked)
            {
                endpoints.Add(new Vector3(x, 0, 0));
            }
        }
        // along the vertical sides
        // from 1 to 7 to skip the corners (they got checked already)
        for (int y = 1; y < 8; y++)
        {
            if (!MainGrid[0, y].isBlocked)
            {
                endpoints.Add(new Vector3(0, 0, y));
            }
            if (!MainGrid[8, y].isBlocked)
            {
                endpoints.Add(new Vector3(8, 0, y));
            }
        }

        return endpoints;
    }

    int TargetPath(int endX, int endZ)
    {
        SetDistance();

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
            for (int i = 0; i < Enum.GetNames(typeof(Directions)).Length; i++)
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

    bool TestDirection(int x, int z, int step, Directions dir)
    {
        int rows = MainGrid.GetLength(0);
        int columns = MainGrid.GetLength(1);

        try
        {
            switch (dir)
            {
                case Directions.North:
                    return z + 1 < columns && !MainGrid[x, z + 1].isBlocked && MainGrid[x, z + 1].visited == step;
                case Directions.South:
                    return z - 1 > -1 && !MainGrid[x, z - 1].isBlocked && MainGrid[x, z - 1].visited == step;
                case Directions.West:
                    return x - 1 > -1 && !MainGrid[x - 1, z].isBlocked && MainGrid[x - 1, z].visited == step;
                case Directions.East:
                    return x + 1 < rows && !MainGrid[x + 1, z].isBlocked && MainGrid[x + 1, z].visited == step;

                case Directions.NorthEast:
                    return z + 1 < columns && x + 1 < rows && !MainGrid[x + 1, z + 1].isBlocked && MainGrid[x + 1, z + 1].visited == step;
                case Directions.SouthEast:
                    return z - 1 > -1 && x + 1 < rows && !MainGrid[x + 1, z - 1].isBlocked && MainGrid[x + 1, z - 1].visited == step;
                case Directions.NorthWest:
                    return z + 1 < columns && x - 1 > -1 && !MainGrid[x - 1, z + 1].isBlocked && MainGrid[x - 1, z + 1].visited == step;
                case Directions.SouthWest:
                    return z - 1 > -1 && x - 1 > -1 && !MainGrid[x - 1, z - 1].isBlocked && MainGrid[x - 1, z - 1].visited == step;
            }
        }
        catch
        {
            return false;
        }
        return false;
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
    Dictionary<Directions, Vector3> directionToVector = new()
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
}