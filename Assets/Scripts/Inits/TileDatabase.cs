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

    public Tile GetTile(int categoryIndex, int tileIndex, string name)
    {
        if (categoryIndex < 0 || categoryIndex >= Categories.Count)
            throw new ArgumentOutOfRangeException(nameof(categoryIndex), $"Unknown tile category index! Trying to find \"{name}\".");

        var cat = Categories[categoryIndex];
        if(tileIndex < 0 || tileIndex >= cat.Tiles.Count)
            throw new ArgumentOutOfRangeException(nameof(categoryIndex), $"Unknown tile index in category \"{cat.Name}\"! Trying to find \"{name}\".");

        var tile = cat.Tiles[tileIndex];

        if (tile.Name != name)
        {
            if (tilesByName.TryGetValue(name, out var newTile))
                return newTile;
            else
                throw new ArgumentException($"Missing tile: {name}!");
        }

        return tile;
    }
}