using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Lingo.LingoParsing;
using UnityEngine;


namespace Lingo
{
    internal static class MiddleMan
    {

        public interface ILingoData
        {
            //public abstract object ToLingoObject(); //replaced by MiddleMan.ToLingoObjectFromAttributes
        }

        public class LingoIndexAttribute : Attribute
        {
            public int Index;
            public string Key;
            public bool AccessKey;
            public bool Nullable;
            public bool Skippable;

            public LingoIndexAttribute(int index, string key, bool nullable = false, bool skippable = false)
            {
                Index = index;
                Key = key;
                AccessKey = key != null;
                Nullable = nullable;
                Skippable = skippable;
            }
        }
        #region oldAndShouldntBeUsed
        public static object[] ToGenericOrNull<T>(T[] arr)
        {
            if (arr is null) return null;
            else { return Array.ConvertAll(arr, (o) => (object)o); }
        }

        public static bool TryCast<T>(this object obj, ref T result)
        {
            if (obj is T t)
            { result = t; return true; }
            else { return false; }
        }

        public static bool TryLValuePair<T>(this object obj, ref T result, string key)
        {
            if (obj is KeyValuePair<string, object> p && p.Key == key && p.Value is T t)
            { result = t; return true; }
            else { return false; }
        }
        public static bool TryLValuePairArray<T>(this object obj, ref T[] result, string key)
        {
            if (obj is KeyValuePair<string, object> p && p.Key == key && p.Value is object[] a)
            { try { result = Array.ConvertAll(a.ToArray(), (o) => (T)o); return true; } catch { return false; } }
            else { return false; }
        }

        public static bool TryLArray<T>(this object obj, ref T[] result)
        {
            if (obj is object[] a)
            { try { result = Array.ConvertAll(a.ToArray(), (o) => (T)o); return true; } catch { return false; } }
            else { return false; }
        }
        #endregion

        #region ToLingoObject
        public static object ToLingoObjectFromAttributes<T>(T parent) where T : class
        {
            Dictionary<LingoIndexAttribute, MemberInfo> corr = new();
            List<LingoIndexAttribute> attrl = new();

            List<object> result = new();
            foreach (MemberInfo member in GetFieldsAndProperties(parent.GetType()))
            {
                var list = member.GetCustomAttributes(typeof(LingoIndexAttribute), true);
                
                foreach (object attr in member.GetCustomAttributes(typeof(LingoIndexAttribute), true)) // There should be only one or zero anyways
                {
                    LingoIndexAttribute fieldAttr = (LingoIndexAttribute)attr;
                    attrl.Add(fieldAttr);
                    corr.Add(fieldAttr, member);
                }
            }

            attrl = attrl.OrderBy(x => x.Index).ToList();

            foreach (LingoIndexAttribute attr in attrl)
            {
                if (attr.Skippable && corr[attr].GetValue(parent) == null)
                { continue; }

                if (attr.AccessKey)
                {
                    result.Add(new KeyValuePair<string, object>(attr.Key, ConvertToLingoType(corr[attr].GetValue(parent))));
                }
                else
                {
                    result.Add(ConvertToLingoType(corr[attr].GetValue(parent)));
                }

            }

            if (result.Count == 1) return result[0];
            else { return result.ToArray(); }
        }

        /// <summary>
        /// if the object isn't a valid Lingo Type, converts it to the most appropriate
        /// </summary>
        public static object ConvertToLingoType(object obj)
        {
            switch (obj)
            {
                case int i:
                    return (float)i;

                case bool b:
                    return b? 1f : 0f;

                case float:
                case string:
                case Vector2:
                case Color:
                case Rect:
                case KeyValuePair<string, object>:
                case object[]:
                    return obj;
            }
            if (obj != null && obj.GetType().GetInterfaces().Contains(typeof(IEnumerable)))
            {
                List<object> newList = new();

                foreach (object item in (IEnumerable)obj)
                { newList.Add(ConvertToLingoType(item)); }

                return newList.ToArray();
            }

            if (obj != null && obj.GetType().IsEnum)
            {
                return obj.ToString();
            }

            return obj;
        }
        #endregion

