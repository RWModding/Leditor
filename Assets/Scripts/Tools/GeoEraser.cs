using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using LevelModel;

public class GeoEraser : BrushTool
{
    protected override void Apply(Vector2Int pos)
    {
        Level.SetGeoCell(pos, Layer, default);
    }
}