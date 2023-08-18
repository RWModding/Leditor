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

        Vector3[] verts = new Vector3[xDivs * 4 + yDivs * 4];
        Vector2[] uvs = new Vector2[verts.Length];
        int[] inds = new int[xDivs * 4 + yDivs * 4];

        // Make a quad per horizontal line
        int i = 0;
        for(int x = 0; x < xDivs; x++)
        {
            verts[i + 0] = verts[i + 1] = new Vector2(x, 0f);
            verts[i + 2] = verts[i + 3] = new Vector2(x, 1 - yDivs);
            uvs[i + 0] = new Vector2(-1f, 0f);
            uvs[i + 1] = new Vector2(1f, 0f);
            uvs[i + 2] = new Vector2(-1f, 0f);
            uvs[i + 3] = new Vector2(1f, 0f);
            i += 4;
        }

        // Do the same for vertical lines
        for(int y = 0; y < yDivs; y++)
        {
            verts[i + 0] = verts[i + 1] = new Vector2(xDivs - 1, -y);
            verts[i + 2] = verts[i + 3] = new Vector2(0f, -y);
            uvs[i + 0] = new Vector2(0f, -1f);
            uvs[i + 1] = new Vector2(0f, 1f);
            uvs[i + 2] = new Vector2(0f, -1f);
            uvs[i + 3] = new Vector2(0f, 1f);
            i += 4;
        }

        // Make indices
        for(i = 0; i < inds.Length; i++)
        {
            inds[i] = i;
        }

        _mesh.Clear();
        _mesh.SetVertices(verts);
        _mesh.SetUVs(0, uvs);
        _mesh.SetIndices(inds, MeshTopology.Quads, 0);
    }
}
