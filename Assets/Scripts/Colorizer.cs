using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class Colorizer : MonoBehaviour
{
    public PaletteColor Color
    {
        get => _color;
        set
        {
            _color = value;
            Apply();
        }
    }

    [SerializeField]
    private PaletteColor _color;

    public bool PreserveAlpha;

    private void Start()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    public static Color GetColor(PaletteColor slot)
    {
        return slot switch
        {
            PaletteColor.Background => new Color(0.95f, 0.95f, 0.95f),
            PaletteColor.Panel => new Color(0.97f, 0.99f, 1f),
            PaletteColor.PanelHeader => new Color(0.79f, 0.86f, 0.89f),
            PaletteColor.Text => new Color(0.16f, 0.16f, 0.16f),
            PaletteColor.SubPanel => new Color(0.8f, 0.8f, 0.85f),
            PaletteColor.SubPanelSelected => new Color(0.9f, 0.8f, 0.65f),
            PaletteColor.HeaderSubPanel => new Color(0.59f, 0.64f, 0.66f),
            PaletteColor.HeaderSubPanelSelected => new Color(0.83f, 0.72f, 0.42f),
            _ => UnityEngine.Color.red
        };
}

    private void Apply()
    {
        Color color = GetColor(_color);

        if(TryGetComponent(out Camera cam))
            cam.backgroundColor = new Color(color.r, color.g, color.b, 0f);

        if (TryGetComponent(out Image img))
        {
            if (PreserveAlpha)
                color.a = img.color.a;
            img.color = color;
        }

        if (TryGetComponent(out TextMeshProUGUI text))
        {
            if (PreserveAlpha)
                color.a = text.color.a;
            text.color = color;
        }
    }

    public enum PaletteColor
    {
        Background,
        Panel,
        PanelHeader,
        Text,
        SubPanel,
        SubPanelSelected,
        HeaderSubPanel,
        HeaderSubPanelSelected
    }
}
