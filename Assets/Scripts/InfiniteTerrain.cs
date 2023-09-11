using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{
    private const float ViewerMoveThresholdForChunkUpdate = 25f;
    private const float SqrViewerMoveThresholdForChunkUpdate =
        ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;
    private const float colliderGenerationDistanceThreshold = 10f;
    
    public LODInfo[] detailLevels;
    public int lodColliderIndex = 1;
    private static float maxViewDistance;

    public Transform viewer;
    public Material terrainMaterial;

    public static Vector2 viewerPos;
    private Vector2 viewerPosOld;
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
        viewerPos = new Vector2(viewer.position.x, viewer.position.z) / mapGenerator.terrainSettings.uniformScale;
        if (viewerPos != viewerPosOld)
        {
            foreach (TerrainChunk chunk in chunksVisibleLastUpdate)
            {
                chunk.UpdateCollisionMesh();
            }
        }
        if ((viewerPosOld - viewerPos).sqrMagnitude > SqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPosOld = viewerPos;
            UpdateVisibleChunks();
        }
    }

    private void UpdateVisibleChunks()
    {
        foreach(TerrainChunk chunk in chunksVisibleLastUpdate)
        {
            chunk.SetVisible(false);
        }
        chunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPos.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPos.y / chunkSize);

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
                    terrainChunk = new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, terrainMaterial, lodColliderIndex);
                    chunkDictionary.Add(viewedChunkCoord, terrainChunk);
                }
            }
        }
    }

    public class TerrainChunk
    {
        private GameObject meshObject;
        private Vector2 pos;
        private Bounds bounds;

        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private MeshCollider collider;

        private LODInfo[] detailLevels;
        private LODMesh[] lodMeshes;
        private int colliderLODIndex;
        private int previousLODIndex = -1;
        private bool hasSetCollider;

        private MapData mapData;
        private bool mapDataReceived;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material, int colliderLODIndex)
        {
            pos = coord * size;
            bounds = new Bounds(pos, Vector2.one * size);
            Vector3 posV3 = new Vector3(pos.x, 0, pos.y) * mapGenerator.terrainSettings.uniformScale;
            this.detailLevels = detailLevels;
            this.colliderLODIndex = colliderLODIndex;

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
            for(int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod);
                lodMeshes[i].UpdateCallback += UpdateTerrainChunk;
                if (i == colliderLODIndex) lodMeshes[i].UpdateCallback += UpdateCollisionMesh;
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
                float distanceToViewer = Mathf.Sqrt(bounds.SqrDistance(viewerPos));
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
                    }
                    chunksVisibleLastUpdate.Add(this);
                }
                SetVisible(visible);
            }
        }

        public void UpdateCollisionMesh()
        {
            if (!hasSetCollider)
            {
                float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPos);
                if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].SqrVisibleDstThreshold)
                {
                    if (!lodMeshes[colliderLODIndex].hasRequested)
                    {
                        lodMeshes[colliderLODIndex].RequestMesh(mapData);
                    }
                }
                if (!(sqrDstFromViewerToEdge <
                      colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)) return;
                if (lodMeshes[colliderLODIndex].hasMesh)
                {
                    collider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                    hasSetCollider = true;
                }
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
        public bool hasRequested;
        public bool hasMesh;
        private int lod;
        public event System.Action UpdateCallback;

        public LODMesh(int lod)
        {
            this.lod = lod;
        }

        private void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;
            UpdateCallback?.Invoke();
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
        [Range(0, MeshGenerator.NumSupportedLoDs - 1)]
        public int lod;
        public float visibleDistanceThreshold;
        public float SqrVisibleDstThreshold => visibleDistanceThreshold * visibleDistanceThreshold;
    }
}
