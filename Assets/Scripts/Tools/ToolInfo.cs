using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ToolInfo : MonoBehaviour
{
    public ToolGroup Tools;
    public TextMeshProUGUI Name;
    public GameObject KeybindPrefab;

    private Tool _currentTool;
    private List<KeybindLabel> _keybinds = new();

    private void Update()
    {
        Tool newTool = Tools && Tools.CurrentTool ? Tools.CurrentTool : null;

        if(newTool != _currentTool)
        {
            _currentTool = newTool;
            UpdateMenu();
        }
    }

    private void UpdateMenu()
    {
        foreach(var keybind in _keybinds)
        {
            Destroy(keybind.gameObject);
        }
        _keybinds.Clear();

        if(!_currentTool)
        {
            Name.gameObject.SetActive(false);
        }
        else
        {
            Name.gameObject.SetActive(true);
            Name.text = _currentTool.gameObject.name;

            var newKeybinds = new List<string>(_currentTool.KeybindIds);

            if(_currentTool.ShowLayerSelector)
            {
                newKeybinds.Add("Change Layer");
            }

            foreach(var keybindId in newKeybinds)
            {
                var keybind = Instantiate(KeybindPrefab).GetComponent<KeybindLabel>();
                keybind.transform.SetParent(transform, false);
                keybind.ActionId = keybindId;
                _keybinds.Add(keybind);
            }
        }
    }
}
