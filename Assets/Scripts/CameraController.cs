using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    private static readonly float zoomFac = Mathf.Pow(2f, 1f / 4f);

    private LevelLoader _editor;
    private Camera _camera;
    private int _zoom = Mathf.CeilToInt(Mathf.Log(20f, zoomFac));

    void Awake()
    {
        _editor = GetComponentInParent<LevelLoader>();
        _camera = GetComponent<Camera>();
        Zoom(0);
    }

    public void Update()
    {
        RestrictToLevel();
    }

    private void RestrictToLevel()
    {
        if (_editor.LevelData == null) return;

        var camSize = new Vector2(_camera.orthographicSize * _camera.aspect, _camera.orthographicSize);
        var geoSize = new Vector2(_editor.LevelData.Width, _editor.LevelData.Height);

        var min = -camSize + Vector2.one;
        var max = geoSize + camSize - Vector2.one;

        Vector2 pos = transform.localPosition;
        pos.x = Mathf.Max(min.x, Mathf.Min(max.x, pos.x));
        pos.y = Mathf.Max(-max.y, Mathf.Min(-min.y, pos.y));
        transform.localPosition = new Vector3(pos.x, pos.y, transform.localPosition.z);
    }

    public void Zoom(int delta)
    {
        if (_editor.LevelData != null)
        {
            int maxZoom = Mathf.CeilToInt(Mathf.Log(Mathf.Max(_editor.LevelData.Height, _editor.LevelData.Width) * 2f, zoomFac));
            _zoom = Mathf.Clamp(_zoom + delta, 8, maxZoom);
        }
        _camera.orthographicSize = Mathf.Pow(zoomFac, _zoom);

        if(Mathf.Abs(_camera.orthographicSize - Mathf.Round(_camera.orthographicSize)) < 0.1f)
        {
            _camera.orthographicSize = Mathf.Round(_camera.orthographicSize);
        }
    }
}