        #region FromLingoObject
        public static bool SyncAllAttributes<T>(T parent, object[] lingos) where T : class
        {
            MemberInfo[] members = GetFieldsAndProperties(parent.GetType());
            foreach (MemberInfo member in members)
            {
                foreach (LingoIndexAttribute attr in member.GetCustomAttributes(typeof(LingoIndexAttribute), true)) // There should be only one or zero anyways
                {
                    if (attr.AccessKey)
                    {
                        if (lingos.TryGetFromKey(attr.Key, out object result))
                        {
                            member.SetAttributeFromLingo(parent, result);
                            if (member.GetValue(parent) == null && !attr.Nullable)
                            {  Debug.LogError($"Empty data assigned to ({member.Name})"); return false; }
                        }
                        else if (!attr.Skippable)
                        {  Debug.LogError($"couldn't find value ({attr.Key})"); return false; }

                    }
                    else
                    {
                        if (lingos.Length < attr.Index && !attr.Skippable) {  Debug.LogError($"not enough elements for ({attr.Key})"); return false; }
                        member.SetAttributeFromLingo(parent, members.Length != 1? lingos[attr.Index] : lingos);
                        if (member.GetValue(parent) == null && !attr.Nullable)
                        {  Debug.LogError($"Empty data assigned to ({member.Name})"); return false; }
                    }

                }
            }
            return true;
        }

        public static bool ValueConversionLegal(object lingoObj, Type cast)
        {
                if (cast == typeof(object)) return true;
            switch (lingoObj)
            {
                case string s:
                    return (cast == typeof(string)) || (cast.IsEnum && Enum.GetNames(cast).Contains(s));

                case float f:
                    return (f == 0 && (Nullable.GetUnderlyingType(cast) != null || cast.IsArray)) || cast == typeof(float) || cast == typeof(int) || cast == typeof(bool);

                case KeyValuePair<string, object>:
                    return (cast == typeof(KeyValuePair<string, object>));// || ValueConversionLegal(kvp.Value, cast) no more implicit key conversions

                case object[] arr:
                    
                    if (cast.IsArray)
                    {
                        Type arrType = cast.GetElementType();
                        foreach (object o in arr)
                        { if (!ValueConversionLegal(o, arrType)) return false; }
                        return true;
                    }
                    else if (cast.GetInterfaces().Contains(typeof(IEnumerable)))
                    {
                        Type arrType = cast.GetGenericArguments().Single();
                        foreach (object o in arr)
                        { if (!ValueConversionLegal(o, arrType)) return false; }
                        return true;
                    }
                    return cast.GetInterfaces().Contains(typeof(ILingoData));

                case null:
                    return false;

                case Vector2:
                case Color:
                case Rect:
                default:
                    return cast == lingoObj.GetType();
            }
        }

        public static bool SetAttributeFromLingo<T>(this MemberInfo member, T parent, object lingoObj) where T : class
        {
            object varr = default;
            if (SetValueFromLingo(ref varr, lingoObj, member.GetType2()))
            { member.SetValue(parent, varr); return true; }
            else {  Debug.LogError("failed"); return false; }
        }

        public static object LingoConvert(object value, Type type)
        {
            object list = default;
            if (SetValueFromLingo(ref list, value, type))
            { return list; }
            else { return null; }
        }

        public static object TryDefault(Type type)
        {
            //this is reeeeaaally slow
            try { return Activator.CreateInstance(type); }
            catch { }
            try { return Convert.ChangeType(default, type); }
            catch { }
            return null;
        }

