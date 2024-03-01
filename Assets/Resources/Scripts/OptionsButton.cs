using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionsButton : MonoBehaviour
{
    [SerializeField] GameObject panel, optionsStuff;

    public void OnClick()
    {
        panel.SetActive(true);
        optionsStuff.SetActive(true);
    }
}
