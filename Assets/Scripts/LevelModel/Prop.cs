using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lingo;
using static Lingo.MiddleMan;
using System;

namespace LevelModel
{
    public class Prop
    {
        public PropCategory Category { get; }
        public string Name { get; }
        public PropType Type { get; }
        public bool Beveled { get; }
        public int Depth { get; }
        public Sprite[] Variants { get; }
        public bool RandomVariant { get; }
        public List<string> Tags { get; }
        public List<string> Notes { get; }
        public RopeParams RopeParams { get; }

        private Vector2Int tileSize;
        private Vector2Int pixelSize;

        public Prop(string saved, PropCategory category)
        {
            if (LingoParsing.FromLingoString(saved) is not object[] arr)
                throw new FormatException("Expected an array!");

            var data = new LProp(arr);

            Category = category;
            Name = data.name;
            Type = ConvertLingoPropType(data.type);
            Beveled = data.colorTreatment == "bevel";
            RandomVariant = data.random;
            Variants = new Sprite[Math.Max(1, data.variations)];
            Tags = new List<string>(data.tags);
            Notes = new List<string>(data.tags);

            tileSize = Vector2Int.RoundToInt(data.size);
            pixelSize = Vector2Int.RoundToInt(data.pixelSize);

            if (Type == PropType.Rope)
                RopeParams = new RopeParams(arr);
        }

        private static PropType ConvertLingoPropType(LProp.Type type)
        {
            return type switch
            {
                LProp.Type.standard or LProp.Type.variedStandard => PropType.Standard,
                LProp.Type.soft or LProp.Type.variedSoft or LProp.Type.coloredSoft => PropType.Soft,
                LProp.Type.simpleDecal or LProp.Type.variedDecal => PropType.Decal,
                LProp.Type.antimatter => PropType.Antimatter,
                LProp.Type.rope => PropType.Rope,
                LProp.Type.@long => PropType.Long,
                _ => throw new FormatException($"Invalid prop type: {type}"),
            };
        }

        private class LProp : ILingoData
        {
            [LingoIndex(0, "nm")]
            public string name;

            [LingoIndex(1, "tp")]
            public Type type;

            [LingoIndex(2, "colorTreatment", skippable: true)]
            public string colorTreatment;

            [LingoIndex(3, "bevel", skippable: true)] //only read when #colorTreatment == "bevel"
            public int bevel;

            [LingoIndex(4, "sz", skippable: true)]
            public Vector2 size;

            [LingoIndex(5, "repeatL", skippable: true)]
            public int[] repeatL;

            [LingoIndex(6, "depth", skippable: true)]
            public int depth;

            [LingoIndex(8, "contourExp", skippable: true)]
            public float contourExp;

            [LingoIndex(9, "selfShade", skippable: true)]
            public bool selfShade;

            [LingoIndex(10, "highLightBorder", skippable: true)]
            public float highLightBorder;

            [LingoIndex(11, "depthAffectHilites", skippable: true)]
            public float depthAffectHighlights;

            [LingoIndex(12, "shadowBorder", skippable: true)]
            public float shadowBorder;

            [LingoIndex(13, "smoothShading", skippable: true)]
            public int smoothShading;

            //for variedSoft & variedDecal
            [LingoIndex(14, "pxlSize", skippable: true)]
            public Vector2 pixelSize;

            [LingoIndex(15, "vars", skippable: true)]
            public int variations;

            [LingoIndex(16, "random", skippable: true)]
            public bool random;

            [LingoIndex(17, "colorize", skippable: true)]
            public bool colorize;

            [LingoIndex(18, "layerExceptions", skippable: true)]
            public int[] layerExceptions;

            [LingoIndex(98, "tags")] //these go last
            public string[] tags;

            [LingoIndex(99, "notes")] //not necessary, but just for fun
            public string[] notes;

            public LProp(object[] saved)
            {
                if (!SyncAllAttributes(this, saved))
                    throw new FormatException("Failed to parse prop data!");
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
                antimatter, //All + #depth, #contourExp
                @long, //All + #depth
                rope //long + segmentLength, collisionDepth, segRad, grav, friction, airFric, stiff, previewColor, previewEvery, edgeDirection, rigid, selfPush, sourcePush
            }
        }
    }

    public class RopeParams
    {
        [LingoIndex(5, "segmentLength")]
        public int SegmentLength;

        [LingoIndex(6, "collisionDepth")]
        public int CollisionDepth;

        [LingoIndex(7, "segRad")]
        public float SegmentRadius;

        [LingoIndex(8, "grav")]
        public float Gravity;

        [LingoIndex(9, "friction")]
        public float Friction;

        [LingoIndex(10, "airFric")]
        public float AirFriction;

        [LingoIndex(11, "stiff")]
        public float Stiffness;

        [LingoIndex(12, "previewColor")]
        public Color PreviewColor;

        [LingoIndex(13, "previewEvery")]
        public int PreviewInterval;

        [LingoIndex(14, "edgeDirection")]
        public float EdgeDirection;

        [LingoIndex(15, "rigid")]
        public float Rigidity;

        [LingoIndex(16, "selfPush")]
        public float SelfPush;

        [LingoIndex(17, "sourcePush")]
        public float SourcePush;

        public RopeParams(object[] saved)
        {
            if (!SyncAllAttributes(this, saved))
                throw new FormatException("Failed to parse rope prop params!");
        }
    }

    public enum PropType
    {
        Standard,
        Decal,
        Soft,
        Antimatter,
        Rope,
        Long
    }

    public class PropInstance
    {
        public Prop Type { get; }
        public Vector2[] Quad { get; }
        public int Depth { get; set; }
        public int Variation { get; set; }
        public int RenderPriority { get; set; }
        public bool PostEffects { get; set; }
    }

    public class LongPropInstance : PropInstance
    {
        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }
    }

    public class RopePropInstance : PropInstance
    {
        public List<Vector2> Points { get; } = new();
    }

    /// <summary>
    /// A group of prop types.
    /// </summary>
    public class PropCategory
    {
        public string Name { get; }
        public Color Color { get; }
        public List<Prop> Props { get; } = new();

        private readonly LCategory data;

        public PropCategory(string saved)
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