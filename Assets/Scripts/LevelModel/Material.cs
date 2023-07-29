using Lingo;
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
            var data = LingoParser.ParsePropertyList(saved);

            Name = data.GetString("nm");
            Color = data.GetColor("color");
        }
    }
}
