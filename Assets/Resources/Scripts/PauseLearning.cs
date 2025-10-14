using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PauseLearning : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI text;

    void OnEnable()
    {
        text.text = MainAI.Instance.pauseAILearning ? "Unpause" : "Pause";
    }
    public void OnClick()
    {
        MainAI.Instance.pauseAILearning = !MainAI.Instance.pauseAILearning;
        text.text = MainAI.Instance.pauseAILearning ? "Unpause" : "Pause";
    }
}
