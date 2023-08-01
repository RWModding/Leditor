using Lingo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LevelModel
{
    /// <summary>
    /// Represents a type of tile, many of which may be placed in a level.
    /// </summary>
    public class Tile
    {
        public TileCategory Category { get; }
        public string Name { get; }
        public string ImageDir { get; }
        public Vector2Int Size { get; }
        public int Layers { get; }
        public List<string> Tags { get; }
        public List<string> Notes { get; }

        public Texture2D Texture => texture == null ? texture = LoadTexture() : texture;

        private Texture2D texture;
        private readonly Type type;
        private readonly int bufferTiles;
        private readonly int[] specs1;
        private readonly int[] specs2;
        private readonly int[] repeatL;
        private readonly int variants;

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
            variants = data.GetInt("rnd");

            if (data.TryGetLinearList("repeatL", out var reps))
                repeatL = reps.Cast<int>().ToArray();
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
            data.Set("tp", variants > 1 ? "variedStandard" : "standard");
            data.Set("colorTreatment", "standard");
            data.Set("sz", Size + bufferTiles * 2 * Vector2.one);
            data.Set("depth", specs2 == null ? 10 : 20);
            data.Set("repeatL", new LinearList(repeatL.Cast<object>()));

            if (variants > 1)
            {
                data.Set("vars", variants);
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

        public Rect GetPreviewSpriteRect()
        {
            int layerHeight = ((type == Type.Box ? Size.y * Size.x : 0) + Size.y + (bufferTiles * 2)) * 20;
            int top = Texture.height;
            if (type != Type.Box)
            {
                top--;
            }

            return new Rect(0, top - ((type == Type.Box ? 1 : repeatL.Length) * layerHeight) - Size.y, Size.x * 16, Size.y * 16);
        }

        private Texture2D LoadTexture()
        {
            var path = Path.Combine(ImageDir, Name + ".png");
            var rawData = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point
            };
            texture.LoadImage(rawData);

            return texture;
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
        public Vector2Int HeadPos { get; }
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