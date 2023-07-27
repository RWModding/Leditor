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
    public string[] CsvPaths;

    public readonly List<TileMaterial> Materials = new();
    private readonly Dictionary<string, TileMaterial> materialsByName = new();

    public void Awake()
    {
        if (CsvPaths == null) return;

        foreach(var path in CsvPaths)
        {
            // Open and parse file
            try
            {
                string csv = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, path));
                var reader = new StringReader(csv);
                int lineNum = 0;

                while(reader.ReadLine() is string line)
                {
                    lineNum++;

                    try
                    {
                        var mat = new TileMaterial(line);
                        Materials.Add(mat);
                        materialsByName.Add(mat.Name, mat);
                    }
                    catch(Exception e)
                    {
                        Debug.LogError($"Failed to parse material in {path}, line {lineNum}!");
                        Debug.LogException(e);
                    }
                }
            }
            catch (IOException e)
            {
                Debug.LogError($"Failed to read materials from {path}!");
                Debug.LogException(e);
                return;
            }
        }

        Debug.Log($"Loaded {Materials.Count} materials from {CsvPaths.Length} files");
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
