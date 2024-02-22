using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainAI : MonoBehaviour
{
    const int numActions = 4;

    [SerializeField] GameObject[] Sensors = new GameObject[numActions];

    float[,] QTable; // Initialize this with appropriate dimensions

    float alpha = 0.1f; // Learning rate
    float gamma = 0.9f; // Discount factor

    enum Directions { Up, Down, Left, Right };
    enum Actions { MoveUp, MoveDown, MoveLeft, MoveRight };
    enum States { NoSideBlocked, OneSideBlocked, TwoSideBlocked, ThreeSideBlocked, FourSideBlocked }

    private void Start()
    {
        StartCoroutine(MainFunction());
    }

    IEnumerator MainFunction()
    {
        yield return null;
    }

    bool CheckDirection(Directions dir)
    {
        return Sensors[(int)dir].GetComponent<Sensor>().IsBlocked;
    }

    States GetCurrentState()
    {
        int blockedCount = 0;

        for (int i = 0; i < numActions; i++)
        {
            if (CheckDirection((Directions)i))
            {
                blockedCount++;
            }
        }

        return (States)blockedCount;
    }

    // Q-learning update rule
    void UpdateQValue(Actions chosenAction, float reward)
    {
        States currentState = GetCurrentState();
        int chosenActionIndex = (int)chosenAction;
        States nextState = GetCurrentState(); // You need to determine the next state after taking the action

        int currentStateIndex = (int)currentState;
        int nextStateIndex = (int)nextState;

        QTable[currentStateIndex, chosenActionIndex] = (1 - alpha) * QTable[currentStateIndex, chosenActionIndex] + alpha * (reward + gamma * MaxQValue(nextStateIndex));
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

    Actions ChooseAction()
    {
        // Your logic to choose an action based on Q-values
        // You can use epsilon-greedy or other exploration-exploitation strategies

        // For simplicity, let's assume it chooses the action with the highest Q-value
        int currentState = (int)GetCurrentState();

        float maxQValue = float.MinValue;
        Actions bestAction = Actions.MoveUp;

        for (int a = 0; a < numActions; a++)
        {
            if (QTable[currentState, a] > maxQValue)
            {
                maxQValue = QTable[currentState, a];
                bestAction = (Actions)a;
            }
        }

        return bestAction;
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
