using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolGroup : MonoBehaviour
{
    public GameObject CurrentTool;

    public void OnEnable()
    {
        foreach(RectTransform child in transform)
        {
            ToggleTool(child.gameObject, false);
        }
        ToggleTool(CurrentTool, true);
    }

    private void SwitchTool(GameObject newTool)
    {
        if (CurrentTool == newTool) return;

        ToggleTool(CurrentTool, false);
        CurrentTool = newTool;
        ToggleTool(CurrentTool, true);
    }

    private void ToggleTool(GameObject tool, bool active)
    {
        if (!tool) return;

        if (tool.TryGetComponent(out Colorizer colorizer))
        {
            colorizer.Color = active ? Colorizer.PaletteColor.SubPanelSelected : Colorizer.PaletteColor.SubPanel;
        }

        if (tool.TryGetComponent(out EditTool toolBehav))
        {
            toolBehav.enabled = active;
        }
    }
}