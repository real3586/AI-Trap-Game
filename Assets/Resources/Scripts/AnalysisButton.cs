using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnalysisButton : MonoBehaviour
{
    public void OnClick()
    {
        ActiveManager.Instance.EnterAnalysisMode();
    }
}
