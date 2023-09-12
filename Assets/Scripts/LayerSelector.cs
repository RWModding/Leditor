using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LayerSelector : MonoBehaviour
{
    public int Layer;
    public Image LayerGraphic;
    public TextMeshProUGUI LayerNumber;
    public ToolGroup Tools;

    [SerializeField]
    private Sprite[] _layerSprites;

    private Keybinds.ActionInfo _changeLayer;
    private CanvasGroup _canvasGroup;

    void Start()
    {
        _changeLayer = Keybinds.GetAction("Change Layer");
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    void Update()
    {
        Layer %= 3;

        LayerNumber.text = (Layer + 1).ToString();
        LayerGraphic.sprite = _layerSprites[Layer];

        if(Input.GetKeyDown(_changeLayer.CurrentKey))
        {
            CycleLayers();
        }

        if(_canvasGroup)
        {
            _canvasGroup.alpha = Tools && Tools.CurrentTool && Tools.CurrentTool.ShowLayerSelector ? 1f : 0f;
        }
    }

    public void CycleLayers()
    {
        Layer += Keybinds.Shift ? -1 : 1;
        Layer = (Layer % 3 + 3) % 3;
    }
}
