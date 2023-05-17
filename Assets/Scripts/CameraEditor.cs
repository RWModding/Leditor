using Lingo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraEditor : MonoBehaviour
{
    public GameObject cameraPrefab;

    private EditorFile editorFile;
    private CameraData camData => editorFile.Cameras;

    public bool Visible;

    private List<GameObject> cameraObjects;
    // Start is called before the first frame update
    void Start()
    {
        editorFile = EditorManager.FileToLoad;
        cameraObjects = new();
        foreach (Vector2 vec in camData.cameraPos)
        {
            GameObject childObj = Instantiate(cameraPrefab, transform);
            childObj.transform.position = (vec / 20f) * new Vector3(1f, -1f, 1f);
            childObj.GetComponent<LineRenderer>().forceRenderingOff = Visible;
            cameraObjects.Add(childObj);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Visible = !Visible;
            foreach (GameObject cam in cameraObjects)
            {
            cam.GetComponent<LineRenderer>().forceRenderingOff = Visible;
            }
        }
        
        //for (int i = 0; i < cameraObjects.Count; i++)
        //{ cameraObjects[i].transform.position = camData.cameraPos[i] / 20f; }
    }
}
