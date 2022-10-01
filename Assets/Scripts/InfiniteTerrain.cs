using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{
    public const float maxViewDistance = 600;
    public Transform viewer;
    public Material terrainMaterial;

    public static Vector2 viewerPosition;
    private static MapGenerator mapGenerator;

    private int chunkSize;
    private int chunksInDistance;
    private Dictionary<Vector2, TerrainChunk> chunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> chunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksInDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
    }

    private void UpdateVisibleChunks()
    {
        foreach(TerrainChunk chunk in chunksVisibleLastUpdate)
        {
            chunk.SetVisible(false);
        }
        chunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for(int yOffset = -chunksInDistance; yOffset <= chunksInDistance; yOffset++)
        {
            for(int xOffset = -chunksInDistance; xOffset <= chunksInDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if(chunkDictionary.TryGetValue(viewedChunkCoord, out TerrainChunk terrainChunk))
                {
                    terrainChunk.UpdateTerrainChunk();
                    if (terrainChunk.isVisible()) chunksVisibleLastUpdate.Add(terrainChunk);
                }
                else
                {
                    terrainChunk = new TerrainChunk(viewedChunkCoord, chunkSize, transform, terrainMaterial);
                    chunkDictionary.Add(viewedChunkCoord, terrainChunk);
                }
            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 pos;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        public TerrainChunk(Vector2 coord, int size, Transform parent, Material material)
        {
            pos = coord * size;
            bounds = new Bounds(pos, Vector2.one * size);
            Vector3 posV3 = new Vector3(pos.x, 0, pos.y);

            meshObject = new GameObject("Terrain Chunk");
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer = meshObject.AddComponent<MeshRenderer>();

            meshObject.transform.position = posV3;
            meshObject.transform.SetParent(parent);
            meshRenderer.material = material;
            SetVisible(false);

            mapGenerator.RequestMapData(OnMapDataReceived);
        }

        private void OnMapDataReceived(MapData mapData)
        {
            mapGenerator.RequestMeshData(mapData, OnMeshDataReceived);
            meshRenderer.material.mainTexture = TextureGenerator.TextureFromColorMap(mapData.colorMap, mapData.heightMap.GetLength(0), mapData.heightMap.GetLength(1));
        }

        private void OnMeshDataReceived(MeshData meshData)
        {
            meshFilter.mesh = meshData.CreateMesh();
        }

        public void UpdateTerrainChunk()
        {
            float distanceToViewer = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            SetVisible(distanceToViewer <= maxViewDistance);
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool isVisible()
        {
            return meshObject.activeSelf;
        }
    }
}
