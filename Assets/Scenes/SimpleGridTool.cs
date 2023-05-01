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
        if (EditorManager.Instance.CurrentEditor is IGridEditor editor)
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

    public override void OnDragEnd(Vector2 position)
    {
        throw new NotImplementedException();
    }

    public override void OnDragStart(Vector2 position)
    {
        throw new NotImplementedException();
    }

    public override void OnDragUpdate(Vector2 position)
    {
        throw new NotImplementedException();
    }
}
