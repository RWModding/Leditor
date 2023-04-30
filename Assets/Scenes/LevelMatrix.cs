using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A level's geomtry information.
/// </summary>
public class LevelMatrix
{
    public LColumn[] columns;
    public int Width
    {
        get { return columns.Length; }
    }
    public int Height
    {
        get { return columns[0].cells.Length; }
    }

    /// <summary>
    /// Create the geometry matrix for a level.
    /// </summary>
    /// <param name="height">Level's Height</param>
    /// <param name="width">Level's Width</param>
    public LevelMatrix(int height, int width)
    {
        columns = new LColumn[width];
        for (int i = 0; i < height; i++)
        {
            columns[i] = new LColumn(height);
        }
    }

    /// <summary>
    /// Create the geometry matrix for a level from a saved geometry string.
    /// </summary>
    /// <param name="saved">Level's geometry string</param>
    public LevelMatrix(string saved)
    {
        var savedColumns = JsonConvert.DeserializeObject<List<List<List<List<object>>>>>(saved);

        columns = new LColumn[savedColumns.Count];
        for (var i = 0; i < savedColumns.Count; i++) 
        {
            var savedColumn = savedColumns[i];
            var column = new LColumn(savedColumn.Count);

            for (var j = 0; j < savedColumn.Count; j++)
            {
                var savedCell = savedColumn[j];
                var cell = new LCell();
            
                for (var k = 0; k < savedCell.Count; k++)
                {
                    var savedLayer = savedCell[k];
                    var savedFeatures = (IList) savedLayer[1];

                    var layer = new LLayer((GeoType) Enum.Parse(typeof(GeoType), savedLayer[0].ToString()));

                    foreach (var savedFeature in savedFeatures)
                    {
                        layer.AddFeature((FeatureType) Enum.Parse(typeof(FeatureType), savedFeature.ToString()));
                    }

                    cell.layers[k] = layer;
                }
                column.cells[j] = cell;
            }
            columns[i] = column;
        }
    }

    /// <summary>
    /// Change geometry type at a set index. 0 Indexed.
    /// </summary>
    /// <param name="col">Column</param>
    /// <param name="row">Row</param>
    /// <param name="layer">Layer</param>
    /// <param name="geo">Geometry type</param>
    public void ChangeGeoAtIndex(int col, int row, int layer, GeoType geo)
    {
        CheckIfInBounds(col, row);
        columns[col].cells[row].layers[layer].geoType = geo;
    }
    /// <summary>
    /// Changes geomtry type at a set index for all layers. 0 Indexed.
    /// </summary>
    /// <param name="col">Column</param>
    /// <param name="row">Row</param>
    /// <param name="geo">Geometry type</param>
    public void ChangeGeoAtIndex(int col, int row, GeoType geo)
    {
        ChangeGeoAtIndex(col, row, 0, geo);
        ChangeGeoAtIndex(col, row, 1, geo);
        ChangeGeoAtIndex(col, row, 2, geo);
    }

    /// <summary>
    /// Changes feature type at a set index. 0 indexed.
    /// </summary>
    /// <param name="col">Column</param>
    /// <param name="row">Row</param>
    /// <param name="layer">Layer</param>
    /// <param name="feature">Feature type</param>
    public void ChangeFeatureAtIndex(int col, int row, int layer, FeatureType feature)
    {
        CheckIfInBounds(col, row);
        columns[col].cells[row].layers[layer].ChangeFeature(feature);
    }

    /// <summary>
    /// Changes feature type at a set index for all layers. 0 Indexed.
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    /// <param name="feature"></param>
    public void ChangeFeatureAtIndex(int col, int row, FeatureType feature)
    {
        ChangeFeatureAtIndex(col, row, 0, feature);
        ChangeFeatureAtIndex(col, row, 1, feature);
        ChangeFeatureAtIndex(col, row, 2, feature);
    }

