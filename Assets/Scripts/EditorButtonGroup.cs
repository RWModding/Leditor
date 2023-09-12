using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorButtonGroup : MonoBehaviour
{
    private EditorButton[] _buttons;

    public void Start()
    {
        _buttons = GetComponentsInChildren<EditorButton>();
    }

    public void CloseAll()
    {
        foreach(var button in _buttons)
        {
            button.Open = false;
        }
    }
}
