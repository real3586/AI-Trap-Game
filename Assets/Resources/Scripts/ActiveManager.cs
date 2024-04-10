using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ActiveManager : MonoBehaviour
{
    public static ActiveManager Instance;

    [SerializeField] GameObject optionsButton, optionsStuff, panel, gameStuff, 
        menuStuff, winAndLose, playOptions, arrowObject, userModeStuff, analysisStuff, analysisButton,
        superAnalysisStuff, blockHighlight;
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
        analysisButton.SetActive(false);
        analysisStuff.SetActive(false);
        blockHighlight.SetActive(false);
    }

    public void EscapeOptions()
    {
        optionsStuff.SetActive(false);
        panel.SetActive(false);
        analysisButton.SetActive(true);
    }

    public void DisplayOptions()
    {
        panel.SetActive(true);
        optionsStuff.SetActive(true);
        analysisButton.SetActive(false);
    }

    public void EnterAnalysisMode()
    {
        gameStuff.SetActive(false);
        analysisButton.SetActive(false);
        analysisStuff.SetActive(true);
        superAnalysisStuff.SetActive(false);

        GameManager.Instance.IsAnalysisMode = true;
        MainAI.Instance.AnalysisModeUpdate();
    }

    public void ExitAnalysisMode()
    {
        analysisStuff.SetActive(false);
        gameStuff.SetActive(true);
        analysisButton.SetActive(true);
        blockHighlight.SetActive(false);

        GameManager.Instance.IsAnalysisMode = false;
    }

    public void SuperAnalysis()
    {
        blockHighlight.SetActive(true);
        superAnalysisStuff.SetActive(true);
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
        analysisButton.SetActive(true);

        GameManager.Instance.mode = Enums.Modes.Classic;
        MainAI.Instance.ResetTextFields();
    }

    public void PlayUser()
    {
        arrowObject.SetActive(false);
        gameStuff.SetActive(true);
        panel.SetActive(false);
        playOptions.SetActive(false);
        optionsButton.SetActive(true);
        analysisButton.SetActive(true);

        userModeStuff.SetActive(false);

        GameManager.Instance.mode = Enums.Modes.User;
        MainAI.Instance.ResetTextFields();
    }

    public void PlayAlgo()
    {
        arrowObject.SetActive(false);
        userModeStuff.SetActive(false);
        gameStuff.SetActive(true);
        panel.SetActive(false);
        playOptions.SetActive(false);
        optionsButton.SetActive(true);
        analysisButton.SetActive(true);

        GameManager.Instance.mode = Enums.Modes.Algo;
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
        analysisButton.SetActive(false);
    }
}
