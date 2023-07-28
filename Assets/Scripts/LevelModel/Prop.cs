using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lingo;
using System;
using System.Linq;

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
            var data = LingoParser.ParsePropertyList(saved);

            Category = category;
            Name = data.GetString("nm");
            Type = ConvertLingoPropType(data.GetString("tp"));
            Beveled = data.TryGetString("colorTreatment", out var colorTreatment) && colorTreatment == "bevel";
            RandomVariant = data.TryGetInt("random", out var random) && random > 0;
            Variants = new Sprite[Math.Max(1, data.TryGetInt("vars", out var vars) ? vars : 1)];
            Tags = data.GetLinearList("tags").Cast<string>().ToList();
            Notes = data.GetLinearList("notes").Cast<string>().ToList();

            tileSize = data.TryGetVector2("sz", out var sz) ? Vector2Int.RoundToInt(sz) : Vector2Int.zero;
            pixelSize = data.TryGetVector2("pxlSize", out var pxlSize) ? Vector2Int.RoundToInt(pxlSize) : Vector2Int.zero;

            if (Type == PropType.Rope)
                RopeParams = new RopeParams(data);
        }

        private static PropType ConvertLingoPropType(string type)
        {
            return type switch
            {
                "standard" or "variedStandard" => PropType.Standard,
                "soft" or "variedSoft" or "coloredSoft" => PropType.Soft,
                "simpleDecal" or "variedDecal" => PropType.Decal,
                "antimatter" => PropType.Antimatter,
                "rope" => PropType.Rope,
                "long" => PropType.Long,
                _ => throw new FormatException($"Invalid prop type: {type}"),
            };
        }
    }

    public class RopeParams
    {
        public int SegmentLength;
        public int CollisionDepth;
        public float SegmentRadius;
        public float Gravity;
        public float Friction;
        public float AirFriction;
        public float Stiffness;
        public Color PreviewColor;
        public int PreviewInterval;
        public float EdgeDirection;
        public float Rigidity;
        public float SelfPush;
        public float SourcePush;

        public RopeParams(PropertyList props)
        {
            SegmentLength = props.GetInt("segmentLength");
            CollisionDepth = props.GetInt("collisionDepth");
            SegmentRadius = props.GetFloat("segRad");
            Gravity = props.GetFloat("grav");
            Friction = props.GetFloat("friction");
            AirFriction = props.GetFloat("airFric");
            Stiffness = props.GetFloat("stiff");
            PreviewColor = props.GetColor("previewColor");
            PreviewInterval = props.GetInt("previewEvery");
            EdgeDirection = props.GetFloat("edgeDirection");
            Rigidity = props.GetFloat("rigid");
            SelfPush = props.GetFloat("selfPush");
            SourcePush = props.GetFloat("sourcePush");
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

        public PropCategory(string saved)
        {
            var data = LingoParser.ParseLinearList(saved);

            Name = data.GetString(0);
            Color = data.GetColor(1);
        }
    }
}