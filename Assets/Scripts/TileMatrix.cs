using Lingo;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Lingo.MiddleMan;

public class TileData : ILingoData
{
    [LingoIndex(0, "lastKeys")]
    private KeyValuePair<string, object>[] lastKeys;

    [LingoIndex(1, "Keys")]
    private KeyValuePair<string, object>[] keys;

    [LingoIndex(2, "workLayer")]
    private int workLayer;

    [LingoIndex(3, "lstMsPs")]
    private Vector2 lstMsPs;

    [LingoIndex(4, "tlMatrix")]
    public List<List<TileCell[]>> tlMatrix;

    [LingoIndex(5, "defaultMaterial")]
    public string defaultMaterial;

    [LingoIndex(6, "toolType")]
    private string toolType;

    [LingoIndex(7, "toolData")]
    private string toolData;

    [LingoIndex(8, "tmPos")]
    private Vector2 tmPos;

    [LingoIndex(9, "tmSavPosL")]
    private object[] tmSavPosL;

    [LingoIndex(10, "specialEdit")]
    private int specialEdit;

    //-- TODO: handle saving the data back to a string
    public static TileData LoadTiles(object[] lingos)
    {
        TileData result = new();
        SyncAllAttributes(result, lingos);

        foreach (var column in result.tlMatrix)
        {
            foreach (var cell in column)
            {
                foreach (var layer in cell)
                {
                    try
                    {
                        switch (layer.tp)
                        {
                            case "default":
                                break;
                            case "material":
                                layer.name = (string)layer.data;
                                break;
                            case "tileBody":
                                var tbData = (object[])layer.data;
                                layer.headPos = (Vector2)tbData[0];
                                layer.headLayer = int.Parse(tbData[1].ToString());
                                break;
                            case "tileHead":
                                var thData = layer.data as object[];
                                Vector2 tileInfo = (Vector2)thData[0];
                                layer.categoryIndex = (int)tileInfo.x;
                                layer.tileIndex = (int)tileInfo.y;
                                layer.name = layer.data.ToString();
                                break;
                            default:
                                Debug.LogError($"invalid tile type {layer.ToLingoString()}");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"could not parse data from tile {layer.ToLingoString()}");
                        Debug.LogException(ex);
                    }
                }
            }
        }

        return result;
    }
}

public class TileCell : ILingoData
{
    [LingoIndex(0, "tp")]
    public string tp;

    [LingoIndex(1, "data")]
    public object data;

    public int categoryIndex;
    public int tileIndex;

    public string name;
    public Vector2 headPos;
    public int headLayer;
}