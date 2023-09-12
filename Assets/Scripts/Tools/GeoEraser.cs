using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using LevelModel;

public class GeoEraser : Tool
{
    private int _diameter;
    private GameObject _preview;
    private Mesh _previewMesh;
    private Vector2 Offset => (_diameter - 1) * new Vector2(-0.5f, -0.5f);
    private Keybinds.ActionInfo _shrink;
    private Keybinds.ActionInfo _grow;

    protected override void OnDrag(MouseData mouse) => Apply(mouse);
    protected override void OnPointerDown(MouseData mouse) => Apply(mouse);

    private void Start()
    {
        _shrink = Keybinds.GetAction("Brush Shrink");
        _grow = Keybinds.GetAction("Brush Grow");

        BrushUtils.MakeOutline(out _preview, out _previewMesh);
        _preview.transform.SetParent(transform, false);
        BrushUtils.UpdateOutlineMesh(_previewMesh, _diameter);
    }

    private void Apply(MouseData mouse)
    {
        if (Level == null || mouse.EventData.button == PointerEventData.InputButton.Middle)
            return;

        if (mouse.EventData.button == PointerEventData.InputButton.Left)
        {
            var pos = mouse.LevelPos + Offset;
            var tile = Vector2Int.FloorToInt(pos);

            for (int x = 0; x < _diameter; x++)
            {
                for (int y = 0; y < _diameter; y++)
                {
                    if (BrushUtils.CircleContains(x, y, _diameter))
                    {
                        var p = new Vector2Int(tile.x + x, tile.y + y);
                        Level.SetGeoCell(p, Layer, default);
                    }
                }
            }

            LevelLoader.RefreshView(new RectInt(tile, new Vector2Int(_diameter, _diameter)), Layer);
        }
    }

    private void Update()
    {
        int delta = Keybinds.Shift ? 4 : 1;
        if (Input.GetKeyDown(_shrink.CurrentKey))
        {
            BrushUtils.Resize(-delta);
        }
        else if (Input.GetKeyDown(_grow.CurrentKey))
        {
            BrushUtils.Resize(delta);
        }

        if (_diameter != BrushUtils.SharedDiameter)
        {
            _diameter = BrushUtils.SharedDiameter;
            BrushUtils.UpdateOutlineMesh(_previewMesh, _diameter);
        }

        var pos = GetLevelPos(Input.mousePosition) + Offset;
        var tile = Vector2Int.FloorToInt(pos);

        _preview.transform.position = (Vector2)tile * new Vector2(1f, -1f);
    }
}