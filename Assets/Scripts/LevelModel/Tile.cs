using Lingo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Lingo.MiddleMan;

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
        public Texture2D Texture { get; }
        public Sprite[,] IconSprites
        {
            get
            {
                if (data.iconSprites == null)
                    ParseTileImage(data);

                return data.iconSprites;
            }
        }

        /// <summary>
        /// The position of the top left corner of the tile relative to its head.
        /// </summary>
        public Vector2Int TopLeftOffset => (Vector2Int.one - Size) / 2;

        private readonly LTile data;

        public Tile(string saved, TileCategory category)
        {
            data = new LTile(saved);
            ParseTileImage(data);

            Category = category;
            Name = data.name;
            Size = new Vector2Int((int)data.size.x, (int)data.size.y);
            Texture = data.texture;
            Layers = data.specs2 == null ? 1 : 2;
        }

        public GeoType? GetGeo(Vector2Int pos, int layer)
        {
            pos.x -= TopLeftOffset.x;
            pos.y -= TopLeftOffset.y;
            int i = pos.x + pos.y * Size.x;

            int[] specs = layer switch
            {
                0 => data.specs,
                1 => data.specs2,
                _ => null
            };

            if(specs != null)
            {
                if (pos.x >= 0 && pos.y >= 0 && pos.x < Size.x && pos.y < Size.y)
                    return data.specs[i] == -1 ? null : (GeoType)data.specs[i];
                else
                    return null;
            }
            else
            {
                return null;
            }
        }

        private static void ParseTileImage(LTile tile)
        {
            var path = Path.Combine(Application.streamingAssetsPath, "Tiles", tile.name + ".png");

            if (!File.Exists(path))
            {
                Debug.LogError($"missing image for tile {tile.name} at {path}");
                return;
            }

            var rawData = File.ReadAllBytes(path);
            tile.texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            tile.texture.LoadImage(rawData);

            var layerHeight = ((tile.type == LTile.Type.box ? tile.size.y * tile.size.x : 0) + tile.size.y + (tile.bfTiles * 2)) * 20;
            var top = tile.texture.height;
            if (tile.type != LTile.Type.box)
            {
                top--;
            }

            tile.iconSprites = new Sprite[(int)tile.size.x, (int)tile.size.y];
            for (var x = 0; x < tile.size.x; x++)
            {
                for (var y = 0; y < tile.size.y; y++)
                {
                    var rect = new Rect(x * 16, top - ((tile.type == LTile.Type.box ? 1 : tile.repeatL?.Length ?? 1) * layerHeight) - 16 - (y * 16), 16, 16);
                    for (var rectX = rect.x; rectX < rect.x + rect.width; rectX++)
                    {
                        for (var rectY = rect.y; rectY < rect.y + rect.height; rectY++)
                        {
                            var color = tile.texture.GetPixel((int)rectX, (int)rectY);
                            if (color == Color.black)
                            {
                                tile.texture.SetPixel((int)rectX, (int)rectY, Color.white);
                            }
                            else
                            {
                                tile.texture.SetPixel((int)rectX, (int)rectY, default);
                            }
                        }
                    }

                    tile.iconSprites[x, y] = Sprite.Create(tile.texture, rect, new Vector2(0, 1), 16);
                }
            }

            tile.texture.Apply();
        }

        // Helper for parsing tiles
        private class LTile : ILingoData
        {
#pragma warning disable
            [LingoIndex(0, "nm")]
            public string name;
            [LingoIndex(1, "sz")]
            public Vector2 size;
            [LingoIndex(2, "specs")]
            public int[] specs;
            [LingoIndex(3, "specs2", nullable: true)]
            public int[] specs2;
            [LingoIndex(4, "tp")]
            public Type type;
            [LingoIndex(5, "repeatL", skippable: true)]
            public int[] repeatL;
            [LingoIndex(6, "bfTiles")]
            public float bfTiles;
            [LingoIndex(7, "rnd")]
            public float rnd;
            [LingoIndex(8, "tags")]
            public string[] tags;

            public Texture2D texture;
            //-- Layer, X, Y
            public Sprite[,,] renderSprites;
            public Sprite[,] iconSprites;
#pragma warning enable

            public LTile(string saved)
            {
                object obj = LingoParsing.FromLingoString(saved);

                if (obj is not object[] arr) throw new FormatException("Expected an array!");

                if (!SyncAllAttributes(this, arr))
                    throw new FormatException("Failed to parse line!");

                if (type == Type.none)
                    throw new FormatException("Type could not be found!");

                if (repeatL == null && !(type == Type.box || type == Type.voxelStructRockType))
                    throw new FormatException("Missing repeatL!");
            }

            public enum Type
            {
                none,
                voxelStruct,
                voxelStructRockType,
                voxelStructRandomDisplaceHorizontal,
                voxelStructRandomDisplaceVertical,
                box
            }
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

        private readonly LCategory data;

        public TileCategory(string saved)
        {
            data = new LCategory(saved);

            Name = data.name;
            Color = data.color;
        }

        // Helper for parsing tile categorites
        private class LCategory : ILingoData
        {
            public LCategory(string saved)
            {
                object obj = LingoParsing.FromLingoString(saved);

                if (obj is not object[] arr)
                    throw new FormatException("Expected an array!");

                if (!SyncAllAttributes(this, arr))
                    throw new FormatException("Failed to parse line!");
            }

#pragma warning disable
            [LingoIndex(0, null)]
            public string name;

            [LingoIndex(1, null)]
            public Color color;
#pragma warning enable
        }
    }
}