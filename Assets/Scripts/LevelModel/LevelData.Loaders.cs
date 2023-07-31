using Lingo;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelModel
{
    public partial class LevelData
    {
        private PropertyList importedTileData;
        private PropertyList importedEffectData;
        private PropertyList importedCameraData;
        private PropertyList importedPropData;

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
                LinearList cells = LingoParser.ParseLinearList(saved);

                var w = cells.Count;
                var h = cells.GetLinearList(0).Count;

                if (w != level.Width || h != level.Height)
                    throw new FormatException($"Level size of {level.Width}x{level.Height} does not match geometry size of {w}x{h}!");

                var terrain = level.geoTerrain = new byte[w * h * 3];
                var features = level.geoFeatures = new();

                for (int x = 0; x < cells.Count; x++)
                {
                    var column = cells.GetLinearList(x);

                    for (int y = 0; y < column.Count; y++)
                    {
                        var layers = column.GetLinearList(y);

                        for (int z = 0; z < layers.Count; z++)
                        {
                            var cell = layers.GetLinearList(z);
                            var parsedTerrain = cell.GetInt(0);
                            var parsedFeatures = cell.GetLinearList(1);

                            terrain[x + y * w + z * w * h] = (byte)GetGeoType(parsedTerrain);

                            FeatureFlags flags = GetFeatureFlags(parsedFeatures);
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
            private static FeatureFlags GetFeatureFlags(LinearList features)
            {
                FeatureFlags flags = FeatureFlags.None;
                for (int i = 0; i < features.Count; i++)
                {
                    var feature = features.GetInt(i);
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
                level.importedTileData = LingoParser.ParsePropertyList(saved);
                var tlMatrix = level.importedTileData.GetLinearList("tlMatrix");
                var w = tlMatrix.Count;
                var h = tlMatrix.GetLinearList(0).Count;

                if (w != level.Width || h != level.Height)
                    throw new FormatException($"Level size of {level.Width}x{level.Height} does not match tile size of {w}x{h}!");

                level.visualCells = new VisualCell[w * h * 3];
                level.DefaultMaterial = level.MaterialDatabase[level.importedTileData.GetString("defaultMaterial")];

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
                            var tile = tlMatrix.GetLinearList(x).GetLinearList(y).GetPropertyList(layer);
                            VisualCell cell;

                            switch(tile.GetString("tp"))
                            {
                                case "material":
                                    cell = level.MaterialDatabase[tile.GetString("data")];
                                    break;

                                case "tileHead":
                                    var tileSpec = tile.GetLinearList("data");
                                    //var tileLoc = tileSpec.GetVector2(0);
                                    cell = tileHeads[new(x, y, layer)] = new TileInstance(
                                        tile: level.TileDatabase[tileSpec.GetString(1)],
                                        headPos: new Vector2Int(x, y),
                                        headLayer: layer
                                    );
                                    break;

                                case "tileBody":
                                    var headSpec = tile.GetLinearList("data");
                                    var headLoc = headSpec.GetVector2(0);
                                    var headPos = new Vector3Int((int)headLoc.x - 1, (int)headLoc.y - 1, headSpec.GetInt(1) - 1);
                                    if (!tileBodies.TryGetValue(headPos, out var bodyList))
                                    {
                                        tileBodies[headPos] = bodyList = new List<Vector3Int>();
                                    }
                                    bodyList.Add(new(x, y, layer));

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

                // Unload expensive parts of the imported data that won't be used later
                level.importedTileData.SetObject("tlMatrix", LingoParser.Placeholder);
            }
        }

        /// <summary>
        /// Loads and saves effect data.
        /// </summary>
        private static class EffectLoader
        {
            /// <summary>
            /// Load effect data from a Lingo string.
            /// </summary>
            public static void Load(LevelData level, string saved)
            {
                level.importedEffectData = LingoParser.ParsePropertyList(saved);

                var loadedEffects = level.importedEffectData.GetLinearList("effects");
                level.importedEffectData.SetObject("effects", LingoParser.Placeholder);

                level.Effects = new List<EffectInstance>();
                for(int i = 0; i < loadedEffects.Count; i++)
                {
                    string name = "Unknown";
                    try
                    {
                        var loadData = loadedEffects.GetPropertyList(i);
                        name = loadData.GetString("nm");
                        var effectType = level.EffectDatabase[name];
                        level.Effects.Add(effectType.Instantiate(new(level.Width, level.Height), loadData));
                    }
                    catch(Exception e)
                    {
                        throw new FormatException($"Failed to load effect {i + 1}: {name}", e);
                    }
                }
            }
        }

        /// <summary>
        /// Loads and saves camera data.
        /// </summary>
        private static class CameraLoader
        {
            /// <summary>
            /// Load camera data from a Lingo string.
            /// </summary>
            public static void Load(LevelData level, string saved)
            {
                level.importedCameraData = LingoParser.ParsePropertyList(saved);

                var cams = level.importedCameraData.GetLinearList("cameras");
                var quads = level.importedCameraData.GetLinearList("quads");

                level.Cameras = new List<LevelCamera>();
                for(int i = 0; i < cams.Count; i++)
                {
                    level.Cameras.Add(new LevelCamera(cams.GetVector2(i), quads.GetLinearList(i)));
                }

                level.importedCameraData.SetObject("cameras", LingoParser.Placeholder);
            }
        }

        /// <summary>
        /// Loads and saves prop data.
        /// </summary>
        private static class PropLoader
        {
            /// <summary>
            /// Load prop data from a Lingo string.
            /// </summary>
            public static void Load(LevelData level, string saved)
            {
                level.importedPropData = LingoParser.ParsePropertyList(saved);

                var props = level.importedPropData.GetLinearList("props");

                level.Props = new List<PropInstance>();
                for (int i = 0; i < props.Count; i++)
                {
                    var propData = props.GetLinearList(i);
                    var prop = level.PropDatabase[propData.GetString(1)];
                    var propInstance = prop.Instantiate(propData);

                    level.Props.Add(propInstance);
                }

                // Clear out imported props from memory after parsing
                level.importedPropData.SetObject("props", LingoParser.Placeholder);
            }
        }
    }
}