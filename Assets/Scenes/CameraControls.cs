using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.Image;

public class CameraControls : MonoBehaviour
{
    private const float KeyboardZoomSpeed = 15f;
    private const float ScrollZoomSpeed = 40f;

    private new Camera camera;
    
    private bool dragging;
    private Vector3 cameraStart;
    private Vector3 dragStart;

    private Vector2 minPos;
    private Vector2 maxPos;

    private float minZoom;
    private float maxZoom;

    private void Awake()
    {
        camera = GetComponent<Camera>();

    }

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }


    void Update()
    {
        HandleCameraDrag();
        HandleCameraZoom();
        HandleCameraConstraints();
    }

    public void SetContraints(Vector2 minPos, Vector2 maxPos, float minZoom = -1, float maxZoom = -1)
    {
        this.minPos = minPos;
        this.maxPos = maxPos;

        if (minZoom != -1)
        {
            this.minZoom = minZoom;
        }
        if (maxZoom != -1)
        {
            this.maxZoom = maxZoom;
        }
    }

    private void HandleCameraConstraints()
    {
        camera.orthographicSize = Mathf.Clamp(camera.orthographicSize, 1, 100);

        var pos = camera.transform.position;
        camera.transform.position = new Vector3(Mathf.Clamp(pos.x, minPos.x, maxPos.x), Mathf.Clamp(pos.y, minPos.y, maxPos.y), pos.z);
    }

    private void HandleCameraZoom()
    {
        var zoom = 0f;

        var scrollDelta = Input.mouseScrollDelta.y * Time.deltaTime;
        if (scrollDelta != 0)
        {
            zoom += scrollDelta * ScrollZoomSpeed;
        }

        if (Input.GetKey(KeyCode.KeypadMinus) || Input.GetKey(KeyCode.Minus))
        {
            zoom -= KeyboardZoomSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.Equals))
        {
            zoom += KeyboardZoomSpeed * Time.deltaTime;
        }

        if (zoom != 0)
        {
            camera.orthographicSize -= zoom;
        }
    }

    private void HandleCameraDrag()
    {
        if (Input.GetMouseButton(1))
        {
            if (!dragging)
            {
                dragging = true;
                dragStart = camera.ScreenToWorldPoint(Input.mousePosition);
            }

            Vector2 dragOffset = dragStart - camera.ScreenToWorldPoint(Input.mousePosition);

            camera.transform.position += new Vector3(dragOffset.x, dragOffset.y, 0);
        }
        else
        {
            dragging = false;
        }
    }
}