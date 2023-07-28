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
        public Vector2Int Size { get; }
        public int Layers { get; }
        public Texture2D Texture
        {
            get
            {
                if (texture == null) LoadIconSprites();

                return texture;
            }
        }
        public Sprite[,] IconSprites
        {
            get
            {
                if (iconSprites == null) LoadIconSprites();

                return iconSprites;
            }
        }

        private Sprite[,] iconSprites;
        private Texture2D texture;
        private readonly Type type;
        private readonly int bufferTiles;
        private readonly int[] specs1;
        private readonly int[] specs2;
        private readonly int[] repeatL;

        /// <summary>
        /// The position of the top left corner of the tile relative to its head.
        /// </summary>
        public Vector2Int TopLeftOffset => (Vector2Int.one - Size) / 2;

        public Tile(string saved, TileCategory category)
        {
            var data = LingoParser.ParsePropertyList(saved);

            Category = category;
            Name = data.GetString("nm");
            Size = Vector2Int.FloorToInt(data.GetVector2("sz"));
            Layers = data.GetLinearList("specs2") == null ? 1 : 2;
            type = Enum.Parse<Type>(data.GetString("tp"), true);
            bufferTiles = data.GetInt("bfTiles");
            specs1 = data.GetLinearList("specs").Cast<int>().ToArray();
            specs2 = data.GetLinearList("specs2")?.Cast<int>().ToArray();
            if (data.TryGetLinearList("repeatL", out var reps))
                repeatL = reps.Cast<int>().ToArray();
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

        private void LoadIconSprites()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "Tiles", Name + ".png");

            if (!File.Exists(path))
            {
                Debug.LogError($"Missing image for tile {Name} at {path}!");
                texture = Texture2D.whiteTexture;
                iconSprites = new Sprite[0, 0];
                return;
            }

            var rawData = File.ReadAllBytes(path);
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            texture.LoadImage(rawData);

            var layerHeight = ((type == Type.Box ? Size.y * Size.x : 0) + Size.y + (bufferTiles * 2)) * 20;
            var top = texture.height;
            if (type != Type.Box)
            {
                top--;
            }

            iconSprites = new Sprite[Size.x, Size.y];
            for (var x = 0; x < Size.x; x++)
            {
                for (var y = 0; y < Size.y; y++)
                {
                    var rect = new Rect(x * 16, top - ((type == Type.Box ? 1 : repeatL?.Length ?? 1) * layerHeight) - 16 - (y * 16), 16, 16);
                    for (var rectX = rect.x; rectX < rect.x + rect.width; rectX++)
                    {
                        for (var rectY = rect.y; rectY < rect.y + rect.height; rectY++)
                        {
                            var color = texture.GetPixel((int)rectX, (int)rectY);
                            if (color == Color.black)
                            {
                                texture.SetPixel((int)rectX, (int)rectY, Color.white);
                            }
                            else
                            {
                                texture.SetPixel((int)rectX, (int)rectY, Color.clear);
                            }
                        }
                    }

                    iconSprites[x, y] = Sprite.Create(texture, rect, new Vector2(0, 1), 16);
                }
            }

            texture.Apply();
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