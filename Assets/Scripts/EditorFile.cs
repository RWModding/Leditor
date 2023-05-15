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
    public object[][] LingoData;

    public LevelMatrix Geometry;
    public TileData Tiles;
    public CameraData Cameras;

    public string GeometryString { get => Lines[0]; set => Lines[0] = value; }
    public string TilesString { get => Lines[1]; set => Lines[1] = value; }
    public string EffectsString { get => Lines[2]; set => Lines[2] = value; }
    public string LightingString { get => Lines[3]; set => Lines[3] = value; }
    public string Settings1String { get => Lines[4]; set => Lines[4] = value; } //Director calls this... "LEVEL"
    public string Settings2String { get => Lines[5]; set => Lines[5] = value; } //Director calls this "LevelOverview"
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
        Tiles = TileData.LoadTiles(LingoData[1]);
        Cameras = CameraData.LoadCams(LingoData[6]);
    }

    internal void LoadLingo()
    {
        LingoData = new object[9][];
        for (int i = 0; i < 9; i++)
        {
            if (Lines.Length < i || LingoParsing.FromLingoString(Lines[i]) is not object[] arr)
            {
                //if this happens the level has something broken or missing but it's not necessarily unloadable!
                Debug.LogError($"bad level: line {i} failed to parse"); 
                LingoData[i] = new object[0];
            }
            else
            { LingoData[i] = arr; }
        }
    }

    internal void VersionFix() //there are still a few vanilla files missing these sections
    {
        //loadlevel lines 166 - 391, for any lingo folk
        //I've slightly truncated it, since no need to add any runtime properties that the leditor would individually add

        /// things the leditor adds if missing
        /// gTEprops: #specialEdit = 0
        /// gLEVEL: lots of stuff but it literally doesn't matter
        /// LOprops: #light = 1, #size = point(52, 40), #extraTiles = [1,1,1,3], #tileSeed = random(400), #colGlows = [0,0]
        /// gCameraProps: #quads = default quads per camera (as below)
        /// gPEprops: #color = 0, #props = []
        /// 
        /// and also resetting if entire lines are missing or if the main parameter from those lines isn't present
        /// 
        /// and also also there are some funny loops through the tiles, effect, and props to fix some version differences
        /// might be worth taking a look some time...

        if (LingoData[6].Length == 0)
        { 
            LingoData[6] = LingoData[6].SetFromKey("cameras", new object[] { new Vector2(20f, 30f) });
            LingoData[6] = LingoData[6].SetFromKey("selectedCamera", 0f);
            LingoData[6] = LingoData[6].SetFromKey("quads", LingoParsing.FromLingoString("[[0,0],[0,0],[0,0],[0,0]]"));
            LingoData[6] = LingoData[6].SetFromKey("keys", LingoParsing.FromLingoString("[#n: 0, #d: 0, #e: 0, #p: 0]")); //shortcut
            LingoData[6] = LingoData[6].SetFromKey("lastKeys", LingoParsing.FromLingoString("[#n: 0, #d: 0, #e: 0, #p: 0]"));
        }

        //Lingo: spelrelaterat.ResetgEnvEditorProps
        if (LingoData[7].Length == 0 || LingoData[7].TryGetFromKey("waterLevel", out _))
        {
            LingoData[7] = LingoData[7].SetFromKey("waterLevel", -1f);
            LingoData[7] = LingoData[7].SetFromKey("waterInFront", 1f);
            LingoData[7] = LingoData[7].SetFromKey("waveLength", 60f);
            LingoData[7] = LingoData[7].SetFromKey("waveAmplitude", 5f);
            LingoData[7] = LingoData[7].SetFromKey("waveSpeed", 10f);
        }

        //Lingo: spelrelaterat.resetPropEditorProps
        if (LingoData[8].Length == 0 || LingoData[8].TryGetFromKey("props", out _))
        {
            //ugh this is a lot
            //but if props are added and not the rest, official editor will break
            LingoData[8] = LingoData[8].SetFromKey("props", new object[0]);
            LingoData[8] = LingoData[8].SetFromKey("lastKeys", new object[0]);
            LingoData[8] = LingoData[8].SetFromKey("Keys", new object[0]);
            LingoData[8] = LingoData[8].SetFromKey("workLayer", 1f);
            LingoData[8] = LingoData[8].SetFromKey("lstMsPs", new Vector2());
            LingoData[8] = LingoData[8].SetFromKey("pmPos", new Vector2(1, 1));
            LingoData[8] = LingoData[8].SetFromKey("pmSavPosL", new object[0]);
            LingoData[8] = LingoData[8].SetFromKey("propRotation", 0f);
            LingoData[8] = LingoData[8].SetFromKey("propStretchX", 1f);
            LingoData[8] = LingoData[8].SetFromKey("propStretchY", 1f);
            LingoData[8] = LingoData[8].SetFromKey("propFlipX", 1f);
            LingoData[8] = LingoData[8].SetFromKey("propFlipY", 1f);
            LingoData[8] = LingoData[8].SetFromKey("depth", 0f);
            LingoData[8] = LingoData[8].SetFromKey("color", 0f);
        }

        if (!LingoData[6].TryGetFromKey("cameras", out _))
        { LingoData[6] = LingoData[6].SetFromKey("cameras", new object[] { new Vector2(20f, 30f) }); }

        if (!LingoData[6].TryGetFromKey("quads", out _))
        {
            LingoData[6].TryGetFromKey("cameras", out object cams);

            object defQuads = LingoParsing.FromLingoString("[[0,0],[0,0],[0,0],[0,0]]");
            
            List<object> list = new();
            for (int i = 0; i < (cams as object[]).Length; i++)
            { list.Add(defQuads); }

            LingoData[6] = LingoData[6].SetFromKey("quads", list.ToArray());
        }

        if (Lines.Length < LingoData.Length)
        {
            Array.Resize(ref Lines, LingoData.Length);
        }

        for (int i = 0; i < LingoData.Length; i++)
        { Lines[i] = LingoData[i].ToLingoString(); }
    }

    internal void SaveToDisk()
    {
        GeometryString = Geometry.ToString();
        CamerasString = Cameras.ToLingoString();
        File.WriteAllLines(TxtFile, Lines);
    }
}
