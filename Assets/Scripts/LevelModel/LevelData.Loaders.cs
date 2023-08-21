using Lingo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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
            private const FeatureFlags ShortcutEntranceFeature = (FeatureFlags)0x8;

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

            public static string Save(LevelData level)
            {
                var terrain = level.geoTerrain;
                var features = level.geoFeatures;
                int w = level.Width;
                int h = level.Height;
                var matrix = new LinearList { Capacity = w };
                var noFeatures = new LinearList();

                for (int x = 0; x < w; x++)
                {
                    var column = new LinearList { Capacity = h };

                    for(int y = 0; y < h; y++)
                    {
                        var layers = new LinearList { Capacity = 3 };

                        for(int z = 0; z < 3; z++)
                        {
                            var cell = new LinearList { Capacity = 2 };
                            GeoType geoType = (GeoType)terrain[x + y * w + z * w * h];

                            cell.Add((int)geoType);
                            if (features.TryGetValue(new(x, y, z), out var flags) || geoType == GeoType.ShortcutEntrance)
                                cell.Add(GetFeatureList(flags | (geoType == GeoType.ShortcutEntrance ? ShortcutEntranceFeature : 0)));
                            else
                                cell.Add(noFeatures);

                            layers.Add(cell);
                        }

                        column.Add(layers);
                    }

                    matrix.Add(column);
                }

                return LingoParser.ToLingoString(matrix);
            }

            // Convert Lingo geometry index to the corresponding GeoType
            private static GeoType GetGeoType(int index)
            {
                if (index < 0 || index > 9) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid geometry type: {index}");
                if (index == 8) index = 0;

                return (GeoType)index;
            }

            // Generate FeatureFlags from a list of Lingo bit indices
            private static FeatureFlags GetFeatureFlags(LinearList features)
            {
                FeatureFlags flags = FeatureFlags.None;
                for (int i = 0; i < features.Count; i++)
                {
                    if (i == 4) continue; // Remove shortcut entrances

                    var feature = (FeatureFlags)(1 << (features.GetInt(i) - 1));
                    flags |= feature;
                }

                return flags;
            }

            private static LinearList GetFeatureList(FeatureFlags features)
            {
                var res = new LinearList();
                for(int i = 0; i < 31; i++)
                {
                    if(((int)features & (1 << i)) != 0)
                    {
                        res.Add(i + 1);
                    }
                }
                return res;
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
                level.tiles = new();
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
                                    string tileName;
                                    if(tile.TryGetPropertyList("data", out var tileProps))
                                        tileName = tileProps.GetString("nm");
                                    else
                                        tileName = tile.GetLinearList("data").GetString(1);

                                    cell = tileHeads[new(x, y, layer)] = new TileInstance(
                                        tile: level.TileDatabase[tileName],
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

                level.tiles.AddRange(tileHeads.Values);

                // Unload expensive parts of the imported data that won't be used later
                level.importedTileData.SetObject("tlMatrix", LingoParser.Placeholder);
            }

            public static string Save(LevelData level)
            {
                int w = level.Width;
                int h = level.Height;
                var visualCells = level.visualCells;
                var matrix = new LinearList { Capacity = w };
                var defaultMat = new PropertyList();
                defaultMat.Set("tp", "default");
                defaultMat.Set("data", 0);

                for (int x = 0; x < w; x++)
                {
                    var column = new LinearList { Capacity = h };

                    for (int y = 0; y < h; y++)
                    {
                        var layers = new LinearList { Capacity = 3 };

                        for (int z = 0; z < 3; z++)
                        {
                            PropertyList cell;
                            var vis = visualCells[x + y * w + z * w * h];

                            if(vis == null)
                            {
                                cell = defaultMat;
                            }
                            else if(vis is TileMaterial mat)
                            {
                                var matCell = new PropertyList();
                                matCell.Set("tp", "material");
                                matCell.Set("data", mat.Name);
                                cell = matCell;
                            }
                            else if(vis is TileInstance tile)
                            {
                                if(tile.HeadPos.x == x && tile.HeadPos.y == y && tile.HeadLayer == z)
                                {
                                    var headCell = new PropertyList();
                                    headCell.Set("tp", "tileHead");
                                    headCell.Set("data", LinearList.Make(
                                        level.TileDatabase.GetTileIndex(tile.Tile) + new Vector2(3f, 1f),
                                        tile.Tile.Name
                                    ));
                                    cell = headCell;
                                }
                                else
                                {
                                    var bodyCell = new PropertyList();
                                    bodyCell.Set("tp", "tileBody");
                                    bodyCell.Set("data", LinearList.Make(
                                        (Vector2)tile.HeadPos + Vector2.one,
                                        tile.HeadLayer + 1
                                    ));
                                    cell = bodyCell;
                                }
                            }
                            else
                            {
                                throw new FormatException($"Unknown visual cell type: {vis.GetType().Name}");
                            }

                            layers.Add(cell);
                        }

                        column.Add(layers);
                    }

                    matrix.Add(column);
                }

                level.importedTileData.Set("defaultMaterial", level.DefaultMaterial.Name);
                level.importedTileData.Set("tlMatrix", matrix);
                string saved = LingoParser.ToLingoString(level.importedTileData);
                level.importedTileData.SetObject("tlMatrix", LingoParser.Placeholder);

                return saved;
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

            public static string Save(LevelData level)
            {
                var effects = new LinearList { Capacity = level.Effects.Count };

                foreach(var effect in level.Effects)
                {
                    effects.Add(effect.Save());
                }

                level.importedEffectData.Set("effects", effects);
                string saved = LingoParser.ToLingoString(level.importedEffectData);
                level.importedEffectData.SetObject("effects", LingoParser.Placeholder);

                return saved;
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
                
                // Load cameras or default
                if(!level.importedCameraData.TryGetLinearList("cameras", out var cams) || cams == null)
                {
                    cams = LinearList.Make(new Vector2(level.Width * 10f - 35f * 20f, level.Height * 10f - 20f * 20f));
                    level.importedCameraData.Set("cameras", cams);
                }
                
                // Load camera quads or default
                if (!level.importedCameraData.TryGetLinearList("quads", out var quads) || quads == null)
                {
                    quads = new LinearList();
                    for(int i = 0; i < cams.Count; i++)
                    {
                        quads.Add(LinearList.Make(LinearList.Make(0, 0), LinearList.Make(0, 0), LinearList.Make(0, 0), LinearList.Make(0, 0)));
                    }
                    level.importedCameraData.Set("quads", quads);
                }

                level.Cameras = new List<LevelCamera>();
                for(int i = 0; i < cams.Count; i++)
                {
                    level.Cameras.Add(new LevelCamera(cams.GetVector2(i), quads.GetLinearList(i)));
                }

                level.importedCameraData.SetObject("cameras", LingoParser.Placeholder);
                level.importedCameraData.SetObject("quads", LingoParser.Placeholder);
            }

            public static string Save(LevelData level)
            {
                var cams = new LinearList();
                var quads = new LinearList();

                foreach(var cam in level.Cameras)
                {
                    var quad = new LinearList();
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 offset = cam.CornerOffsets[i];

                        float rad = 0f;
                        float dist = 0f;
                        if(offset != Vector2.zero)
                        {
                            rad = Mathf.Atan2(offset.x, -offset.y);
                            dist = offset.magnitude;
                        }

                        quad.Add(LinearList.Make(Mathf.RoundToInt(rad * Mathf.Rad2Deg), dist / LevelCamera.MaxOffsetDistance));
                    }

                    cams.Add(cam.Center - LevelCamera.Size / 2f);
                    quads.Add(quad);
                }

                level.importedCameraData.Set("cameras", cams);
                level.importedCameraData.Set("quads", quads);
                string saved = LingoParser.ToLingoString(level.importedCameraData);
                level.importedCameraData.SetObject("cameras", LingoParser.Placeholder);
                level.importedCameraData.SetObject("quads", LingoParser.Placeholder);

                return saved;
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

            public static string Save(LevelData level)
            {
                var props = new LinearList();

                foreach(var prop in level.Props)
                {
                    props.Add(prop.Save());
                }

                level.importedPropData.Set("props", props);
                string saved = LingoParser.ToLingoString(level.importedPropData);
                level.importedPropData.SetObject("props", LingoParser.Placeholder);

                return saved;
            }
        }
    }
}