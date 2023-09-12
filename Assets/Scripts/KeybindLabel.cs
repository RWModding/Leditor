using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeybindLabel : MonoBehaviour
{
    public string ActionId;
    public Image Graphic;
    public TextMeshProUGUI Name;

    public void Start()
    {
        var action = Keybinds.GetAction(ActionId);
        Graphic.sprite = Keybinds.GetSprite(action.CurrentKey, true);
        Name.text = action.Name;
    }
}
