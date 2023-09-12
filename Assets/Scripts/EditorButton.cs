using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class EditorButton : MonoBehaviour
{
    public Transform Toolbar;
    public Button ToggleButton;
    private ToolButton[] _buttons;
    private Colorizer _colorizer;

    public bool Open;
    public bool Selected => _buttons.Any(button => button.Selected);

    private void Start()
    {
        _buttons = GetComponentsInChildren<ToolButton>();
        _colorizer = ToggleButton ? ToggleButton.GetComponent<Colorizer>() : null;
    }

    private void Update()
    {
        if(_colorizer)
        {
            _colorizer.Color = Selected ? Colorizer.PaletteColor.HeaderSubPanelSelected : Colorizer.PaletteColor.PanelHeader;
        }

        Toolbar.gameObject.SetActive(Open);
    }

    public void Toggle()
    {
        Toggle(!Open);
    }

    public void Toggle(bool nowOpen)
    {
        var group = GetComponentInParent<EditorButtonGroup>();
        if (nowOpen && group)
        {
            group.CloseAll();
        }
        Open = nowOpen;
    }
}
