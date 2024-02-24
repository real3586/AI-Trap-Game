using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainAI : MonoBehaviour
{
    public static MainAI Instance { get; private set; }

    const int numActions = 4;

    public bool[,] MainGrid = new bool[9, 9];
    float[,] QTable; // Initialize this with appropriate dimensions

    [SerializeField] GameObject[] sensors = new GameObject[4];

    float alpha = 0.1f; // Learning rate
    float gamma = 0.9f; // Discount factor

    public bool IsLerping { get; private set; }

    enum Directions { Up, Down, Left, Right };
    enum Actions { MoveUp, MoveDown, MoveLeft, MoveRight };
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

    public void AddBlock(int x, int y)
    {
        // if there is already a block don't do anything
        if (MainGrid[x, y] == true)
        {
            return;
        }
        MainGrid[x, y] = true;
    }

    List<int> GetCurrentPosition(float xPos, float yPos)
    {
        List<int> ints = new();
        int blockCountNE = 0, blockCountNW = 0, blockCountSE = 0, blockCountSW = 0;

        for (int x = 0; x < MainGrid.Length; x++)
        {
            for (int y = 0; y < MainGrid.Length; y++)
            {
                // if true, a square is there (blocked)
                // if not, continue
                if (MainGrid[x, y])
                {
                    // check where the block is relative to the agent
                    // agent will learn to avoid directions with large amounts of blocks
                    if (x <= xPos && y <= yPos)
                    {
                        blockCountNE++;
                    }
                    if (x <= xPos && y >= yPos)
                    {
                        blockCountSE++;
                    }                    
                    if (x >= xPos && y >= yPos)
                    {
                        blockCountSE++;
                    }
                    if (x >= xPos && y <= yPos)
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
    bool GetDirectionBlocked(Directions dir)
    {
        return sensors[(int)dir].GetComponent<Sensor>().IsBlocked;
    }

    // Find the maximum Q-value for a given state
    float MaxQValue(int state)
    {
        float maxQ = float.MinValue;
        for (int a = 0; a < numActions; a++)
        {
            if (QTable[state, a] > maxQ)
            {
                maxQ = QTable[state, a];
            }
        }
        return maxQ;
    }

    public void MoveAI(int direction)
    {
        Directions dir = (Directions)direction;
        Vector3 currentPos = transform.position;

        switch (dir)
        {
            case Directions.Left:
                try
                {
                    var x = MainGrid[(int)currentPos.x - 1, (int)currentPos.z];
                }
                catch
                {
                    return;
                }
                if (!sensors[(int)Directions.Left].GetComponent<Sensor>().IsBlocked)
                {
                    StartCoroutine(LerpFunction(currentPos, currentPos + Vector3.left));
                }
                break;
            case Directions.Right:
                try
                {
                    var x = MainGrid[(int)currentPos.x + 1, (int)currentPos.z];
                }
                catch
                {
                    return;
                }
                if (!sensors[(int)Directions.Right].GetComponent<Sensor>().IsBlocked)
                {
                    StartCoroutine(LerpFunction(currentPos, currentPos + Vector3.right));
                }
                break;
            case Directions.Up:
                try
                {
                    var x = MainGrid[(int)currentPos.x, (int)currentPos.z + 1];
                }
                catch
                {
                    return;
                }
                if (!sensors[(int)Directions.Up].GetComponent<Sensor>().IsBlocked)
                {
                    StartCoroutine(LerpFunction(currentPos, currentPos + Vector3.forward));
                }
                break;
            case Directions.Down:
                try
                {
                    var x = MainGrid[(int)currentPos.x, (int)currentPos.z - 1];
                }
                catch
                {
                    return;
                }
                if (!sensors[(int)Directions.Down].GetComponent<Sensor>().IsBlocked)
                {
                    StartCoroutine(LerpFunction(currentPos, currentPos + Vector3.back));
                }
                break;
        }
    }

    // from mystic mazes
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

    void PerformAction(Actions action)
    {
        // Your logic to perform the chosen action in the environment
    }

    float GetReward()
    {
        // Your logic to get the reward based on the current state and action
        // This could involve checking the new state after performing the action
        return 0f; // Placeholder value; replace it with your actual reward logic
    }
}
