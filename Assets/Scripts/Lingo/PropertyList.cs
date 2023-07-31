using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lingo
{
    /// <summary>
    /// A collection of keys and values parsed from Lingo data. Pairs are iterated in insertion order.
    /// </summary>
    public class PropertyList : IEnumerable<KeyValuePair<string, object>>
    {
        private readonly Dictionary<string, object> dict = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> keys = new();

        public int Count => dict.Count;
        public IReadOnlyCollection<string> Keys => keys.AsReadOnly();

        public PropertyList() { }

        public PropertyList(IEnumerable<KeyValuePair<string, object>> pairs)
        {
            foreach (var pair in pairs)
            {
                dict.Add(pair.Key, pair.Value);
                keys.Add(pair.Key);
            }
        }

        public float GetFloat(string key) => TryGet(key, out int i) ? i : Get<float>(key);
        public int GetInt(string key) => Get<int>(key);
        public string GetString(string key) => Get<string>(key);
        public Vector2 GetVector2(string key) => Get<Vector2>(key);
        public Color GetColor(string key) => Get<Color>(key);
        public LinearList GetLinearList(string key) => Get<LinearList>(key);
        public PropertyList GetPropertyList(string key) => Get<PropertyList>(key);

        public bool TryGetFloat(string key, out float value)
        {
            if(TryGet(key, out int i))
            {
                value = i;
                return true;
            }
            return TryGet(key, out value);
        }
        public bool TryGetInt(string key, out int value) => TryGet(key, out value);
        public bool TryGetString(string key, out string value) => TryGet(key, out value);
        public bool TryGetVector2(string key, out Vector2 value) => TryGet(key, out value);
        public bool TryGetColor(string key, out Color value) => TryGet(key, out value);
        public bool TryGetLinearList(string key, out LinearList value) => TryGet(key, out value);
        public bool TryGetPropertyList(string key, out PropertyList value) => TryGet(key, out value);

        public void Set(string key, float value) => SetObject(key, value);
        public void Set(string key, int value) => SetObject(key, value);
        public void Set(string key, string value) => SetObject(key, value);
        public void Set(string key, Vector2 value) => SetObject(key, value);
        public void Set(string key, Color value) => SetObject(key, value);
        public void Set(string key, LinearList value) => SetObject(key, value);
        public void Set(string key, PropertyList value) => SetObject(key, value);

        public void Clear()
        {
            dict.Clear();
            keys.Clear();
        }

        public bool ContainsKey(string key) => dict.ContainsKey(key);

        public bool Remove(string key)
        {
            if (dict.Remove(key))
            {
                for(int i = 0; i < keys.Count; i++)
                {
                    if (keys[i].Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        keys.RemoveAt(i);
                        break;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public void SetObject(string key, object obj)
        {
            if (!ContainsKey(key))
                keys.Add(key);

            dict[key] = obj;
        }

        private T Get<T>(string key)
        {
            if (!dict.TryGetValue(key, out object obj))
                throw new KeyNotFoundException($"Could not find required property: #{key}");

            if (obj is int i && i == 0 && !typeof(T).IsValueType)
                return default;

            if (obj is not T objT)
                throw new InvalidCastException($"Expected property #{key} to be {typeof(T).Name}, got {obj?.GetType().Name ?? "null"}");

            return objT;
        }

        private bool TryGet<T>(string key, out T value)
        {
            if (dict.TryGetValue(key, out object obj) && obj is T objT)
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

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (string key in keys)
            {
                yield return new(key, dict[key]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
