using Lingo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace LevelModel
{
    /// <summary>
    /// Represents a type of effect.
    /// </summary>
    public class Effect
    {
        public EffectCategory Category { get; }
        public string Name { get; }
        public Option[] Options { get; }
        public RangeOption[] IntOptions { get; } // Only used by "Ivy" effect

        private readonly PropertyList copyVerbatim;

        public Effect(string saved, EffectCategory category)
        {
            var data = LingoParser.ParsePropertyList(saved);

            Category = category;
            Name = data.GetString("nm");

            copyVerbatim = new PropertyList();
            foreach(var pair in data)
            {
                if (pair.Key == "options") continue;
                
                copyVerbatim.SetObject(pair.Key, pair.Value);
            }

            if (!copyVerbatim.ContainsKey("crossScreen"))
                copyVerbatim.Set("crossScreen", 0);

            if(data.TryGetLinearList("options", out var opts))
            {
                Options = new Option[opts.Count];
                for(int i = 0; i < opts.Count; i++)
                    Options[i] = new Option(opts.GetLinearList(i));
            }
            else
            {
                Options = new Option[0];
            }

            if(data.TryGetLinearList("intOptions", out opts))
            {
                IntOptions = new RangeOption[opts.Count];
                for(int i = 0; i < opts.Count; i++)
                    IntOptions[i] = new RangeOption(opts.GetLinearList(i));
            }
            else
            {
                IntOptions = new RangeOption[0];
            }
        }

        public EffectInstance Instantiate(Vector2Int size, PropertyList loadData = null)
        {
            if(loadData == null)
            {
                loadData = new PropertyList();

                // Generate empty tile matrix
                var matrix = new LinearList();
                matrix.Capacity = size.x;

                var emptyColumn = new object[size.y];
                Array.Fill(emptyColumn, 0);

                for(int x = 0; x < size.x; x++)
                {
                    matrix.Add(new LinearList(emptyColumn));
                }

                // Set up options
                var setOptions = LinearList.Make(
                    LinearList.Make("Delete/Move", LinearList.Make("Delete", "Move Back", "Move Forth"), "")
                );

                foreach(var option in Options)
                {
                    setOptions.Add(LinearList.Make(option.Name, new LinearList(option.Options), option.Default));
                }

                foreach(var intOption in IntOptions)
                {
                    setOptions.Add(LinearList.Make(intOption.Name, new LinearList(), intOption.Default));
                }

                setOptions.Add(LinearList.Make("Seed", new LinearList(), UnityEngine.Random.Range(1, 500)));

                // Try to stay consistent with the official editor's ordering
                loadData.SetObject("nm", null);
                loadData.SetObject("tp", null);
                loadData.Set("mtrx", matrix);
                loadData.Set("options", setOptions);

                foreach (var pair in copyVerbatim)
                {
                    loadData.SetObject(pair.Key, pair.Value);
                }
            }

            return new EffectInstance(this, loadData);
        }

        public class Option
        {
            public string Name { get; }
            public string[] Options { get; }
            public string Default { get; }

            public Option(LinearList data)
            {
                Name = data.GetString(0);
                Options = data.GetLinearList(1).Cast<string>().ToArray();
                Default = data.GetString(2);
            }
        }

        public class RangeOption
        {
            public string Name { get; }
            public int Min { get; }
            public int Max { get; }
            public int Default { get; }

            public RangeOption(LinearList data)
            {
                Name = data.GetString(0);
                Min = data.GetInt(1);
                Max = data.GetInt(2);
                Default = data.GetInt(3);
            }
        }
    }

    /// <summary>
    /// A layer of an effect applied to a level.
    /// </summary>
    public class EffectInstance
    {
        public Effect Effect { get; }
        public int Seed
        {
            get => GetIntOption("Seed");
            set => SetIntOption("Seed", value);
        }

        private readonly PropertyList data;
        private Vector2Int size;
        private float[] amounts;

        public EffectInstance(Effect effect, PropertyList data)
        {
            Effect = effect;

            this.data = data;

            var matrix = data.GetLinearList("mtrx");
            size = new Vector2Int(matrix.Count, matrix.GetLinearList(0).Count);

            amounts = new float[size.x * size.y];
            for(int x = 0; x < size.x; x++)
            {
                var column = matrix.GetLinearList(x);

                for(int y = 0; y < size.y; y++)
                {
                    amounts[x + y * size.x] = column.GetFloat(y) / 100f;
                }
            }

            // Save some memory by unloading the matrix
            data.SetObject("mtrx", LingoParser.Placeholder);
        }

        public float GetAmount(Vector2Int pos)
        {
            if (pos.x < 0 || pos.y < 0 || pos.x >= size.x || pos.y >= size.y) return 0f;

            return amounts[pos.x + pos.y * size.x];
        }

        public void SetAmount(Vector2Int pos, float amount)
        {
            if (pos.x < 0 || pos.y < 0 || pos.x >= size.x || pos.y >= size.y) return;

            amounts[pos.x + pos.y * size.x] = amount;
        }

        public string GetOption(string name) => FindOption(name).GetString(2);
        public void SetOption(string name, string value) => FindOption(name)[2] = value;

        public int GetIntOption(string name) => FindOption(name).GetInt(2);
        public void SetIntOption(string name, int value) => FindOption(name)[2] = value;

        public void Resize(Vector2Int size, Vector2Int offset)
        {
            Utils.Resize3DArray(ref amounts, this.size, size, offset, 1);
            this.size = size;
        }

        public PropertyList Save()
        {
            int w = size.x;
            int h = size.y;
            var matrix = new LinearList { Capacity = w };

            for (int x = 0; x < w; x++)
            {
                var column = new LinearList { Capacity = h };

                for (int y = 0; y < h; y++)
                {
                    float amount = amounts[x + y * w];

                    if (amount == 0f) column.Add(0);
                    else if (amount == 1f) column.Add(100);
                    else column.Add(amount * 100f);
                }

                matrix.Add(column);
            }

            var copy = data.DeepClone();
            copy.Set("mtrx", matrix);
            return copy;
        }

        private LinearList FindOption(string name)
        {
            var myOptions = data.GetLinearList("options");
            for (int i = 0; i < myOptions.Count; i++)
            {
                if(myOptions.TryGetLinearList(i, out var opt) && opt.TryGetString(0, out var optName) && name == optName)
                {
                    return opt;
                }
            }

            // Add default value, if it doesn't exist
            var strOptions = Effect.Options;
            foreach(var parentOption in strOptions)
            {
                if (parentOption.Name == name)
                {
                    myOptions.Add(LinearList.Make(parentOption.Name, new LinearList(parentOption.Options), parentOption.Default));
                    return myOptions.GetLinearList(myOptions.Count - 1);
                }
            }

            var intOptions = Effect.IntOptions;
            foreach(var parentOption in intOptions)
            {
                if (parentOption.Name == name)
                {
                    myOptions.Add(LinearList.Make(parentOption.Name, new LinearList(), parentOption.Default));
                    return myOptions.GetLinearList(myOptions.Count - 1);
                }
            }

            throw new KeyNotFoundException($"Couldn't find effect option: {name}");
        }
    }

    /// <summary>
    /// A group of effect types.
    /// </summary>
    public class EffectCategory
    {
        public string Name;
        public List<Effect> Effects = new();

        public EffectCategory(string saved)
        {
            var data = LingoParser.ParseLinearList(saved);

            Name = data.GetString(0);
        }
    }
}
