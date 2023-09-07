using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail, bool useFlatShading)
    {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        int meshSimplificationIncrement = levelOfDetail == 0 ? 1 : levelOfDetail * 2;

        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;

        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;
        int verticesPerLine = ((meshSize - 1) / meshSimplificationIncrement + 1);

        MeshData mesh = new MeshData(verticesPerLine, useFlatShading);
        int[,] vertexIndexMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;
        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                if(x == 0 || x == borderedSize - 1 || y == 0 || y == borderedSize - 1)
                {
                    vertexIndexMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndexMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }


        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for(int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                int vertexIndex = vertexIndexMap[x, y];
                Vector2 percent = new Vector2((x - meshSimplificationIncrement) / (float)meshSize, (y - meshSimplificationIncrement) / (float)meshSize);
                Vector3 vertex = new Vector3(topLeftX + percent.x * meshSizeUnsimplified,
                    heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier,
                    topLeftZ - percent.y * meshSizeUnsimplified);
                mesh.AddVertex(vertex, percent, vertexIndex);

                if (x < borderedSize - 1 && y < borderedSize - 1)
                {
                    int topLeftIndex = vertexIndex;
                    int topRightIndex = vertexIndexMap[x + meshSimplificationIncrement, y];
                    int bottomRight = vertexIndexMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];
                    int bottomLeft = vertexIndexMap[x, y + meshSimplificationIncrement];
                    mesh.AddTriangle(topLeftIndex, topRightIndex, bottomRight);
                    mesh.AddTriangle(bottomRight, bottomLeft, topLeftIndex);
                }
            }
        }
        mesh.Finalize();
        return mesh;
    }
}

public class MeshData
{
    private Vector3[] meshVertices;
    private Vector3[] borderVertices;
    private Vector3[] bakedNormals;

    private int[] triangles;
    private int[] borderTriangles;
    private Vector2[] uvs;

    private int triangleIndex;
    private int borderTriangleIndex;
    private bool useFlatShading;

    public MeshData(int verticesPerLine, bool useFlatShading)
    {
        meshVertices = new Vector3[verticesPerLine * verticesPerLine];
        uvs = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];
        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[verticesPerLine * 24];
        this.useFlatShading= useFlatShading;
    }

    public void AddVertex(Vector3 vertexPos, Vector2 uv, int index)
    {
        if(index >= 0)
        {
            meshVertices[index] = vertexPos;
            uvs[index] = uv;
        }
        else
        {
            borderVertices[-index - 1] = vertexPos;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3;
        }
        else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    }

    private Vector3[] CalculateNormals()
    {
        Vector3[] normals = new Vector3[meshVertices.Length];
        int triangleCount = triangles.Length/ 3;
        int borderTriangleCount = borderTriangles.Length/3;

        for (int i = 0; i < triangleCount; i++)
        {
            int triangleIndex = i * 3;
            int vertexIndexA = triangles[triangleIndex];
            int vertexIndexB = triangles[triangleIndex + 1];
            int vertexIndexC = triangles[triangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromVertices(vertexIndexA, vertexIndexB, vertexIndexC);
            normals[vertexIndexA] += triangleNormal;
            normals[vertexIndexB] += triangleNormal;
            normals[vertexIndexC] += triangleNormal;
        }

        for(int i = 0; i < borderTriangleCount; i++)
        {
            int triangleIndex = i * 3;
            int borderVertexIndexA = borderTriangles[triangleIndex];
            int borderVertexIndexB = borderTriangles[triangleIndex + 1];
            int borderVertexIndexC = borderTriangles[triangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromVertices(borderVertexIndexA, borderVertexIndexB, borderVertexIndexC);
            if(borderVertexIndexA > 0) normals[borderVertexIndexA] += triangleNormal;
            if (borderVertexIndexB > 0) normals[borderVertexIndexB] += triangleNormal;
            if (borderVertexIndexC > 0) normals[borderVertexIndexC] += triangleNormal;
        }

        for(int i = 0; i < normals.Length; i++)
        {
            normals[i].Normalize();
        }
        return normals;
    }

    private Vector3 SurfaceNormalFromVertices(int a, int b, int c)
    {
        Vector3 pointA = a >= 0 ? meshVertices[a] : borderVertices[-a - 1];
        Vector3 pointB = b >= 0 ? meshVertices[b] : borderVertices[-b - 1];
        Vector3 pointC = c >= 0 ? meshVertices[c] : borderVertices[-c - 1];
        Vector3 ab = pointB - pointA;
        Vector3 ac = pointC - pointA;
        return Vector3.Cross(ab, ac).normalized;
    }

    private void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    private void FlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[triangles.Length];

        for(int i = 0; i < triangles.Length; i++)
        {
            flatShadedVertices[i] = meshVertices[triangles[i]];
            flatShadedUvs[i] = uvs[triangles[i]];
            triangles[i] = i;
        }

        meshVertices = flatShadedVertices;
        uvs= flatShadedUvs;
    }

    public void Finalize()
    {
        if (useFlatShading)
        {
            FlatShading();
        }
        else
        {
            BakeNormals();
        }
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = meshVertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        if (useFlatShading)
        {
            mesh.RecalculateNormals();
        }
        else
        {
            mesh.normals = bakedNormals;
        }
        return mesh;
    }
}