    /// <summary>
    /// Checks if a specified index is within the level's bounds. 0 Indexed.
    /// </summary>
    /// <param name="col">Column</param>
    /// <param name="row">Row</param>
    /// <returns></returns>
    public bool CheckIfInBounds(int col, int row)
    {
        if (Width < col + 1 || Height < row + 1)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Resizes a level's matrix. Does not effect tiles or props!
    /// </summary>
    /// <param name="left">Tiles to add to the left</param>
    /// <param name="right">Tiles to add to the right</param>
    /// <param name="top">Tiles to add to the top</param>
    /// <param name="bottom">Tiles to add to the bottom</param>
    public void ResizeLevel(int left, int right, int top, int bottom)
    {
        int newwidth = Width + left + right;
        int newheight = Height + top + bottom;

        LColumn[] old = columns;
        LColumn[] newcols = new LColumn[newwidth];
        for (int i = 0; i < newwidth; i++)
        {
            newcols[i] = new LColumn(newheight);
        }

        for (int i = left; i < (newwidth - right); i++)
        {
            LColumn currentcol = columns[i];
            LColumn selectedcol = old[i - left];
            currentcol = selectedcol;
        }
    }
    public override string ToString()
    {
        string res = "[";
        for (int i = 0; i < columns.Length; i++)
        {
            res += columns[i].ToString();
            if (i < columns.Length - 1)
            {
                res += ",";
            }
        }
        res += "]";
        return res;
    }
}

/// <summary>
/// A column in the geo editor.
/// </summary>
public class LColumn
{
    public LCell[] cells;
    public LColumn(int rows)
    {
        cells = new LCell[rows];
        for (int i = 0; i < rows; i++)
        {
            cells[i] = new LCell();
        }
    }
    public override string ToString()
    {
        string res = "[";
        for (int i = 0; i < cells.Length; i++)
        {
            res += cells[i].ToString();
            if (i < cells.Length - 1)
            {
                res += ",";
            }
        }
        res += "]";
        return res;
    }
}

/// <summary>
/// a cell in the geometry editor.
/// </summary>
public class LCell
{
    public LLayer[] layers;

    public LCell()
    {
        layers = new LLayer[3]; //in theory w/ a custom renderer this could be changed to allow more layers.
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i] = new LLayer(GeoType.Air);
        }
    }

    public override string ToString()
    {
        string res = "[";
        for (int i = 0; i < layers.Length; i++)
        {
            res += layers[i].ToString();
            if (i < layers.Length - 1)
            {
                res += ",";
            }
        }
        res += "]";
        return res;
    }
}

/// <summary>
/// A geo layer for a cell.
/// </summary>
public class LLayer
{
    public GeoType geoType;
    public List<FeatureType> featureType = new List<FeatureType>();


    public LLayer(GeoType geoType)
    {
        this.geoType = geoType;
    }

    public void ChangeFeature(FeatureType feature)
    {
        if (featureType.Contains(feature))
        {
            RemoveFeature(feature);
        }
        else
        {
            AddFeature(feature);
        }
    }
    //having these separate methods just so clearall can just call remove feature etc.
    public void RemoveFeature(FeatureType feature)
    {
        if (featureType.Contains(feature))
        {
            featureType.Remove(feature);
        }
    }
    public void AddFeature(FeatureType feature)
    {
        if (!featureType.Contains(feature))
        {
            featureType.Add(feature);
        }
    }

    public override string ToString()
    {
        string res = string.Format("[{0},[", geoType);
        for (int i = 0; i < featureType.Count; i++)
        {
            res += featureType[i].ToString();
            if (i < featureType.Count - 1)
            {
                res += ",";
            }
        }
        res += "]]";
        return res;
    }

}

public enum GeoType
{
    Air = 0,
    Solid = 1,
    BLSlope = 2,
    BRSlope = 3,
    TLSlope = 4,
    TRSlope = 5,
    Platform = 6,
    Entrance = 7,

    GlassWall = 9
}

public enum FeatureType
{
    nothing = 0,
    horbeam = 1,
    vertbeam = 2,
    hive = 3,
    shortcutentrance = 4,
    shortcutdot = 5,
    entrance = 6,
    dragonDen = 7,

    rock = 9,
    spear = 10,
    crack = 11,
    forbidBats = 12,
    garbageHole = 13,
    waterfall = 18,
    WHAMH = 19,
    wormGrass = 20,
    scavengerHole = 21,

}