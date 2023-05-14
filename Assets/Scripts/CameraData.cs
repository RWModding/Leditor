using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lingo;
using static Lingo.MiddleMan;
using UnityEngine;


public class CameraData : ILingoData
{
    [LingoIndex(0, "cameras")]
    public List<Vector2> cameraPos;

    //privates are only around for serialization
    [LingoIndex(1, "selectedCamera")]
    private int selectedCamera;
    [LingoIndex(3, "Keys")]
    private KeyValuePair<string, object>[] keys;
    [LingoIndex(4, "lastKeys")]
    private KeyValuePair<string, object>[] lastKeys;

    [LingoIndex(2, "quads")]
    public List<CamQuad[]> camQuads;

    public static CameraData LoadCams(object[] lingos)
    {
        CameraData result = new();
        SyncAllAttributes(result, lingos);
        return result;
    }
}
public class CamQuad : ILingoData
{
    [LingoIndex(0, null)]
    public int angle;

    [LingoIndex(1, null)]
    public float radius; //value of 0 to 1
}


