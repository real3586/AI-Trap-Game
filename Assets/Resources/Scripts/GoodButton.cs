using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoodButton : MonoBehaviour
{
    [SerializeField] float weight;

    public void OnClick()
    {
        GameManager.Instance.DecisionOutcome = weight;
        GameManager.Instance.UserProvidedFeedback = true;
    }
}
