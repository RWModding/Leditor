using SimpleFileBrowser;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using LevelModel;
using System;

public class LevelLoader : MonoBehaviour
{
    public LevelData LevelData { get; private set; }

    private readonly RectInt?[] _viewDirty = new RectInt?[4];

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

    public void RefreshView(RectInt dirtyRect, int layer)
    {
        if (layer < 0 || layer > 2) throw new ArgumentOutOfRangeException($"Refresh layer must be between 0 and 2, but was {layer}!");

        _viewDirty[layer] = Combine(dirtyRect, _viewDirty[layer]);
        _viewDirty[3] = Combine(dirtyRect, _viewDirty[3]);

        static RectInt Combine(RectInt a, RectInt? b)
        {
            if (b == null) return a;

            a.xMin = Math.Min(a.xMin, b.Value.xMin);
            a.xMax = Math.Max(a.xMax, b.Value.xMax);
            a.yMin = Math.Min(a.yMin, b.Value.yMin);
            a.yMax = Math.Max(a.yMax, b.Value.yMax);
            return a;
        }
    }

    private void Update()
    {
        if (_viewDirty[3] != null)
        {
            BroadcastMessage("OnLevelViewRefreshed", _viewDirty, SendMessageOptions.DontRequireReceiver);

            Array.Fill(_viewDirty, null);
        }
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

    public struct RefreshArea
    {
        public RectInt Rect;
        public int Layer;
    }
}
