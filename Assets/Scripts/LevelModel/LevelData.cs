using Lingo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LevelModel
{
    /// <summary>
    /// Represents a level project file.
    /// </summary>
    public partial class LevelData
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public int BufferTilesLeft { get; set; }
        public int BufferTilesRight { get; set; }
        public int BufferTilesTop { get; set; }
        public int BufferTilesBottom { get; set; }

        public TileMaterial DefaultMaterial { get; set; }
        public List<EffectInstance> Effects { get; private set; }
        public List<PropInstance> Props { get; private set; }

        public TileDatabase TileDatabase { get; }
        public MaterialDatabase MaterialDatabase { get; }
        public PropDatabase PropDatabase { get; }
        public EffectDatabase EffectDatabase { get; }

        /// <summary>
        /// The angle of sunlight measured in degrees clockwise from straight up.
        /// </summary>
        public float LightAngle
        {
            get => lightSettings.GetFloat("lightAngle");
            set => lightSettings.Set("lightAngle", (Mathf.RoundToInt(value) % 360 + 360) % 360);
        }

        /// <summary>
        /// The amount that sunlight moves when going between layers, from 1 to 10.
        /// </summary>
        public int LightDistance
        {
            get => lightSettings.GetInt("flatness");
            set => lightSettings.Set("flatness", Mathf.Clamp(value, 1, 10));
        }

        public bool SunlightEnabled
        {
            get => levelOverviewProps.GetInt("light") > 0;
            set => levelOverviewProps.Set("light", value ? 1 : 0);
        }

        public int TileSeed
        {
            get => levelOverviewProps.GetInt("tileSeed");
            set => levelOverviewProps.Set("tileSeed", value);
        }

        public int WaterLevel
        {
            get => waterProps.GetInt("waterLevel");
            set => waterProps.Set("waterLevel", value);
        }

        public bool WaterInFrontOfTerrain
        {
            get => waterProps.GetInt("waterInFront") > 0;
            set => waterProps.Set("waterInFront", value ? 1 : 0);
        }

        public List<LevelCamera> Cameras { get; private set; }

        // Geo
        private byte[] geoTerrain;
        private Dictionary<Vector3Int, FeatureFlags> geoFeatures;

        // Tiles
        private VisualCell[] visualCells;

        // Misc settings
        private readonly PropertyList lightSettings;
        private readonly PropertyList levelProps;
        private readonly PropertyList levelOverviewProps;
        private readonly PropertyList waterProps;

        /// <summary>
        /// Load a level from the contents of its project file.
        /// </summary>
        public LevelData(string saved, TileDatabase tileDatabase, MaterialDatabase materialDatabase, PropDatabase propDatabase, EffectDatabase effectDatabase)
        {
            string[] lines = saved.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            TileDatabase = tileDatabase;
            MaterialDatabase = materialDatabase;
            PropDatabase = propDatabase;
            EffectDatabase = effectDatabase;

            // Load simple property lists
            lightSettings = LingoParser.ParsePropertyList(lines[3]);
            levelProps = LingoParser.ParsePropertyList(lines[4]);
            levelOverviewProps = LingoParser.ParsePropertyList(lines[5]);
            waterProps = LingoParser.ParsePropertyList(lines[7]);

            var size = Vector2Int.RoundToInt(levelOverviewProps.GetVector2("size"));
            Width = size.x;
            Height = size.y;

            var bufferTiles = levelOverviewProps.GetLinearList("extraTiles");
            BufferTilesLeft = bufferTiles.GetInt(0);
            BufferTilesTop = bufferTiles.GetInt(1);
            BufferTilesRight = bufferTiles.GetInt(2);
            BufferTilesBottom = bufferTiles.GetInt(3);

            // Load more involved data
            GeoLoader.Load(this, lines[0]);
            TileLoader.Load(this, lines[1]);
            EffectLoader.Load(this, lines[2]);
            CameraLoader.Load(this, lines[6]);
            PropLoader.Load(this, lines[8]);
        }

        /// <summary>
        /// Get level geometry data at a point.
        /// </summary>
        /// <returns>The geometry cell, or air if the point is out of bounds.</returns>
        public GeoCell GetGeoCell(Vector2Int pos, int layer)
        {
            if (pos.x < 0 || pos.y < 0 || layer < 0 ||
                pos.x >= Width || pos.y >= Height || layer >= 3) return default;
            // pos.x = Mathf.Clamp(pos.x, 0, Width - 1);
            // pos.y = Mathf.Clamp(pos.y, 0, Height - 1);
            // layer = Mathf.Clamp(layer, 0, 2);

            return new GeoCell(
                (GeoType)geoTerrain[pos.x + pos.y * Width + layer * Width * Height],
                geoFeatures.TryGetValue(new Vector3Int(pos.x, pos.y, layer), out var flags) ? flags : FeatureFlags.None
            );
        }

        /// <summary>
        /// Sets level geometry data at a point.
        /// </summary>
        /// <remarks>
        /// Out of bounds calls are ignored.
        /// </remarks>
        public void SetGeoCell(Vector2Int pos, int layer, GeoCell cell)
        {
            if (pos.x < 0 || pos.y < 0 || layer < 0 ||
                pos.x >= Width || pos.y >= Height || layer >= 3) return;

            geoTerrain[pos.x + pos.y * Width + layer * Width * Height] = (byte)cell.terrain;
            if(cell.features == FeatureFlags.None)
            {
                geoFeatures.Remove(new Vector3Int(pos.x, pos.y, layer));
            }
            else
            {
                geoFeatures[new Vector3Int(pos.x, pos.y, layer)] = cell.features;
            }
        }

        /// <summary>
        /// Gets the tile or material at a point.
        /// </summary>
        /// <returns>A <see cref="TileInstance"/>, <see cref="TileMaterial" />, or <c>null</c> if the default material should be used.</returns>
        public VisualCell GetVisualCell(Vector2Int pos, int layer)
        {
            if (pos.x < 0 || pos.y < 0 || layer < 0 ||
                pos.x >= Width || pos.y >= Height || layer >= 3) return null;

            return visualCells[pos.x + pos.y * Width + layer * Width * Height];
        }

        private void SetVisualCell(Vector2Int pos, int layer, VisualCell visuals)
        {
            if (pos.x < 0 || pos.y < 0 || layer < 0 ||
                pos.x >= Width || pos.y >= Height || layer >= 3) return;

            visualCells[pos.x + pos.y * Width + layer * Width * Height] = visuals;
        }

        /// <summary>
        /// Check if a tile can be placed at a location.
        /// </summary>
        /// <returns><see langword="true"/> if <paramref name="tile"/> can be placed at this location, <see langword="false"/> otherwise.</returns>
        public bool CanPlaceTile(Vector2Int headPos, int headLayer, Tile tile)
        {
            if (headPos.x < 0 || headPos.y < 0 || headPos.x >= Width || headPos.y >= Width || headLayer < 0 || headLayer >= 3)
                return false;

            var min = headPos + tile.TopLeftOffset;
            for (int z = headLayer; z < headLayer + tile.Layers; z++)
            {
                if (z > 2) continue;

                for (int y = min.y; y < min.y + tile.Size.y; y++)
                {
                    for (int x = min.x; x < min.x + tile.Size.x; x++)
                    {
                        var pos = new Vector2Int(x, y);

                        // Check for mismatched geometry
                        if (tile.GetGeo(pos - headPos, z - headLayer) is GeoType tileGeo)
                        {
                            var levelGeo = GetGeoCell(pos, z).terrain;
                            if (tileGeo != levelGeo)
                                return false;

                            // Check for mismatched tiles
                            if (GetVisualCell(pos, z) is TileInstance)
                                return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Create a tile with its head at the given location.
        /// </summary>
        /// <param name="addGeo">Whether to change the level geometry to match this tile.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="headPos"/> or <paramref name="headLayer"/> is outside of the level.</exception>
        public TileInstance PlaceTile(Vector2Int headPos, int headLayer, Tile tile, bool addGeo)
        {
            if (headPos.x < 0 || headPos.y < 0 || headPos.x >= Width || headPos.y >= Width || headLayer < 0 || headLayer >= 3)
                throw new ArgumentOutOfRangeException(nameof(headPos), "Tile head must be inside of the level!");

            var inst = new TileInstance(tile, headPos, headLayer);
            var min = headPos + tile.TopLeftOffset;
            for (int z = headLayer; z < headLayer + tile.Layers; z++)
            {
                if (z > 2) continue;

                for (int y = min.y; y < min.y + tile.Size.y; y++)
                {
                    for (int x = min.x; x < min.x + tile.Size.x; x++)
                    {
                        var pos = new Vector2Int(x, y);

                        // Add geometry
                        if (tile.GetGeo(pos - headPos, z - headLayer) is GeoType tileGeo)
                        {
                            if (addGeo)
                            {
                                var cell = GetGeoCell(pos, z);
                                cell.terrain = tileGeo;
                                SetGeoCell(pos, z, cell);
                            }

                            // Remove conflicting tiles
                            if (GetVisualCell(pos, z) is TileInstance hitTile
                                && pos == hitTile.HeadPos && z == hitTile.HeadLayer)
                            {
                                RemoveTile(hitTile);
                            }
                        }
                    }
                }
            }

            return inst;
        }

        /// <summary>
        /// Remove a tile instance from the level.
        /// </summary>
        public void RemoveTile(TileInstance tile)
        {
            var min = tile.TopLeft;
            for (int z = tile.HeadLayer; z < tile.HeadLayer + tile.Tile.Layers; z++)
            {
                if (z > 2) continue;

                for (int y = min.y; y < min.y + tile.Tile.Size.y; y++)
                {
                    for (int x = min.x; x < min.x + tile.Tile.Size.x; x++)
                    {
                        var pos = new Vector2Int(x, y);

                        if(GetVisualCell(pos, z) == tile)
                        {
                            SetVisualCell(pos, z, null);
                        }
                    }
                }
            }
        }
    }
}