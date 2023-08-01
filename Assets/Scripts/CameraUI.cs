using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
{
    public CameraController CameraController;
    private Camera cam;
    private Vector2 dragOrigin;

    void Awake()
    {
        cam = CameraController.GetComponent<Camera>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragOrigin = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - dragOrigin;
        delta *= cam.orthographicSize / cam.pixelHeight * 2f;

        cam.transform.localPosition -= new Vector3(delta.x, delta.y);
        dragOrigin = eventData.position;
    }

    public void OnScroll(PointerEventData eventData)
    {
        CameraController.Zoom(-(int)eventData.scrollDelta.y);
    }
}
