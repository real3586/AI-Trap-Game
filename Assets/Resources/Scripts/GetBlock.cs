using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetBlock : MonoBehaviour
{
    public void PlaceBlock()
    {
        switch (GameManager.Instance.mode)
        {
            case Enums.Modes.Algo:
                GameManager.Instance.RunSequenceAlgo();
                break;
            case Enums.Modes.GAN:
                GameManager.Instance.RunSequenceGAN();
                break;
            default:
                GameManager.Instance.RunSequence();
                break;
        }
    }
}
