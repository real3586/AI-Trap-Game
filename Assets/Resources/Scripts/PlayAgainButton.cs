using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayAgainButton : MonoBehaviour
{
    public void OnClick()
    {
        GameManager.Instance.ResetGame();
    }
}
