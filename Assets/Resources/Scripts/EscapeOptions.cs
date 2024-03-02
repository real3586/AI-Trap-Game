using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EscapeOptions : MonoBehaviour
{
    public void OnClick()
    {
        ActiveManager.Instance.EscapeOptions();
    }
}
