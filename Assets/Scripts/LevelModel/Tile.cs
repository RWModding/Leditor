using Lingo;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LevelModel
{
    // TODO:
    // Box type tiles complicate preview logic a lot
    // Generate equivalent voxel struct versions?

    /// <summary>
    /// Represents a type of tile, many of which may be placed in a level.
    /// </summary>
    public class Tile : IDisposable
    {
        public TileCategory Category { get; }
        public string Name { get; }
        public string ImageDir { get; }
        public Vector2Int Size { get; }
        public int Layers { get; }
        public List<string> Tags { get; }
        public List<string> Notes { get; }
        public int Variants { get; }

        private Texture2D renderTexture;
        private Texture2D previewTexture;
        private Sprite[] renderSprites;
        private Sprite previewSprite;
        private readonly Type type;
        private readonly int bufferTiles;
        private readonly int[] specs1;
        private readonly int[] specs2;
        private readonly int[] repeatL;

        /// <summary>
        /// The position of the top left corner of the tile relative to its head.
        /// </summary>
        public Vector2Int TopLeftOffset => (Vector2Int.one - Size) / 2;

        public Tile(string saved, TileCategory category, string imageDir)
        {
            var data = LingoParser.ParsePropertyList(saved);

            Category = category;
            Name = data.GetString("nm");
            ImageDir = imageDir;
            Size = Vector2Int.FloorToInt(data.GetVector2("sz"));
            Layers = data.GetLinearList("specs2") == null ? 1 : 2;
            type = Enum.Parse<Type>(data.GetString("tp"), true);
            bufferTiles = data.GetInt("bfTiles");
            specs1 = data.GetLinearList("specs").Cast<int>().ToArray();
            specs2 = data.GetLinearList("specs2")?.Cast<int>().ToArray();
            Tags = data.TryGetLinearList("tags", out var tags) ? tags.Cast<string>().ToList() : new List<string>();
            Notes = data.TryGetLinearList("notes", out var notes) ? notes.Cast<string>().ToList() : new List<string>();
            Variants = data.GetInt("rnd");

            //if (data.TryGetLinearList("repeatL", out var reps))
            if (type == Type.VoxelStructRockType)
                repeatL = new int[] { 10 };
            else if (type != Type.Box)
                repeatL = data.GetLinearList("repeatL").Cast<int>().ToArray();
        }


        private static readonly HashSet<string> propTags = new(StringComparer.OrdinalIgnoreCase) {
            "notMegaTrashProp",
            "effectColorA",
            "effectColorB",
            "colored",
            "customColor",
            "customColorRainbow",
            "randomRotat",
            "randomFlipX",
            "randomFlipY",
            "Circular Sign",
            "Circular Sign B",
            "Larger Sign",
            "Larger Sign B",
            "notTrashProp",
            "INTERNAL"
        };
        public Prop CreateProp(PropCategory category)
        {
            if (type != Type.VoxelStruct && type != Type.VoxelStructRandomDisplaceHorizontal && type != Type.VoxelStructRandomDisplaceVertical)
                return null;

            var data = new PropertyList();
            data.Set("nm", Name);
            data.Set("tp", Variants > 1 ? "variedStandard" : "standard");
            data.Set("colorTreatment", "standard");
            data.Set("sz", Size + bufferTiles * 2 * Vector2.one);
            data.Set("depth", specs2 == null ? 10 : 20);
            data.Set("repeatL", new LinearList(repeatL.Cast<object>()));

            if (Variants > 1)
            {
                data.Set("vars", Variants);
                data.Set("random", 1);
            }

            data.Set("tags", new LinearList(Tags.Where(propTags.Contains)));
            data.Set("layerExceptions", new LinearList());
            data.Set("notes", LinearList.Make("Tile as prop"));

            return new Prop(data, category, ImageDir);
        }

        public GeoType? GetGeo(Vector2Int pos, int layer)
        {
            pos.x -= TopLeftOffset.x;
            pos.y -= TopLeftOffset.y;
            int i = pos.x + pos.y * Size.x;

            int[] specs = layer switch
            {
                0 => specs1,
                1 => specs2,
                _ => null
            };

            if (specs == null)
                return null;

            if (pos.x >= 0 && pos.y >= 0 && pos.x < Size.x && pos.y < Size.y)
                return specs[i] == -1 ? null : (GeoType)specs[i];
            else
                return null;
        }

        public Sprite GetRenderSprite(int layer, int variant = 0)
        {
            if (renderTexture == null) LoadTexture();

            return renderSprites[variant + layer * Variants];
        }

        public Sprite GetPreviewSprite()
        {
            if (previewTexture == null) LoadTexture();

            return previewSprite;
        }

        private Texture2D LoadTexture()
        {
            // Load the raw texture
            var path = Path.Combine(ImageDir, Name + ".png");
            var rawData = File.ReadAllBytes(path);
            var rawTex = new Texture2D(2, 2, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point
            };
            rawTex.LoadImage(rawData);

            if (type == Type.Box)
                renderTexture = CreateBoxTexture(rawTex);
            else
                renderTexture = CreateVoxelTexture(rawTex);

            renderSprites = new Sprite[Variants * Layers];
            for(int variant = 0; variant < Variants; variant++)
            {
                for (int layer = 0; layer < Layers; layer++)
                {
                    float w = (Size.x + 2 * bufferTiles) * 20f;
                    float h = (Size.y + 2 * bufferTiles) * 20f;
                    var rect = new Rect(variant * w, layer * h, w, h);
                    renderSprites[variant + layer * Variants] = Sprite.Create(renderTexture, rect, new Vector2(bufferTiles * 20f / w, 1f - bufferTiles * 20f / h), 20f, 0, SpriteMeshType.FullRect);
                }
            }

            previewTexture = CreatePreviewTexture(rawTex);
            previewSprite = Sprite.Create(previewTexture, new Rect(0f, 0f, previewTexture.width, previewTexture.height), new Vector2(0f, 1f), 16f, 0, SpriteMeshType.FullRect);

            // Clean up
            UnityEngine.Object.Destroy(rawTex);

            return rawTex;
        }

        private Texture2D CreatePreviewTexture(Texture2D rawTex)
        {
            // Find rect that contains preview
            int layerHeight = ((type == Type.Box ? Size.y * Size.x : 0) + Size.y + (bufferTiles * 2)) * 20;
            int top = type == Type.Box ? rawTex.height : rawTex.height - 1;

            var rect = new RectInt(0, top - layerHeight * (repeatL?.Length ?? 1) - Size.y * 16, Size.x * 16, Size.y * 16);

            if (rect.x < 0 || rect.y < 0)
            {
                Debug.LogWarning($"Tile {Name} preview invalid");
                rect.x = Math.Max(rect.x, 0);
                rect.y = Math.Max(rect.y, 0);
            }

            var finalTex = new Texture2D(rect.width, rect.height, TextureFormat.ARGB32, false) { filterMode = FilterMode.Point };
            Graphics.CopyTexture(rawTex, 0, 0, rect.x, rect.y, rect.width, rect.height, finalTex, 0, 0, 0, 0);

            // Replace white with clear, everything else with white
            var pixelData = finalTex.GetPixelData<uint>(0);
            var len = pixelData.Length;
            for (int i = 0; i < len; i++)
            {
                pixelData[i] = pixelData[i] == 0xFFFFFFFFu ? 0u : 0xFFFFFFFFu;
            }
            finalTex.SetPixelData(pixelData, 0);
            finalTex.Apply();

            return finalTex;
        }

        private Texture2D CreateBoxTexture(Texture2D rawTex)
        {
            var mat = new Material(Shader.Find("Custom/TilePreview"));
            int width = (Size.x + bufferTiles * 2) * 20;
            int height = (Size.y + bufferTiles * 2) * 20;

            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var lastRt = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);

            // Copy just the front face of the box tile
            var rect = new Rect(width * (Variants - 1), rawTex.height - Size.x * Size.y * 20 - height, width, height);
            mat.SetVector("_SrcRect", new Vector4(rect.x / rawTex.width, rect.y / rawTex.height, rect.width / rawTex.width, rect.height / rawTex.height));
            Graphics.Blit(rawTex, rt, mat);

            // Copy RenderTexture to a Texture2D
            var finalTex = new Texture2D(width * Variants, height, TextureFormat.ARGB32, false) { filterMode = FilterMode.Point };
            for (int variant = 0; variant < Variants; variant++)
            {
                Graphics.CopyTexture(rt, 0, 0, 0, 0, width, height, finalTex, 0, 0, width * variant, 0);
            }

            // Clean up
            RenderTexture.active = lastRt;
            RenderTexture.ReleaseTemporary(rt);

            return finalTex;
        }

        private Texture2D CreateVoxelTexture(Texture2D rawTex)
        {
            var mat = new Material(Shader.Find("Custom/TilePreview"));
            int width = (Size.x + bufferTiles * 2) * 20;
            int height = (Size.y + bufferTiles * 2) * 20;
            var rt = RenderTexture.GetTemporary(width * Variants, height, 0, RenderTextureFormat.ARGB32);
            var finalTex = new Texture2D(width * Variants, height * Layers, TextureFormat.ARGB32, false) { filterMode = FilterMode.Point };

            var lastRt = RenderTexture.active;
            RenderTexture.active = rt;

            for(int layer = 0; layer < Layers; layer++)
            {
                // This is a temporary, so it may have stuff in it
                GL.Clear(true, true, Color.clear);

                // Draw all variants back to front to a RenderTexture
                (int first, int last) = GetLayerRange(layer);
                for(int i = last; i >= first; i--)
                {
                    var rect = GetVoxelRenderRect(rawTex, i);
                    mat.SetVector("_SrcRect", new Vector4(rect.x / rawTex.width, rect.y / rawTex.height, rect.width / rawTex.width, rect.height / rawTex.height));
                    Graphics.Blit(rawTex, rt, mat);
                }

                // Copy that into a Texture2D
                Graphics.CopyTexture(rt, 0, 0, 0, 0, rt.width, rt.height, finalTex, 0, 0, 0, height * layer);
            }

            // Clean up
            RenderTexture.active = lastRt;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.Destroy(mat);
            return finalTex;
        }

        private (int first, int last) GetLayerRange(int layer)
        {
            if(layer == 0)
            {
                int depth = 0;
                for(int i = 0; i < repeatL.Length; i++)
                {
                    depth += repeatL[i];
                    if (depth >= 10)
                        return (0, i);
                }
                return (0, repeatL.Length - 1);
            }
            else
            {
                int depth = 0;
                int start = -1;
                for (int i = 0; i < repeatL.Length; i++)
                {
                    depth += repeatL[i];
                    if (start == -1 && depth >= 10)
                        start = i;
                    if (depth >= 20)
                        return (start, i);
                }
                return (Math.Max(start, 0), repeatL.Length - 1);
            }
        }

        private Rect GetVoxelRenderRect(Texture2D tex, int layer)
        {
            int top = tex.height - 1;
            int width = (Size.x + bufferTiles * 2) * 20 * Variants;
            int height = (Size.y + bufferTiles * 2) * 20;

            return new Rect(
                x: 0f,
                y: top - height * (layer + 1),
                width,
                height);
        }

        public void Dispose()
        {
            if(renderTexture != null)
            {
                UnityEngine.Object.Destroy(renderTexture);
                foreach(var renderSprite in renderSprites)
                {
                    UnityEngine.Object.Destroy(renderSprite);
                }
                UnityEngine.Object.Destroy(previewTexture);
                UnityEngine.Object.Destroy(previewSprite);

                renderTexture = null;
                renderSprites = null;
                previewTexture = null;
                previewSprite = null;
            }
        }

        private enum Type
        {
            None,
            VoxelStruct,
            VoxelStructRockType,
            VoxelStructRandomDisplaceHorizontal,
            VoxelStructRandomDisplaceVertical,
            Box
        }
    }

    /// <summary>
    /// Represents an individual tile placed in a level.
    /// </summary>
    public class TileInstance : VisualCell
    {
        public Tile Tile { get; }
        public Vector2Int HeadPos { get; private set; }
        public int HeadLayer { get; }
        public Vector2Int TopLeft => HeadPos + Tile.TopLeftOffset;

        public TileInstance(Tile tile, Vector2Int headPos, int headLayer)
        {
            Tile = tile;
            HeadPos = headPos;
            HeadLayer = headLayer;
        }
    }

    /// <summary>
    /// A group of tile types.
    /// </summary>
    public class TileCategory
    {
        public string Name { get; }
        public Color Color { get; }
        public List<Tile> Tiles { get; } = new();

        public TileCategory(string saved)
        {
            var data = LingoParser.ParseLinearList(saved);

            Name = data.GetString(0);
            Color = data.GetColor(1);
        }
    }
}