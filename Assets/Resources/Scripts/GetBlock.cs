using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetBlock : MonoBehaviour
{
    public void PlaceBlock()
    {
        if (GameManager.Instance.mode == Enums.Modes.Algo)
        {
            GameManager.Instance.RunSequenceAlgo();
        }
        else
        {
            GameManager.Instance.RunSequence();
        }
    }
}
