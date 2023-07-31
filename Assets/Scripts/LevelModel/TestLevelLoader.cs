using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using LevelModel;
using System;
using System.Linq;
using Unity.VisualScripting;

public class TestLevelLoader : MonoBehaviour
{
    public string path;

    // Start is called before the first frame update
    void Start()
    {
        var levelText = File.ReadAllText(path);

        var level = new LevelData(
            saved: levelText,
            tileDatabase: FindAnyObjectByType<TileDatabase>(),
            materialDatabase: FindAnyObjectByType<MaterialDatabase>(),
            propDatabase: FindAnyObjectByType<PropDatabase>(),
            effectDatabase: FindAnyObjectByType<EffectDatabase>()
        );

        Texture2D[] layers = new Texture2D[3];

        int sortingLayer = 0;

        // Geometry
        for (int z = 2; z >= 0; z--)
        {
            // Geo
            var tex = layers[z] = new Texture2D(level.Width, level.Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            Color32[] data = new Color32[level.Width * level.Height];

            float mul = 1f - 0.2f * z;

            int i = 0;
            for (int y = level.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    var c = new Color32(0, 0, 0, 255);

                    if (level.GetVisualCell(new Vector2Int(x, y), z) is TileMaterial mat)
                    {
                        c.r = (byte)(mat.Color.r * 255f);
                        c.g = (byte)(mat.Color.g * 255f);
                        c.b = (byte)(mat.Color.b * 255f);
                    }

                    c.r = (byte)(c.r * mul + 127f * (1f - mul));
                    c.g = (byte)(c.g * mul + 127f * (1f - mul));
                    c.b = (byte)(c.b * mul + 127f * (1f - mul));
                    data[i++] = level.GetGeoCell(new(x, y), z).terrain != LevelModel.GeoType.Air ? c : new Color32(0, 0, 0, 0);
                }
            }

            tex.SetPixelData(data, 0);
            tex.Apply();

            var layerGo = new GameObject($"Layer {z + 1}");
            var layerSpr = layerGo.AddComponent<SpriteRenderer>();
            layerSpr.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0f, 1f), 1f);
            layerSpr.sortingOrder = sortingLayer++;
            
            layerGo.transform.parent = transform;
            layerGo.transform.localPosition = new Vector3(0f, 0f, 0f);

