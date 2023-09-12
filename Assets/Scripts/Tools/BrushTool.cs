using System;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class BrushTool : Tool
{
    private Keybinds.ActionInfo _shrink;
    private Keybinds.ActionInfo _grow;

    private int _diameter;
    private GameObject _brushPreview;
    private Mesh _brushMesh;
    private Vector2 BrushOffset => (_diameter - 1) * new Vector2(-0.5f, -0.5f);

    private GameObject _rectPreview;
    private Mesh _rectMesh;
    private Vector2Int? _rectStart;
    private Vector2Int _rectSize;

    protected override void OnDrag(MouseData mouse) => ApplyBrush(mouse);
    protected override void OnPointerDown(MouseData mouse)
    {
        if (mouse.EventData.button == PointerEventData.InputButton.Left)
        {
            ApplyBrush(mouse);
        }
        else if (mouse.EventData.button == PointerEventData.InputButton.Right)
        {
            _rectStart = mouse.LevelTile;
        }
    }

    protected override void OnPointerUp(MouseData mouse)
    {
        if (mouse.EventData.button == PointerEventData.InputButton.Right && _rectStart is Vector2Int start)
        {
            var end = mouse.LevelTile;
            var min = Vector2Int.Min(start, end);
            var max = Vector2Int.Max(start, end);

            for(int x = min.x; x <= max.x; x++)
            {
                for(int y = min.y; y <= max.y; y++)
                {
                    Apply(new Vector2Int(x, y));
                }
            }

            _rectStart = null;
            LevelLoader.RefreshView(new RectInt(min, max - min + new Vector2Int(1, 1)), Layer);
        }
    }

    protected virtual void Start()
    {
        _shrink = Keybinds.GetAction("Brush Shrink");
        _grow = Keybinds.GetAction("Brush Grow");

        BrushUtils.MakeOutline(out _brushPreview, out _brushMesh);
        _brushPreview.transform.SetParent(transform, false);
        BrushUtils.UpdateOutlineMesh(_brushMesh, _diameter);

        BrushUtils.MakeOutline(out _rectPreview, out _rectMesh);
        _rectPreview.transform.SetParent(transform, false);
    }

    private void ApplyBrush(MouseData mouse)
    {
        if (mouse.EventData.button != PointerEventData.InputButton.Left)
            return;

        var pos = mouse.LevelPos + BrushOffset;
        var tile = Vector2Int.FloorToInt(pos);

        for (int x = 0; x < _diameter; x++)
        {
            for (int y = 0; y < _diameter; y++)
            {
                if (BrushUtils.CircleContains(x, y, _diameter))
                {
                    Apply(new Vector2Int(tile.x + x, tile.y + y));
                }
            }
        }

        LevelLoader.RefreshView(new RectInt(tile, new Vector2Int(_diameter, _diameter)), Layer);
    }

    protected abstract void Apply(Vector2Int pos);

    protected virtual void Update()
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

        // Update brush preview
        if (_rectStart == null)
        {
            if (_diameter != BrushUtils.SharedDiameter)
            {
                _diameter = BrushUtils.SharedDiameter;
                BrushUtils.UpdateOutlineMesh(_brushMesh, _diameter);
            }

            var pos = GetLevelPos(Input.mousePosition) + BrushOffset;
            var tile = Vector2Int.FloorToInt(pos);
            _brushPreview.transform.position = (Vector2)tile * new Vector2(1f, -1f);

            _brushPreview.SetActive(true);
        }
        else
        {
            _brushPreview.SetActive(false);
        }

        // Update rect preview
        if (_rectStart is Vector2Int start)
        {
            var end = Vector2Int.FloorToInt(GetLevelPos(Input.mousePosition));
            var size = end - start;
            size.x = Math.Abs(size.x) + 1;
            size.y = Math.Abs(size.y) + 1;
            if (_rectSize != size)
            {
                _rectSize = size;
                BrushUtils.UpdateRectMesh(_rectMesh, _rectSize.x, _rectSize.y);
            }
            _rectPreview.transform.position = Vector2Int.Min(start, end) * new Vector2(1f, -1f);

            _rectPreview.SetActive(true);
        }
        else
        {
            _rectPreview.SetActive(false);
        }
    }
}
