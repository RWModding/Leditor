using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using LevelModel;
using System;

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
            propDatabase: FindAnyObjectByType<PropDatabase>()
        );

        Texture2D[] layers = new Texture2D[3];

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
                    data[i++] = level.GetGeoCell(new Vector2Int(x, y), z).terrain != LevelModel.GeoType.Air ? c : new Color32(0, 0, 0, 0);
                }
            }

            tex.SetPixelData(data, 0);
            tex.Apply();

            var layerGo = new GameObject($"Layer {z + 1}");
            var layerSpr = layerGo.AddComponent<SpriteRenderer>();
            layerSpr.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0f, 1f), 1f);
            layerSpr.sortingOrder = (2 - z) * 2;
            
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
                        spr.sortingOrder = (2 - z) * 2 + 1;

                        go.transform.parent = layerGo.transform;
                        go.transform.localPosition = new Vector3(x, -y, 0f);
                    }
                }
            }
        }
    }
}
