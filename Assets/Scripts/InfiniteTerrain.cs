using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{
    public const float maxViewDistance = 300;
    public Transform viewer;

    public static Vector2 viewerPosition;

    private int chunkSize;
    private int chunksInDistance;
    private Dictionary<Vector2, TerrainChunk> chunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> chunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start()
    {
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
                    terrainChunk = new TerrainChunk(viewedChunkCoord, chunkSize, transform);
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

        public TerrainChunk(Vector2 coord, int size, Transform parent)
        {
            pos = coord * size;
            bounds = new Bounds(pos, Vector2.one * size);
            Vector3 posV3 = new Vector3(pos.x, 0, pos.y);

            meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject.transform.position = posV3;
            meshObject.transform.localScale = Vector3.one * size / 10f;
            meshObject.transform.SetParent(parent);
            SetVisible(false);
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
