using LevelModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class EffectDatabase : MonoBehaviour
{
    public string InitPath;

    public readonly List<EffectCategory> Categories = new();
    private readonly Dictionary<string, Effect> effectsByName = new(StringComparer.OrdinalIgnoreCase);

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
            Debug.LogError("Failed to read Init.txt for effects!");
            Debug.LogException(e);
            return;
        }

        // Load all lines
        EffectCategory cat = null;
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
                    cat = new EffectCategory(str);
                    Categories.Add(cat);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load effect category on line {lineNum}!");
                    Debug.LogException(e);
                }
            }
            else if (cat != null)
            {
                // Parse tile
                try
                {
                    var effect = new Effect(line, cat);
                    cat.Effects.Add(effect);
                    effectsByName[effect.Name] = effect;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load effect on line {lineNum}!");
                    Debug.LogException(e);
                }
            }
        }

        //Debug.Log($"Loaded {Categories.Sum(cat => cat.Effects.Count)} effects from {Categories.Count} categories");
    }

    public Effect this[string name]
    {
        get
        {
            if (!effectsByName.TryGetValue(name, out var mat))
                throw new KeyNotFoundException($"Could not find effect: {name}");

            return mat;
        }
    }
}
