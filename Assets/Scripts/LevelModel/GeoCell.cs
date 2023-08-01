using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelModel
{
    // The contents of a geometry cell
    public struct GeoCell
    {
        public GeoType terrain;
        public FeatureFlags features;

        public GeoCell(GeoType terrain, FeatureFlags features)
        {
            this.terrain = terrain;
            this.features = features;
        }

        public bool HasFeature(FeatureFlags feature) => features.HasFlag(feature);
        public void SetFeature(FeatureFlags feature, bool state)
        {
            if (state)
                features &= ~feature;
            else
                features |= feature;
        }
    }

    public enum GeoType : byte
    {
        Air = 0,
        Solid = 1,
        BLSlope = 2,
        BRSlope = 3,
        TLSlope = 4,
        TRSlope = 5,
        Platform = 6,
        ShortcutEntrance = 7,
        GlassWall = 9
    }

    [Flags]
    public enum FeatureFlags
    {
        None           = 0x000000,
        HorizontalBeam = 0x000001,
        VerticalBeam   = 0x000002,
        Hive           = 0x000004,
        ShortcutDot    = 0x000010,
        Entrance       = 0x000020,
        DragonDen      = 0x000040,
        Rock           = 0x000100,
        Spear          = 0x000200,
        Crack          = 0x000400,
        ForbidBats     = 0x000800,
        GarbageHole    = 0x010000,
        Waterfall      = 0x020000,
        WhackAMoleHole = 0x040000,
        WormGrass      = 0x080000,
        ScavengerHole  = 0x100000,
    }
}
