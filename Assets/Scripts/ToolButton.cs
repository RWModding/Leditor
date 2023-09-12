using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolButton : MonoBehaviour
{
    public Tool Tool;
    private Colorizer _colorizer;

    public bool Selected => Tool && Tool.isActiveAndEnabled;

    private void Start()
    {
        _colorizer = GetComponent<Colorizer>();
    }

    private void Update()
    {
        _colorizer.Color = Selected ? Colorizer.PaletteColor.HeaderSubPanelSelected : Colorizer.PaletteColor.HeaderSubPanel;
    }

    public void Toggle()
    {
        if(Tool)
        {
            Tool.Toggle();
        }
    }
}
