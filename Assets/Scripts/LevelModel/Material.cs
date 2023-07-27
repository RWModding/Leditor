using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LevelModel
{
    public class TileMaterial : VisualCell
    {
        public readonly string Name;
        public readonly Color Color;

        public TileMaterial(string saved)
        {
            var args = saved.Split(',');

            Name = args[0];
            Color = new Color(
                float.Parse(args[2]) / 255f,
                float.Parse(args[3]) / 255f,
                float.Parse(args[4]) / 255f
            );
        }
    }
}
