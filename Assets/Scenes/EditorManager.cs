using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Transactions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SimpleFileBrowser;

public class EditorManager : MonoBehaviour
{
    public static EditorManager Instance;
    public static EditorFile FileToLoad;

    [HideInInspector]
    public GeoEditor CurrentEditor;

    //-- Assigned through the editor
    public GameObject PreviewCameraPrefab;
    public GameObject TabPrefab;
    public GameObject TabsObj;
    public ToolPalette Tools;
    public CameraControls CameraControls;
    public Button SaveButton;
    public Button LoadButton;
    public Button RenderButton;
    public Button SettingsButton;

    private List<EditorTab> Tabs = new();
    private EditorTab CurrentTab;

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
        FileBrowser.ShowLoadDialog(OnFilePicked, OnFilePickCancel, FileBrowser.PickMode.Files);
    }

    private void OnFilePickCancel()
    {
    }

    private void OnFilePicked(string[] paths)
    {
        var path = paths?.FirstOrDefault();
        if (!string.IsNullOrEmpty(path))
        {
            FileToLoad = new EditorFile(path);

            var oldTab = Tabs.FirstOrDefault(x => x.File.Name == FileToLoad.Name);
            if (oldTab != null)
            {
                CloseTab(oldTab);
            }

            SceneManager.LoadScene("GeoEditor", LoadSceneMode.Additive);
        }
    }

    private void OnSaveClick()
    {
        if (CurrentTab == null || CurrentEditor == null) return;
        CurrentTab.File.SaveToDisk();
        UpdateTabPreview(CurrentTab);
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
        tabObj.name = tab.File.Name;
        var preview = tab.GameObject.transform.Find("Preview");
        tab.GameObject.transform.Find("CloseButton").GetComponent<Button>().onClick.AddListener(() => CloseTab(tab));
        preview.GetComponent<Button>().onClick.AddListener(() => SelectTab(tab));
        preview.GetComponentInChildren<TextMeshProUGUI>().text = tab.File.Name;

        Tabs.Add(tab);
        OrganizeTabs();
        SelectTab(tab);
        UpdateTabPreview(tab);
    }

    public void OrganizeTabs()
    {
        for (int i = 0; i < Tabs.Count; i++)
        {
            var tab = Tabs[i];
            tab.GameObject.transform.localPosition = new Vector3(i * 100, 0);
        }
    }

    //-- TODO: Ask for confirmation
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
        CurrentTab = tab;
        CameraControls.SetContraints(new Vector2(-1, -(tab.Editor.CurrentLevelMatrix.Height + 1)), new Vector2(tab.Editor.CurrentLevelMatrix.Width + 1, 1));

        foreach (var otherTab in Tabs)
        {
            if (otherTab != tab) {
                otherTab.EditorRoot.SetActive(false);
            }
        }
    }

    public void UpdateTabPreview(EditorTab tab)
    {
        EditorTab oldTab = null;
        if (tab != CurrentTab)
        {
            oldTab = CurrentTab;
            SelectTab(tab);
        }

        var width = tab.Editor.CurrentLevelMatrix.Width;
        var height = tab.Editor.CurrentLevelMatrix.Height;
        
        var previewCameraObj = Instantiate(PreviewCameraPrefab);
        var previewCamera = previewCameraObj.GetComponent<Camera>();
        
        previewCameraObj.transform.SetPositionAndRotation(new Vector3(width / 2f, -height / 2f, -10), Quaternion.identity);

        var orthoSize = Mathf.CeilToInt(Mathf.Max(width, height) / 2f);

        previewCamera.aspect = 1;
        previewCamera.orthographicSize = orthoSize;

        var texSize = previewCamera.pixelHeight;

        var renderTexture = new RenderTexture(texSize, texSize, 24);

        var texture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);

        previewCamera.targetTexture = renderTexture;
        previewCamera.Render();

        Graphics.ConvertTexture(renderTexture, texture);

        previewCamera.targetTexture = null;
        RenderTexture.active = null;

        var image = tab.GameObject.transform.Find("Preview").GetComponent<RawImage>();
        image.texture = texture;

        if (tab.PreviewTexture != null)
        {
            Destroy(tab.PreviewTexture);
        }
        tab.PreviewTexture = texture;


        Destroy(renderTexture);
        Destroy(previewCameraObj);

        if (oldTab != null)
        {
            SelectTab(oldTab);
        }
    }

    public class EditorTab
    {
        public GameObject GameObject;
        public GameObject EditorRoot;
        public EditorFile File;
        public GeoEditor Editor;
        public Texture2D PreviewTexture;

        public EditorTab(GeoEditor geoEditor, GameObject tab)
        {
            GameObject = tab;
            EditorRoot = geoEditor.RootObj;
            File = geoEditor.LoadedFile;
            Editor = geoEditor;
        }
    }
}