using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayButton : MonoBehaviour
{
    [SerializeField] GameObject menuStuff;
    [SerializeField] GameObject panel;

    public void PlayGame()
    {
        // make everything go away first
        menuStuff.SetActive(false);
        panel.SetActive(false);

        GameManager.Instance.StartGame();
    }
}
