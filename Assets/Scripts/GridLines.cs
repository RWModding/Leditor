using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class GridLines : MonoBehaviour
{
    [HideInInspector]
    public static GridLines Instance;

    private Material lineMaterial;

    public Vector2 gridStart;
    private Color gridColor = new Color(0, 0, 1, 0.1f);
    private Color cursorRectColor = new Color(1, 0, 1);

    [HideInInspector]
    public bool rectSelectMode;
    [HideInInspector]
    public bool rectDeleteMode;
    [HideInInspector]
    public Vector2 rectSelectStart;
    [HideInInspector]
    public Vector2 rectSelectEnd;

    private Color rectSelectColor = new Color(1, 0, 1);
    private Color rectDeleteColor = new Color(1, 0, 0);

    private new Camera camera;

    private void Awake()
    {
        Instance = this;
        camera = GetComponent<Camera>();
    }

    void Start()
    {
        var shader = Shader.Find("Hidden/Internal-Colored");
        lineMaterial = new Material(shader);
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
    }

    void OnPostRender()
    {
        if (ReferenceEquals(EditorManager.Instance.CurrentEditor, null)) return;

        var levelMatrix = EditorManager.Instance.CurrentEditor.CurrentLevelMatrix;

        var size = new Vector2(levelMatrix.Width, levelMatrix.Height);

        lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(EditorManager.Instance.CurrentEditor.RootObj.transform.localToWorldMatrix);

        GL.Begin(GL.LINES);
        GL.Color(gridColor);

        for (var x = 0; x <= size.x; x++)
        {
            GL.Vertex3(gridStart.x + x, gridStart.y, -5);
            GL.Vertex3(gridStart.x + x, gridStart.y - size.y, -5);
        }
        for (var y = 0; y <= size.y; y++)
        {
            GL.Vertex3(gridStart.x, gridStart.y - y, -5);
            GL.Vertex3(gridStart.x + size.x, gridStart.y - y, -5);
        }
        GL.End();

        if (rectSelectMode || rectDeleteMode)
        {
            var top    = Mathf.Max(rectSelectStart.y, rectSelectEnd.y);
            var right  = Mathf.Max(rectSelectStart.x, rectSelectEnd.x);
            var bottom = Mathf.Min(rectSelectStart.y, rectSelectEnd.y);
            var left   = Mathf.Min(rectSelectStart.x, rectSelectEnd.x);

            DrawRect(new Vector2(left, top), new Vector2(right, bottom), rectDeleteMode ? rectDeleteColor : rectSelectColor);
        }
        else
        {
            Vector2 mousePos = camera.ScreenToWorldPoint(Input.mousePosition);

            if (mousePos.x >= gridStart.x && mousePos.x < gridStart.x + size.x && mousePos.y <= gridStart.y && mousePos.y > gridStart.y - size.y)
            {
                DrawRect(mousePos, mousePos, cursorRectColor);
            }
        }

        if (NetworkManager.Singleton.IsClient)
        {
            foreach (var player in Networking.Players)
            {
                if (player.PlayerID != NetworkManager.Singleton.LocalClientId && player.CurrentTabName == EditorManager.Instance.CurrentTab?.File.Name)
                {
                    var mousePos = player.CursorPos;

                    if (mousePos.x >= gridStart.x && mousePos.x < gridStart.x + size.x && mousePos.y <= gridStart.y && mousePos.y > gridStart.y - size.y)
                    {
                        DrawRect(mousePos, mousePos, player.CursorColor);
                    }
                }
            }
        }

        if (Operation.Current != null)
        {
            foreach (var action in Operation.Current.Actions)
            {
                DrawRect(new (action.Position.x, action.Position.y), new (action.Position.x, action.Position.y), cursorRectColor);
            }
        }

        GL.PopMatrix();
    }

    private void DrawRect(Vector2 topLeft, Vector2 bottomRight, Color color)
    {
        GL.Begin(GL.LINE_STRIP);
        GL.Color(color);

        GL.Vertex3((int)topLeft.x, (int)topLeft.y, -5.1f);
        GL.Vertex3((int)bottomRight.x + 1, (int)topLeft.y, -5.1f);
        GL.Vertex3((int)bottomRight.x + 1, (int)bottomRight.y - 1, -5.1f);
        GL.Vertex3((int)topLeft.x, (int)bottomRight.y - 1, -5.1f);
        GL.Vertex3((int)topLeft.x, (int)topLeft.y, -5.1f);

        GL.End();
    }
}