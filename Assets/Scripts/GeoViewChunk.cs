using LevelModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GeoViewChunk : MonoBehaviour
{
    public RectInt LevelRect;
    public int Layer;
    public LevelData Level;
    public Material GeoMaterial;

    private Mesh _geoMesh;
    private Dictionary<TileInstance, SpriteRenderer> _tiles = new();
    private GameObject _geo;

    public void Awake()
    {
        if (Level == null)
        {
            var loader = GetComponentInParent<LevelLoader>();
            if (loader != null)
                Level = loader.LevelData;
        }

        // Setup geometry mesh
        _geoMesh = new Mesh();

        _geo = new GameObject("Geometry", typeof(MeshRenderer), typeof(MeshFilter));
        _geo.transform.parent = transform;
        _geo.GetComponent<MeshFilter>().sharedMesh = _geoMesh;

        var renderer = _geo.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = GeoMaterial;
    }

    public void OnLevelViewRefreshed(RectInt rect)
    {
        if(rect.Overlaps(LevelRect))
            Refresh();
    }

    public void Refresh()
    {
        if (Level == null) throw new InvalidOperationException("Level is null!");

        gameObject.layer = LayerMask.NameToLayer($"Layer{Layer + 1}");
        RefreshGeo();
        RefreshTiles();
    }

    private void RefreshGeo()
    {
        int xMin = LevelRect.xMin;
        int yMin = LevelRect.yMin;
        int xMax = LevelRect.xMax;
        int yMax = LevelRect.yMax;
        int w = xMax - xMin;
        int h = yMax - yMin;
        int layer = Layer;
        var level = Level;
        var mesh = _geoMesh;

        var vertices = new List<Vector3>();
        var indices = new List<int>();
        var hit = new bool[w * h];

        var geoRenderer = _geo.GetComponent<Renderer>();
        geoRenderer.material = GeoMaterial;
        geoRenderer.material.mainTexture = GetGeoTexture(level, xMin, yMin, w, h, layer);
        _geo.layer = gameObject.layer;

        // Add solid tiles, slopes, and floors
        for (int y = yMin; y < yMin + h; y++)
        {
            for (int x = xMin; x < xMin + w; x++)
            {
                if (hit[x - xMin + (y - yMin) * w]) continue;

                switch (level.GetGeoCell(new(x, y), layer).terrain)
                {
                    case GeoType.Solid:
                        // Find line of terrain rightwards
                        int endX = x;
                        while (endX < xMin + w
                            && level.GetGeoCell(new(endX, y), layer).terrain == GeoType.Solid
                            && !hit[endX - xMin + (y - yMin) * w])
                        {
                            endX++;
                        }

                        // Find rectangle of terrain downwards
                        int endY = y;
                        while (endY < yMin + h)
                        {
                            bool rowSolid = true;
                            for (int testX = x; testX < endX; testX++)
                            {
                                if (level.GetGeoCell(new(testX, endY), layer).terrain != GeoType.Solid
                                    || hit[testX - xMin + (endY - yMin) * w])
                                {
                                    rowSolid = false;
                                    break;
                                }
                            }
                            if (!rowSolid) break;
                            endY++;
                        }

                        // Hit all
                        for (int hitY = y; hitY < endY; hitY++)
                        {
                            for (int hitX = x; hitX < endX; hitX++)
                            {
                                hit[hitX - xMin + (hitY - yMin) * w] = true;
                            }
                        }

                        AddQuad(vertices, indices, x, y, endX - x, endY - y);
                        break;

                    case GeoType.Platform:
                        // Find line of platforms rightwards
                        endX = x;
                        while (endX < xMin + w
                            && level.GetGeoCell(new(endX, y), layer).terrain == GeoType.Platform
                            && !hit[endX - xMin + (y - yMin) * w])
                        {
                            endX++;
                        }

                        // Hit all
                        for (int hitX = x; hitX < endX; hitX++)
                        {
                            hit[hitX - xMin + (y - yMin) * w] = true;
                        }

                        AddQuad(vertices, indices, x, y, endX - x, 0.5f);
                        break;

                    case GeoType.BLSlope:
                        AddTri(vertices, indices, new Vector2(x, y), new Vector2(x + 1, y + 1), new Vector2(x, y + 1));
                        break;

                    case GeoType.BRSlope:
                        AddTri(vertices, indices, new Vector2(x + 1, y), new Vector2(x + 1, y + 1), new Vector2(x, y + 1));
                        break;

                    case GeoType.TLSlope:
                        AddTri(vertices, indices, new Vector2(x, y), new Vector2(x + 1, y), new Vector2(x, y + 1));
                        break;

                    case GeoType.TRSlope:
                        AddTri(vertices, indices, new Vector2(x, y), new Vector2(x + 1, y), new Vector2(x + 1, y + 1));
                        break;
                }

                hit[(x - xMin) + (y - yMin) * w] = true;
            }
        }

        // Add horizontal poles
        for (int y = yMin; y < yMin + h; y++)
        {
            for (int x = xMin; x < xMin + w; x++)
            {
                if ((level.GetGeoCellFeatures(new(x, y), layer) & FeatureFlags.HorizontalBeam) != 0)
                {
                    int poleStartX = x;
                    while ((level.GetGeoCellFeatures(new(x, y), layer) & FeatureFlags.HorizontalBeam) != 0)
                        x++;

                    AddQuad(vertices, indices, poleStartX, y + 0.4f, x - poleStartX, 0.2f);
                }
            }
        }

        // Add vertical poles
        for (int x = xMin; x < xMin + w; x++)
        {
            for (int y = yMin; y < yMin + h; y++)
            {
                if ((level.GetGeoCellFeatures(new(x, y), layer) & FeatureFlags.VerticalBeam) != 0)
                {
                    int poleStartY = y;
                    while ((level.GetGeoCellFeatures(new(x, y), layer) & FeatureFlags.VerticalBeam) != 0)
                        y++;

                    AddQuad(vertices, indices, x + 0.4f, poleStartY, 0.2f, y - poleStartY);
                }
            }
        }

        var uvs = new Vector2[vertices.Count];
        var rect = new Rect(xMin, -yMin - h, w, h);
        for (int i = 0; i < vertices.Count; i++)
        {
            uvs[i] = Rect.PointToNormalized(rect, vertices[i]);
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
    }

    private void RefreshTiles()
    {
        int xMin = LevelRect.xMin;
        int yMin = LevelRect.yMin;
        int xMax = LevelRect.xMax;
        int yMax = LevelRect.yMax;
        int layer = Layer;
        var level = Level;

        // Gather a list of all tiles that used to be in the chunk
        var oldTiles = _tiles.Keys.ToHashSet();

        // Add or update sprites for the current tiles
        foreach (var inst in level.Tiles)
        {
            if (inst.HeadLayer == layer
                && inst.HeadPos.x >= xMin && inst.HeadPos.y >= yMin
                && inst.HeadPos.x < xMax && inst.HeadPos.y < yMax)
            {
                if (!_tiles.TryGetValue(inst, out SpriteRenderer spriteRenderer) || !spriteRenderer)
                {
                    var tileObj = new GameObject(inst.Tile.Name);
                    tileObj.transform.parent = transform;
                    tileObj.layer = gameObject.layer;
                    spriteRenderer = tileObj.AddComponent<SpriteRenderer>();
                    _tiles[inst] = spriteRenderer;
                }

                spriteRenderer.transform.localPosition = new Vector3(inst.TopLeft.x, -inst.TopLeft.y, -0.1f);
                spriteRenderer.sprite = inst.Tile.GetRenderSprite(layer - inst.HeadLayer);

                oldTiles.Remove(inst);
            }
        }

        // Remove tiles that don't exist anymore
        foreach (var inst in oldTiles)
        {
            if (_tiles.TryGetValue(inst, out var spriteRenderer))
            {
                Destroy(spriteRenderer.gameObject);
                _tiles.Remove(inst);
            }
        }
    }

    private static Texture2D GetGeoTexture(LevelData level, int x, int y, int w, int h, int layer)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false, true)
        {
            filterMode = FilterMode.Point
        };
        var data = new Color32[w * h];

        var mat = new Color32(0, 255, 0, 255);
        var tile = new Color32(0, 255, 0, 0);

        int i = 0;
        for (int ty = y + h - 1; ty >= y; ty--)
        {
            for (int tx = x; tx < x + w; tx++)
            {
                data[i++] = level.GetVisualCell(new(tx, ty), layer) is TileInstance ? tile : mat;
            }
        }

        tex.SetPixelData(data, 0);
        tex.Apply(false, true);
        return tex;
    }

    private static void AddQuad(List<Vector3> verts, List<int> inds, float x, float y, float w, float h)
    {
        y = -y - h;

        int baseInd = verts.Count;
        verts.Add(new Vector3(x, y));
        verts.Add(new Vector3(x, y + h));
        verts.Add(new Vector3(x + w, y));
        verts.Add(new Vector3(x + w, y + h));
        inds.Add(baseInd + 0);
        inds.Add(baseInd + 1);
        inds.Add(baseInd + 2);
        inds.Add(baseInd + 2);
        inds.Add(baseInd + 1);
        inds.Add(baseInd + 3);
    }

    private static void AddTri(List<Vector3> verts, List<int> inds, Vector2 a, Vector2 b, Vector2 c)
    {
        int baseInd = verts.Count;
        verts.Add(new Vector3(a.x, -a.y));
        verts.Add(new Vector3(b.x, -b.y));
        verts.Add(new Vector3(c.x, -c.y));
        inds.Add(baseInd + 0);
        inds.Add(baseInd + 1);
        inds.Add(baseInd + 2);
    }
}
