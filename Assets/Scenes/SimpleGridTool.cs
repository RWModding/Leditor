using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleGridTool : Tool
{
    private string _name;
    public override string Name => _name;

    private string _description;
    public override string Description => _description;
    
    public override bool SupportsDrag => false;

    public override GameObject Button { get; set; }

    private GeoType? geo;
    private FeatureType? feature;

    public SimpleGridTool(string name, string description = null, GeoType? geoType = null, FeatureType? featureType = null)
    {
        _name = name;
        _description = description ?? name;

        geo = geoType;
        feature = featureType;
    }

    public override void OnClick(Vector2 position)
    {
        if (EditorManager.Instance.CurrentEditor is GeoEditor editor)
        {
            var pos = new Vector3Int(Mathf.FloorToInt(position.x), Mathf.CeilToInt(position.y));
            if (geo.HasValue)
            {
                editor.TryPlace<GeoType>((int)geo.Value, pos);
            }
            else if (feature.HasValue)
            {
                editor.TryPlace<FeatureType>((int)feature.Value, pos);
            }
        }
    }


    private Vector2 currentDragStart;
    public override void OnDragEnd(Vector2 position)
    {
        GridLines.Instance.rectSelectMode = false;

        if (EditorManager.Instance.CurrentEditor is GeoEditor editor)
        {
            var top = Mathf.CeilToInt(Mathf.Max(currentDragStart.y, position.y));
            var right = Mathf.FloorToInt(Mathf.Max(currentDragStart.x, position.x));
            var bottom = Mathf.CeilToInt(Mathf.Min(currentDragStart.y, position.y));
            var left = Mathf.FloorToInt(Mathf.Min(currentDragStart.x, position.x));

            for (var x = left; x <= right; x++)
            {
                for (var y = top; y >= bottom; y--)
                {
                    var pos = new Vector3Int(x, y);
                    if (geo.HasValue)
                    {
                        editor.TryPlace<GeoType>((int)geo.Value, pos, true);
                    }
                    else if (feature.HasValue)
                    {
                        editor.TryPlace<FeatureType>((int)feature.Value, pos, true);
                    }
                }
            }
        }
    }

    public override void OnDragStart(Vector2 position)
    {
        if (EditorManager.Instance.CurrentEditor is GeoEditor editor) {
            if (editor.CheckPosInBounds(position))
            {
                GridLines.Instance.rectSelectMode = true;
                GridLines.Instance.rectSelectStart = position;
                GridLines.Instance.rectSelectEnd = position;
                currentDragStart = position;
            }
        }
    }

    public override void OnDragUpdate(Vector2 position)
    {
        if (EditorManager.Instance.CurrentEditor is GeoEditor editor)
        {
            GridLines.Instance.rectSelectEnd = editor.ClampPosToBounds(position);
        }
    }
}
