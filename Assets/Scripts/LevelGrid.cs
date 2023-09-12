using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class LevelGrid : MonoBehaviour
{
    private Mesh _mesh;

    public void OnLevelLoaded()
    {
        var loader = GetComponentInParent<LevelLoader>();
        GenerateMesh(loader.LevelData.Width + 1, loader.LevelData.Height + 1);
        GetComponent<MeshRenderer>().sortingOrder = 10;
    }

    void GenerateMesh(int xDivs, int yDivs)
    {
        if(!_mesh)
        {
            _mesh = new Mesh();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        Vector3[] verts = new Vector3[xDivs * 2 + yDivs * 2];
        int[] inds = new int[xDivs * 2 + yDivs * 2];

        // Make a vert pair per horizontal line
        int i = 0;
        for(int x = 0; x < xDivs; x++)
        {
            verts[i + 0] = new Vector2(x, 0f);
            verts[i + 1] = new Vector2(x, 1 - yDivs);
            i += 2;
        }

        // Do the same for vertical lines
        for(int y = 0; y < yDivs; y++)
        {
            verts[i + 0] = new Vector2(xDivs - 1, -y);
            verts[i + 1] = new Vector2(0f, -y);
            i += 2;
        }

        // Make indices
        for(i = 0; i < inds.Length; i++)
        {
            inds[i] = i;
        }

        _mesh.Clear();
        _mesh.SetVertices(verts);
        _mesh.SetIndices(inds, MeshTopology.Lines, 0);
    }
}
