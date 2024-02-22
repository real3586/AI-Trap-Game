using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sensor : MonoBehaviour
{
    public bool IsBlocked { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Block"))
        {
            IsBlocked = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Block"))
        {
            IsBlocked = false;
        }
    }
}
