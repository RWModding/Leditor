using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using LevelModel;
using System.Linq;

/// <summary>
/// Loads tile information from a text file.
/// </summary>
public class TileDatabase : MonoBehaviour
{
    public string InitPath;

    /// <summary>
    /// A list of tile categories in the order that they appear in Init.txt.
    /// </summary>
    public readonly List<TileCategory> Categories = new();
    private readonly Dictionary<string, Tile> tilesByName = new();

    public void Awake()
    {
        // Open init file
        string init;
        try
        {
            init = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, InitPath));
        }
        catch (IOException e)
        {
            Debug.LogError("Failed to read Init.txt for tiles!");
            Debug.LogException(e);
            return;
        }

        // Load all lines
        TileCategory cat = null;
        int lineNum = 0;
        var reader = new StringReader(init);
        while (reader.ReadLine() is string line)
        {
            lineNum++;

            if (string.IsNullOrEmpty(line) || line.StartsWith("--")) continue;

            if (line[0] == '-')
            {
                // Parse category
                try
                {
                    string str = line.Substring(1);
                    cat = new TileCategory(str);
                    Categories.Add(cat);
                }
                catch(Exception e)
                {
                    Debug.LogError($"Failed to load tile category on line {lineNum}!");
                    Debug.LogException(e);
                }
            }
            else if(cat != null)
            {
                // Parse tile
                try
                {
                    var tile = new Tile(line, cat);
                    cat.Tiles.Add(tile);
                    tilesByName[tile.Name] = tile;
                }
                catch(Exception e)
                {
                    Debug.LogError($"Failed to load tile on line {lineNum}!");
                    Debug.LogException(e);
                }
            }
        }

        Debug.Log($"Loaded {Categories.Sum(cat => cat.Tiles.Count)} tiles from {Categories.Count} categories");
    }

    public Tile this[string name]
    {
        get
        {
            if (tilesByName.TryGetValue(name, out var newTile))
                return newTile;
            else
                throw new ArgumentException($"Missing tile: {name}!");
        }
    }
}