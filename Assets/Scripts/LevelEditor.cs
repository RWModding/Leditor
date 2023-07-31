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
            LevelData = new LevelData(
                File.ReadAllText(path),
                FindAnyObjectByType<TileDatabase>(),
                FindAnyObjectByType<MaterialDatabase>(),
                FindAnyObjectByType<PropDatabase>(),
                FindAnyObjectByType<EffectDatabase>()
            );

            Debug.Log($"Loaded {Path.GetFileNameWithoutExtension(path)}");

            LevelLoaded();
        }
        catch(Exception e)
        {
            Debug.LogError($"Failed to load level: {path}");
            Debug.LogException(e);
        }
    }

    private void LevelLoaded()
    {
        BroadcastMessage("OnLevelLoaded", SendMessageOptions.DontRequireReceiver);
    }
}
