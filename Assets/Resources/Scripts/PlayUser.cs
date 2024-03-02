using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayUser : MonoBehaviour
{
    public void OnClick()
    {
        ActiveManager.Instance.PlayUser();
    }
}
