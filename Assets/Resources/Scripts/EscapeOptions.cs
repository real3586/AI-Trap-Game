using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EscapeOptions : MonoBehaviour
{
    [SerializeField] GameObject panel, optionsStuff;

    public void OnClick()
    {
        optionsStuff.SetActive(false);
        panel.SetActive(false);
    }
}
