using Lingo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using static FeatureType;
using static Lingo.Inits;

public static class StaticData
{
    public static readonly Dictionary<GeoType, Tile> GeoTiles = new();
    public static readonly Dictionary<Tile, GeoType> GeoForTile = new();
    public static readonly Dictionary<FeatureType, Sprite> FeatureSprites = new();

    public static GameObject FeaturePrefab;

    public static Dictionary<Category, List<LTile>> TileCategories;
    public static Dictionary<Category, List<Prop>> PropCategories;

    public static void Init()
    {
        #region Geometry
        FeaturePrefab = Resources.Load<GameObject>("geo/feature");

        GeoTiles[GeoType.Air] = null;
        GeoTiles[GeoType.Solid] = (Tile)Resources.Load("geo/palette/solid");
        GeoTiles[GeoType.BLSlope] = (Tile)Resources.Load("geo/palette/blslope");
        GeoTiles[GeoType.BRSlope] = (Tile)Resources.Load("geo/palette/brslope");
        GeoTiles[GeoType.TLSlope] = (Tile)Resources.Load("geo/palette/tlslope");
        GeoTiles[GeoType.TRSlope] = (Tile)Resources.Load("geo/palette/trslope");
        GeoTiles[GeoType.Platform] = (Tile)Resources.Load("geo/palette/platform");
        GeoTiles[GeoType.Entrance] = (Tile)Resources.Load("geo/palette/entrance");
        GeoTiles[GeoType.GlassWall] = (Tile)Resources.Load("geo/palette/glasswall");

        foreach (KeyValuePair<GeoType, Tile> kvp in GeoTiles)
        {
            if (kvp.Value != null)
            {
                GeoForTile[kvp.Value] = kvp.Key;
            }
        }

        var sprites = Resources.LoadAll<Sprite>("geo/texture");

        foreach (FeatureType feature in Enum.GetValues(typeof(FeatureType)))
        {
            var spriteName = feature switch
            {
                horbeam => "horbeam",
                vertbeam => "vertbeam",
                hive => "hive",
                shortcutentrance => "shortcutentrance_invalid",
                shortcutdot => "shortcutdot",
                entrance => "entrance",
                dragonDen => "dragonden",
                rock => "rock",
                spear => "spear",
                crack => "crack",
                forbidBats => "forbidbats",
                garbageHole => "garbagehole",
                waterfall => "waterfall",
                WHAMH => "whamh",
                wormGrass => "wormgrass",
                scavengerHole => "scavengerhole",
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
        #endregion

        string[] tileInit = File.ReadAllLines(Path.Combine(Application.streamingAssetsPath, "Tiles", "init.txt"));
        Inits.LoadTilesInit(tileInit);
    }
}
