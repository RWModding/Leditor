using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lingo;
using System;
using System.Linq;
using System.IO;

namespace LevelModel
{
    public class Prop
    {
        public PropCategory Category { get; }
        public string Name { get; }
        public PropType Type { get; }
        public bool Beveled { get; }
        public int Depth { get; }
        public bool RandomVariant { get; }
        public List<string> Tags { get; }
        public List<string> Notes { get; }
        public RopeParams RopeParams { get; }

        private readonly Vector2Int previewSize;
        private readonly int variantCount;

        public Prop(string saved, PropCategory category)
        {
            var data = LingoParser.ParsePropertyList(saved);

            Category = category;
            Name = data.GetString("nm");
            Type = ConvertLingoPropType(data.GetString("tp"));
            Beveled = data.TryGetString("colorTreatment", out var colorTreatment) && colorTreatment == "bevel";
            RandomVariant = data.TryGetInt("random", out var random) && random > 0;
            variantCount = Math.Max(1, data.TryGetInt("vars", out var vars) ? vars : 1);
            Tags = data.GetLinearList("tags").Cast<string>().ToList();
            Notes = data.GetLinearList("notes").Cast<string>().ToList();

            if (data.TryGetVector2("pxlSize", out var pxlSize))
            {
                previewSize = Vector2Int.RoundToInt(pxlSize);
            }
            else if (data.TryGetVector2("sz", out var sz))
            {
                previewSize = Vector2Int.RoundToInt(sz) * 20;
            }

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

        private Texture2D texture;
        private Sprite[] previews;
        private Sprite GetPreviewSprite(int variant)
        {
            texture ??= LoadTexture();

            // rect(prop.sz.locH*20*(v2-1), (c2-1)*prop.sz.locV*20, prop.sz.locH*20*v2, c2*prop.sz.locV*20)+rect(0,1,0,1)
            throw new NotImplementedException();
        }

        private Texture2D LoadTexture()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "Props", Name + ".png");

            var rawData = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            tex.LoadImage(rawData);

            return tex;
            // TODO: Is cropping to content necessary?
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
        public Prop Prop { get; }
        public Vector2[] Quad { get; }
        public int Depth { get; set; }
        public int Variation { get; set; }
        public int RenderOrder { get; set; }
        public bool PostEffects { get; set; }
        public int? CustomColor { get; set; }
        public int CustomDepth { get; set; }
        public int Seed { get; set; }

        protected readonly PropertyList extraData;
        protected readonly PropertyList settings;

        public PropInstance(Prop prop, LinearList saved)
        {
            Prop = prop;

            Depth = -saved.GetInt(0);
            Quad = new Vector2[4];

            var quadList = saved.GetLinearList(3);
            for(int i = 0; i < 4; i++)
            {
                Quad[i] = quadList.GetVector2(i);
            }
            if (quadList.Count > 4)
                throw new ArgumentException("Prop quad may not be more than 4 points!");

            extraData = saved.GetPropertyList(4);
            settings = extraData.GetPropertyList("settings");

            CustomColor = settings.TryGetInt("color", out int color) ? color : null;
            Variation = settings.TryGetInt("variation", out int variation) ? variation - 1 : 0;
            CustomDepth = settings.TryGetInt("customDepth", out int customDepth) ? customDepth : -1;
            RenderOrder = settings.GetInt("renderOrder");
            Seed = settings.GetInt("seed");
            PostEffects = settings.GetInt("renderTime") > 0;
        }
    }

    public class LongPropInstance : PropInstance
    {
        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }

        public LongPropInstance(Prop prop, LinearList saved) : base(prop, saved)
        {
        }
    }

    public class RopePropInstance : PropInstance
    {
        public float Thickness { get; set; }
        public RopeAttachment Attachment { get; set; }
        public bool ApplyColor { get; set; }
        public List<Vector2> Points { get; } = new();

        public RopePropInstance(Prop prop, LinearList saved) : base(prop, saved)
        {
            Attachment = settings.GetInt("release") switch
            {
                -1 => RopeAttachment.Right,
                0 => RopeAttachment.Both,
                1 => RopeAttachment.Left,
                _ => throw new FormatException($"Invalid rope prop #release value: {settings.GetInt("release")}")
            };
            Thickness = settings.TryGetFloat("thickness", out float thickness) ? thickness : 0f;
            ApplyColor = settings.TryGetInt("applyColor", out int applyColor) && applyColor > 0;
        }
    }

    public enum RopeAttachment
    {
        Both,
        Left,
        Right
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