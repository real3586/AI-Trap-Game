using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ActiveManager : MonoBehaviour
{
    public static ActiveManager Instance;

    [SerializeField] GameObject optionsButton, optionsStuff, panel, gameStuff, 
        menuStuff, winAndLose, playOptions, arrowObject;
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
    }

    public void PlayClassic()
    {
        gameStuff.SetActive(true);
        panel.SetActive(false);
        playOptions.SetActive(false);        
        optionsButton.SetActive(true);
    }

    public void PlayUser()
    {

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
