using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockedSquare : MonoBehaviour
{
    [SerializeField] Material red;
    [SerializeField] Material green;

    public void ErrorFlash()
    {
        StartCoroutine(FlashCoroutine());
    }

    IEnumerator FlashCoroutine()
    {
        GetComponent<MeshRenderer>().material = red;
        yield return new WaitForSecondsRealtime(0.15f);
        GetComponent<MeshRenderer>().material = green;
    }
}
