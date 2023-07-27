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

        bool HasFeature(FeatureFlags feature) => throw new NotImplementedException();
        void SetFeature(FeatureFlags feature, bool state) => throw new NotImplementedException();
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
        None = 0x0000,
        HorizontalBeam = 0x0001,
        VerticalBeam = 0x0002,
        Hive = 0x0004,
        ShortcutDot = 0x0008,
        DragonDen = 0x0010,
        Rock = 0x0020,
        Spear = 0x0040,
        Crack = 0x0080,
        ForbidBats = 0x0100,
        GarbageHole = 0x0200,
        Waterfall = 0x0400,
        WhackAMoleHole = 0x0800,
        WormGrass = 0x1000,
        ScavengerHole = 0x2000
    }
}
