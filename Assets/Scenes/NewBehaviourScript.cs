using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using static FeatureType;

public class NewBehaviourScript : MonoBehaviour
{
    private readonly Dictionary<GeoType, Tile> GeoTiles = new();
    private readonly Dictionary<FeatureType, Sprite> FeatureSprites = new();
    private readonly List<Tilemap> GeoLayers = new();
    private readonly List<GameObject> FeatureLayers = new();
    private readonly Color[] LayerColors = new Color[3] { new Color(0, 0, 0, 0.5f), new Color(0, 1, 0, 0.333f), new Color(1, 0, 0, 0.333f) };

    private GameObject SpecialFeatures;
    private GameObject FeaturePrefab;
    private GridLines gridLines;
    private new Camera camera;
    private CameraControls cameraControls;

    private void Awake()
    {
        camera = Camera.main;
        cameraControls = camera.gameObject.GetComponent<CameraControls>();

        GeoLayers.AddRange(GetComponentsInChildren<Tilemap>());
        for (var i  = 0; i < GeoLayers.Count; i++)
        {
            GeoLayers[i].color = LayerColors[i];
        }

        LoadGeoTiles();
        LoadFeatureSprites();
        LoadFeatureObjects();
    }

    #region Asset loading
    private void LoadFeatureObjects()
    {
        for (var i = 1; i <= 3; i++)
        {
            FeatureLayers.Add(transform.Find("FeaturesLayer" + i).gameObject);
        }
        SpecialFeatures = transform.Find("SpecialFeatures").gameObject;

        FeaturePrefab = Resources.Load<GameObject>("geo/feature");
    }

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

    void Start()
    {
        var matrix = new LevelMatrix(File.ReadAllLines("E:\\Rain World leditor\\LevelEditorProjects\\World\\SI\\SI_D05.txt")[0].Trim());

        LoadLevel(matrix);

        gridLines = camera.AddComponent<GridLines>();
        gridLines.start = new Vector2(transform.position.x, transform.position.y);
        gridLines.size = new Vector2(matrix.Width, matrix.Height);
        gridLines.transformMatrix = transform.localToWorldMatrix;
    }

    private void OnDestroy()
    {
        Destroy(gridLines);
    }

    #region Level loading
    private void LoadLevel(LevelMatrix matrix)
    {
        for (var z = 0; z < GeoLayers.Count; z++)
        {
            var positions = new Vector3Int[matrix.Width * matrix.Height];
            var tiles = new Tile[positions.Length];

            for (var x = 0; x < matrix.Width; x++)
            {
                for (var y = 0; y < matrix.Height; y++)
                {
                    var i = x * matrix.Height + y;
                    var cellLayer = matrix.columns[x].cells[y].layers[z];

                    positions[i] = new(x, -y);
                    tiles[i] = GeoTiles[cellLayer.geoType];

                    foreach (var feature in cellLayer.featureType)
                    {
                        var isSpecial = feature != crack && feature != vertbeam && feature != horbeam && feature != hive;
                        var featureObj = Instantiate(FeaturePrefab, new Vector3(x, -y), Quaternion.identity, isSpecial ? SpecialFeatures.transform : FeatureLayers[z].transform);
                        var featureSprite = featureObj.GetComponent<SpriteRenderer>();
                        featureSprite.sprite = FeatureSprites[feature];
                        featureSprite.color = isSpecial ? Color.white : LayerColors[z];
                        featureObj.name = $"FL{z}_{x}_{y}";
                    }
                }
            }

            GeoLayers[z].SetTiles(positions, tiles);
        }

        cameraControls.SetContraints(new Vector2(-1, -(matrix.Height + 1)), new Vector2(matrix.Width + 1, 1));
    }
    #endregion

    void Update()
    {
        
    }
}
