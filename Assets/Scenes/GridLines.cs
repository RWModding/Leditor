using UnityEngine;

public class GridLines : MonoBehaviour
{

    Material lineMaterial;

    public Vector2 start;
    public Vector2 size;
    public Color color = new Color(0, 0, 1, 0.1f);
    public Color cursorColor = new Color(1, 0, 1);
    public Matrix4x4 transformMatrix;
    
    new Camera camera;


    private void Awake()
    {
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
        lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(transformMatrix);

        GL.Begin(GL.LINES);

        GL.Color(color);

        for (var x = 0; x <= size.x; x++)
        {
            GL.Vertex3(start.x + x, start.y, -5);
            GL.Vertex3(start.x + x, start.y - size.y, -5);
        }
        for (var y = 0; y <= size.y; y++)
        {
            GL.Vertex3(start.x, start.y - y, -5);
            GL.Vertex3(start.x + size.x, start.y - y, -5);
        }

        GL.Color(cursorColor);

        Vector2 mousePos = camera.ScreenToWorldPoint(Input.mousePosition);

        if (mousePos.x > start.x && mousePos.x < start.x + size.x && mousePos.y < start.y && mousePos.y > start.y - size.y)
        {
            //-- top
            GL.Vertex3((int)mousePos.x, (int)mousePos.y, -5.1f);
            GL.Vertex3((int)mousePos.x + 1, (int)mousePos.y, -5.1f);

            //-- bottom
            GL.Vertex3((int)mousePos.x, (int)mousePos.y - 1, -5.1f);
            GL.Vertex3((int)mousePos.x + 1, (int)mousePos.y - 1, -5.1f);

            //-- left
            GL.Vertex3((int)mousePos.x, (int)mousePos.y, -5.1f);
            GL.Vertex3((int)mousePos.x, (int)mousePos.y - 1, -5.1f);

            //-- right
            GL.Vertex3((int)mousePos.x + 1, (int)mousePos.y, -5.1f);
            GL.Vertex3((int)mousePos.x + 1, (int)mousePos.y - 1, -5.1f);
        }

        GL.End();
        GL.PopMatrix();
    }
}