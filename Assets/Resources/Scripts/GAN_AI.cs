using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GAN_AI : MonoBehaviour
{
    public static GAN_AI Instance { get; set; }

    /// <summary>
    /// The state of the MainAI
    /// </summary>
    public MainAI.State AIState;

    public struct GANState
    {
        /// <summary>
        /// What was the MainAI's state?
        /// </summary>
        public MainAI.State state;

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
    public struct GridItem
    {
        /// <summary>
        /// Does this square have a block?
        /// </summary>
        public bool isBlocked;
        public int x, z;
        /// <summary>
        /// From 0 to 1, or (0, 1].
        /// </summary>
    }
    /// <summary>
    /// The MainGrid holds a grid of the GridItem struct.
    /// </summary>
    public readonly GridItem[,] MainGrid = new GridItem[9, 9];

    void Awake()
    {
        Instance = this;
    }

    public IEnumerator GANSequence()
    {
        // get all available block choices

        // then search past GAN states to find a matching MainAI state
        // that is, find a GAN state in the past where MainAI was in the same state as it is now
        // and also ideally really close
        // can make a similarity score function if needed, which weights distance greatly

        // if we found a good move, and that move is valid (ie. there's nothing there) add it to the move list
        // weigh good moves more than worse ones

        // if move list is empty, make a random one
        yield return null;
    }
}
