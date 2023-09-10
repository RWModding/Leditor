using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class EditorPanel : MonoBehaviour
{
    public RectTransform ToggleButton;

    public void Toggle()
    {
        var view = transform.Find("Content View");
        if(view != null)
        {
            view.gameObject.SetActive(!view.gameObject.activeSelf);
        }

        ToggleButton.localEulerAngles = new Vector3(0f, 0f, view.gameObject.activeSelf ? 0f : -90f);
    }
}
