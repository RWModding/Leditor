using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EditorManager : MonoBehaviour
{
    public static EditorManager Instance;
    public static EditorFile FileToLoad;

    [HideInInspector]
    public GeoEditor CurrentEditor;

    //-- Assigned through the editor
    public GameObject TabPrefab;
    public GameObject TabsObj;
    public ToolPalette Tools;
    public Button SaveButton;
    public Button LoadButton;
    public Button RenderButton;
    public Button SettingsButton;

    private List<EditorTab> Tabs = new();

    private void Awake()
    {
        Instance = this;

        SaveButton.onClick.AddListener(OnSaveClick);
        LoadButton.onClick.AddListener(OnLoadClick);
        RenderButton.onClick.AddListener(OnRenderClick);
        SettingsButton.onClick.AddListener(OnSettingsClick);
    }

    private void OnSettingsClick()
    {

    }

    private void OnRenderClick()
    {

    }

    private void OnLoadClick()
    {
        NativeFilePicker.PickFile(OnFilePicked, "txt", "png");
    }

    private void OnFilePicked(string path)
    {
        if (path != null)
        {
            FileToLoad = new EditorFile(path);
            SceneManager.LoadScene("GeoEditor", LoadSceneMode.Additive);
        }
    }

    private void OnSaveClick()
    {

    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void OnEditorLoaded(GeoEditor geoEditor)
    {
        var tabObj = Instantiate(TabPrefab, default, Quaternion.identity, TabsObj.transform);
        var tab = new EditorTab(geoEditor, tabObj);
        tab.GameObject.transform.Find("Preview").GetComponent<Button>().onClick.AddListener(() => SelectTab(tab));
        tab.GameObject.transform.Find("CloseButton").GetComponent<Button>().onClick.AddListener(() => CloseTab(tab));

        Tabs.Add(tab);
        OrganizeTabs();
        SelectTab(tab);
    }

    public void OrganizeTabs()
    {
        for (int i = 0; i < Tabs.Count; i++)
        {
            var tab = Tabs[i];
            tab.GameObject.transform.localPosition = new Vector3(i * 100, 0);
        }
    }
    public void CloseTab(EditorTab tab)
    {
        if (CurrentEditor == tab.Editor)
        {
            CurrentEditor = null;
        }

        SceneManager.UnloadSceneAsync(tab.EditorRoot.scene);
        Tabs.Remove(tab);
        Destroy(tab.GameObject);
        OrganizeTabs();
    }

    public void SelectTab(EditorTab tab)
    {
        tab.EditorRoot.SetActive(true);
        CurrentEditor = tab.Editor;

        foreach (var otherTab in Tabs)
        {
            if (otherTab != tab) {
                otherTab.EditorRoot.SetActive(false);
                //StartCoroutine(SuperDeactivateObject(otherTab.EditorRoot));
            }
        }
    }

    public IEnumerator SuperDeactivateObject(GameObject obj)
    {
        var falseFrames = 0;
        while (falseFrames < 10)
        {
            if (obj.activeInHierarchy)
            {
                obj.SetActive(false);
                falseFrames = 0;
            }
            else
            {
                falseFrames++;
            }
            yield return null;
        }
    }

    public class EditorTab
    {
        public GameObject GameObject;
        public GameObject EditorRoot;
        public EditorFile File;
        public GeoEditor Editor;

        public EditorTab(GeoEditor geoEditor, GameObject tab)
        {
            GameObject = tab;
            EditorRoot = geoEditor.RootObj;
            File = geoEditor.LoadedFile;
            Editor = geoEditor;
        }
    }
}