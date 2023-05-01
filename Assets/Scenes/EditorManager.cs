using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorManager : MonoBehaviour
{
    public static EditorManager Instance;

    public IEditor CurrentEditor;
    public ToolPalette Tools;

    private void Awake()
    {
        Instance = this;
        Tools = gameObject.AddComponent<ToolPalette>();
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}