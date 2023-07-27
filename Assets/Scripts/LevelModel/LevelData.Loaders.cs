using Lingo;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LingoIndexAttribute = Lingo.MiddleMan.LingoIndexAttribute;

namespace LevelModel
{
    public partial class LevelData
    {
        /// <summary>
        /// Loads and saves geometry data.
        /// </summary>
        private static class GeoLoader
        {
            /// <summary>
            /// Load geometry from a Lingo string.
            /// </summary>
            public static void Load(LevelData level, string saved)
            {
                ParsedGeoCell[][][] cells = null;
                MiddleMan.SetValueFromLingo(ref cells, LingoParsing.FromLingoString(saved));

                var w = level.Width = cells.Length;
                var h = level.Height = cells[0].Length;
                var terrain = level.geoTerrain = new byte[w * h * 3];
                var features = level.geoFeatures;

                for (int x = 0; x < cells.Length; x++)
                {
                    var column = cells[x];
                    for (int y = 0; y < column.Length; y++)
                    {
                        var layers = column[y];
                        for (int z = 0; z < layers.Length; z++)
                        {
                            var cell = layers[z];

                            terrain[x + y * w + z * w * h] = (byte)GetGeoType(cell.terrain);

                            FeatureFlags flags = GetFeatureFlags(cell.features);
                            if (flags != FeatureFlags.None)
                            {
                                features.Add(new Vector3Int(x, y, z), flags);
                            }
                        }
                    }
                }
            }

            // Convert Lingo geometry index to the corresponding GeoType
            private static GeoType GetGeoType(int index)
            {
                if (index < 0 || index > 9) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid geometry type: {index}");
                if (index == 8) index = 0;

                return (GeoType)index;
            }

            // Maps Lingo feature indices to FeatureFlags bits
            private static readonly FeatureFlags[] featureIndex = new FeatureFlags[]
            {
                FeatureFlags.None,
                FeatureFlags.HorizontalBeam,
                FeatureFlags.VerticalBeam,
                FeatureFlags.Hive,
                FeatureFlags.None,
                FeatureFlags.ShortcutDot,
                FeatureFlags.None,
                FeatureFlags.DragonDen,
                FeatureFlags.None,
                FeatureFlags.Rock,
                FeatureFlags.Spear,
                FeatureFlags.Crack,
                FeatureFlags.ForbidBats,
                FeatureFlags.None,
                FeatureFlags.None,
                FeatureFlags.None,
                FeatureFlags.None,
                FeatureFlags.GarbageHole,
                FeatureFlags.Waterfall,
                FeatureFlags.WhackAMoleHole,
                FeatureFlags.WormGrass,
                FeatureFlags.ScavengerHole
            };

            // Generate FeatureFlags from a list of Lingo bit indices
            private static FeatureFlags GetFeatureFlags(int[] features)
            {
                FeatureFlags flags = FeatureFlags.None;
                for (int i = 0; i < features.Length; i++)
                {
                    var feature = features[i];
                    if (feature < 0 || feature > featureIndex.Length)
                    {
                        throw new ArgumentException($"Invalid feature type: {feature}");
                    }
                    flags |= featureIndex[feature];
                }

                return flags;
            }

            // Helper class for parsing geometry data
            private struct ParsedGeoCell : MiddleMan.ILingoData
            {
                [MiddleMan.LingoIndex(0, null)]
                public int terrain;

                [MiddleMan.LingoIndex(1, null)]
                public int[] features;
            }
        }

