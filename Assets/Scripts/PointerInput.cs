using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PointerInput : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IScrollHandler,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public CameraController CameraController;
    public ToolGroup Tools;
    private Camera _cam;
    private Vector2 _dragOrigin;

    private Texture2D _cursorOverride;
    private Vector2 _cursorHotspot;
    private bool _pointerOver;
    private LevelLoader _loader;

    private Tool CurrentTool => Tools && _loader && _loader.LevelData != null ? Tools.CurrentTool : null;

    void Awake()
    {
        _cam = CameraController.GetComponent<Camera>();
        if (Tools)
        {
            _loader = Tools.GetComponentInParent<LevelLoader>();
        }
    }

    private void Update()
    {
        if (_pointerOver)
        {
            var newOverride = CurrentTool ? CurrentTool.CursorOverride : null;
            var newHotspot = CurrentTool ? CurrentTool.CursorHotspot : Vector2.zero;

            if (_cursorOverride != newOverride || newOverride && _cursorHotspot != newHotspot)
            {
                Cursor.SetCursor(newOverride, newHotspot, CursorMode.Auto);
                _cursorOverride = newOverride;
                _cursorHotspot = newHotspot;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _pointerOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Cursor.SetCursor(null, default, CursorMode.Auto);
        _cursorOverride = null;
        _pointerOver = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Middle)
        {
            _dragOrigin = eventData.position;
        }
        else
        {
            if (CurrentTool)
                CurrentTool.OnBeginDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Middle)
        {
            Vector2 delta = eventData.position - _dragOrigin;
            delta *= _cam.orthographicSize / _cam.pixelHeight * 2f;

            _cam.transform.localPosition -= new Vector3(delta.x, delta.y);
            _dragOrigin = eventData.position;
        }
        else
        {
            if (CurrentTool)
                CurrentTool.OnDrag(eventData);
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (Keybinds.Shift)
            BrushUtils.Resize((int)eventData.scrollDelta.y);
        else
            CameraController.Zoom(-(int)eventData.scrollDelta.y);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (CurrentTool)
            CurrentTool.OnEndDrag(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Middle)
        {
            if (CurrentTool)
                CurrentTool.OnPointerClick(eventData);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (CurrentTool)
            CurrentTool.OnPointerUp(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (CurrentTool)
            CurrentTool.OnPointerDown(eventData);
    }
}