        public static bool SetValueFromLingo<T>(ref T value, object lingoObj, Type type = null)
        {
            if (type == null){ type = typeof(T); } //type needs to be defined in cases when value is an object
            if (!ValueConversionLegal(lingoObj, type)) {  Debug.LogError($"Incorrect data for type ({type.Name}) tried to parse from ({lingoObj.ToLingoString()})"); return false; }

            try
            {
                if (type.GetInterfaces().Contains(typeof(ILingoData)) && lingoObj is object[] arr2)
                {
                    value = (T)TryDefault(type);
                    if (!SyncAllAttributes(value as ILingoData, arr2))
                    {
                        return false;
                    }
                }
                /*else if (typeof(T) == typeof(object))
                { value = (T)lingoObj; return true; }*/

                else if (type == typeof(string) || type == typeof(Vector2) || type == typeof(Color) || type == typeof(Rect) || type == typeof(KeyValuePair<string, object>))
                {
                    if (lingoObj is float f && f == 0)
                    { lingoObj = null; }
                    value = (T)lingoObj;
                }

                else if (type == typeof(float))
                { value = (T)lingoObj; }

                else if (type == typeof(int))
                { value = (T)Convert.ChangeType(lingoObj, type); }

                else if (type == typeof(bool))
                { value = (T)Convert.ChangeType((float)lingoObj == 1f, type); }

                else if (type.IsEnum)
                {
                    value = (T)Convert.ChangeType(Enum.Parse(type, (string)lingoObj), type);
                }

                else if (type.IsArray)
                {
                    if (lingoObj is float f && f == 0)
                    { value = default; }

                    else if (lingoObj is object[] arr)
                    {
                        //this is absolutely disgusting and there's got to be a better way... but it works
                        /*var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type.GetElementType()));
                        foreach (object o in arr)
                        {
                            var add = Activator.CreateInstance(type.GetElementType());
                            SetValueFromLingo(ref add, o);
                            list.Add(add);
                        }
                        var array = Array.CreateInstance(type.GetElementType(), list.Count);
                        list.CopyTo(array, 0);
                        value = (T)(object)array;*/
                        Array filledArray = Array.CreateInstance(type.GetElementType(), arr.Length);
                        Array.Copy(Array.ConvertAll(arr, v => LingoConvert(v, type.GetElementType())), filledArray, arr.Length);
                        value = (T)(object)filledArray;
                        //value = (T)Convert.ChangeType(Array.ConvertAll(arr, v => Convert.ChangeType(v, typeof(T).GetElementType())), typeof(T));
                        //value = (T)(object)(Array.ConvertAll(arr, x => { var v = Convert.ChangeType(default, typeof(T).GetElementType()); SetValueFromLingo(ref v, x); return v; }), typeof(T));
                        //value = (T)(object)Array.ConvertAll(arr, x => { var v = Convert.ChangeType(default, typeof(T).GetElementType()); SetValueFromLingo(ref v, x); return v; });
                    }
                }
                else if (type.GetInterfaces().Contains(typeof(IEnumerable)))
                {
                    if (lingoObj is float f && f == 0)
                    { value = default; }

                    else if (lingoObj is object[] arr)
                    {
                        //this is absolutely disgusting and there's got to be a better way... but it works
                        Type listType = type.GetGenericArguments().Single();
                        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(listType));
                        foreach (object obj in arr)
                        { list.Add(LingoConvert(obj, listType)); }
                        value = (T)(object)list;
                        /*var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type.GetElementType()));
                        foreach (object o in arr)
                        {
                            var add = Activator.CreateInstance(type.GetElementType());
                            SetValueFromLingo(ref add, o);
                            list.Add(add);
                        }
                        value = (T)(object)list;*/
                        //value = (T)Convert.ChangeType(Array.ConvertAll(arr, v => Convert.ChangeType(v, typeof(T).GetElementType())), typeof(T));
                        //value = (T)(object)(Array.ConvertAll(arr, x => { var v = Convert.ChangeType(default, typeof(T).GetElementType()); SetValueFromLingo(ref v, x); return v; }), typeof(T));
                        //value = (T)(object)Array.ConvertAll(arr, x => { var v = Convert.ChangeType(default, typeof(T).GetElementType()); SetValueFromLingo(ref v, x); return v; });
                    }
                }
                else if (type == typeof(object))
                { value = (T)lingoObj; }

            }
            catch (Exception e)
            {
                 Debug.LogError($"exception in SetValueFromLingo while parsing field ({value}), {lingoObj.ToLingoString()}");
                 Debug.LogError(e.ToString());
                return false;
            }
            return true;
        }
        #endregion

        #region MemberInfo Generalizers
        public static MemberInfo[] GetFieldsAndProperties(this Type type)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            MemberInfo[] members = type.GetFields(flags);
            return members.Concat(type.GetProperties(flags)).ToArray();
        }
        private static Type GetListType<T>(IEnumerable<T> list)
        {
            return typeof(T);
        }
        // some logic borrowed from James Newton-King, http://www.newtonsoft.com
        public static void SetValue(this MemberInfo member, object property, object value)
        {
            if (member.MemberType == MemberTypes.Property)
                ((PropertyInfo)member).SetValue(property, value, null);
            else if (member.MemberType == MemberTypes.Field)
                ((FieldInfo)member).SetValue(property, value);
            else
                throw new Exception("Property must be of type FieldInfo or PropertyInfo");
        }

        public static object GetValue(this MemberInfo member, object property)
        {
            if (member.MemberType == MemberTypes.Property)
                return ((PropertyInfo)member).GetValue(property, null);
            else if (member.MemberType == MemberTypes.Field)
                return ((FieldInfo)member).GetValue(property);
            else
                throw new Exception("Property must be of type FieldInfo or PropertyInfo");
        }

        public static Type GetType2(this MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                default:
                    throw new ArgumentException("MemberInfo must be if type FieldInfo or PropertyInfo", "member");
            }
        }
        #endregion
    }
}
