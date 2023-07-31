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
        public string ImageDir { get; }
        public PropType Type { get; }
        public bool Beveled { get; }
        public int Depth { get; }
        public int Variants { get; }
        public bool RandomVariant { get; }
        public List<string> Tags { get; }
        public List<string> Notes { get; }
        public RopeParams RopeParams { get; }

        private readonly Vector2Int previewSize;
        private readonly int[] repeatL;

        public Prop(string saved, PropCategory category, string imageDir) : this(LingoParser.ParsePropertyList(saved), category, imageDir)
        {
        }

        public Prop(PropertyList data, PropCategory category, string imageDir)
        {
            Category = category;
            ImageDir = imageDir;
            Name = data.GetString("nm");
            Type = ConvertLingoPropType(data.GetString("tp"));
            Beveled = data.TryGetString("colorTreatment", out var colorTreatment) && colorTreatment == "bevel";
            RandomVariant = data.TryGetInt("random", out var random) && random > 0;
            Variants = Math.Max(1, data.TryGetInt("vars", out var vars) ? vars : 1);
            Tags = data.TryGetLinearList("tags", out var tags) ? tags.Cast<string>().ToList() : new List<string>();
            Notes = data.TryGetLinearList("notes", out var notes) ? notes.Cast<string>().ToList() : new List<string>();

            if (Type == PropType.Standard)
            {
                repeatL = data.GetLinearList("repeatL").Cast<int>().ToArray();
            }
            else if (Type == PropType.Rope)
            {
                RopeParams = new RopeParams(data);
            }

            Depth = Type == PropType.Standard ? repeatL.Sum() : data.GetInt("depth");

            if (data.TryGetVector2("pxlSize", out var pxlSize))
            {
                previewSize = Vector2Int.RoundToInt(pxlSize);
            }
            else if (data.TryGetVector2("sz", out var sz))
            {
                previewSize = Vector2Int.RoundToInt(sz) * 20;
            }

            previews = new Sprite[Variants];
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

        public PropInstance Instantiate(LinearList loadData = null)
        {
            if (loadData == null)
                throw new NotImplementedException("Creating new props isn't implemented yet!");

            return Type switch
            {
                PropType.Long => new LongPropInstance(this, loadData),
                PropType.Rope => new RopePropInstance(this, loadData),
                _ => new RectPropInstance(this, loadData),
            };
        }

        private Texture2D texture;
        private Texture2D previewTexture;
        private Sprite[] previews;
        public Sprite GetPreviewSprite(int variant)
        {
            if (previews[variant] is not Sprite spr)
                previews[variant] = spr = CreatePreviewSprite(variant);

            return spr;
        }

        private Sprite CreatePreviewSprite(int variant)
        {
            // rect(prop.sz.locH*20*(v2-1), (c2-1)*prop.sz.locV*20, prop.sz.locH*20*v2, c2*prop.sz.locV*20)+rect(0,1,0,1)
            if (texture == null) LoadTexture();

            Rect rect;

            if (previewSize == Vector2Int.zero)
            {
                rect = new Rect(0f, 0f, texture.width, texture.height);
            }
            else
            {
                rect = new Rect(
                    x: variant * previewSize.x,
                    y: 0,
                    width: previewSize.x,
                    height: previewSize.y
                );
            }
            return Sprite.Create(previewTexture, rect, new Vector2(0f, 1f), 20f);
        }

        private void LoadTexture()
        {
            var path = Path.Combine(ImageDir, Name + ".png");

            var rawData = File.ReadAllBytes(path);
            texture = new Texture2D(2, 2, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point
            };
            texture.LoadImage(rawData);

            previewTexture = GeneratePreview(texture, previewSize, Variants, repeatL ?? new int[] { 0 });
        }

        private static Material propPreviewMat;
        private static Texture2D GeneratePreview(Texture2D image, Vector2Int previewSize, int vars, int[] repeatL)
        {
            if(propPreviewMat == null) propPreviewMat = new Material(Shader.Find("Custom/PropPreview"));

            int top = previewSize == Vector2Int.zero ? image.height : image.height - 1;
            if (previewSize == Vector2Int.zero)
                previewSize = new Vector2Int(image.width, image.height);


            var preview = new Texture2D(previewSize.x * vars, previewSize.y, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point
            };
            var rt = RenderTexture.GetTemporary(preview.width, preview.height, 0, RenderTextureFormat.ARGB32);

            // Clear temporary render texture
            var lastRt = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, Color.clear);

            // Copy all slices of input to the render texture
            int depth = 0;
            float srcH = image.height;
            float maxDepth = Mathf.Max(repeatL.Sum(), 1);

            for (int i = 0; i < repeatL.Length; i++)
            {
                propPreviewMat.color = new Color(1f, 1f, 1f, depth / maxDepth);
                propPreviewMat.SetVector("_SrcRect", new Vector4(0f, (top - preview.height * (i + 1)) / srcH, 1f, preview.height / srcH));
                depth += repeatL[i];

                Graphics.Blit(image, rt, propPreviewMat);
            }

            // Save result to non-render texture
            Graphics.CopyTexture(rt, preview);
            
            // Clean up
            RenderTexture.active = lastRt;
            RenderTexture.ReleaseTemporary(rt);

            return preview;
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

    public abstract class PropInstance
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
                Quad[i] = quadList.GetVector2(i) * 20f / 16f;
            }
            if (quadList.Count > 4)
                throw new ArgumentException("Prop quad may not be more than 4 points!");

            extraData = saved.GetPropertyList(4);
            settings = extraData.GetPropertyList("settings");

            CustomColor = settings.TryGetInt("color", out int color) ? color : null;
            Variation = settings.TryGetInt("variation", out int variation) ? variation - 1 : 0;
            CustomDepth = settings.TryGetInt("customDepth", out int customDepth) ? customDepth : -1;
            RenderOrder = settings.GetInt("renderorder");
            Seed = settings.GetInt("seed");
            PostEffects = settings.GetInt("renderTime") > 0;
        }
    }

    public class RectPropInstance : PropInstance
    {
        public RectPropInstance(Prop prop, LinearList saved) : base(prop, saved)
        {
            if (prop.Type == PropType.Long || prop.Type == PropType.Rope)
                throw new ArgumentException($"Cannot create a {nameof(RectPropInstance)} from a {prop.Type} prop!");
        }
    }

    public class LongPropInstance : PropInstance
    {
        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }

        public LongPropInstance(Prop prop, LinearList saved) : base(prop, saved)
        {
            if (prop.Type != PropType.Long)
                throw new ArgumentException($"Cannot create a {nameof(LongPropInstance)} from a {prop.Type} prop!");

            Start = (Quad[0] + Quad[3]) / 2f;
            End = (Quad[1] + Quad[2]) / 2f;
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
            if (prop.Type != PropType.Rope)
                throw new ArgumentException($"Cannot create a {nameof(RopePropInstance)} from a {prop.Type} prop!");

            Attachment = settings.GetInt("release") switch
            {
                -1 => RopeAttachment.Right,
                0 => RopeAttachment.Both,
                1 => RopeAttachment.Left,
                _ => throw new FormatException($"Invalid rope prop #release value: {settings.GetInt("release")}")
            };
            Thickness = settings.TryGetFloat("thickness", out float thickness) ? thickness : 0f;
            ApplyColor = settings.TryGetInt("applyColor", out int applyColor) && applyColor > 0;

            Points.AddRange(extraData.GetLinearList("points").Cast<Vector2>());
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

        public PropCategory(string name, Color color)
        {
            Name = name;
            Color = color;
        }
    }
}