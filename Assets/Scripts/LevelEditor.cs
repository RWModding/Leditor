using SimpleFileBrowser;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using LevelModel;
using System;

public class LevelEditor : MonoBehaviour
{
    public LevelData LevelData { get; private set; }

    public void LoadPressed()
    {
        FileBrowser.SetFilters(false, "txt");
        FileBrowser.ShowLoadDialog(
            onSuccess: paths => LoadLevel(paths[0]),
            onCancel: null,
            FileBrowser.PickMode.Files,
            title: "Load Level"
        );
    }

    private void LoadLevel(string path)
    {
        try
        {
            var text = File.ReadAllText(path);
            LevelData = new LevelData(
                text,
                FindAnyObjectByType<TileDatabase>(),
                FindAnyObjectByType<MaterialDatabase>(),
                FindAnyObjectByType<PropDatabase>(),
                FindAnyObjectByType<EffectDatabase>()
            );

            //ValidateSave(text, LevelData);

            Debug.Log($"Loaded {Path.GetFileNameWithoutExtension(path)}");

            LevelLoaded();
        }
        catch(Exception e)
        {
            Debug.LogError($"Failed to load level: {path}");
            Debug.LogException(e);
        }
    }

    // Make sure that loading and saving a file doesn't modify it. Not all differences are errors
    // Integral floats in points/rects are converted to int: point(12.0000, 34) -> point(12, 34)
    // Big numbers are subject to rounding errors: 1234.5678 -> 1234.5680
    // Geo feature order changes: [1, [2, 1]] -> [1, [1, 2]]
    private void ValidateSave(string loadText, LevelData level)
    {
        var loadLines = loadText.Split('\r');
        var saveLines = level.Save().Split('\r');

        for(int i = 0; i < loadLines.Length && i < saveLines.Length; i++)
        {
            var a = loadLines[i] + new string(' ', 20);
            var b = saveLines[i] + new string(' ', 20);
            if(a != b)
            {
                for(int j = 0; j < a.Length && j < b.Length; j++)
                {
                    if (a[j] != b[j])
                    {
                        Debug.LogWarning($"Line {i + 1} char {j + 1}\n" + a.Substring(Math.Max(j - 10, 0), 20) + "\n" + b.Substring(Math.Max(j - 10, 0), 20));
                        break;
                    }
                }
            }
        }
    }

    private void LevelLoaded()
    {
        BroadcastMessage("OnLevelLoaded", SendMessageOptions.DontRequireReceiver);
    }
}
