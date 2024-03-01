using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetBlock : MonoBehaviour
{
    public void PlaceBlock()
    {
        GameManager.Instance.RunSequence();
    }
}
