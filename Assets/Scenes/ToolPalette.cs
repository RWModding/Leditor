using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static FeatureType;
using static GeoType;

public class ToolPalette : MonoBehaviour
{
    private const int Columns = 4;
    private const float Spacing = 10f;
    private readonly Color SelectedColor = new Color(0.1666074f, 0.9056604f, 0.841648f, 0.5019608f);
    private readonly Color DefaultColor = new Color(1, 1, 1, 0.2f);

    public List<Tool> Tools;
    public Tool SelectedTool;
    private GameObject PaletteRoot;
    private GameObject PaletteBackground;

    private GameObject ButtonPrefab;

    private new Camera camera;

    private void Awake()
    {
        camera = Camera.main;

        PaletteRoot = GameObject.Find("ToolPalette");
        PaletteBackground = PaletteRoot.transform.Find("Background").gameObject;
        ButtonPrefab = Resources.Load<GameObject>("tools/button");

        Tools = new List<Tool>
        {
            new SimpleGridTool("Wall", geoType: Solid),
            new SimpleGridTool("Air", geoType: Air),
            new SimpleGridTool("Slope", geoType: BLSlope),
            new SimpleGridTool("Platform", geoType: Platform),
            
            new SimpleGridTool("Hor. Beam", featureType: horbeam),
            new SimpleGridTool("Ver. Beam", featureType: vertbeam),
            new SimpleGridTool("Rock", featureType: rock),
            new SimpleGridTool("Spear", featureType: spear),
            
            new SimpleGridTool("ShrtCt Entrance", featureType: shortcutentrance),
            new SimpleGridTool("ShrtCt Dot", featureType: shortcutdot),
            new SimpleGridTool("Enemy Den", featureType: dragonDen),
            new SimpleGridTool("Entrance", featureType: entrance),
            
            new SimpleGridTool("Wack Hole", featureType: WHAMH),
            new SimpleGridTool("Scav Hole", featureType: scavengerHole),
            new SimpleGridTool("Garb Hole", featureType: garbageHole),
            new SimpleGridTool("Waterf", featureType: waterfall),

            new SimpleGridTool("Hive", featureType: hive),
            new SimpleGridTool("Worm Grass", featureType: wormGrass),
            new SimpleGridTool("Forbid Bats", featureType: forbidBats),
            new SimpleGridTool("Glass Wall", geoType: GlassWall),

            new SimpleGridTool("Crack", featureType: crack),
        };


        var rect = ButtonPrefab.GetComponent<RectTransform>();
        var width = rect.rect.width;
        var height = rect.rect.height;

        var rows = Mathf.Ceil((float)Tools.Count / Columns);

        var backgroundHeight = Spacing + (rows * Spacing) + (rows * height) + Spacing;
        var backgroundWidth = Spacing + (Columns * Spacing) + (Columns * width) + Spacing;

        var backgroundTransform = PaletteBackground.GetComponent<RectTransform>();
        backgroundTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, backgroundHeight);
        backgroundTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, backgroundWidth);

        for (var i = 0; i < Tools.Count; i++)
        {
            var tool = Tools[i];

            var row = i / Columns;
            var collumn = i % Columns;

            var yPos = Spacing + (row * Spacing) + (row * width);
            var xPos = Spacing + (collumn * Spacing) + (collumn * height); 

            var button = Instantiate(ButtonPrefab, PaletteBackground.transform);
            tool.Button = button;

            button.transform.localPosition = new Vector2(xPos, -yPos);
            button.GetComponent<Button>().onClick.AddListener(() => OnToolSelected(button, tool));
            button.name = tool.Name;
            button.GetComponentInChildren<TextMeshProUGUI>().text = tool.Description;
        }

        Tools.FirstOrDefault().Button.GetComponent<Button>().onClick.Invoke();
    }

    private void OnToolSelected(GameObject button, Tool tool)
    {
        if (SelectedTool == tool) return;

        SelectedTool = tool;

        foreach (var t in Tools)
        {
            t.Button.GetComponent<Image>().color = t == tool ? SelectedColor : DefaultColor;
        }
    }

    void Start()
    {
        
    }

    private Vector3 lastClickPos;
    private bool clickHeld;
    
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            var clickPos = camera.ScreenToWorldPoint(Input.mousePosition);
            if (!clickHeld || Vector2Int.CeilToInt(clickPos) != Vector2Int.CeilToInt(lastClickPos))
            {
                SelectedTool.OnClick(clickPos);
                lastClickPos = clickPos;
                clickHeld = true;
            }
        }
        else
        {
            clickHeld = false;
        }
    }
}
