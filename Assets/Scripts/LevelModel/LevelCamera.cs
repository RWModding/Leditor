using Lingo;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelModel
{
    public class LevelCamera
    {
        public const float MaxOffsetDistance = 4f * 20f;
        public static readonly Vector2 Size = new Vector2(70f, 40f) * 20f;

        public Vector2 Center { get; set; }
        public Vector2[] CornerOffsets { get; }

        public LevelCamera(Vector2 topLeftAnchor, LinearList quad = null)
        {
            Center = topLeftAnchor + Size / 2f;
            CornerOffsets = new Vector2[4];

            if (quad != null)
            {
                for(int i = 0; i < 4; i++)
                {
                    var quadPoint = quad.GetLinearList(i);

                    // Degrees clockwise from straight up
                    var rad = quadPoint.GetFloat(0) * Mathf.Deg2Rad;
                    // Offset between 0 and 4 tiles
                    var dist = quadPoint.GetFloat(1) * MaxOffsetDistance;
                    CornerOffsets[i] = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * dist;
                }
            }
        }

        public Vector2 GetStretchedCorner(int index, int depth)
        {
            // Layer 1 is stretched by at most -18.75 pixels
            // Layer 27 is stretched by about the amount the camera editor shows
            // Layer 30 is stretched by at most 90 pixels

            float fac = (depth - 5) * 1.5f;
            Vector2 corner = index switch
            {
                0 => new Vector2(-1f, -1f),
                1 => new Vector2(1f, -1f),
                2 => new Vector2(1f, 1f),
                3 => new Vector2(-1f, 1f),
                _ => throw new IndexOutOfRangeException("Camera corner index must be between 0 and 3!")
            } * Size / 2f + Center;

            return corner + CornerOffsets[index] / MaxOffsetDistance * fac * 2.5f;
        }
    }
}
