using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;

public class CameraObject : MonoBehaviour
{
    private LineRenderer lineRenderer;

    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 4;
        lineRenderer.loop = true;
    }

    // Update is called once per frame
    void Update()
    {
        lineRenderer.SetPositions(new Vector3[] { transform.position, transform.position + (Vector3.right * 1400f) / 20f, transform.position + (Vector3.down * 800f + Vector3.right * 1400f) / 20f, transform.position + (Vector3.down * 800f) / 20f });
    }
}
