using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolGroup : MonoBehaviour
{
    public Tool CurrentTool
    {
        get => _currentTool;
        set
        {
            if (_currentTool == value) return;

            ToggleTool(_currentTool, false);
            _currentTool = value;
            ToggleTool(_currentTool, true);
        }
    }
    public LayerSelector LayerSelector;

    [SerializeField]
    private Tool _currentTool;

    public void Start()
    {
        foreach (Transform child in transform)
        {
            if(child.TryGetComponent(out Tool tool))
                ToggleTool(tool, false);
        }
        ToggleTool(CurrentTool, true);
    }

    private static void ToggleTool(Tool tool, bool active)
    {
        if (!tool) return;

        tool.gameObject.SetActive(active);
    }
}