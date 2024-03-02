using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OkayButton : MonoBehaviour
{
    [SerializeField] float weight;

    public void OnClick()
    {
        GameManager.Instance.DecisionOutcome = weight;
        GameManager.Instance.UserProvidedFeedback = true;
    }
}
