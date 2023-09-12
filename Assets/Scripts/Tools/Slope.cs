using LevelModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Slope : Tool
{
    private GameObject _brushPreview;
    private Mesh _brushMesh;

    protected override void OnPointerDown(MouseData mouse) => Apply(mouse);
    protected override void OnDrag(MouseData mouse) => Apply(mouse);

    private void Start()
    {
        BrushUtils.MakeOutline(out _brushPreview, out _brushMesh);
        _brushPreview.transform.SetParent(transform, false);
        BrushUtils.UpdateOutlineMesh(_brushMesh, 1);
    }

    private void Update()
    {
        _brushPreview.transform.position = Vector2Int.FloorToInt(GetLevelPos(Input.mousePosition)) * new Vector2(1f, -1f);
    }

    private void Apply(MouseData mouse)
    {
        if (mouse.EventData.button != PointerEventData.InputButton.Left) return;

        if(!Keybinds.Shift)
        {
            var cell = Level.GetGeoCell(mouse.LevelTile, Layer);
            if (!IsSlope(cell.terrain) && IdentifySlope(mouse.LevelTile) is GeoType slope)
            {
                cell.terrain = slope;
                Level.SetGeoCell(mouse.LevelTile, Layer, cell);
                LevelLoader.RefreshView(new RectInt(mouse.LevelTile, Vector2Int.one), Layer);
            }
        }
        else
        {
            var cell = Level.GetGeoCell(mouse.LevelTile, Layer);
            if(IsSlope(cell.terrain))
            {
                cell.terrain = GeoType.Solid;
                Level.SetGeoCell(mouse.LevelTile, Layer, cell);
                LevelLoader.RefreshView(new RectInt(mouse.LevelTile, Vector2Int.one), Layer);
            }
        }
    }

    private static bool IsSlope(GeoType geo)
    {
        return geo is GeoType.BRSlope
            or GeoType.BLSlope
            or GeoType.TRSlope
            or GeoType.TLSlope;
    }

    private GeoType? IdentifySlope(Vector2Int pos)
    {
        var rt = Level.GetGeoCell(pos + new Vector2Int(1, 0), Layer).terrain;
        var lt = Level.GetGeoCell(pos + new Vector2Int(-1, 0), Layer).terrain;
        var bt = Level.GetGeoCell(pos + new Vector2Int(0, 1), Layer).terrain;
        var tt = Level.GetGeoCell(pos + new Vector2Int(0, -1), Layer).terrain;
        
        // Don't replace tiles behind slopes
        if (rt is GeoType.BLSlope or GeoType.TLSlope
            || lt is GeoType.BRSlope or GeoType.TRSlope
            || bt is GeoType.TRSlope or GeoType.TLSlope
            || tt is GeoType.BRSlope or GeoType.BLSlope)
            return null;

        bool r = rt == GeoType.Solid;
        bool l = lt == GeoType.Solid;
        bool b = bt == GeoType.Solid;
        bool t = tt == GeoType.Solid;

        if (r && !l && b && !t) return GeoType.BRSlope;
        if (!r && l && b && !t) return GeoType.BLSlope;
        if (r && !l && !b && t) return GeoType.TRSlope;
        if (!r && l && !b && t) return GeoType.TLSlope;
        return null;
    }
}
