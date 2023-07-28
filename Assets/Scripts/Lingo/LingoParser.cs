using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Lingo
{
    /// <summary>
    /// Converts to and from serialized Lingo data.
    /// </summary>
    /// <remarks>
    /// Supported types: <see cref="float"/>, <see cref="int"/>, <see cref="string"/>, <see cref="Vector2"/>, <see cref="Color"/>, <see cref="LinearList"/>, and <see cref="PropertyList"/>. 
    /// </remarks>
    public static class LingoParser
    {
        private const bool strict = false;

        /// <summary>
        /// Deserialize Lingo data into an object.
        /// </summary>
        /// <returns>A <see cref="float"/>, <see cref="int"/>, <see cref="string"/>, <see cref="Vector2"/>, <see cref="Color"/>, <see cref="LinearList"/>, or <see cref="PropertyList"/></returns>
        /// <exception cref="FormatException">The input could not be parsed.</exception>
        public static object Parse(string lingo)
        {
            var tr = new TokenReader(lingo);

            var result = ReadExpression(tr);

            if (!tr.AtEnd && strict)
            {
                throw new FormatException($"Expected end of file, found {tr.Current.type}!");
            }

            return result;
        }

        /// <summary>
        /// Deserialize Lingo data into a <see cref="PropertyList"/>.
        /// </summary>
        /// <exception cref="FormatException">The input could not be parsed.</exception>
        /// <exception cref="InvalidCastException">The input was not a property list.</exception>
        public static PropertyList ParsePropertyList(string lingo)
        {
            var obj = Parse(lingo);

            if(obj is not PropertyList list)
                throw new InvalidCastException($"Expected line to be a property list, got {obj?.GetType().Name ?? "null"}");

            return list;
        }

        /// <summary>
        /// Deserialize Lingo data into a <see cref="LinearList"/>.
        /// </summary>
        /// <exception cref="FormatException">The input could not be parsed.</exception>
        /// <exception cref="InvalidCastException">The input was not a linear list.</exception>
        public static LinearList ParseLinearList(string lingo)
        {
            var obj = Parse(lingo);

            if (obj is not LinearList list)
                throw new InvalidCastException($"Expected line to be a linear list, got {obj?.GetType().Name ?? "null"}");

            return list;
        }

        public static string ToLingoString(object obj)
        {
            return obj switch
            {
                null => "0",
                int i => i.ToString(),
                float f => Fixed(f),
                string s => Escape(s),
                Vector2 v => $"point({FixedOrInt(v.x)}, {FixedOrInt(v.y)})",
                Color c => $"color( {Byte(c.r)}, {Byte(c.g)}, {Byte(c.b)} )",
                Rect r => $"rect({FixedOrInt(r.xMin)}, {FixedOrInt(r.yMin)}, {FixedOrInt(r.xMax)}, {FixedOrInt(r.yMax)})",
                LinearList l => "[" + string.Join(", ", l.Select(ToLingoString)) + "]",
                PropertyList p => p.Count == 0 ? "[:]" : ("[" + string.Join(", ", p.Select(pair => $"#{pair.Key}: {ToLingoString(pair.Value)}")) + "]"),
                _ => throw new ArgumentException($"Could not serialize object of type {obj.GetType().Name}!")
            }; ;

            static string Fixed(float f)
            {
                return f.ToString("F4");
            }

            static string FixedOrInt(float f)
            {
                if (f == Math.Floor(f))
                    return ((int)f).ToString();
                else
                    return Fixed(f);
            }

            static string Byte(float f)
            {
                return ((int)(f * 255f)).ToString();
            }
        }

        private static string Escape(string str)
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
            return str;
        }

        private static object ReadExpression(TokenReader tr)
        {
            // The only supported operation is concatenation
            object acc = ReadNext(tr);

            while (tr.Current.type == TokenReader.TokenType.InfixOperator)
            {
                string op = tr.MoveNext().data;
                object rhs = ReadNext(tr);

                if (op == "&&") acc += " " + rhs;
                else if (op == "&") acc += rhs.ToString();
                else throw new FormatException($"Unknown operator: {op}");
            }

            return acc;
        }

        private static object ReadNext(TokenReader tr)
        {
            var t = tr.MoveNext();
            switch (t.type)
            {
                case TokenReader.TokenType.Number:
                    if (t.data.IndexOf('.') == -1 && int.TryParse(t.data, out int intNum))
                        return intNum;
                    else
                        return float.Parse(t.data);

                case TokenReader.TokenType.String:
                    return t.data;

                case TokenReader.TokenType.Global:
                    return ReadGlobal(t.data, tr);

                case TokenReader.TokenType.ListStart:
                    if (tr.Current.type == TokenReader.TokenType.Symbol || tr.Current.type == TokenReader.TokenType.Colon)
                        return ReadPropertyList(tr);
                    else
                        return ReadLinearList(tr);

                default:
                    throw new FormatException($"Unexpected token: {t.type}");
            }
        }

        private static PropertyList ReadPropertyList(TokenReader tr)
        {
            var list = new PropertyList();

            if (tr.Current.type == TokenReader.TokenType.Colon)
            {
                tr.Index++;
                tr.MoveNext().Assert(TokenReader.TokenType.ListEnd, "Expected closing bracket after colon in empty property list!");
            }
            else
            {
                while (tr.Current.type != TokenReader.TokenType.ListEnd)
                {
                    tr.Current.Assert(TokenReader.TokenType.Symbol, "Expected symbol as key in property list!");
                    var key = tr.MoveNext().data;

                    tr.MoveNext().Assert(TokenReader.TokenType.Colon, "Expected colon following key in property list!");

                    var value = ReadExpression(tr);
                    list.SetObject(key, value);

                    if (tr.Current.type == TokenReader.TokenType.Comma)
                    {
                        tr.Index++;
                    }
                    else if (tr.Current.type != TokenReader.TokenType.ListEnd)
                    {
                        throw new FormatException("Expected comma or closing bracket after property list!");
                    }
                }
                tr.Index++;
            }

            return list;
        }

        private static LinearList ReadLinearList(TokenReader tr)
        {
            var list = new LinearList();

            while (tr.Current.type != TokenReader.TokenType.ListEnd)
            {
                var value = ReadExpression(tr);
                list.Add(value);

                if (tr.Current.type == TokenReader.TokenType.Comma)
                {
                    tr.Index++;
                }
                else if (tr.Current.type != TokenReader.TokenType.ListEnd)
                {
                    throw new FormatException("Expected comma or closing bracket after linear list!");
                }
            }
            tr.Index++;

            return list;
        }

        private static object ReadGlobal(string name, TokenReader tr)
        {
            return name switch
            {
                "BACKSPACE" => "\b",
                "EMPTY" => "\\e",
                "ENTER" => "\n",
                "QUOTE" => "\"",
                "RETURN" => "\r",
                "SPACE" => " ",
                "TAB" => "\t",
                "PI" => Math.PI,
                "point" => ReadPointArgs(tr),
                "color" => ReadColorArgs(tr),
                "rect" => ReadRectArgs(tr),
                _ => strict ? throw new FormatException($"Unknown global: {name}") : 0
            };
        }

        private static Vector2 ReadPointArgs(TokenReader tr)
        {
            tr.MoveNext().Assert(TokenReader.TokenType.ParenStart, "Expected opening parenthesis after point!");

            var x = ReadExpression(tr);
            tr.MoveNext().Assert(TokenReader.TokenType.Comma, "Expected comma after X value of point!");

            var y = ReadExpression(tr);
            tr.MoveNext().Assert(TokenReader.TokenType.ParenEnd, "Expected closing parenthesis after Y value of point!");

            if (!TryFloat(x, out float xf) || !TryFloat(y, out float yf))
                throw new FormatException("All values of a point must be numeric!");

            return new Vector2(xf, yf);
        }

        private static Color ReadColorArgs(TokenReader tr)
        {
            tr.MoveNext().Assert(TokenReader.TokenType.ParenStart, "Expected opening parenthesis after color!");

            var r = ReadExpression(tr);
            tr.MoveNext().Assert(TokenReader.TokenType.Comma, "Expected comma after R value of color!");

            var g = ReadExpression(tr);
            tr.MoveNext().Assert(TokenReader.TokenType.Comma, "Expected comma after G value of color!");

            var b = ReadExpression(tr);
            tr.MoveNext().Assert(TokenReader.TokenType.ParenEnd, "Expected closing parenthesis after B value of color!");

            if (!TryFloat(r, out float rf) || !TryFloat(g, out float gf) || !TryFloat(b, out float bf))
                throw new FormatException("All components of a color must be numeric!");

            return new Color(rf / 255f, gf / 255f, bf / 255f, 1f);
        }

        private static Rect ReadRectArgs(TokenReader tr)
        {
            tr.MoveNext().Assert(TokenReader.TokenType.ParenStart, "Expected opening parenthesis after rect!");

            var xMin = ReadExpression(tr);
            tr.MoveNext().Assert(TokenReader.TokenType.Comma, "Expected comma after X-min value of rect!");

            var yMin = ReadExpression(tr);
            tr.MoveNext().Assert(TokenReader.TokenType.Comma, "Expected comma after Y-min value of rect!");

            var xMax = ReadExpression(tr);
            tr.MoveNext().Assert(TokenReader.TokenType.Comma, "Expected comma after X-max value of rect!");

            var yMax = ReadExpression(tr);
            tr.MoveNext().Assert(TokenReader.TokenType.ParenEnd, "Expected closing parenthesis after Y-max value of rect!");

            if (!TryFloat(xMin, out float xMinF) || !TryFloat(yMin, out float yMinF) || !TryFloat(xMax, out float xMaxF) || !TryFloat(yMax, out float yMaxF))
                throw new FormatException("All components of a rect must be numeric!");

            return new Rect(xMinF, yMinF, xMaxF, yMaxF);
        }

        private static bool TryFloat(object obj, out float num)
        {
            num = default;
            if (obj is float f) num = f;
            else if (obj is int i) num = i;
            else return false;
            return true;
        }

        private class TokenReader
        {
            private readonly List<Token> tokens;

            public int Index { get; set; }
            public bool AtEnd => Index >= tokens.Count;
            public Token Current => Peek(0);

            public Token MoveNext()
            {
                var result = Current;
                Index++;
                return result;
            }

            public TokenReader(string lingo)
            {
                tokens = Tokenize(lingo);
            }

            public Token Peek(int offset)
            {
                int i = Index + offset;
                if (i >= 0 && i < tokens.Count)
                    return tokens[i];
                else
                    return default;
            }

            private List<Token> Tokenize(string str)
            {
                int i = 0;
                var tokens = new List<Token>();

                while (i < str.Length)
                {
                    var c = str[i];

                    if (char.IsWhiteSpace(c))
                    {
                        i++;
                        continue;
                    }

                    if (c == '-' && i + 1 < str.Length && str[i + 1] == '-')
                    {
                        while (i < str.Length && str[i] != '\r' && str[i] != '\n')
                        {
                            i++;
                        }
                        continue;
                    }

                    switch (c)
                    {
                        case '[': tokens.Add(new Token(TokenType.ListStart)); i++; break;
                        case ']': tokens.Add(new Token(TokenType.ListEnd)); i++; break;
                        case '(': tokens.Add(new Token(TokenType.ParenStart)); i++; break;
                        case ')': tokens.Add(new Token(TokenType.ParenEnd)); i++; break;
                        case ':': tokens.Add(new Token(TokenType.Colon)); i++; break;
                        case ',': tokens.Add(new Token(TokenType.Comma)); i++; break;
                        case '&':
                            if (i + 1 < str.Length && str[i + 1] == '&')
                            {
                                tokens.Add(new Token(TokenType.InfixOperator, "&&"));
                                i += 2;
                            }
                            else
                            {
                                tokens.Add(new Token(TokenType.InfixOperator, "&"));
                                i += 1;
                            }
                            break;
                        case '"':
                            tokens.Add(new Token(TokenType.String, ReadString(str, ref i)));
                            break;
                        case '#':
                            i++;
                            tokens.Add(new Token(TokenType.Symbol, ReadName(str, ref i)));
                            break;
                        default:
                            if (char.IsDigit(c) || c == '-' || (c == '.' && i + 1 < str.Length && char.IsDigit(str[i + 1])))
                            {
                                tokens.Add(new Token(TokenType.Number, ReadNumber(str, ref i)));
                            }
                            else if (char.IsLetter(c))
                            {
                                tokens.Add(new Token(TokenType.Global, ReadName(str, ref i)));
                            }
                            else
                            {
                                throw new FormatException($"Unexpected character: {c}");
                            }
                            break;
                    }
                }

                return tokens;
            }

            private string ReadNumber(string str, ref int i)
            {
                int start = i;
                bool hasDecimal = false;

                if (str[i] == '-') i++;

                while (i < str.Length && (char.IsDigit(str[i]) || str[i] == '.'))
                {
                    if (str[i] == '.')
                    {
                        if (hasDecimal) break;
                        hasDecimal = true;
                    }
                    i++;
                }

                return str.Substring(start, i - start);
            }

            private string ReadString(string str, ref int i)
            {
                int startQuote = i;
                i++;
                while (i < str.Length && str[i] != '"')
                {
                    i++;
                }

                if (i >= str.Length)
                    throw new FormatException("Unterminated string!");

                i++;
                return str.Substring(startQuote + 1, i - startQuote - 2);
            }

            private string ReadName(string str, ref int i)
            {
                if (!char.IsLetter(str[i]))
                {
                    throw new FormatException($"Invalid character to start name: {str[i]}");
                }

                int start = i;
                while (i < str.Length && char.IsLetterOrDigit(str[i]))
                {
                    i++;
                }

                return str.Substring(start, i - start);
            }

            public struct Token
            {
                public TokenType type;
                public string data;

                public Token(TokenType type, string data = null)
                {
                    this.type = type;
                    this.data = data;
                }

                public void Assert(TokenType type, string error)
                {
                    if (this.type != type)
                        throw new FormatException(error);
                }
            }

            public enum TokenType
            {
                EOF,
                ListStart,
                ListEnd,
                ParenStart,
                ParenEnd,
                Symbol,
                Colon,
                Comma,
                Global,
                InfixOperator,
                String,
                Number
            }
        }
    }
}
