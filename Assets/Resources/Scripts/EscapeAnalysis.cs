using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EscapeAnalysis : MonoBehaviour
{
    public void OnClick()
    {
        ActiveManager.Instance.ExitAnalysisMode();
    }
}
