using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using LevelModel;
using System;

public class GeoBrush : BrushTool
{
    public Texture2D SolidCursor;
    public Texture2D AirCursor;
    public bool PaintAir;
    
    private Keybinds.ActionInfo _swap;

    protected override void Start()
    {
        base.Start();

        _swap = Keybinds.GetAction("Geo Swap");
    }

    protected override void Apply(Vector2Int pos)
    {
        var cell = Level.GetGeoCell(pos, Layer);
        cell.terrain = (PaintAir != Keybinds.Shift) ? GeoType.Air : GeoType.Solid;
        Level.SetGeoCell(pos, Layer, cell);
    }

    protected override void Update()
    {
        if(Input.GetKeyDown(_swap.CurrentKey))
        {
            PaintAir = !PaintAir;
        }
        CursorOverride = (PaintAir != Keybinds.Shift) ? AirCursor : SolidCursor;

        base.Update();
    }
}