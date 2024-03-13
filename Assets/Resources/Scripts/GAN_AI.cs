using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GAN_AI : MonoBehaviour
{
    public static GAN_AI Instance { get; private set; }

    struct State
    {
        /// <summary>
        /// The state of the MainAI, used to help GAN_AI evaluate its moves.
        /// </summary>
        public MainAI.State state;
        public int blockPosX, blockPosZ;
        public float outcome;
    }
    List<State> QTable = new();

    // similar to the MainAI, uses struct grid to know where objects are
    struct GridItem
    {
        public bool isBlocked;
        public int x, z;
        public int visited;
    }
    readonly GridItem[,] MainGrid = new GridItem[9, 9];

    // literally a copy of the pathfinding stuff from MainAI
    // except TargetPath returns a list, not an integer
    #region Navigation Stuff
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

    List<GridItem> TargetPath(int startX, int startZ, int endX, int endZ)
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
            return null;
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

        return path;
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

    public void AddState(MainAI.State state, int blockPosX, int blockPosZ)
    {
        State newState = new()
        {
            state = state,
            blockPosX = blockPosX,
            blockPosZ = blockPosZ,
            outcome = -1 * state.decisionOutcome
        };
        QTable.Add(newState);
    }

    public void GANSequence()
    {
        int x = (int)MainAI.Instance.transform.position.x;
        int z = (int)MainAI.Instance.transform.position.z;

        // first get all available endpoints
        List<Vector3> validEndpoints = GetValidEndpoints();

        // pathfind to all endpoints and save the path that they take
        List<List<GridItem>> pathsToEndpoints = new();
        for (int i = 0; i < validEndpoints.Count; i++)
        {
            int tempX = (int)validEndpoints[i].x;
            int tempZ = (int)validEndpoints[i].z;
            pathsToEndpoints.Add(TargetPath(x, z, tempX, tempZ));
        }

        // decide where to place the block
    }
}
