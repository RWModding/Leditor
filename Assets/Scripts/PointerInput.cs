using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PointerInput : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IScrollHandler,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public CameraController CameraController;
    public Transform EditorParent;
    private Camera _cam;
    private Vector2 _dragOrigin;
    private EditorBase _currentEditor;

    void Awake()
    {
        _cam = CameraController.GetComponent<Camera>();
    }

    void Update()
    {
        if(EditorParent != null && (_currentEditor == null || !_currentEditor.gameObject.activeInHierarchy))
        {
            _currentEditor = null;

            foreach(Transform child in EditorParent)
            {
                if(child.gameObject.activeInHierarchy && child.TryGetComponent(out EditorBase newEditor))
                {
                    _currentEditor = newEditor;
                    break;
                }
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Middle)
        {
            _dragOrigin = eventData.position;
        }
        else
        {
            if (_currentEditor != null)
                _currentEditor.OnBeginDrag(eventData);
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
            if (_currentEditor != null)
                _currentEditor.OnDrag(eventData);
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        CameraController.Zoom(-(int)eventData.scrollDelta.y);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_currentEditor != null)
            _currentEditor.OnEndDrag(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Middle)
        {
            if (_currentEditor != null)
                _currentEditor.OnPointerClick(eventData);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_currentEditor != null)
            _currentEditor.OnPointerUp(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_currentEditor != null)
            _currentEditor.OnPointerDown(eventData);
    }
}
