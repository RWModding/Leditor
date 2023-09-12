using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class BrushUtils
{
    public static int SharedDiameter = 1;

    public static void Resize(int delta)
    {
        SharedDiameter = Mathf.Clamp(SharedDiameter + delta, 1, 32);
    }

    public static void MakeOutline(out GameObject obj, out Mesh mesh)
    {
        mesh = new Mesh();
        obj = new GameObject("Brush Preview", typeof(MeshRenderer), typeof(MeshFilter));
        obj.GetComponent<MeshFilter>().sharedMesh = mesh;

        var mat = new Material(Shader.Find("Custom/HighContrast"));
        mat.mainTexture = Texture2D.whiteTexture;
        obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    public static void UpdateOutlineMesh(Mesh mesh, int diameter)
    {
        mesh.Clear();

        var verts = new List<Vector3>();
        var inds = new List<int>();

        // Horizontal lines
        for (int y = 0; y <= diameter; y++)
        {
            for (int x = 0; x <= diameter; x++)
            {
                bool topSolid = CircleContains(x, y, diameter);
                bool botSolid = CircleContains(x, y - 1, diameter);
                if (topSolid != botSolid)
                {
                    inds.Add(verts.Count);
                    verts.Add(new Vector2(x, -y));
                    while (CircleContains(x + 1, y, diameter) == topSolid && CircleContains(x + 1, y - 1, diameter) == botSolid)
                        x++;
                    inds.Add(verts.Count);
                    verts.Add(new Vector2(x + 1, -y));
                }
            }
        }

        // Vertical lines
        for (int x = 0; x <= diameter; x++)
        {
            for (int y = 0; y <= diameter; y++)
            {
                bool rightSolid = CircleContains(x, y, diameter);
                bool leftSolid = CircleContains(x - 1, y, diameter);
                if (rightSolid != leftSolid)
                {
                    inds.Add(verts.Count);
                    verts.Add(new Vector2(x, -y));
                    while (CircleContains(x, y + 1, diameter) == rightSolid && CircleContains(x - 1, y + 1, diameter) == leftSolid)
                        y++;
                    inds.Add(verts.Count);
                    verts.Add(new Vector2(x, -(y + 1)));
                }
            }
        }

        mesh.SetVertices(verts);
        mesh.SetIndices(inds, MeshTopology.Lines, 0);
    }

    public static bool CircleContains(int x, int y, int diameter)
    {
        int d2x = x * 2 + 1 - diameter;
        int d2y = y * 2 + 1 - diameter;
        return d2x * d2x + d2y * d2y < diameter * diameter;
    }
}