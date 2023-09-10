using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LevelModel;
using System.Linq;
using UnityEditor;
using System;

public class GeoView : MonoBehaviour
{
    public Material LayerMaterial;
    public Material GeoMaterial;
    public ViewMode Mode;

    private LevelLoader _loader;
    private Camera[] _layerCameras;
    private Transform _chunkParent;
    private ViewMode _lastMode;
    private MeshRenderer[] _layerRenderers;

    void Awake()
    {
        _loader = GetComponentInParent<LevelLoader>();
    }

    void Start()
    {
        var mergedLayers = new GameObject("Merged Layers");
        mergedLayers.transform.parent = transform;

        // Set up cameras and layer sprites
        _layerCameras = new Camera[3];
        _layerRenderers = new MeshRenderer[3];
        for (int i = 0; i < 3; i++)
        {
            var camObj = new GameObject($"Layer {i + 1} Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.enabled = false;
            cam.cullingMask = LayerMask.GetMask($"Layer{i + 1}");
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
            cam.targetTexture = new RenderTexture(1, 1, 16, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point
            };
            cam.farClipPlane = 20f;
            _layerCameras[i] = cam;
            camObj.transform.parent = transform;

            var imageObj = MakeQuadRenderer();
            var ren = imageObj.GetComponent<MeshRenderer>();
            imageObj.name = $"Layer {i + 1} Image";
            ren.material = LayerMaterial;
            ren.material.mainTexture = cam.targetTexture;
            imageObj.transform.parent = mergedLayers.transform;
            imageObj.transform.localPosition = new Vector3(0f, 0f, i);

            _layerRenderers[i] = ren;
        }

        ApplyViewMode();
    }

    private void ApplyViewMode()
    {
        for (int i = 0; i < 3; i++)
        {
            var ren = _layerRenderers[i];

            if (Mode == ViewMode.Tiled)
            {
                var color = i switch
                {
                    0 => Color.white,
                    1 => new Color(0.75f, 0.55f, 0.95f),
                    _ => new Color(0.95f, 0.95f, 0.6f),
                };

                ren.material.SetColor("_ColorR", new Color(0.3f, 0.3f, 0.3f, 0f));
                ren.material.SetColor("_ColorG", new Color(0.4f, 0.4f, 0.4f, 0f));
                ren.material.SetColor("_ColorB", new Color(0.5f, 0.5f, 0.5f, 0.1f));
                ren.material.color = color;
                ren.sortingOrder = 0;
            }
            else
            {
                var color = i switch
                {
                    0 => Color.black,
                    1 => new Color(0.5f, 0.1f, 0.9f, 0.5f),
                    _ => new Color(0.9f, 0.9f, 0.4f, 0.3f),
                };

                ren.material.SetColor("_ColorR", new Color(1f, 1f, 1f, 0f));
                ren.material.SetColor("_ColorG", new Color(1f, 1f, 1f, 0f));
                ren.material.SetColor("_ColorB", new Color(1f, 1f, 1f, 0f));
                ren.material.color = color;
                ren.sortingOrder = i - 3;
            }
        }
    }

    void Update()
    {
        if(Mode != _lastMode)
        {
            _lastMode = Mode;

            ResetChunks();
            ApplyViewMode();
        }
    }

    public void OnLevelLoaded()
    {
        ResetChunks();
    }

    public void OnLevelViewRefreshed(RectInt?[] rects)
    {
        Refresh(rects);
    }

    private void Clear()
    {
        if (_chunkParent)
        {
            Destroy(_chunkParent.gameObject);
            _chunkParent = null;
        }
    }

    private void ResetChunks()
    {
        Clear();

        var level = _loader.LevelData;
        if (level == null) return;

        if (!_chunkParent)
        {
            _chunkParent = new GameObject("Layers").transform;
            _chunkParent.parent = transform;
        }

        const int maxSize = 256;

        for(int y = 0; y < level.Height; y += maxSize)
        {
            for (int x = 0; x < level.Width; x += maxSize)
            {
                for (int layer = 0; layer < 3; layer++)
                {
                    var chunkObj = new GameObject($"Chunk {x} {y} {layer + 1}", typeof(GeoViewChunk));
                    chunkObj.transform.parent = _chunkParent;

                    var chunk = chunkObj.GetComponent<GeoViewChunk>();
                    chunk.LevelRect = new RectInt(x, y, Math.Min(level.Width - x, maxSize), Math.Min(level.Height - y, maxSize));
                    chunk.Layer = layer;
                    chunk.Level = level;
                    chunk.GeoMaterial = GeoMaterial;
                }
            }
        }

        RectInt? fullRect = new RectInt(0, 0, _loader.LevelData.Width, _loader.LevelData.Height);
        Refresh(Enumerable.Repeat(fullRect, 4).ToArray());
    }

    private void Refresh(RectInt?[] rects)
    {
        var level = _loader.LevelData;

        // Refresh chunks that overlap the dirty rect
        _chunkParent.gameObject.SetActive(true);
        foreach (Transform chunkObj in _chunkParent)
        {
            var chunk = chunkObj.GetComponent<GeoViewChunk>();

            if (rects[chunk.Layer] is RectInt rect
                && chunk.LevelRect.Overlaps(rect))
            {
                chunk.Refresh();
            }
        }

        // Stretch cameras to cover entire level
        for (int layer = 0; layer < 3; layer++)
        {
            var cam = _layerCameras[layer];
            cam.orthographicSize = level.Height / 2f;
            cam.aspect = level.Width / (float)level.Height;
            cam.transform.localPosition = new Vector3(level.Width / 2f, -level.Height / 2f, -10f);

            if(cam.targetTexture.width != level.Width * 20 || cam.targetTexture.height != level.Height * 20)
            {
                if (cam.targetTexture.IsCreated())
                    cam.targetTexture.Release();

                cam.targetTexture.width = level.Width * 20;
                cam.targetTexture.height = level.Height * 20;
            }
        }

        // Render all cameras
        for (int layer = 0; layer < 3; layer++)
        {
            if (rects[layer] != null)
            {
                _layerCameras[layer].Render();
            }
        }

        foreach (Transform child in transform.Find("Merged Layers"))
        {
            child.transform.localScale = new Vector3(level.Width, level.Height, 1f);
        }

        _chunkParent.gameObject.SetActive(false);
    }

    private GameObject MakeQuadRenderer()
    {
        var obj = new GameObject();
        var ren = obj.AddComponent<MeshRenderer>();
        ren.material = GeoMaterial;

        var filter = obj.AddComponent<MeshFilter>();

        var mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(0f, 0f),
            new Vector3(1f, 0f),
            new Vector3(0f, -1f),
            new Vector3(1f, -1f),
        };
        mesh.uv = new Vector2[]
        {
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
        };
        mesh.SetIndices(new int[] { 0, 1, 2, 2, 1, 3 }, MeshTopology.Triangles, 0);

        filter.sharedMesh = mesh;

        return obj;
    }

    public enum ViewMode
    {
        GeoOnly,
        GeoAndTiles,
        Tiled
    }
}
