using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Lingo.LingoParsing;
using static Lingo.MiddleMan;

namespace Lingo
{
    public class Inits
    {
        public static void LoadTilesInit(string[] lines)
        {
            foreach (string line in lines)
            {
                Debug.LogWarning(line);
                if (line.Length > 0 && line[0] == '-')
                {
                    string str = line.Substring(1);

                    object obj = FromLingoString(str);

                    if (obj is not object[] arr) continue;

                    if (Category.TryParse(arr, out Category cat))
                    {
                        tileCategories.Add(cat, new());
                        Debug.Log(cat.ToLingoString());
                    }
                }
                else
                {
                    object obj = FromLingoString(line);

                    if (obj is not object[] arr) continue;

                    if (LTile.TryParse(arr, out LTile tile))
                    {
                        Debug.Log(tile.ToLingoString());
                    }
                }
            }
        }

        public static void LoadPropsInit(string[] lines)
        {
            foreach (string line in lines)
            {
                if (line.Length > 0 && line[0] == '-')
                {
                    string str = line.Substring(1);

                    object obj = FromLingoString(str);

                    if (obj is not object[] arr) continue;

                    if (Category.TryParse(arr, out Category cat))
                    {
                        propCategories.Add(cat, new());
                        Debug.Log(cat.ToLingoString());
                    }
                }
                else
                {
                    object obj = FromLingoString(line);

                    if (obj is not object[] arr) { continue; }

                    if (Prop.TryParse(arr, out Prop prop))
                    {
                        Debug.Log(prop.ToLingoString());
                    }
                }
            }
        }

        public static Dictionary<Category, List<LTile>> tileCategories = new();
        public static Dictionary<Category, List<Prop>> propCategories = new();


        public class Category : ILingoData
        {
#pragma warning disable
            [LingoIndex(0, null)]
            string name;

            [LingoIndex(1, null)]
            Color color;
#pragma warning enable

            public static bool TryParse(object[] obj, out Category cat)
            {
                cat = new();
                if (SyncAllAttributes(cat, obj))
                { return true; }
                else
                {
                    Debug.LogError($"failed to parse line ({obj.ToLingoString()})");
                    return false;
                }
            }
        }

        public class LTile : ILingoData
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
#pragma warning enable

