using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LevelModel;
using System.Linq;

public class GeoView : MonoBehaviour
{
    public Color[] LayerColors = new Color[3];
    public Material PreviewMaterial;

    private LevelEditor editor;

    void Awake()
    {
        editor = GetComponentInParent<LevelEditor>();
    }

    public void OnLevelLoaded()
    {
        Refresh();
    }

    private void Clear()
    {
        foreach(Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void Refresh()
    {
        Clear();

        var level = editor.LevelData;

        for (int layer = 0; layer < 3; layer++)
        {
            var chunk = MakeChunk(0, 0, level.Width, level.Height, layer);

            chunk.transform.parent = transform;
            chunk.transform.localPosition = new Vector3(0f, 0f, layer);
            chunk.GetComponent<MeshRenderer>().material.color = LayerColors[layer];
        }
    }

    private GameObject MakeChunk(int startX, int startY, int w, int h, int layer)
    {
        var level = editor.LevelData;
        var obj = new GameObject($"Chunk ({startX}, {startY}, {layer})");
        var renderer = obj.AddComponent<MeshRenderer>();
        var filter = obj.AddComponent<MeshFilter>();
        var mesh = new Mesh();
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        var hit = new bool[w * h];

        renderer.material = PreviewMaterial;
        renderer.material.mainTexture = GetGeoTexture(level, startX, startY, w, h, layer);

        for(int y = startY; y < startY + h; y++)
        {
            for(int x = startX; x < startX + w; x++)
            {
                if (hit[x - startX + (y - startY) * w]) continue;

                switch(level.GetGeoCell(new(x, y), layer).terrain)
                {
                    case GeoType.Solid:
                        // Find line of terrain rightwards
                        int endX = x;
                        while (endX < startX + w
                            && level.GetGeoCell(new(endX, y), layer).terrain == GeoType.Solid
                            && !hit[endX - startX + (y - startY) * w])
                        {
                            endX++;
                        }

                        // Find rectangle of terrain downwards
                        int endY = y;
                        while (endY < startY + h)
                        {
                            bool rowSolid = true;
                            for(int testX = x; testX < endX; testX++)
                            {
                                if (level.GetGeoCell(new(testX, endY), layer).terrain != GeoType.Solid
                                    || hit[testX - startX + (endY - startY) * w])
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
                                hit[hitX - startX + (hitY - startY) * w] = true;
                            }
                        }

                        AddQuad(vertices, indices, x, y, endX - x, endY - y);
                        break;

                    case GeoType.Platform:
                        // Find line of platforms rightwards
                        endX = x;
                        while (endX < startX + w
                            && level.GetGeoCell(new(endX, y), layer).terrain == GeoType.Platform
                            && !hit[endX - startX + (y - startY) * w])
                        {
                            endX++;
                        }

                        // Hit all
                        for (int hitX = x; hitX < endX; hitX++)
                        {
                            hit[hitX - startX + (y - startY) * w] = true;
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

                hit[(x - startX) + (y - startY) * w] = true;
            }
        }

        var uvs = new Vector2[vertices.Count];
        var rect = new Rect(startX, -startY - h, w, h);
        for(int i = 0; i < vertices.Count; i++)
        {
            uvs[i] = Rect.PointToNormalized(rect, vertices[i]);
        }

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        filter.sharedMesh = mesh;

        return obj;
    }

    private static Texture2D GetGeoTexture(LevelData level, int x, int y, int w, int h, int layer)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false, true)
        {
            filterMode = FilterMode.Point
        };
        var data = new Color32[w * h];

        var mat = new Color32(255, 255, 255, 255);
        var tile = new Color32(255, 220, 220, 255);

        int i = 0;
        for(int ty = y + h - 1; ty >= y; ty--)
        {
            for(int tx = x; tx < x + w; tx++)
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
