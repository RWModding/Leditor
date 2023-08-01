using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    private static readonly float zoomFac = Mathf.Pow(2f, 1f / 4f);

    private LevelEditor editor;
    private new Camera camera;
    private int zoom = Mathf.CeilToInt(Mathf.Log(20f, zoomFac));

    void Awake()
    {
        editor = GetComponentInParent<LevelEditor>();
        camera = GetComponent<Camera>();
        Zoom(0);
    }

    public void OnLevelLoaded()
    {
    }

    public void Update()
    {
        if (editor.LevelData == null) return;

        var camSize = new Vector2(camera.orthographicSize * camera.aspect, camera.orthographicSize);
        var geoSize = new Vector2(editor.LevelData.Width, editor.LevelData.Height);

        var min = -camSize + Vector2.one;
        var max = geoSize + camSize - Vector2.one;

        Vector2 pos = transform.localPosition;
        pos.x = Mathf.Max(min.x, Mathf.Min(max.x, pos.x));
        pos.y = Mathf.Max(-max.y, Mathf.Min(-min.y, pos.y));
        transform.localPosition = new Vector3(pos.x, pos.y, transform.localPosition.z);
    }

    public void Zoom(int delta)
    {
        if (editor.LevelData != null)
        {
            int maxZoom = Mathf.CeilToInt(Mathf.Log(Mathf.Max(editor.LevelData.Height, editor.LevelData.Width) * 2f, zoomFac));
            zoom = Mathf.Clamp(zoom + delta, 8, maxZoom);
        }
        camera.orthographicSize = Mathf.Pow(zoomFac, zoom);

        if(Mathf.Abs(camera.orthographicSize - Mathf.Round(camera.orthographicSize)) < 0.1f)
        {
            camera.orthographicSize = Mathf.Round(camera.orthographicSize);
        }
    }
}
