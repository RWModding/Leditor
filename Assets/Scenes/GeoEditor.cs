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

public class GeoEditor : MonoBehaviour, IGridEditor
{
    private readonly Dictionary<GeoType, Tile> GeoTiles = new();
    private readonly Dictionary<Tile, GeoType> GeoForTile = new();
    private readonly Dictionary<FeatureType, Sprite> FeatureSprites = new();
    private readonly List<Tilemap> GeoLayers = new();
    private readonly List<GameObject> FeatureLayerObjs = new();
    private readonly Color[] LayerColors = new Color[3] { new Color(0, 0, 0, 0.5f), new Color(0, 1, 0, 0.333f), new Color(1, 0, 0, 0.333f) };

    //-- Layer, X, Y
    private readonly Dictionary<(int, int, int), List<FeatureBundle>> Features = new();

    //-- X, Y
    private readonly Dictionary<(int, int), List<FeatureBundle>> SpecialFeatures = new();

    private LevelMatrix CurrentLevelMatrix;

    private GameObject SpecialFeaturesObj;
    private GameObject FeaturePrefab;
    private GridLines gridLines;
    private new Camera camera;
    private CameraControls cameraControls;

    private void Awake()
    {
        camera = Camera.main;
        cameraControls = camera.gameObject.GetComponent<CameraControls>();

        GeoLayers.AddRange(GetComponentsInChildren<Tilemap>());
        for (var i = 0; i < GeoLayers.Count; i++)
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
            FeatureLayerObjs.Add(transform.Find("FeaturesLayer" + i).gameObject);
        }
        SpecialFeaturesObj = transform.Find("SpecialFeatures").gameObject;

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

