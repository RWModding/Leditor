using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class NewBehaviourScript : MonoBehaviour
{
    private readonly Dictionary<GeoType, Tile> GeoTiles = new();
    private readonly Dictionary<FeatureType, Sprite> FeatureSprites = new();
    private readonly List<Tilemap> GeoLayers = new();

    private Tilemap tileMap;

    private void Awake()
    {
        GeoLayers.AddRange(GetComponentsInChildren<Tilemap>());

        LoadGeoTiles();
        LoadFeatureSprites();
    }

    #region Asset loading
    private void LoadGeoTiles()
    {
        GeoTiles[GeoType.Air] = null;
        GeoTiles[GeoType.Solid] = (Tile)Resources.Load("geo/palette/solid");
        GeoTiles[GeoType.BLSlope] = (Tile)Resources.Load("geo/palette/blslope");
        GeoTiles[GeoType.BRSlope] = (Tile)Resources.Load("geo/palette/brslope");
        GeoTiles[GeoType.TLSlope] = (Tile)Resources.Load("geo/palette/tlslope");
        GeoTiles[GeoType.TRSlope] = (Tile)Resources.Load("geo/palette/trslope");
        GeoTiles[GeoType.Platform] = (Tile)Resources.Load("geo/palette/platform");
        GeoTiles[GeoType.Entrance] = (Tile)Resources.Load("geo/palette/entrance");
        GeoTiles[GeoType.GlassWall] = (Tile)Resources.Load("geo/palette/glasswall");
    }

    private void LoadFeatureSprites()
    {
        var sprites = Resources.LoadAll<Sprite>("geo/texture");

        foreach (FeatureType feature in Enum.GetValues(typeof(FeatureType)))
        {
            var spriteName = feature switch
            {
                FeatureType.horbeam => "horbeam",
                FeatureType.vertbeam => "vertbeam",
                FeatureType.hive => "hive",
                FeatureType.shortcutentrance => "shortcutentrance_invalid",
                FeatureType.shortcutdot => "shortcutdot",
                FeatureType.entrance => "entrance",
                FeatureType.dragonDen => "dragonden",
                FeatureType.rock => "rock",
                FeatureType.spear => "spear",
                FeatureType.crack => "crack",
                FeatureType.forbidBats => "forbidbats",
                FeatureType.garbageHole => "garbagehole",
                FeatureType.waterfall => "waterfall",
                FeatureType.WHAMH => "whamh",
                FeatureType.wormGrass => "wormgrass",
                FeatureType.scavengerHole => "scavengerhole",
                _ => ""
            };

            if (!string.IsNullOrEmpty(spriteName))
            {
                var sprite = sprites.FirstOrDefault(x => x.name == spriteName);
                if (sprite != null)
                {
                    FeatureSprites[feature] = sprite;
                }
            }
        }
    }
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        var matrix = new LevelMatrix("[[[[9, []], [0, []], [0, []]], [[1, []], [0, []], [0, []]], [[0, []], [1, []], [0, []]], [[0, []], [0, []], [1, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, [11]]], [[0, []], [0, [2]], [9, []]], [[0, []], [0, []], [0, []]], [[0, [21]], [0, []], [0, []]]], [[[1, [11]], [0, []], [0, []]], [[1, []], [1, []], [1, []]], [[1, []], [1, []], [0, []]], [[1, []], [0, []], [1, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, [10]]], [[0, []], [0, [2]], [0, [2]]], [[0, []], [0, []], [0, []]], [[0, [19]], [0, []], [0, []]]], [[[0, []], [1, []], [0, []]], [[1, []], [1, []], [0, []]], [[1, []], [1, []], [1, []]], [[0, []], [1, []], [1, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, [9]]], [[0, []], [0, [2]], [0, [6, 12, 7, 5, 4, 1]]], [[0, []], [0, []], [0, []]], [[0, [13]], [0, []], [0, []]]], [[[0, []], [0, []], [1, []]], [[1, []], [0, []], [1, []]], [[0, []], [1, []], [1, []]], [[1, []], [1, []], [1, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, [12]]], [[0, [9]], [0, []], [0, []]], [[0, [20]], [0, []], [0, []]]], [[[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, [10, 9, 2]], [0, [21, 19, 13, 20, 18, 3]]], [[0, [10]], [0, []], [0, []]], [[0, [12]], [0, []], [0, []]]], [[[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, [18]], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, [11]], [0, []]], [[0, [11]], [0, []], [0, []]], [[0, [3]], [0, []], [0, []]]], [[[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [9, []], [0, []]], [[0, [1, 5]], [0, []], [0, []]], [[0, [18]], [0, []], [0, []]]], [[[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[1, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[1, []], [0, []], [0, []]], [[0, []], [0, [2]], [0, []]], [[0, [2, 5]], [0, [1, 2]], [0, []]], [[0, [6]], [0, []], [0, []]]], [[[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[9, []], [0, []], [0, []]], [[0, []], [0, [5]], [0, []]], [[1, []], [0, [12, 6, 7, 5, 4, 1]], [0, []]], [[9, []], [0, [1, 2]], [0, []]], [[0, [7]], [0, []], [0, []]]], [[[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, []], [0, []], [0, []]], [[0, [21, 19, 13, 20, 18, 3, 12, 5, 6]], [0, []], [0, []]], [[0, []], [0, [21, 19, 13, 20, 18, 3]], [0, []]], [[0, [4]], [0, []], [0, []]], [[0, [5]], [0, []], [0, []]]]]");


        for (var z = 0; z < GeoLayers.Count; z++)
        {
            var positions = new Vector3Int[matrix.Width * matrix.Height];
            var tiles = new Tile[positions.Length];

            for (var x = 0; x < matrix.Width; x++)
            {
                for (var y = 0; y < matrix.Height; y++)
                {
                    var i = x * matrix.Height + y;

                    positions[i] = new(x, -y);
                    tiles[i] = GeoTiles[matrix.columns[x].cells[y].layers[z].geoType];
                }
            }

            GeoLayers[z].SetTiles(positions, tiles);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
