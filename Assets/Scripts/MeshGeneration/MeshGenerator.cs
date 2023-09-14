using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[][] heightMap, MeshSettings meshSettings, int levelOfDetail)
    {
        int skipIncrement = levelOfDetail == 0 ? 1 : levelOfDetail * 2;
        int numVertsPerLine = meshSettings.NumVertsPerLine;
        Vector2 topLeft = new Vector2(-1, 1) * meshSettings.MeshWorldSize / 2f;

        MeshData mesh = new MeshData(numVertsPerLine, skipIncrement, meshSettings.useFlatShading);
        int[][] vertexIndexMap = JaggedArray.CreateJaggedArray<int[][]>(numVertsPerLine, numVertsPerLine);
        int meshVertexIndex = 0;
        int outOfMeshVertexIndex = -1;
        for (int y = 0; y < numVertsPerLine; y++)
        {
            for (int x = 0; x < numVertsPerLine; x++)
            {
                bool isOutOfMeshVertex = x == 0 || x == numVertsPerLine - 1 || y == 0 || y == numVertsPerLine - 1;
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 &&
                                       y > 2 && y < numVertsPerLine - 3 &&
                                       ((x - 2) % skipIncrement != 0 ||
                                        (y - 2) % skipIncrement != 0);
                if(isOutOfMeshVertex)
                {
                    vertexIndexMap[x][y] = outOfMeshVertexIndex;
                    outOfMeshVertexIndex--;
                }
                else if(!isSkippedVertex)
                {
                    vertexIndexMap[x][y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }


        for (int y = 0; y < numVertsPerLine; y++)
        {
            for(int x = 0; x < numVertsPerLine; x++)
            {
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 &&
                                       y > 2 && y < numVertsPerLine - 3 &&
                                       ((x - 2) % skipIncrement != 0 ||
                                        (y - 2) % skipIncrement != 0);
                if (!isSkippedVertex)
                {
                    bool isOutOfMeshVertex = x == 0 || x == numVertsPerLine - 1 || y == 0 || y == numVertsPerLine - 1;
                    bool isMeshEdgeVertex = x == 1 || x == numVertsPerLine - 2 || y == 1 || y == numVertsPerLine - 2 && !isOutOfMeshVertex;
                    bool isMainVertex = (x - 2) % skipIncrement == 0 && (y - 2) % skipIncrement == 0 &&
                                        !isOutOfMeshVertex && !isMeshEdgeVertex;
                    bool isEdgeConnectionVertex =
                        (y == 2 || y == numVertsPerLine - 3 || x == 2 || x == numVertsPerLine - 3) && 
                        !isMainVertex && !isMeshEdgeVertex && !isOutOfMeshVertex;
                    
                    int vertexIndex = vertexIndexMap[x][y];
                    Vector2 percent = new Vector2(x - 1, y - 1) / (numVertsPerLine - 3);
                    Vector2 vertexPos2D = topLeft + new Vector2(percent.x, -percent.y) * meshSettings.MeshWorldSize;
                    float height = heightMap[x][y];

                    if (isEdgeConnectionVertex)
                    {
                        bool isVertical = x == 2 || x == numVertsPerLine - 3;
                        int dstToMainVertexA = (isVertical ? y - 2 : x - 2) % skipIncrement;
                        int dstToMainVertexB = skipIncrement - dstToMainVertexA;
                        float dstPercentFromAToB = dstToMainVertexA / (float)skipIncrement;
                        
                        float heightMainVertexA = heightMap[isVertical ? x : x - dstToMainVertexA][isVertical ? y - dstToMainVertexA : y];
                        float heightMainVertexB = heightMap[isVertical ? x : x + dstToMainVertexB][isVertical ? y + dstToMainVertexB : y];

                        height = heightMainVertexA * (1 - dstPercentFromAToB) + heightMainVertexB * dstPercentFromAToB;
                    }
                    
                    mesh.AddVertex(new Vector3(vertexPos2D.x, height, vertexPos2D.y), percent, vertexIndex);

                    bool createTriangle = x < numVertsPerLine - 1 && y < numVertsPerLine - 1 &&
                                          (!isEdgeConnectionVertex ||
                                          (x != 2 && y != 2));
                    
                    if (createTriangle)
                    {
                        int currentIncrement = (isMainVertex && x != numVertsPerLine - 3 && y != numVertsPerLine - 3)
                            ? skipIncrement
                            : 1;
                        int a = vertexIndexMap[x][y];
                        int b = vertexIndexMap[x + currentIncrement][y];
                        int c = vertexIndexMap[x][y + currentIncrement];
                        int d = vertexIndexMap[x + currentIncrement][y + currentIncrement];
                        mesh.AddTriangle(a, d, c);
                        mesh.AddTriangle(d, a, b);
                    }
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
    private Vector3[] outOfMeshVertices;
    private Vector3[] bakedNormals;

    private int[] triangles;
    private int[] outOfMeshTriangles;
    private Vector2[] uvs;

    private int triangleIndex;
    private int outOfMeshTriangleIndex;
    private bool useFlatShading;

    public MeshData(int numVertsPerLine, int skipIncrement, bool useFlatShading)
    {
        this.useFlatShading= useFlatShading;

        int numMeshEdgeVertices = (numVertsPerLine - 2) * 4 - 4;
        int numEdgeConnectionVertices = (skipIncrement - 1) * (numVertsPerLine - 5) / skipIncrement * 4;
        int numMainVerticesPerLine = (numVertsPerLine - 5) / skipIncrement + 1;
        int numMainVertices = numMainVerticesPerLine * numMainVerticesPerLine;
        
        meshVertices = new Vector3[numMeshEdgeVertices + numEdgeConnectionVertices + numMainVertices];
        uvs = new Vector2[meshVertices.Length];

        int numMeshEdgeTriangles = 8 * (numVertsPerLine - 4);
        int numMainTriangles = (numMainVerticesPerLine - 1) * (numMainVerticesPerLine - 1) * 2;
        triangles = new int[(numMeshEdgeTriangles +numMainTriangles) * 3];
        
        outOfMeshVertices = new Vector3[numVertsPerLine * 4 - 4];
        outOfMeshTriangles = new int[(numVertsPerLine - 2) * 24];
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
            outOfMeshVertices[-index - 1] = vertexPos;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            outOfMeshTriangles[outOfMeshTriangleIndex] = a;
            outOfMeshTriangles[outOfMeshTriangleIndex + 1] = b;
            outOfMeshTriangles[outOfMeshTriangleIndex + 2] = c;
            outOfMeshTriangleIndex += 3;
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
        int borderTriangleCount = outOfMeshTriangles.Length/3;

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
            int borderVertexIndexA = outOfMeshTriangles[triangleIndex];
            int borderVertexIndexB = outOfMeshTriangles[triangleIndex + 1];
            int borderVertexIndexC = outOfMeshTriangles[triangleIndex + 2];

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
        Vector3 pointA = a >= 0 ? meshVertices[a] : outOfMeshVertices[-a - 1];
        Vector3 pointB = b >= 0 ? meshVertices[b] : outOfMeshVertices[-b - 1];
        Vector3 pointC = c >= 0 ? meshVertices[c] : outOfMeshVertices[-c - 1];
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
