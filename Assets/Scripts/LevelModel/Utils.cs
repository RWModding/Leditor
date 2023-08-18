using System;
using UnityEngine;

namespace LevelModel
{
    internal static  class Utils
    {
        public static void Resize3DArray<T>(ref T[] array, Vector2Int oldSize, Vector2Int newSize, Vector2Int offset, int depth)
        {
            var dst = new T[newSize.x * newSize.y * depth];

            for (int z = 0; z < depth; z++)
            {
                int srcZOffset = z * oldSize.x * oldSize.y;
                int dstZOffset = z * newSize.x * newSize.y;
                int srcMinY = Math.Max(0, -offset.y);
                int srcMaxY = Math.Min(oldSize.y, newSize.y - offset.y);
                for (int y = srcMinY; y < srcMaxY; y++)
                {
                    int srcMinX = Math.Max(0, -offset.x);
                    int srcMaxX = Math.Min(oldSize.x, newSize.x - offset.x);

                    Array.Copy(
                        sourceArray: array,
                        sourceIndex: srcMinX + y * oldSize.y + srcZOffset,
                        destinationArray: dst,
                        destinationIndex: srcMinX + offset.x + (y + offset.y) * newSize.y + dstZOffset,
                        length: srcMaxX - srcMinX
                    );
                }
            }

            array = dst;
        }
    }
}
