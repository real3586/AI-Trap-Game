using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PassiveLearningButton : MonoBehaviour
{
    private void Start()
    {
        if (GetComponent<Toggle>().isOn != GameManager.Instance.AllowPassiveLearning)
        {
            GameManager.Instance.AllowPassiveLearning = GetComponent<Toggle>().isOn;
        }
    }
    public void ToggleDisplay()
    {
        lock (this)
        {
            if (GameManager.Instance.AllowPassiveLearning != GetComponent<Toggle>().isOn)
            {
                GameManager.Instance.AllowPassiveLearning = !GameManager.Instance.AllowPassiveLearning;
            }
        }
    }
}
