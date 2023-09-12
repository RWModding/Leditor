using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class EditorButton : MonoBehaviour
{
    public RectTransform Toolbar;
    public Button ToggleButton;
    private ToolButton[] _buttons;
    private Colorizer _colorizer;
    private float _nameLabelAlpha;
    private float _nameLabelAlphaVel;

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

        var rect = Toolbar.rect;
        rect.xMax += 100f;

        bool showNames = Open
            && RectTransformUtility.ScreenPointToLocalPointInRectangle(Toolbar, Input.mousePosition, null, out var mousePos)
            && rect.Contains(mousePos);

        _nameLabelAlpha = Mathf.SmoothDamp(_nameLabelAlpha, showNames ? 1f : 0f, ref _nameLabelAlphaVel, 0.15f);

        foreach (var button in _buttons)
        {
            if (button.NameLabel)
            {
                button.NameLabel.alpha = _nameLabelAlpha;
            }
        }
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
