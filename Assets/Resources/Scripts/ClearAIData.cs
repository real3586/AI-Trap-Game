using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ClearAIData : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI dataPointsText;

    public void OnClick()
    {
        MainAI.Instance.ClearAI();
        dataPointsText.text = "Current Data Points: 0";
    }
}
