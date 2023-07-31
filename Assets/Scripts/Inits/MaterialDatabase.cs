using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using LevelModel;

/// <summary>
/// Loads material information from text files.
/// </summary>
public class MaterialDatabase : MonoBehaviour
{
    public string InitPath;

    public readonly List<TileMaterial> Materials = new();
    private readonly Dictionary<string, TileMaterial> materialsByName = new();

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
            Debug.LogError("Failed to read Init.txt for materials!");
            Debug.LogException(e);
            return;
        }

        // Load all lines
        int lineNum = 0;
        var reader = new StringReader(init);
        while (reader.ReadLine() is string line)
        {
            lineNum++;

            if (string.IsNullOrEmpty(line) || line.StartsWith("--")) continue;

            if (line[0] == '-')
            {
                // Material categories aren't implemented
            }
            else
            {
                // Parse material
                try
                {
                    var mat = new TileMaterial(line);
                    Materials.Add(mat);
                    materialsByName.Add(mat.Name, mat);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse material on line {lineNum}!");
                    Debug.LogException(e);
                }
            }
        }

        //Debug.Log($"Loaded {Materials.Count} materials");
    }

    public TileMaterial this[string name]
    {
        get
        {
            if (!materialsByName.TryGetValue(name, out var mat))
                throw new KeyNotFoundException($"Could not find material: {name}");

            return mat;
        }
    }
}