        foreach (KeyValuePair<GeoType, Tile> kvp in GeoTiles)
        {
            if (kvp.Value != null)
            {
                GeoForTile[kvp.Value] = kvp.Key;
            }
        }
    }

    private void LoadFeatureSprites()
    {
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
    }
    #endregion

    void Start()
    {
        //-- TODO: Placeholder... should be replaced when a proper manager is implemented
        EditorManager.Instance.CurrentEditor = this;

        CurrentLevelMatrix = new LevelMatrix(File.ReadAllLines("E:\\Rain World leditor\\LevelEditorProjects\\World\\SI\\SI_D05.txt")[0].Trim());

        LoadLevel(CurrentLevelMatrix);

        gridLines = camera.AddComponent<GridLines>();
        gridLines.start = new Vector2(transform.position.x, transform.position.y);
        gridLines.size = new Vector2(CurrentLevelMatrix.Width, CurrentLevelMatrix.Height);
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

                    Features.Add((z, x, y), new());
                    SpecialFeatures.TryAdd((x, y), new());

                    foreach (var feature in cellLayer.featureType)
                    {
                        var isSpecial = LevelMatrix.IsFeatureSpecial(feature);
                        var featureObj = Instantiate(FeaturePrefab, new Vector3(x, -y), Quaternion.identity, isSpecial ? SpecialFeaturesObj.transform : FeatureLayerObjs[z].transform);
                        var featureSprite = featureObj.GetComponent<SpriteRenderer>();
                        featureSprite.sprite = FeatureSprites[feature];
                        featureSprite.color = isSpecial ? Color.white : LayerColors[z];
                        featureObj.name = $"FL{z}_{x}_{y}";

                        if (isSpecial)
                        {
                            SpecialFeatures[(x, y)].Add(new (feature, featureObj));
                        }
                        else
                        {
                            Features[(z, x, y)].Add(new(feature, featureObj));
                        }
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
        //-- TODO: Placeholder... should be replaced with something proper
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SelectedLayer++;
            if (SelectedLayer > 2)
            {
                SelectedLayer = 0;
            }
        }
    }

    public int SelectedLayer { get; set; }

    public bool TryPlace<T>(int obj, Vector3Int pos) where T : Enum
    {
        if (pos.x < 0 || pos.x > CurrentLevelMatrix.Width || pos.y > 0 || pos.y < -CurrentLevelMatrix.Height)
        {
            return false;
        }

        if (typeof(T) == typeof(GeoType))
        {
            var layer = GeoLayers[SelectedLayer];
            if (IsSlope((GeoType)obj))
            {
                var up = GetGeoForTile(layer.GetTile<Tile>(new Vector3Int(pos.x, pos.y + 1)));
                var down = GetGeoForTile(layer.GetTile<Tile>(new Vector3Int(pos.x, pos.y - 1)));
                var left = GetGeoForTile(layer.GetTile<Tile>(new Vector3Int(pos.x - 1, pos.y)));
                var right = GetGeoForTile(layer.GetTile<Tile>(new Vector3Int(pos.x + 1, pos.y)));

                if (IsSlope(up) || IsSlope(down) || IsSlope(left) || IsSlope(right))
                {
                    return false;
                }

                if (down == GeoType.Solid && left == GeoType.Solid && up != GeoType.Solid && right != GeoType.Solid)
                {
                    layer.SetTile(pos, GeoTiles[GeoType.BLSlope]);
                    return true;
                }
                else if (down == GeoType.Solid && right == GeoType.Solid && up != GeoType.Solid && left != GeoType.Solid)
                {
                    layer.SetTile(pos, GeoTiles[GeoType.BRSlope]);
                    return true;
                }
                else if (up == GeoType.Solid && left == GeoType.Solid && down != GeoType.Solid && right != GeoType.Solid)
                {
                    layer.SetTile(pos, GeoTiles[GeoType.TLSlope]);
                    return true;
                }
                else if (up == GeoType.Solid && right == GeoType.Solid && down != GeoType.Solid && left != GeoType.Solid)
                {
                    layer.SetTile(pos, GeoTiles[GeoType.TRSlope]);
                    return true;
                }

                return false;
            }

            layer.SetTile(pos, GeoTiles[(GeoType)obj]);
            return true;
        }
        else if (typeof(T) == typeof(FeatureType))
        {
            var feature = (FeatureType)obj;
            var isSpecial = LevelMatrix.IsFeatureSpecial(feature);

            var featureList = isSpecial ? SpecialFeatures[(pos.x, -pos.y)] : Features[(SelectedLayer, pos.x, -pos.y)];
            var sameFeature = featureList.FirstOrDefault(x => x.feature == feature);
            if (sameFeature == null)
            {
                var featureObj = Instantiate(FeaturePrefab, new Vector3(pos.x, pos.y), Quaternion.identity, isSpecial ? SpecialFeaturesObj.transform : FeatureLayerObjs[SelectedLayer].transform);
                var featureSprite = featureObj.GetComponent<SpriteRenderer>();
                featureSprite.sprite = FeatureSprites[feature];
                featureSprite.color = isSpecial ? Color.white : LayerColors[SelectedLayer];
                featureObj.name = $"FL{SelectedLayer}_{pos.x}_{-pos.y}";

                featureList.Add(new(feature, featureObj));
            }
            else
            {
                Destroy(sameFeature.gameObject);
                featureList.Remove(sameFeature);
            }

            return true;
        }
        else
        {
            throw new ArgumentException("obj should be of type GeoType or FeatureType");
        }
    }

    public void Clear(Vector3Int pos)
    {
        throw new NotImplementedException();
    }

    public void ClearAll()
    {
        throw new NotImplementedException();
    }

    public GeoType GetGeoForTile(Tile tile)
    {
        if (tile == null)
        {
            return GeoType.Air;
        }
        return GeoForTile[tile];
    }

    public static bool IsSlope(GeoType geo)
    {
        return geo >= GeoType.BLSlope && geo <= GeoType.TRSlope;
    }

    public class FeatureBundle {
        public readonly FeatureType feature;
        public readonly GameObject gameObject;

        public FeatureBundle(FeatureType feature, GameObject gameObject)
        {
            this.feature = feature;
            this.gameObject = gameObject;
        }
    }
}