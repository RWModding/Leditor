using Lingo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class EditorFile
{
    public string Path;
    public string Name;
    public string[] Lines;
    public List<object[]> LingoData;

    public LevelMatrix Geometry;
    public tlMatrix Tiles;
    public CameraData Cameras;

    public string GeometryString { get => Lines[0]; set => Lines[0] = value; }
    public string TilesString { get => Lines[1]; set => Lines[1] = value; }
    public string EffectsString { get => Lines[2]; set => Lines[2] = value; }
    public string LightingString { get => Lines[3]; set => Lines[3] = value; }
    public string Settings1String { get => Lines[4]; set => Lines[4] = value; }
    public string Settings2String { get => Lines[5]; set => Lines[5] = value; }
    public string CamerasString { get => Lines[6]; set => Lines[6] = value; }
    public string WaterString { get => Lines[7]; set => Lines[7] = value; }
    public string PropsString { get => Lines[8]; set => Lines[8] = value; }

    public string TxtFile => Path + Name + ".txt";
    public string PngFile => Path + Name + ".png";

    public EditorFile(string name, string levelString)
    {
        Name = name;
        Lines = levelString.Split('\n');
        Geometry = new LevelMatrix(GeometryString);
    }

    public EditorFile(string path)
    {
        Path = System.IO.Path.GetDirectoryName(path) + System.IO.Path.DirectorySeparatorChar;
        Name = System.IO.Path.GetFileNameWithoutExtension(path);

        Lines = File.ReadAllLines(TxtFile);
        LoadLingo();
        VersionFix();

        Geometry = new LevelMatrix(GeometryString);
        Cameras = CameraData.LoadCams(LingoData[6]);
        
    }

    internal void LoadLingo()
    {
        LingoData = new List<object[]>();
        
        foreach (string line in Lines)
        {
        object obj = LingoParsing.FromLingoString(line);
            if (obj is not object[] arr)
            { Debug.LogError("bad level"); } //if this happens the level is unloadable and should cancel 
            else
            { LingoData.Add(arr); }
        }
    }

    internal void VersionFix() //there are still a few vanilla files missing these sections
    {
        //loadlevel lines 166 - 391, for any lingo folk
       //I've slightly truncated it, since no need to add any runtime properties that the leditor would individually add
        if (LingoData.Count < 7)
        { 
            LingoData.Add(new object[0]);
            LingoData[6].SetFromKey("cameras", new object[] { new Vector2(20f, 30f) });
            LingoData[6].SetFromKey("selectedCamera", 0f);
            LingoData[6].SetFromKey("quads", LingoParsing.FromLingoString("[[0,0],[0,0],[0,0],[0,0]]"));
            LingoData[6].SetFromKey("keys", LingoParsing.FromLingoString("[#n: 0, #d: 0, #e: 0, #p: 0]")); //shortcut
            LingoData[6].SetFromKey("lastKeys", LingoParsing.FromLingoString("[#n: 0, #d: 0, #e: 0, #p: 0]"));
        }

        if (LingoData.Count < 8)
        {
            LingoData.Add(new object[0]);
            LingoData[7].SetFromKey("waterLevel", -1f);
            LingoData[7].SetFromKey("waterInFront", 1f);
            LingoData[7].SetFromKey("waveLength", 60f);
            LingoData[7].SetFromKey("waveAmplitude", 5f);
            LingoData[7].SetFromKey("waveSpeed", 10f);
        }

        if (LingoData.Count < 9)
        {
            LingoData.Add(new object[0]);
            LingoData[8].SetFromKey("props", new object[0]);
        }

        if (!LingoData[6].TryGetFromKey("cameras", out _))
        { LingoData[6].SetFromKey("cameras", new object[] { new Vector2(20f, 30f) }); }

        if (!LingoData[6].TryGetFromKey("quads", out _))
        {
            LingoData[6].TryGetFromKey("cameras", out object cams);

            object defQuads = LingoParsing.FromLingoString("[[0,0],[0,0],[0,0],[0,0]]");
            
            List<object> list = new();
            for (int i = 0; i < (cams as object[]).Length; i++)
            { list.Add(defQuads); }

            LingoData[6].SetFromKey("quads", list.ToArray());
        }

        for (int i = 0; i < LingoData.Count; i++)
        { Lines[i] = LingoData[i].ToLingoString(); }
    }

    internal void SaveToDisk()
    {
        GeometryString = Geometry.ToString();
        CamerasString = Cameras.ToLingoString();
        File.WriteAllLines(TxtFile, Lines);
    }
}
