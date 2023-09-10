using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Dropdown : MonoBehaviour
{
    public string[] options = new string[] { "Option 1" };
    public string defaultOption;
    public UnityEvent<string> OnSelect;

    [SerializeField] private Button _button;
    [SerializeField] private RectTransform _menu;
    [SerializeField] private RectTransform _buttonTemplate;
    private GameObject[] _buttonInstances;

    private void Start()
    {
        _button.onClick.AddListener(ToggleMenu);
        _buttonTemplate.gameObject.SetActive(false);
        _menu.gameObject.SetActive(false);

        UpdateOptions();
        Select(defaultOption);
    }

    private void UpdateOptions()
    {
        var optionsContainer = _menu.GetComponent<RectTransform>();

        float opHeight = _buttonTemplate.sizeDelta.y;
        float opSpacing = -_buttonTemplate.anchoredPosition.y;
        optionsContainer.sizeDelta = new Vector2(optionsContainer.sizeDelta.x, opSpacing + (opHeight + opSpacing) * options.Length);

        if(_buttonInstances != null)
        {
            foreach(var button in _buttonInstances)
            {
                Destroy(button);
            }
        }

        _buttonInstances = new GameObject[options.Length];
        for (int i = 0; i < options.Length; i++)
        {
            var optionValue = options[i];
            var optionRect = Instantiate(_buttonTemplate, _menu).GetComponent<RectTransform>();
            _buttonInstances[i] = optionRect.gameObject;

            optionRect.anchoredPosition = new Vector2(optionRect.anchoredPosition.x, -opSpacing - (opSpacing + opHeight) * i);
            optionRect.gameObject.SetActive(true);

            optionRect.GetComponentInChildren<TextMeshProUGUI>().text = optionValue;

            optionRect.GetComponent<Button>().onClick.AddListener(() => Select(optionValue));
        }
    }

    public void ToggleMenu()
    {
        _menu.gameObject.SetActive(!_menu.gameObject.activeSelf);
    }

    public void Select(string value)
    {
        _menu.gameObject.SetActive(false);
        _button.GetComponentInChildren<TextMeshProUGUI>().text = value;

        OnSelect?.Invoke(value);
    }
}
