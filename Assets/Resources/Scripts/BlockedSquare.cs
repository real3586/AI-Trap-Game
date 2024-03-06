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

    public void OnMouseDown()
    {
        if (!GameManager.Instance.IsAnalysisMode)
        {
            return;
        }
        else
        {
            ActiveManager.Instance.SuperAnalysis();

            int xPos = (int)transform.position.x;
            int zPos = (int)transform.position.z;
            GameManager.Instance.Analyze(xPos, zPos);
        }
    }
}