            public static bool TryParse(object[] obj, out LTile tile)
            {
                tile = new();
                if (SyncAllAttributes(tile, obj))
                {
                    if (tile.type == Type.none)
                    {
                        Debug.LogError($"type could not be found");
                        Debug.LogError($"failed to parse line ({obj.ToLingoString()})");
                    }

                    if (tile.repeatL == null && !(tile.type == Type.box || tile.type == Type.voxelStructRockType))
                    {
                        Debug.LogError($"missing repeatL");
                        Debug.LogError($"failed to parse line ({obj.ToLingoString()})");
                        Debug.LogError($"failed to parse line ({tile.ToLingoString()})");
                    }
                    return true;
                }
                else
                {
                    Debug.LogError($"failed to parse line ({obj.ToLingoString()})");
                    return false;
                }
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

        public partial class Prop : ILingoData
        {
            [LingoIndex(0, "nm")]
            public string name;
            [LingoIndex(1, "tp")]
            public Type type;

            [LingoIndex(98, "tags")] //these go last
            public string[] tags;
            [LingoIndex(99, "notes")] //not necessary, but just for fun
            public string[] notes;

            public static bool TryParse(object[] lingoObj, out Prop prop)
            {
                prop = null;

                object propType = null;

                Type type = Type.none;

                foreach (object obj in lingoObj)
                {
                    if (obj is KeyValuePair<string, object> kvp && kvp.Key == "tp" && kvp.Value is string s)
                    {
                        if (!Enum.TryParse(s, out type))
                        {
                            Debug.LogError($"failed to parse type");
                            Debug.LogError($"({lingoObj.ToLingoString()})");
                            return false;
                        }
                    }
                }

                if (type == Type.none)
                {
                    Debug.LogError($"failed to parse type");
                    Debug.LogError($"({lingoObj.ToLingoString()})");
                    return false;
                }

                switch (type)
                {
                    case Type.standard:
                        propType = new StandardProp();
                        break;
                    case Type.variedStandard:
                        propType = new VariedStandardProp();
                        break;
                    case Type.simpleDecal:
                        propType = new DecalProp();
                        break;
                    case Type.variedDecal:
                        propType = new VariedDecalProp();
                        break;
                    case Type.soft:
                        propType = new SoftProp();
                        break;
                    case Type.variedSoft:
                    case Type.coloredSoft:
                        propType = new SoftProp();
                        break;
                    case Type.antimatter:
                        propType = new AntiMatterProp();
                        break;
                }
                prop = (Prop)propType;

                if (SyncAllAttributes(propType, lingoObj))
                {
                    if (prop.type == Type.none)
                    {
                        Debug.LogError($"type could not be found");
                        Debug.LogError($"failed to parse line ({lingoObj.ToLingoString()})");
                    }
                    return true;
                }
                else
                {
                    Debug.LogError($"failed to parse line ({lingoObj.ToLingoString()})");
                    return false;
                }
            }

            public enum Type
            {
                none, //All: #name, type, tags, notes
                standard, //All + #colorTreatment, #bevel, #sz, #repeatL, #layerExceptions
                variedStandard, //standard + #vars, #random, #colorize
                soft, //simpleDecal + #round, #contourExp, #selfShade, #highLightBorder, #depthAffectHilites, #shadowBorder, #smoothShading
                variedSoft, //soft + #pxlSize, #vars, #random, #colorize
                coloredSoft, //soft + #pxlSize, #colorize
                simpleDecal, //All + #depth
                variedDecal, //simpleDecal + #pxlSize, #vars, #random
                antimatter //#depth, #contourExp
            }

        }

        public class StandardProp : Prop, ILingoData
        {
            [LingoIndex(2, "colorTreatment", skippable: true)]
            public ColorTreatment colorTreatment;
            [LingoIndex(3, "bevel", skippable: true)] //only read when #colorTreatment == "bevel"
            public int bevel;
            [LingoIndex(4, "sz")]
            public Vector2 size;
            [LingoIndex(5, "repeatL", skippable: true)]
            public int[] repeatL;

            //for standard - but never read?
            [LingoIndex(18, "layerExceptions", skippable: true)]
            public int[] layerExceptions;

            public enum ColorTreatment
            {
                none,
                bevel,
                standard
            }
        }

        public class DecalProp : Prop, ILingoData
        {
            [LingoIndex(6, "depth", skippable: true)]
            public int depth;
        }

        public class AntiMatterProp : DecalProp, ILingoData
        {
            [LingoIndex(8, "contourExp")]
            public float contourExp;
        }
        public class SoftProp : AntiMatterProp, ILingoData
        {
            [LingoIndex(9, "selfShade")]
            public bool selfShade;
            [LingoIndex(10, "highLightBorder")]
            public float highLightBorder;
            [LingoIndex(11, "depthAffectHilites")]
            public float depthAffectHighlights;
            [LingoIndex(12, "shadowBorder")]
            public float shadowBorder;
            [LingoIndex(13, "smoothShading")]
            public int smoothShading;
        }

        public interface IVariedProp
        {
            [LingoIndex(15, "vars", skippable: true)]
            public int variations { get; set; }

            [LingoIndex(16, "random", skippable: true)]
            public bool random { get; set; }

            [LingoIndex(17, "colorize", skippable: true)]
            public bool colorize { get; set; }

            //for variedSoft & variedDecal
            [LingoIndex(14, "pxlSize", skippable: true)]
            public Vector2 pixelSize { get; set; }

        }

        public class VariedStandardProp : StandardProp, IVariedProp, ILingoData
        {
            [LingoIndex(15, "vars", skippable: true)]
            public int variations { get; set; }

            [LingoIndex(16, "random", skippable: true)]
            public bool random { get; set; }

            [LingoIndex(17, "colorize", skippable: true)]
            public bool colorize { get; set; }

            [LingoIndex(14, "pxlSize", skippable: true)]
            public Vector2 pixelSize { get; set; }
        }

        public class VariedDecalProp : DecalProp, IVariedProp, ILingoData
        {
            public int variations { get; set; }
            public bool random { get; set; }
            public bool colorize { get; set; }
            public Vector2 pixelSize { get; set; }
        }
        public class VariedSoftProp : SoftProp, IVariedProp, ILingoData
        {
            public int variations { get; set; }
            public bool random { get; set; }
            public bool colorize { get; set; }
            public Vector2 pixelSize { get; set; }
        }
    }
}
