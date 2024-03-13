using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HotkeyManager : MonoBehaviour
{
    [SerializeField] GameObject getBlock, play, playAgain, analyze, classic, user, options;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G) && getBlock.activeInHierarchy)
        {
            getBlock.GetComponent<GetBlock>().PlaceBlock();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (play.activeInHierarchy)
            {
                play.GetComponent<PlayButton>().OnClick();
            }
            else if (playAgain.activeInHierarchy)
            {
                playAgain.GetComponent<PlayAgainButton>().OnClick();
            }
        }
        if (Input.GetKeyDown(KeyCode.I) && analyze.activeInHierarchy)
        {
            analyze.GetComponent<AnalysisButton>().OnClick();
        }
        if (Input.GetKeyDown(KeyCode.C) && classic.activeInHierarchy)
        {
            classic.GetComponent<PlayClassic>().OnClick();
        }
        if (Input.GetKeyDown(KeyCode.U) && user.activeInHierarchy)
        {
            user.GetComponent<PlayUser>().OnClick();
        }
        if (Input.GetKeyDown(KeyCode.O) && options.activeInHierarchy)
        {
            options.GetComponent<OptionsButton>().OnClick();
        }
    }
}
