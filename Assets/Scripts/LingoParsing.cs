using System;
using System.Collections.Generic;
using System.Linq;
using static Lingo.MiddleMan;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace Lingo
{

    /// Current valid types are Float, String, Vector2, Color, KeyValuePair<string, object>, & Array
    /// Lingo also has a Vector4, should be really easy to add (maybe generalize VectorN?)
    /// 
    /// At this stage, Float must represent every number, due to the possibilities of ambiguity
    /// it can always be cast to something else more appropriate later
    public static class LingoParsing
    {
        public static string ToLingoString(this object obj)
        {
            //string format = "{0:#########0.####}";
            switch (obj)
            {
                case string s: return ToLingoFromString(s);
                case float f: return LF(f);
                case Vector2 v: return $"point({v.x}, {LF(v.y)})";
                case Color c: return $"color({LF(c.r)}, {LF(c.g)}, {LF(c.b)})";
                case Rect r: return $"rect({LF(r.xMin)}, {LF(r.yMin)}, {LF(r.xMax)}, {LF(r.yMax)})";
                case KeyValuePair<string, object> kvp: return $"#{kvp.Key}: {kvp.Value.ToLingoString()}";
                case object[] o: return $"[{string.Join(", ", o.Select(x => x.ToLingoString()))}]";
                case null: return "0";
                case ILingoData:
                    return ToLingoObjectFromAttributes(obj).ToLingoString();
                default: return obj.ToString();
            }
        }

        /// <summary>
        ///scientific notation breaks Lingo, so can't save any numbers that have too many digits
        ///this format for some reason applies some generous rounding to large numbers
        ///but that's okay, numbers that large are usually runtime numbers that don't matter
        /// </summary>
        public static string LF(float number) => string.Format("{0:#########0.####}", number);

        public static object FromLingoString(string str)
        {
            str = str.Trim();

            if (string.IsNullOrEmpty(str) || str.StartsWith("--")) //-- is for lingo comments
            { return null; }

            else if (TryStringFromLingo(str, out string s))
            { return s; }

            else if (float.TryParse(str, out float num))
            { return num; }

            else if (TryVector2FromLingo(str, out Vector2 vec))
            { return vec; }

            else if (TryColorFromLingo(str, out Color color))
            { return color; }

            else if (TryRectFromLingo(str, out Rect rect))
            { return rect; }

            else if (TryPairFromLingo(str, out KeyValuePair<string, object> pair))
            { return pair; }

            else if (str[0] == '[' && str[str.Length - 1] == ']')
            { return ArrayFromLingo(str.Substring(1, str.Length - 2)); }

            else
            { return 0f; }// yes, unrecognized is parsed as null
        }

        public static string ToLingoFromString(string str)
        {
            //curse you Wrayk, for being the only person to use Lingo escape characters
            string pattern = "\"";
            str = Regex.Replace(str, pattern, "\" & QUOTE & \"");
            pattern = "\b";
            str = Regex.Replace(str, pattern, "\" & BACKSPACE & \"");
            pattern = "\n";
            str = Regex.Replace(str, pattern, "\" & ENTER & \"");
            pattern = Math.PI.ToString();
            str = Regex.Replace(str, pattern, "\" & PI & \"");
            pattern = "\r";
            str = Regex.Replace(str, pattern, "\" & RETURN & \"");
            pattern = " ";
            //str = Regex.Replace(str, "(\\ )*", "\"SPACE & \"");
            pattern = "\t";
            str = Regex.Replace(str, pattern, "\" & TAB & \"");


            if (str.StartsWith("\" & ")) str = str.Substring(4);
            else str = "\"" + str;

            if (str.EndsWith(" & \"")) str = str.Substring(0, str.Length - 4);
            else str = str + "\"";



            pattern = " & \"\" & ";
            str = Regex.Replace(str, pattern, " & ");
            return $"{str}";
        }

        public static bool TryStringFromLingo(string str, out string lstr)
        {
            lstr = "";
            if (string.IsNullOrEmpty(str)) return false;

            string[] array = RecursiveParsing(str, '&', new char[] { '"' });
            //string[] array = str.Split('&');
            foreach (string v in array)
            {
                string s = v.Trim();
                string check = lstr;
                lstr += s switch
                {
                    "BACKSPACE" => "\b",
                    "EMPTY" => "\\e",
                    "ENTER" => "\n",
                    "PI" => Math.PI.ToString(), //this technically could be used as a float as well... but nobody will, right?
                    "QUOTE" => "\"",
                    "RETURN" => "\r",
                    "SPACE" => " ",
                    "TAB" => "\t",
                    _ when (s[0] == '"' && s[s.Length - 1] == '"') => s.Substring(1, s.Length - 2),
                    _ => ""
                };
                if (check == lstr) return false; //if it hasn't changed, it's not a string
            }
            if (string.IsNullOrEmpty(lstr)) return false;
            lstr = Regex.Replace(lstr, "\\e", "");
            return true;
        }

        public static bool TryVector2FromLingo(string str, out Vector2 vec)
        {
            vec = new Vector2();
            if (!str.StartsWith("point")) return false;
            str = str.Substring(5).Trim();

            if (str[0] != '(' || str[str.Length - 1] != ')') return false;
            str = str.Substring(1, str.Length - 2);

            string[] array = str.Split(',');
            if (array.Length != 2) return false;

            array[0] = array[0].Trim();
            array[1] = array[1].Trim();

            return (float.TryParse(array[0], out vec.x) && float.TryParse(array[1], out vec.y));
        }

        public static bool TryColorFromLingo(string str, out Color color)
        {
            color = Color.black;
            if (!str.StartsWith("color")) return false;
            str = str.Substring(5).Trim();

            if (str[0] != '(' || str[str.Length - 1] != ')') return false;
            str = str.Substring(1, str.Length - 2);

            string[] array = str.Split(',');
            if (array.Length != 3) return false;

            array[0] = array[0].Trim();
            array[1] = array[1].Trim();
            array[2] = array[2].Trim();

            return (float.TryParse(array[0], out color.r) && float.TryParse(array[1], out color.g) && float.TryParse(array[2], out color.b));
        }

        public static bool TryRectFromLingo(string str, out Rect rect)
        {
            rect = new Rect();
            if (!str.StartsWith("rect")) return false;
            str = str.Substring(4).Trim();

            if (str[0] != '(' || str[str.Length - 1] != ')') return false;
            str = str.Substring(1, str.Length - 2);

            string[] array = str.Split(',');
            if (array.Length != 4) return false;

            array[0] = array[0].Trim();
            array[1] = array[1].Trim();
            array[2] = array[2].Trim();
            array[3] = array[3].Trim();

            if (float.TryParse(array[0], out float left) && float.TryParse(array[1], out float top) && float.TryParse(array[2], out float right) && float.TryParse(array[3], out float bottom))
            { rect.xMin = left; rect.yMin = top; rect.xMax = right; rect.yMax = bottom; return true; }
            else { return false; }
        }

        public static bool TryPairFromLingo(string str, out KeyValuePair<string, object> pair)
        {
            pair = new();
            if (str[0] != '#' || !str.Contains(':')) return false;

            str = str.Substring(1);
            string[] array = new string[0];

            int i = 0;
            while (i < str.Length)
            {
                if (str[i] == ':')
                {
                    array = new string[2]
                        {
                            str.Substring(0, i).Trim(),
                            str.Substring(i + 1).Trim()
                        };
                    break;
                }
                else if (str[i] == '[' || str[i] == '(' || str[i] == ']' || str[i] == ')')
                { return false; }
                i++;
            }

            if (array.Length != 2) return false;

            object obj = FromLingoString(array[1]);
            if (obj == null) return false;

            pair = new KeyValuePair<string, object>(array[0], obj);
            return true;
        }

        public static object[] ArrayFromLingo(string str)
        {
            List<object> result = new();

            foreach (string s in RecursiveParsing(str, ',', new char[] { '(', '[' }, new char[] { ')', ']' }))
            {
                result.Add(FromLingoString(s));
            }

            return result.ToArray();
        }

        public static string[] RecursiveParsing(string str, char split, char[] nestDown, char[] nestUp = null)
        {
            List<string> result = new();

            int nextIndex = 0;

            int lastComma = 0;

            int nest = 0; //this is Bad Code - early versions would only need to read each character once
            //but now it only reads each section after it's finished scrubbing over the entire thing
            //which means nested arrays are read as many times as they are nested

            while (nextIndex < str.Length)
            {
                if (str[nextIndex] == split && nest == 0)
                {
                    result.Add(str.Substring(lastComma, nextIndex - lastComma));
                    lastComma = nextIndex + 1;
                }

                if (nestUp != null)
                {
                    if (nestDown.Contains(str[nextIndex]))
                    { nest++; }

                    else if (nestUp.Contains(str[nextIndex]))
                    { nest--; }
                }
                else
                {
                    if (nestDown.Contains(str[nextIndex]))
                    { nest = (nest == 1) ? 0 : 1; }
                }

                if (nest < 0) { break; } //end loop if it's leaving designated brackets (only happens on line ends)

                nextIndex++;
            }
            if (lastComma < nextIndex)
            { result.Add(str.Substring(lastComma, nextIndex - lastComma)); }
            return result.ToArray();
        }

        public static bool TryGetFromKey(this object[] obj, string key, out object result)
        {
            //this is virtually how every keyed value in Lingo is accessed
            result = null;
            foreach (object o in obj)
            {
                if (o is KeyValuePair<string, object> kvp && kvp.Key == key)
                {
                    result = kvp.Value;
                    return true;
                }
            }
            return false;
        }
        public static object[] SetFromKey(this object[] obj, string key, object value)
        {
            for (int i = 0; i < obj.Length; i++)
            {
                if (obj[i] is KeyValuePair<string, object> kvp && kvp.Key == key)
                {
                    obj[i] = new KeyValuePair<string, object>(key, value);
                    return obj;
                }
            }
            return obj.Concat(new object[] { new KeyValuePair<string, object>(key, value) }).ToArray();
        }
    }
}