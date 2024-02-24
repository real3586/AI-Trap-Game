using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceManager : MonoBehaviour
{
    public bool CollidingWithAnything { get; private set; }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Block") || other.CompareTag("Player"))
        {
            CollidingWithAnything = true;
        }
        else
        {
            CollidingWithAnything = false;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        CollidingWithAnything = false;
    }
}
