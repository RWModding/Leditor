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

    private void Start()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    private void Apply()
    {
        Color color = _color switch
        {
            PaletteColor.Background => new Color(0.95f, 0.95f, 0.95f),
            PaletteColor.Panel => new Color(0.97f, 0.99f, 1f, 0.8f),
            PaletteColor.PanelHeader => new Color(0.79f, 0.86f, 0.89f),
            PaletteColor.Text => new Color(0.16f, 0.16f, 0.16f),
            PaletteColor.SubPanel => new Color(0.4f, 0.4f, 0.4f, 0.3f),
            PaletteColor.SubPanelSelected => new Color(0.7f, 0.6f, 0.45f, 0.7f),
            _ => UnityEngine.Color.red
        };

        if(TryGetComponent(out Camera cam))
            cam.backgroundColor = new Color(color.r, color.g, color.b, 0f);

        if (TryGetComponent(out Image img))
            img.color = color;

        if (TryGetComponent(out TextMeshProUGUI text))
            text.color = color;
    }

    public enum PaletteColor
    {
        Background,
        Panel,
        PanelHeader,
        Text,
        SubPanel,
        SubPanelSelected
    }
}
