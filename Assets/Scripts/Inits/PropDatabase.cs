using LevelModel;
using Lingo;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Loads prop information from a text file.
/// </summary>
public class PropDatabase : MonoBehaviour
{
    public TileDatabase TileToPropDatabase;
    public string[] InitPaths;

    public readonly List<PropCategory> Categories = new();
    private readonly Dictionary<string, Prop> propsByName = new(StringComparer.OrdinalIgnoreCase);

    public void Awake()
    {
        // Load normal props
        foreach (var path in InitPaths)
        {
            LoadProps(Path.Combine(Application.streamingAssetsPath, path));
        }

        //Debug.Log($"Loaded {Categories.Sum(cat => cat.Props.Count)} props from {Categories.Count} categories");
    }

    public void Start()
    {
        // Load tiles as props
        if(TileToPropDatabase != null)
        {
            LoadTilesAsProps(TileToPropDatabase);
        }
    }

    private void LoadTilesAsProps(TileDatabase db)
    {
        var tilesAsProps = new PropCategory("Tiles as props", new Color(1f, 0f, 0f));
        Categories.Add(tilesAsProps);

        foreach (var tileCat in db.Categories)
        {
            foreach(var tile in tileCat.Tiles)
            {
                var prop = tile.CreateProp(tilesAsProps);
                if (prop != null)
                {
                    tilesAsProps.Props.Add(prop);
                    propsByName[prop.Name] = prop;
                }
            }
        }
    }

    private void LoadProps(string path)
    {
        // Open init file
        string init;
        try
        {
            init = File.ReadAllText(path);
        }
        catch (IOException e)
        {
            Debug.LogError("Failed to read Init.txt for props!");
            Debug.LogException(e);
            return;
        }
        string dirName = Path.GetDirectoryName(path);

        // Load all lines
        PropCategory cat = null;
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
                    cat = new PropCategory(str);
                    Categories.Add(cat);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load prop category on line {lineNum} of {path}!");
                    Debug.LogException(e);
                }
            }
            else
            {
                // Parse prop
                try
                {
                    var prop = new Prop(line, cat, dirName);
                    cat.Props.Add(prop);
                    propsByName[prop.Name] = prop;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load prop on line {lineNum} of {path}!");
                    Debug.LogException(e);
                }
            }
        }
    }

    public Prop this[string name]
    {
        get
        {
            if (!propsByName.TryGetValue(name, out var mat))
                throw new KeyNotFoundException($"Could not find prop: {name}");

            return mat;
        }
    }
}
