using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Rand = UnityEngine.Random;
using TMPro;

public class GAN_AI : MonoBehaviour
{
    public static GAN_AI Instance { get; set; }

    /// <summary>
    /// The state of the MainAI
    /// </summary>
    public MainAI.MainState AIState;

    public struct GANState
    {
        /// <summary>
        /// What was the MainAI's state?
        /// </summary>
        public MainAI.MainState state;

        /// <summary>
        /// Where the GAN AI placed its block
        /// </summary>
        public Vector2 blockPlacedPos;

        /// <summary>
        /// Did this result in a negative outcome for the MainAI?
        /// </summary>
        public float outcome;
    }
    List<GANState> QTable = new();

    [SerializeField] GameObject blockPrefab, blockParent;
    [SerializeField] GameObject GANStuff;
    [SerializeField] TextMeshProUGUI GANText;
    [SerializeField] Button newBlockButton;
    [SerializeField] bool devSkipConfirmation = false;

    void Awake()
    {
        Instance = this;
    }

    public IEnumerator GANSequence()
    {
        // get the current state
        GANState gState = new()
        {
            // if there are no states, initialize with an empty state
            // no struct constructors for some reason (sadge)
            state = MainAI.Instance.QTable.Count == 0 ? new MainAI.MainState() : MainAI.Instance.QTable[^1]
        };


        // get all available block choices
        List<Vector2> blockChoices = GetAllBlockChoices();

        // decide the next action
        gState.blockPlacedPos = DecideNextAction(gState.state, blockChoices);

        // place the block
        PlaceBlock(gState.blockPlacedPos);

        // determine the outcome via a outcome algorithm similar to what MainAI has

        // add the state to the qtable
        QTable.Add(gState);

        // update what the user sees, and wait for their response
        GANStuff.SetActive(true);
        Debug.Log(gState);
        GANText.text = "The GAN AI decided to place the block at (" + gState.blockPlacedPos.x + ", " + gState.blockPlacedPos.y + ")";

        // disallow the player to place new blocks
        // but there is a dev feature that can skip this
        if (!devSkipConfirmation) newBlockButton.gameObject.SetActive(false);

        yield return new WaitUntil(() => GameManager.Instance.UserProvidedFeedback);
        GameManager.Instance.UserProvidedFeedback = false;
        newBlockButton.gameObject.SetActive(true);
        GANStuff.SetActive(false);
    }
    
    List<Vector2> GetAllBlockChoices()
    {
        List<Vector2> blockChoices = new();
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                // the square isn't already blocked
                if (!MainAI.Instance.MainGrid[i, j].isBlocked)
                {
                    // the MainAI isn't already on that square
                    if (MainAI.Instance.transform.position.x != i || MainAI.Instance.transform.position.z != j)
                        blockChoices.Add(new Vector2(i, j));
                }
            }
        }
        return blockChoices;
    }

    Vector2 DecideNextAction(MainAI.MainState mainState, List<Vector2> blockChoices)
    {
        // check if there are any entries s.t. whatever state the MainAI was in matches whatever state it is in now
        if (QTable.Count > 1000 && // change to 0 later, but 0 causes an error sometimes
            QTable.Any(entry => entry.state.status.SequenceEqual(mainState.status)) /*||
            QTable.Any(entry => SimilarityScore(state, entry) >= similarityThreshold)*/) // disregard similarity score for now
        {
            // if we found a good move, and that move is valid (ie. there's nothing there) add it to the move list
            // weigh good moves more than worse ones

            // empty code, for now will make only random decisions

            // copy paste of code below, make sure to delete
            // BEGIN DELETE
            int rand = Rand.Range(0, blockChoices.Count);
            return blockChoices[rand];
            // END DELETE
        }
        else
        {
            // if no good moves, make a random move from the list of available moves
            int rand = Rand.Range(0, blockChoices.Count);
            return blockChoices[rand];
        }
    }
    
    void PlaceBlock(Vector2 blockPos)
    {
        // create the block object and move it to the proper place
        GameObject newBlock = Instantiate(blockPrefab, new Vector3(blockPos.x, 1, blockPos.y), Quaternion.identity, blockParent.transform);

        // update the main grid to reflect this
        MainAI.Instance.AddBlock((int)blockPos.x, (int)blockPos.y);
    }
}