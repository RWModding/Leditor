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
using Unity.Netcode;

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
    public Button HostButton;
    public Button ConnectButton;
    public TextMeshProUGUI InviteCode;
    public GameObject InviteCodeInputGameObject;
    public TextMeshProUGUI InviteCodeInputText;

    public List<EditorTab> Tabs = new();
    public EditorTab CurrentTab;

    private void Awake()
    {
        Instance = this;

        SaveButton.onClick.AddListener(OnSaveClick);
        LoadButton.onClick.AddListener(OnLoadClick);
        RenderButton.onClick.AddListener(OnRenderClick);
        SettingsButton.onClick.AddListener(OnSettingsClick);
        HostButton.onClick.AddListener(OnHostClick);
        ConnectButton.onClick.AddListener(OnConnectClick);
    }

    void Start()
    {
        StaticData.Init();
    }

    void Update()
    {
        var allowNetworkButtons = string.IsNullOrEmpty(InviteCode.text) && Tabs.Count == 0;

        InviteCodeInputGameObject.SetActive(allowNetworkButtons);
        HostButton.interactable = allowNetworkButtons;
        ConnectButton.interactable = allowNetworkButtons;
    }

    private void OnHostClick()
    {
        StartCoroutine(Networking.ConfigureTransportAndStartNgoAsHost(InviteCode));
    }

    private void OnConnectClick()
    {
        if (string.IsNullOrEmpty(InviteCodeInputText.text)) return;
        var inviteCode = InviteCodeInputText.text.Trim().ToUpper().Substring(0, 6);
        InviteCodeInputText.text = "";
        StartCoroutine(Networking.ConfigureTransportAndStartNgoAsConnectingPlayer(InviteCode, inviteCode));
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
            var file = new EditorFile(path);
            var levelString = string.Join('\n', file.Lines);

            if (NetworkManager.Singleton.IsServer)
            {
                Networking.Instance.SendOpenTab(file.Name, levelString, false);
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                Networking.Instance.SendOpenTab(file.Name, levelString, true);
            }
            else
            {
                OpenTab(file);
            }
        }
    }

    public void OpenTab(EditorFile file)
    {
        FileToLoad = file;
        ActuallyOpenTab();
    }

    public void OpenTab(string levelName, string levelString)
    {
        FileToLoad = new EditorFile(levelName, levelString);
        ActuallyOpenTab();
    }

    private void ActuallyOpenTab()
    {
        var oldTab = Tabs.FirstOrDefault(x => x.File.Name == FileToLoad.Name);
        if (oldTab != null)
        {
            CloseTab(oldTab);
        }

        SceneManager.LoadScene("GeoEditor", LoadSceneMode.Additive);
    }

    private void OnSaveClick()
    {
        if (CurrentTab == null || CurrentEditor == null) return;
        CurrentTab.File.SaveToDisk();
        UpdateTabPreview(CurrentTab);
    }

    public void OnEditorLoaded(GeoEditor geoEditor)
    {
        var tabObj = Instantiate(TabPrefab, default, Quaternion.identity, TabsObj.transform);
        var tab = new EditorTab(geoEditor, tabObj);
        tabObj.name = tab.File.Name;
        var preview = tab.GameObject.transform.Find("Preview");
        tab.GameObject.transform.Find("CloseButton").GetComponent<Button>().onClick.AddListener(() => CloseTabClick(tab));
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

    public void CloseTabClick(EditorTab tab)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Networking.Instance.SendCloseTab(tab.File.Name, false);
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            Networking.Instance.SendCloseTab(tab.File.Name, true);
        }
        else
        {
            CloseTab(tab);
        }
    }

    public void CloseTab(string tabName)
    {
        var tab = Tabs.FirstOrDefault(x => x.File.Name == tabName);
        if (tab != null)
        {
            CloseTab(tab);
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