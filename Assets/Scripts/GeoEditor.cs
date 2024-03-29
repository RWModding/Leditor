using SimpleFileBrowser;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Tilemaps;
using static FeatureType;
using static StaticData;
using Debug = UnityEngine.Debug;
using Lingo;
using static Lingo.Inits;

public class GeoEditor : MonoBehaviour, IGridEditor
{
    private readonly List<Tilemap> GeoLayers = new();
    private readonly List<Tilemap> TileLayers = new();
    private readonly List<GameObject> FeatureLayerObjs = new();
    private readonly Color[] LayerColors = new Color[3] { new Color(0, 0, 0, 0.5f), new Color(0, 1, 0, 0.333f), new Color(1, 0, 0, 0.333f) };

    //TILE EDITOR STUFF
    //private List<TileCategory> TileCats;
    //private List<MaterialCategory> materialCats;

    //-- Layer, X, Y
    private readonly Dictionary<(int, int, int), List<FeatureBundle>> Features = new();

    //-- X, Y
    private readonly Dictionary<(int, int), List<FeatureBundle>> SpecialFeatures = new();

    //-- Layer, X, Y
    private readonly Dictionary<(int, int, int), TileBundle> Tiles = new();

    public LevelMatrix CurrentLevelMatrix;
    public EditorFile LoadedFile;

    public GameObject RootObj;
    private GameObject SpecialFeaturesObj;
    private new Camera camera;

    private void Awake()
    {
        camera = Camera.main;
        RootObj = gameObject.scene.GetRootGameObjects().First();

        GeoLayers.AddRange(GetComponentsInChildren<Tilemap>().Where(x => x.CompareTag(Tags.GeoLayer)));
        for (var i = 0; i < GeoLayers.Count; i++)
        {
            GeoLayers[i].color = LayerColors[i];
        }

        TileLayers.AddRange(GetComponentsInChildren<Tilemap>().Where(x => x.CompareTag(Tags.TileLayer)));

        for (var i = 1; i <= 3; i++)
        {
            FeatureLayerObjs.Add(transform.Find("FeaturesLayer" + i).gameObject);
        }
        SpecialFeaturesObj = transform.Find("SpecialFeatures").gameObject;

        // TILE EDITOR STUFF

        //LoadTilesFromInit(tileInit); //really this should be loaded only once when the program is started.
        //LoadMaterials();
    }

    #region Tile Loading

    /*public void LoadMaterials()
    {
        materialCats = new List<MaterialCategory>();
        var matpath = new DirectoryInfo(Application.streamingAssetsPath + "/Materials/");
        var materialFiles = matpath.GetFiles();
        for(int i = 0; i < materialFiles.Length; i++)
        {
            if (materialFiles[i].Extension != ".csv")
            {
                continue;
            }
            var material = File.ReadAllText(materialFiles[i].FullName);
            string[] allmats = material.Split("\n");
            MaterialCategory current = new MaterialCategory(materialFiles[i].Name);
            foreach (string mat in allmats)
            {
                string[] splitmat = mat.Split(',');
                Mat loadedMat = new(splitmat[0], new Color(int.Parse(splitmat[2]), int.Parse(splitmat[3]), int.Parse(splitmat[4])), splitmat[1]);
                current.materials.Add(loadedMat);
            }
            materialCats.Add(current);

        }
    }*/

    #endregion

    void Start()
    {
        LoadedFile = EditorManager.FileToLoad;
        //EditorManager.FileToLoad = null; //there's got to be a better way than this, right?

        CurrentLevelMatrix = LoadedFile.Geometry;

        LoadLevel();
        EditorManager.Instance.OnEditorLoaded(this);
    }

    #region Level loading
    private void LoadLevel()
    {
        var matrix = CurrentLevelMatrix;

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
                        var featureObj = Instantiate(FeaturePrefab, new Vector3(x, -y, isSpecial ? -1 : 0), Quaternion.identity, isSpecial ? SpecialFeaturesObj.transform : FeatureLayerObjs[z].transform);
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
    }
    #endregion

