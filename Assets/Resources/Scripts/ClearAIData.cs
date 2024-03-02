using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ClearAIData : MonoBehaviour
{
    public void OnClick()
    {
        ActiveManager.Instance.ClearAI();
    }
}
