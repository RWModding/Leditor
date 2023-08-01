using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lingo
{
    /// <summary>
    /// A collection of items parsed from Lingo data.
    /// </summary>
    public class LinearList : List<object>
    {
        public LinearList() { }
        public LinearList(IEnumerable<object> collection) : base(collection) { }

        public static LinearList Make(params object[] items) => new LinearList(items);

        public float GetFloat(int key) => TryGet(key, out int i) ? i : Get<float>(key);
        public int GetInt(int key) => Get<int>(key);
        public string GetString(int key) => Get<string>(key);
        public Vector2 GetVector2(int key) => Get<Vector2>(key);
        public Color GetColor(int key) => Get<Color>(key);
        public LinearList GetLinearList(int key) => Get<LinearList>(key);
        public PropertyList GetPropertyList(int key) => Get<PropertyList>(key);

        public bool TryGetFloat(int key, out float value)
        {
            if (TryGet(key, out int i))
            {
                value = i;
                return true;
            }
            return TryGet(key, out value);
        }
        public bool TryGetInt(int key, out int value) => TryGet(key, out value);
        public bool TryGetString(int key, out string value) => TryGet(key, out value);
        public bool TryGetVector2(int key, out Vector2 value) => TryGet(key, out value);
        public bool TryGetColor(int key, out Color value) => TryGet(key, out value);
        public bool TryGetLinearList(int key, out LinearList value) => TryGet(key, out value);
        public bool TryGetPropertyList(int key, out PropertyList value) => TryGet(key, out value);

        public LinearList DeepClone()
        {
            var copy = new LinearList();
            for (int i = 0; i < Count; i++)
            {
                var value = this[i];
                if (value is PropertyList valuePropList) value = valuePropList.DeepClone();
                else if (value is LinearList valueLinearList) value = valueLinearList.DeepClone();
                copy.Add(value);
            }

            return copy;
        }

        private T Get<T>(int key)
        {
            if (key < 0 || key >= Count)
                throw new IndexOutOfRangeException($"Index {key} is out of range!");

            if (this[key] is int i && i == 0 && !typeof(T).IsValueType)
                return default;

            if (this[key] is not T objT)
                throw new InvalidCastException($"Expected value at index {key} to be {typeof(T).Name}, got {this[key]?.GetType().Name ?? "null"}");

            return objT;
        }

        private bool TryGet<T>(int key, out T value)
        {
            if (key >= 0 && key < Count && this[key] is T objT)
            {
                value = objT;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }
}
