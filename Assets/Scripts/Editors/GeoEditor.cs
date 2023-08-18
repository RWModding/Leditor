using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using LevelModel;

public class GeoEditor : EditorBase
{
    public GameObject[] Tools = new GameObject[0];

    private string _currentTool;
    private LevelLoader _loader;

    private void Awake()
    {
        _loader = FindAnyObjectByType<LevelLoader>();
    }

    private void Start()
    {
        if(Tools?.Length > 0)
            SetTool(Tools[0]);

        foreach (var tool in Tools)
        {
            tool.GetComponent<Button>().onClick.AddListener(() => SetTool(tool));
        }
    }

    public void SetTool(GameObject tool)
    {
        foreach(var oldTool in Tools)
        {
            oldTool.GetComponent<Colorizer>().Color = Colorizer.PaletteColor.SubPanel;
        }

        tool.GetComponent<Colorizer>().Color = Colorizer.PaletteColor.SubPanelSelected;
        _currentTool = tool.name;
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        ApplyTool(eventData.position);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        ApplyTool(eventData.position);
    }

    private void ApplyTool(Vector2 screenPoint)
    {
        var level = _loader.LevelData;
        if (level == null) return;

        var cam = Camera.main;
        var worldPos = cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, -cam.transform.localPosition.z));

        int x = Mathf.FloorToInt(worldPos.x);
        int y = Mathf.FloorToInt(-worldPos.y);
        if (x >= 0 && y >= 0
            && x < level.Width && y < level.Height)
        {
            var cell = level.GetGeoCell(new(x, y), 0);
            var oldCell = cell;

            switch (_currentTool)
            {
                case "Air": cell.terrain = GeoType.Air; break;
                case "Solid": cell.terrain = GeoType.Solid; break;
                default: return;
            }

            if (cell != oldCell)
            {
                level.SetGeoCell(new(x, y), 0, cell);
                _loader.RefreshView(new RectInt(x, y, 1, 1), 0);
            }
        }
    }
}
