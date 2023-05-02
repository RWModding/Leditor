using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EditorFile
{
    public string Path;
    public string Name;
    public string[] Lines;

    public LevelMatrix Geometry;

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

    public EditorFile(string path)
    {
        Path = System.IO.Path.GetDirectoryName(path) + System.IO.Path.DirectorySeparatorChar;
        Name = System.IO.Path.GetFileNameWithoutExtension(path);

        Lines = File.ReadAllLines(TxtFile);
        Geometry = new LevelMatrix(GeometryString);
    }
}
