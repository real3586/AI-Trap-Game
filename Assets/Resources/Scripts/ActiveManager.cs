using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ActiveManager : MonoBehaviour
{
    public static ActiveManager Instance;

    [SerializeField] GameObject optionsButton, optionsStuff, panel, gameStuff, 
        menuStuff, winAndLose, playOptions, arrowObject, userModeStuff;
    [SerializeField] TextMeshProUGUI dataPointsText;

    private void Awake()
    {
        Instance = this;

        optionsButton.SetActive(false);
        optionsStuff.SetActive(false);
        panel.SetActive(true);
        gameStuff.SetActive(false);
        menuStuff.SetActive(true);
        winAndLose.SetActive(false);
        playOptions.SetActive(false);
        userModeStuff.SetActive(false);
        arrowObject.SetActive(false);
    }

    public void EscapeOptions()
    {
        optionsStuff.SetActive(false);
        panel.SetActive(false);
    }

    public void PlayGame()
    {
        menuStuff.SetActive(false);
        playOptions.SetActive(true);
        winAndLose.SetActive(false);
        gameStuff.SetActive(false);
        arrowObject.SetActive(false);
        optionsButton.SetActive(false);
    }

    public void PlayClassic()
    {
        gameStuff.SetActive(true);
        panel.SetActive(false);
        playOptions.SetActive(false);        
        optionsButton.SetActive(true);
        arrowObject.SetActive(false);

        GameManager.Instance.isUserMode = false;
        MainAI.Instance.ResetTextFields();
    }

    public void PlayUser()
    {
        arrowObject.SetActive(false);
        userModeStuff.SetActive(false);
        gameStuff.SetActive(true);
        panel.SetActive(false);
        playOptions.SetActive(false);
        optionsButton.SetActive(true);

        GameManager.Instance.isUserMode = true;
        MainAI.Instance.ResetTextFields();
    }

    public void ClearAI()
    {
        MainAI.Instance.ClearAI();
        dataPointsText.text = "Current Data Points: 0";
    }

    public void GameEnd()
    {
        panel.SetActive(true);
        winAndLose.SetActive(true);
    }
}
