using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayButton : MonoBehaviour
{
    [SerializeField] GameObject gameStuff, menuStuff, optionsButton;
    [SerializeField] GameObject panel;

    private void Awake()
    {
        // make the gamestuff disappear
        gameStuff.SetActive(false);
    }

    public void PlayGame()
    {
        // make everything go away first
        menuStuff.SetActive(false);
        panel.SetActive(false);

        // make the gamestuff appear
        gameStuff.SetActive(true);
        optionsButton.SetActive(true);
    }
}
