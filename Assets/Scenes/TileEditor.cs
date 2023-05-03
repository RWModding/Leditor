using Newtonsoft.Json;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEditor;
using System.IO;

public class TileEditor : MonoBehaviour
{
    private List<Category> categories = new();

    
    void Awake()
    {
        string path = EditorUtility.OpenFilePanel("INIT FILE","","txt"); //this should be replaced eventually.  
        string[] tileInit = File.ReadAllLines(path);
        LoadTilesFromInit(tileInit); //really this should be loaded only once when the program is started.
    }

    // Start is called before the first frame update
    private void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadTilesFromInit(string[] Init)
    {
        //-["Tile Test", color(210, 180, 180)]
        //[#nm:"3DBrick", #sz:point(1,1), #specs:[1], #specs2:0, #tp:"voxelStruct", #repeatL:[1, 1, 1, 7], #bfTiles:0, #rnd:1, #ptPos:0, #tags:[]]
        Category cat = null; // =^._.^=
        foreach (string line in Init)
        {
            if (line.StartsWith("-"))
            {
                var categoryinit = JsonConvert.DeserializeObject<List<object>>(line.Substring(1));
                string catName = categoryinit[0].ToString();
                Color catColor = (Color)categoryinit[1];
                cat = new Category(catName,catColor);
            }
            else
            {
                var tileInit = JsonConvert.DeserializeObject<List<object>>(line);
                string Name = tileInit[0].ToString();
                Vector2 Size = (Vector2)tileInit[1]; //gwah this probably wont work but fuck it.
                
                var savedSpec = JsonConvert.DeserializeObject<List<GeoType>>(tileInit[2].ToString());
                GeoType[] spec = new GeoType[savedSpec.Count];
                for (int k = 0; k < savedSpec.Count; k++)
                {
                    spec[k] = savedSpec[k];
                }
                GeoType[] spec2 = null;
                if ((int)tileInit[3] != 0)
                {
                    var savedSpec2 = JsonConvert.DeserializeObject<List<GeoType>>(tileInit[3].ToString());
                     spec2 = new GeoType[savedSpec2.Count];
                    for (int k = 0; k < savedSpec2.Count; k++)
                    {
                        spec2[k] = savedSpec2[k];
                    }
                }
                var renderType = (RenderType)tileInit[4];
                int[] layers = null;
                if(renderType != RenderType.voxelStructRockType && renderType != RenderType.box)
                {
                    var layerin = JsonConvert.DeserializeObject<int[]>(tileInit[5].ToString());
                     layers = new int[layerin.Length];
                    for (int k = 0; k < savedSpec.Count; k++)
                    {
                        layers[k] = layerin[k];
                    }
                }
                var buffertiles = JsonConvert.DeserializeObject<int>(tileInit[layers == null ? 5 : 6].ToString());

                var rnd = JsonConvert.DeserializeObject<int>(tileInit[layers == null ? 5 : 6].ToString());
                var tags = JsonConvert.DeserializeObject<List<string>>(tileInit[layers == null ? 8 : 9].ToString());

                string ImagePath = Application.dataPath + "/Tiles/" + Name + ".png";
                Texture2D image = (Texture2D)Resources.Load(ImagePath);
                if(layers == null)
                {
                    layers = new int[]{ 1 };
                }
                TileImageStuff tileimage = new(image, buffertiles, Size, layers.Length);

                cat.tiles.Add(new LETile(Name,Size,spec,spec2, renderType,layers,buffertiles,rnd,tags,tileimage));
            }
        }
    }
}