        /// <summary>
        /// Loads and saves tile data.
        /// </summary>
        private static class TileLoader
        {
            /// <summary>
            /// Load tile data from a Lingo string.
            /// </summary>
            public static void Load(LevelData level, string saved)
            {
                var data = new TileData(saved);
                var w = data.tlMatrix.Count;
                var h = data.tlMatrix[0].Count;

                if (w != level.Width || h != level.Height)
                    throw new FormatException($"Geometry size of {level.Width}x{level.Height} does not match tile size of {w}x{h}!");

                level.visualCells = new VisualCell[w * h * 3];
                level.DefaultMaterial = level.MaterialDatabase[data.defaultMaterial];

                var tlMatrix = data.tlMatrix;
                var tileHeads = new Dictionary<Vector3Int, TileInstance>();
                var tileBodies = new Dictionary<Vector3Int, List<Vector3Int>>();

                // Fill level data, not including tile bodies
                int i = 0;
                for(int layer = 0; layer < 3; layer++)
                {
                    for(int y = 0; y < h; y++)
                    {
                        for(int x = 0; x < w; x++)
                        {
                            var tileData = tlMatrix[x][y][layer];
                            VisualCell cell;

                            switch(tileData.tp)
                            {
                                case "material":
                                    cell = level.MaterialDatabase[tileData.name];
                                    break;

                                case "tileHead":
                                    cell = tileHeads[new(x, y, layer)] = new TileInstance(
                                        tile: level.TileDatabase.GetTile(tileData.categoryIndex - 3, tileData.tileIndex - 1, tileData.name),
                                        headPos: new Vector2Int(x, y),
                                        headLayer: layer
                                    );
                                    break;

                                case "tileBody":
                                    {
                                        var headPos = new Vector3Int((int)tileData.headPos.x - 1, (int)tileData.headPos.y - 1, tileData.headLayer - 1);
                                        if (!tileBodies.TryGetValue(headPos, out var bodyList))
                                        {
                                            tileBodies[headPos] = bodyList = new List<Vector3Int>();
                                        }
                                        bodyList.Add(new(x, y, layer));
                                    }

                                    cell = null;
                                    break;

                                default:
                                    cell = null;
                                    break;
                            }

                            level.visualCells[i++] = cell;
                        }
                    }
                }

                // Link tile bodies to heads
                foreach(var pair in tileBodies)
                {
                    if(tileHeads.TryGetValue(pair.Key, out var tile))
                    {
                        foreach(var bodyPos in pair.Value)
                        {
                            level.visualCells[bodyPos.x + bodyPos.y * w + bodyPos.z * w * h] = tile;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Missing tile head at {pair.Key}!");
                    }
                }
            }

            private class TileData : MiddleMan.ILingoData
            {
                [LingoIndex(0, "lastKeys")]
                private KeyValuePair<string, object>[] lastKeys;

                [LingoIndex(1, "Keys")]
                private KeyValuePair<string, object>[] keys;

                [LingoIndex(2, "workLayer")]
                private int workLayer;

                [LingoIndex(3, "lstMsPs")]
                private Vector2 lstMsPs;

                [LingoIndex(4, "tlMatrix")]
                public List<List<TileCell[]>> tlMatrix;

                [LingoIndex(5, "defaultMaterial")]
                public string defaultMaterial;

                [LingoIndex(6, "toolType")]
                private string toolType;

                [LingoIndex(7, "toolData")]
                private string toolData;

                [LingoIndex(8, "tmPos")]
                private Vector2 tmPos;

                [LingoIndex(9, "tmSavPosL")]
                private object[] tmSavPosL;

                [LingoIndex(10, "specialEdit")]
                private int specialEdit;

                public TileData(string saved)
                {
                    if (LingoParsing.FromLingoString(saved) is not object[] lingos)
                        throw new FormatException("Could not parse tile data!");

                    if(!MiddleMan.SyncAllAttributes(this, lingos))
                        throw new FormatException("Could not parse tile attributes!");

                    foreach (var column in tlMatrix)
                    {
                        foreach (var cell in column)
                        {
                            foreach (var layer in cell)
                            {
                                try
                                {
                                    switch (layer.tp)
                                    {
                                        case "default":
                                            break;
                                        case "material":
                                            layer.name = (string)layer.data;
                                            break;
                                        case "tileBody":
                                            var tbData = (object[])layer.data;
                                            layer.headPos = (Vector2)tbData[0];
                                            layer.headLayer = int.Parse(tbData[1].ToString());
                                            break;
                                        case "tileHead":
                                            var thData = layer.data as object[];
                                            Vector2 tileInfo = (Vector2)thData[0];
                                            layer.categoryIndex = (int)tileInfo.x;
                                            layer.tileIndex = (int)tileInfo.y;
                                            layer.name = thData[1].ToString();
                                            break;
                                        default:
                                            throw new FormatException($"Invalid tile type {layer.ToLingoString()}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    throw new FormatException($"could not parse data from tile {layer.ToLingoString()}", ex);
                                }
                            }
                        }
                    }
                }
            }

            private class TileCell : MiddleMan.ILingoData
            {
                [LingoIndex(0, "tp")]
                public string tp;

                [LingoIndex(1, "data")]
                public object data;

                public int categoryIndex;
                public int tileIndex;

                public string name;
                public Vector2 headPos;
                public int headLayer;
            }
        }

        private static class EffectLoader
        {
            /// <summary>
            /// Load effect data from a Lingo string.
            /// </summary>
            public static void Load(LevelData level, string saved)
            {
                //var data = new EffectData(saved);
                //var w = data.tlMatrix.Count;
                //var h = data.tlMatrix[0].Count;
                //
                //if (w != level.Width || h != level.Height)
                //    throw new FormatException($"Geometry size of {level.Width}x{level.Height} does not match tile size of {w}x{h}!");
            }

            private class EffectData : MiddleMan.ILingoData
            {

            }
        }
    }
}