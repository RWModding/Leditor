using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class Keybinds : MonoBehaviour
{
    public SpriteAtlas KeySprites;
    private Dictionary<KeyCode, Sprite> _lightSprites;
    private Dictionary<KeyCode, Sprite> _darkSprites;

    private static readonly ActionInfo[] _actions = new ActionInfo[]
    {
        new("Geo Paint", "Terrain Paint", KeyCode.Mouse0) { MouseOnly = true },
        new("Geo Rect", "Rectangle", KeyCode.Mouse1) { MouseOnly = true },
        new("Geo Swap", "Swap Paint Type", KeyCode.X),

        new("Geo Erase", "Terrain Erase", KeyCode.Mouse0) { MouseOnly = true },
        new("Geo Erase Rect", "Erase Rectangle", KeyCode.Mouse1) { MouseOnly = true },

        new("Brush Shrink", "Shrink Brush", KeyCode.LeftBracket),
        new("Brush Grow", "Grow Brush", KeyCode.RightBracket),

        new("Slope Create", "Create Slope", KeyCode.Mouse0) { MouseOnly = true },

        new("Change Layer", "Change Layer", KeyCode.L),
    };

    private static Dictionary<string, ActionInfo> _actionsById;
    private static Keybinds _instance;

    public static bool Shift => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

    private void Awake()
    {
        _instance = this;

        if(_actionsById == null)
        {
            _actionsById = new Dictionary<string, ActionInfo>(_actions.Length);
            foreach(var action in _actions)
            {
                _actionsById.Add(action.Id, action);
            }
        }

        _lightSprites = new();
        _darkSprites = new();
        var hit = new HashSet<KeyCode>();
        foreach(KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            if (!hit.Add(key)) continue;

            var name = GetSpriteName(key);
            var light = KeySprites.GetSprite(name + "_Light");
            var dark = KeySprites.GetSprite(name + "_Dark");

            if (light) _lightSprites.Add(key, light);
            if (dark) _darkSprites.Add(key, dark);
        }

        _lightSprites[KeyCode.None] = KeySprites.GetSprite("Blank_Key_Light");
        _darkSprites[KeyCode.None] = KeySprites.GetSprite("Blank_Key_Dark");
    }

    private static string GetSpriteName(KeyCode key)
    {
        return key switch
        {
            KeyCode.Alpha0 => "0",
            KeyCode.Alpha1 => "1",
            KeyCode.Alpha2 => "2",
            KeyCode.Alpha3 => "3",
            KeyCode.Alpha4 => "4",
            KeyCode.Alpha5 => "5",
            KeyCode.Alpha6 => "6",
            KeyCode.Alpha7 => "7",
            KeyCode.Alpha8 => "8",
            KeyCode.Alpha9 => "9",
            KeyCode.DownArrow => "Arrow_Down",
            KeyCode.UpArrow => "Arrow_Up",
            KeyCode.RightArrow => "Arrow_Right",
            KeyCode.LeftArrow => "Arrow_Left",
            KeyCode.LeftAlt => "Alt",
            KeyCode.RightAlt => "Alt",
            KeyCode.LeftBracket => "Bracket_Left",
            KeyCode.RightBracket => "Bracket_Right",
            KeyCode.CapsLock => "Caps_Lock",
            KeyCode.LeftCommand => "Command",
            KeyCode.RightCommand => "Command",
            KeyCode.LeftControl => "Ctrl",
            KeyCode.RightControl => "Ctrl",
            KeyCode.Delete => "Del",
            KeyCode.Return => "Enter",
            KeyCode.Escape => "Esc",
            KeyCode.Comma => "Mark_Left",
            KeyCode.Period => "Mark_Right",
            KeyCode.Mouse0 => "Mouse_Left",
            KeyCode.Mouse2 => "Mouse_Middle",
            KeyCode.Mouse1 => "Mouse_Right",
            KeyCode.Numlock => "Num_Lock",
            KeyCode.PageDown => "Page_Down",
            KeyCode.PageUp => "Page_Up",
            KeyCode.Print => "Print_Screen",
            KeyCode.Slash => "Question",
            KeyCode.LeftShift => "Shift",
            KeyCode.RightShift => "Shift",
            KeyCode.BackQuote => "Tilda",
            KeyCode.LeftWindows => "Win",
            KeyCode.RightWindows => "Win",
            KeyCode.Equals => "Plus",

            KeyCode.Keypad0 => "0",
            KeyCode.Keypad1 => "1",
            KeyCode.Keypad2 => "2",
            KeyCode.Keypad3 => "3",
            KeyCode.Keypad4 => "4",
            KeyCode.Keypad5 => "5",
            KeyCode.Keypad6 => "6",
            KeyCode.Keypad7 => "7",
            KeyCode.Keypad8 => "8",
            KeyCode.Keypad9 => "9",
            KeyCode.KeypadDivide => "Slash",
            KeyCode.KeypadEnter => "Enter",
            KeyCode.KeypadMinus => "Minus",
            KeyCode.KeypadMultiply => "Asterisk",
            KeyCode.KeypadPeriod => "Del",
            KeyCode.KeypadPlus => "Plus_Tall",

            _ => key.ToString()
        } + "_Key";
    }

    public static Sprite GetSprite(KeyCode key, bool dark)
    {
        var sprites = dark ? _instance._darkSprites : _instance._lightSprites;
        return sprites.TryGetValue(key, out var sprite) ? sprite : sprites[KeyCode.None];
    }

    public static ActionInfo GetAction(string id)
    {
        return _actionsById[id];
    }

    public class ActionInfo
    {
        public readonly string Id;
        public readonly string Name;
        public readonly KeyCode DefaultKey;
        public bool MouseOnly;
        public bool Rebindable;
        public KeyCode CurrentKey => DefaultKey;

        public ActionInfo(string id, string name, KeyCode defaultKey)
        {
            Id = id;
            Name = name;
            DefaultKey = defaultKey;
            MouseOnly = false;
            Rebindable = true;
        }
    }
}
