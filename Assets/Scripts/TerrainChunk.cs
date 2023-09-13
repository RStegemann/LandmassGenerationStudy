using UnityEngine;

public class TerrainChunk
{
    private const float ColliderGenerationDistanceThreshold = 10f;
    public event System.Action<TerrainChunk, bool> OnVisibilityChanged;

    public Vector2 coord;
    private GameObject meshObject;
    private Vector2 sampleCenter;
    private Bounds bounds;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private LODInfo[] detailLevels;
    private LODMesh[] lodMeshes;
    private int colliderLODIndex;
    private int previousLODIndex = -1;
    private bool hasSetCollider;
    private float maxViewDistance;

    private HeightMap heightMap;
    private HeightMapSettings heightMapSettings;
    private MeshSettings meshSettings;
    private bool heightMapReceived;
    private Transform viewer;
    private Vector2 ViewerPos => new Vector2(viewer.position.x, viewer.position.z);

    public TerrainChunk(Vector2 coord, 
        HeightMapSettings heightMapSettings, 
        MeshSettings meshSettings, 
        LODInfo[] detailLevels, 
        Transform parent, 
        Material material, 
        int colliderLODIndex,
        Transform viewer)
    {
        this.coord = coord;
        this.detailLevels = detailLevels;
        this.colliderLODIndex = colliderLODIndex;
        this.heightMapSettings = heightMapSettings;
        this.meshSettings = meshSettings;
        this.viewer = viewer;
        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
        
        sampleCenter = coord * meshSettings.MeshWorldSize / meshSettings.meshScale;
        Vector2 position = coord * meshSettings.MeshWorldSize;
        bounds = new Bounds(position, Vector2.one * meshSettings.MeshWorldSize);

        meshObject = new GameObject("Terrain Chunk");
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;

        meshObject.transform.position = new Vector3(position.x, 0, position.y);
        meshObject.transform.SetParent(parent);
        SetVisible(false);

        lodMeshes = new LODMesh[detailLevels.Length];
        for(int i = 0; i < detailLevels.Length; i++)
        {
            lodMeshes[i] = new LODMesh(detailLevels[i].lod);
            lodMeshes[i].UpdateCallback += UpdateTerrainChunk;
            if (i == colliderLODIndex) lodMeshes[i].UpdateCallback += UpdateCollisionMesh;
        }
    }

    private void OnHeightMapReceived(object heightMap)
    {
        this.heightMap = (HeightMap)heightMap;
        heightMapReceived = true;
        UpdateTerrainChunk();
    }

    public void UpdateTerrainChunk()
    {
        if (heightMapReceived)
        {
            float distanceToViewer = Mathf.Sqrt(bounds.SqrDistance(ViewerPos));
            bool wasVisible = isVisible();
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
                        lodMesh.RequestMesh(heightMap, meshSettings);
                    }
                }
            }

            if (wasVisible != visible)
            {
                SetVisible(visible);
                OnVisibilityChanged?.Invoke(this, visible);
            }
        }
    }

    public void UpdateCollisionMesh()
    {
        if (!hasSetCollider)
        {
            float sqrDstFromViewerToEdge = bounds.SqrDistance(ViewerPos);
            if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].SqrVisibleDstThreshold)
            {
                if (!lodMeshes[colliderLODIndex].hasRequested)
                {
                    lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);
                }
            }
            if (!(sqrDstFromViewerToEdge <
                  ColliderGenerationDistanceThreshold * ColliderGenerationDistanceThreshold)) return;
            if (lodMeshes[colliderLODIndex].hasMesh)
            {
                meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                hasSetCollider = true;
            }
        }
    }

    public void Load()
    {
        ThreadedDataRequester.RequestData(
            () => HeightMapGenerator.GenerateHeightMap(meshSettings.NumVertsPerLine, meshSettings.NumVertsPerLine, this.heightMapSettings, sampleCenter),
            OnHeightMapReceived
        );
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

public class LODMesh
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

    private void OnMeshDataReceived(object meshData)
    {
        mesh = ((MeshData)meshData).CreateMesh();
        hasMesh = true;
        UpdateCallback?.Invoke();
    }

    public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
    {
        hasRequested = true;
        ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod), OnMeshDataReceived);
    }
}

[System.Serializable]
public struct LODInfo
{
    [Range(0, MeshSettings.NumSupportedLoDs - 1)]
    public int lod;
    public float visibleDistanceThreshold;
    public float SqrVisibleDstThreshold => visibleDistanceThreshold * visibleDistanceThreshold;
}