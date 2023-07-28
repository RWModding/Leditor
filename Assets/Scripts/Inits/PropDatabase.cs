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
    public string[] InitPaths;

    public readonly List<PropCategory> Categories = new();
    private readonly Dictionary<string, Prop> propsByName = new();

    public void Awake()
    {
        foreach (var path in InitPaths)
        {
            // Open init file
            string init;
            try
            {
                init = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, path));
            }
            catch (IOException e)
            {
                Debug.LogError("Failed to read Init.txt for props!");
                Debug.LogException(e);
                return;
            }

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
                        Debug.LogError($"Failed to load prop category on line {lineNum}!");
                        Debug.LogException(e);
                    }
                }
                else
                {
                    // Parse prop
                    try
                    {
                        var prop = new Prop(line, cat);
                        cat.Props.Add(prop);
                        propsByName[prop.Name] = prop;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to load prop on line {lineNum}!");
                        Debug.LogException(e);
                    }
                }
            }
        }

        Debug.Log($"Loaded {Categories.Sum(cat => cat.Props.Count)} props from {Categories.Count} categories");
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
