using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorButtonController : MonoBehaviour
{
    public Transform EditorParent;

    private void Start()
    {
        SwitchEditor(EditorParent.GetChild(0).gameObject);
    }

    public void SwitchEditor(GameObject editor)
    {
        bool setActive = editor != null && !editor.activeSelf;

        foreach(Transform oldEditor in EditorParent)
        {
            oldEditor.gameObject.SetActive(false);
        }

        if (setActive)
            editor.SetActive(true);
        else
            EditorParent.GetChild(0).gameObject.SetActive(true);
    }
}
