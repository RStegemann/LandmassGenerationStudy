using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{
    public LODInfo[] detailLevels;
    public static float maxViewDistance = 600;

    public Transform viewer;
    public Material terrainMaterial;

    public static Vector2 viewerPosition;
    private static MapGenerator mapGenerator;

    private int chunkSize;
    private int chunksInDistance;
    private Dictionary<Vector2, TerrainChunk> chunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> chunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        chunkSize = mapGenerator.MapChunkSize() - 1;
        maxViewDistance = detailLevels[detailLevels.Length-1].visibleDistanceThreshold;
        chunksInDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);
        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / mapGenerator.terrainSettings.uniformScale;
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
                }
                else
                {
                    terrainChunk = new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, terrainMaterial);
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
        MeshCollider collider;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        LODMesh collisionLodMesh;

        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            pos = coord * size;
            bounds = new Bounds(pos, Vector2.one * size);
            Vector3 posV3 = new Vector3(pos.x, 0, pos.y) * mapGenerator.terrainSettings.uniformScale;
            this.detailLevels = detailLevels;

            meshObject = new GameObject("Terrain Chunk");
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer = meshObject.AddComponent<MeshRenderer>();

            meshObject.transform.position = posV3;
            meshObject.transform.SetParent(parent);
            meshObject.transform.localScale = new Vector3(mapGenerator.terrainSettings.uniformScale, mapGenerator.terrainSettings.uniformScale, mapGenerator.terrainSettings.uniformScale);
            collider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;

            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            {
                for(int i = 0; i < detailLevels.Length; i++)
                {
                    lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
                    if (detailLevels[i].useForCollider)
                    {
                        collisionLodMesh = lodMeshes[i];
                    }
                }
            }
            mapGenerator.RequestMapData(pos, OnMapDataReceived);
        }

        private void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;
            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float distanceToViewer = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = distanceToViewer <= maxViewDistance;
                if (visible)
                {
                    int lodIndex = 0;

                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (distanceToViewer > detailLevels[i].visibleDistanceThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequested)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                        if(lodIndex == 0)
                        {
                            if (collisionLodMesh.hasMesh){
                                collider.sharedMesh = collisionLodMesh.mesh;
                            }else if (!collisionLodMesh.hasRequested)
                            {
                                collisionLodMesh.RequestMesh(mapData);
                            }
                        }
                        else
                        {
                            collider.sharedMesh = null;
                        }
                    }
                    chunksVisibleLastUpdate.Add(this);
                }
                SetVisible(visible);
            }
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

    private class LODMesh
    {
        public Mesh mesh;
        public bool hasRequested = false;
        public bool hasMesh = false;
        private int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        private void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;
            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequested = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDistanceThreshold;
        public bool useForCollider;
    }
}