            // Tiles
            for (int y = 0; y < level.Height; y++)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    if (level.GetVisualCell(new(x, y), z) is TileInstance tileInst)
                    {
                        var relPos = new Vector2Int(x, y) - tileInst.TopLeft;

                        if (relPos.x < 0 || relPos.y < 0 || relPos.x >= tileInst.Tile.IconSprites.GetLength(0) || relPos.y >= tileInst.Tile.IconSprites.GetLength(1))
                            continue;
                        
                        var go = new GameObject($"{tileInst.Tile.Name} ({x}, {y}, {z})");
                        var spr = go.AddComponent<SpriteRenderer>();

                        spr.sprite = tileInst.Tile.IconSprites[relPos.x, relPos.y];
                        spr.color = tileInst.Tile.Category.Color;
                        spr.sortingOrder = sortingLayer;

                        go.transform.parent = layerGo.transform;
                        go.transform.localPosition = new Vector3(x, -y, 0f);
                    }
                }
            }
            sortingLayer++;
        }

        // Effects
        {
            var effectsGroup = new GameObject("Effects");
            effectsGroup.transform.parent = transform;

            int effectIndex = 0;
            foreach (var inst in level.Effects)
            {
                var color = Color.HSVToRGB(effectIndex / (float)level.Effects.Count, 1f, UnityEngine.Random.value * 0.5f + 0.25f);

                var tex = new Texture2D(level.Width, level.Height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point
                };

                for (int y = 0; y < level.Height; y++)
                {
                    for (int x = 0; x < level.Width; x++)
                    {
                        tex.SetPixel(x, level.Height - y - 1, new Color(color.r, color.g, color.b, inst.GetAmount(new(x, y))));
                    }
                }

                tex.Apply();

                var go = new GameObject($"{inst.Effect.Name} {effectIndex}");
                var layerSpr = go.AddComponent<SpriteRenderer>();
                layerSpr.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0f, 1f), 1f);
                layerSpr.color = new Color(1f, 1f, 1f, 0.4f);
                layerSpr.sortingOrder = sortingLayer++;

                go.transform.parent = effectsGroup.transform;
                go.transform.localPosition = new Vector3(0f, 0f, 0f);

                effectIndex++;
            }
        }

        // Cameras
        int camIndex = 0;
        foreach(var cam in level.Cameras)
        {
            var go = new GameObject($"Camera {camIndex}");
            var ren = go.AddComponent<MeshRenderer>();
            var fil = go.AddComponent<MeshFilter>();

            ren.sortingOrder = sortingLayer;
            ren.sharedMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(0.25f, 0.25f, 1f, 1f)
            };

            var scl = new Vector2(1f, -1f) / 20f;
            var mesh = new Mesh();
            mesh.SetVertices(new Vector3[] {
                cam.GetStretchedCorner(0, 27) * scl,
                cam.GetStretchedCorner(1, 27) * scl,
                cam.GetStretchedCorner(2, 27) * scl,
                cam.GetStretchedCorner(3, 27) * scl
            });
            mesh.SetIndices(new int[] { 0, 1, 2, 3, 0 }, MeshTopology.LineStrip, 0);
            fil.sharedMesh = mesh;

            go.transform.parent = transform;
            go.transform.localPosition = new Vector3(0f, 0f, 0f);

            camIndex++;
        }
        sortingLayer++;

        // Buffer tiles
        {
            var go = new GameObject($"Buffer Tiles");
            var ren = go.AddComponent<MeshRenderer>();
            var fil = go.AddComponent<MeshFilter>();

            ren.sortingOrder = sortingLayer++;
            ren.sharedMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(1f, 1f, 1f, 1f)
            };

            var mesh = new Mesh();
            mesh.SetVertices(new Vector3[] {
                new Vector2(level.BufferTilesLeft, -level.BufferTilesTop),
                new Vector2(level.Width - 1 - level.BufferTilesRight, -level.BufferTilesTop),
                new Vector2(level.Width - 1 - level.BufferTilesRight, -level.Height + 1 + level.BufferTilesBottom),
                new Vector2(level.BufferTilesLeft, -level.Height + 1 + level.BufferTilesBottom),
            });
            mesh.SetIndices(new int[] { 0, 1, 2, 3, 0 }, MeshTopology.LineStrip, 0);
            fil.sharedMesh = mesh;

            go.transform.parent = transform;
            go.transform.localPosition = new Vector3(0f, 0f, 0f);
        }

        // Props
        {
            var propsGroup = new GameObject("Props");
            propsGroup.transform.parent = transform;

            foreach (var propInstance in level.Props)
            {
                var spr = propInstance.Prop.GetPreviewSprite(propInstance.Variation);

                var go = new GameObject($"{propInstance.Prop.Name}");
                var ren = go.AddComponent<MeshRenderer>();
                var fil = go.AddComponent<MeshFilter>();

                ren.sortingOrder = sortingLayer;
                ren.sharedMaterial = new Material(Shader.Find("Sprites/Default"))
                {
                    color = new Color(1f, 1f, 1f, 1f),
                    mainTexture = spr.texture
                };

                Vector2[] uv = spr.uv;
                Vector3[] verts = new Vector3[uv.Length];
                Vector2[] quad = propInstance.Quad;

                for(int i = 0; i < uv.Length; i++)
                {
                    var t = Rect.PointToNormalized(spr.rect, uv[i] * spr.texture.Size());

                    verts[i] = Vector2.Lerp(Vector2.Lerp(quad[3], quad[2], t.x), Vector2.Lerp(quad[0], quad[1], t.x), t.y)
                        / new Vector2(20f, -20f);
                }

                var mesh = new Mesh();
                mesh.SetVertices(verts);
                mesh.SetUVs(0, uv);
                mesh.SetIndices(spr.triangles, MeshTopology.Triangles, 0);
                fil.sharedMesh = mesh;

                go.transform.parent = propsGroup.transform;
                go.transform.localPosition = new Vector3(0f, 0f, 0f);
            }
            sortingLayer++;

            foreach(var propInstance in level.Props)
            {
                // Rope and long prop lines
                Vector2[] points;
                Color color;

                switch(propInstance)
                {
                    case LongPropInstance lp:
                        points = new Vector2[] { lp.Start, lp.End };
                        color = Color.red;
                        break;

                    case RopePropInstance rp:
                        points = rp.Points.ToArray();
                        color = rp.Prop.RopeParams.PreviewColor;
                        break;

                    default:
                        continue;
                }

                var go = new GameObject($"{propInstance.Prop.Name} Line");
                var ren = go.AddComponent<MeshRenderer>();
                var fil = go.AddComponent<MeshFilter>();

                ren.sortingOrder = sortingLayer;
                ren.sharedMaterial = new Material(Shader.Find("Sprites/Default"))
                {
                    color = color,
                };

                var mesh = new Mesh();
                mesh.SetVertices(points.Select(v => new Vector3(v.x, -v.y) / 20f).ToArray());
                mesh.SetIndices(Enumerable.Range(0, points.Length).ToArray(), MeshTopology.LineStrip, 0);
                fil.sharedMesh = mesh;

                go.transform.parent = propsGroup.transform;
                go.transform.localPosition = new Vector3(0f, 0f, 0f);
            }
        }

        // Misc settings
        Debug.Log(string.Join(", ", level.DefaultMaterial.Name, level.LightAngle, level.LightDistance, level.SunlightEnabled, level.TileSeed, level.WaterInFrontOfTerrain, level.WaterLevel));
    }
}