    void Update()
    {
        //-- TODO: Placeholder... should be replaced with something proper
        if (!FileBrowser.IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                SelectedLayer++;
                if (SelectedLayer > 2)
                {
                    SelectedLayer = 0;
                }
            }
        }
    }

    public int SelectedLayer { get; set; }

    public bool TryPlace<T>(int obj, Vector3Int pos, bool alwaysPlace = false) where T : Enum
    {
        if (pos.x < 0 || pos.x > CurrentLevelMatrix.Width - 1 || pos.y > 0 || pos.y < -CurrentLevelMatrix.Height + 1)
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
                    GeoLayers[SelectedLayer].SetTile(pos, GeoTiles[GeoType.BLSlope]);
                    SetTile(SelectedLayer, pos, GeoType.BLSlope);
                    return true;
                }
                else if (down == GeoType.Solid && right == GeoType.Solid && up != GeoType.Solid && left != GeoType.Solid)
                {
                    GeoLayers[SelectedLayer].SetTile(pos, GeoTiles[GeoType.BRSlope]);
                    SetTile(SelectedLayer, pos, GeoType.BRSlope);
                    return true;
                }
                else if (up == GeoType.Solid && left == GeoType.Solid && down != GeoType.Solid && right != GeoType.Solid)
                {
                    GeoLayers[SelectedLayer].SetTile(pos, GeoTiles[GeoType.TLSlope]);
                    SetTile(SelectedLayer, pos, GeoType.TLSlope);
                    return true;
                }
                else if (up == GeoType.Solid && right == GeoType.Solid && down != GeoType.Solid && left != GeoType.Solid)
                {
                    GeoLayers[SelectedLayer].SetTile(pos, GeoTiles[GeoType.TRSlope]);
                    SetTile(SelectedLayer, pos, GeoType.TRSlope);
                    return true;
                }

                return false;
            }

            SetTile(SelectedLayer, pos, (GeoType)obj);
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
                AddFeature(isSpecial ? 0 : SelectedLayer, pos, feature);
            }
            else if (!alwaysPlace)
            {
                RemoveFeature(isSpecial ? 0 : SelectedLayer, pos, feature);
            }

            return true;
        } else if (typeof(T) == typeof(LTile))
        {
            throw new ArgumentException("Tiles Not Implemented yet.");
        } /*else if(typeof(T) == typeof(Mat))
        {
            throw new ArgumentException("Materials Not Implemented yet.");
        }*/
        else
        {
            throw new ArgumentException("obj should be of type GeoType or FeatureType");
        }
    }

    private void SetTile(int layer, Vector3Int pos, GeoType type)
    {
        Operation.NewAction(Operation.ActionType.SetGeoType, layer, new Vector3Int(pos.x, pos.y, pos.z), (int)CurrentLevelMatrix.columns[pos.x].cells[-pos.y].layers[layer].geoType, (int)type);
    }

    private void AddFeature(int layer, Vector3Int pos, FeatureType featureType)
    {
        Operation.NewAction(Operation.ActionType.AddFeature, layer, new Vector3Int(pos.x, pos.y, pos.z), (int)featureType, (int)featureType);
    }

    private void RemoveFeature(int layer, Vector3Int pos, FeatureType featureType)
    {
        Operation.NewAction(Operation.ActionType.RemoveFeature, layer, new Vector3Int(pos.x, pos.y, pos.z), (int)featureType, (int)featureType);
    }

    public void CommitOperation(Operation.Bundle bundle)
    {
        foreach (var action in bundle.Actions)
        {
            var pos = action.Position;
            var layer = action.Layer;

            if (action.Type == Operation.ActionType.SetGeoType) {
                var geoType = (GeoType)action.NewValue;

                GeoLayers[action.Layer].SetTile(pos, GeoTiles[geoType]);
                CurrentLevelMatrix.columns[pos.x].cells[-pos.y].layers[layer].geoType = geoType;
            }
            else if (action.Type == Operation.ActionType.AddFeature || action.Type == Operation.ActionType.RemoveFeature) {
                var feature = (FeatureType)action.NewValue;
                var isSpecial = LevelMatrix.IsFeatureSpecial(feature);

                var featureList = isSpecial ? SpecialFeatures[(pos.x, -pos.y)] : Features[(action.Layer, pos.x, -pos.y)];
                var sameFeature = featureList.FirstOrDefault(x => x.feature == feature);

                if (action.Type == Operation.ActionType.AddFeature)
                {
                    if (sameFeature == null)
                    {
                        var featureObj = Instantiate(FeaturePrefab, new Vector3(pos.x, pos.y, isSpecial ? -1 : 0), Quaternion.identity, isSpecial ? SpecialFeaturesObj.transform : FeatureLayerObjs[action.Layer].transform);
                        var featureSprite = featureObj.GetComponent<SpriteRenderer>();
                        featureSprite.sprite = FeatureSprites[feature];
                        featureSprite.color = isSpecial ? Color.white : LayerColors[action.Layer];
                        featureObj.name = $"FL{action.Layer}_{pos.x}_{-pos.y}";

                        featureList.Add(new(feature, featureObj));
                    }
                    CurrentLevelMatrix.columns[pos.x].cells[-pos.y].layers[isSpecial ? 1 : layer].AddFeature(feature);
                }
                else if (action.Type == Operation.ActionType.RemoveFeature)
                {
                    if (sameFeature != null)
                    {
                        Destroy(sameFeature.gameObject);
                        featureList.Remove(sameFeature);
                    }
                    CurrentLevelMatrix.columns[pos.x].cells[-pos.y].layers[isSpecial ? 1 : layer].RemoveFeature(feature);
                }
            }
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

    public bool CheckPosInBounds(Vector2 pos)
    {
        return pos.x >= 0 && pos.y <= 0 && pos.x < CurrentLevelMatrix.Width && pos.y > -CurrentLevelMatrix.Height;
    }

    public Vector2 ClampPosToBounds(Vector2 pos)
    {
        return new Vector2(Mathf.Clamp(pos.x, 0, CurrentLevelMatrix.Width - 0.0001f), Mathf.Clamp(pos.y, -CurrentLevelMatrix.Height + 0.0001f, 0));
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

    public class TileBundle
    {
        public readonly LTile tile;
        public readonly GameObject gameObject;

        public TileBundle(LTile tile, GameObject gameObject)
        {
            this.tile = tile;
            this.gameObject = gameObject;
        }
    }
}
