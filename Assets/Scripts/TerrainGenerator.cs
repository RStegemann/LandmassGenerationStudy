using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    private const float ViewerMoveThresholdForChunkUpdate = 25f;

    private const float SqrViewerMoveThresholdForChunkUpdate =
        ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;
    
    public LODInfo[] detailLevels;
    public int lodColliderIndex = 1;
    
    public Transform viewer;
    public Material terrainMaterial;
    
    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureSettings textureSettings;

    private float meshWorldSize;
    private int chunksInDistance;
    
    private Vector2 viewerPos;
    private Vector2 viewerPosOld;
    private Dictionary<Vector2, TerrainChunk> chunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    private void Start()
    {
        textureSettings.ApplyToMaterial(terrainMaterial);
        textureSettings.UpdateMeshHeights(terrainMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);
        float maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
        meshWorldSize = meshSettings.MeshWorldSize;
        chunksInDistance = Mathf.RoundToInt(maxViewDistance / meshWorldSize);
        UpdateVisibleChunks();
    }

    private void Update()
    {
        Vector3 p = viewer.position;
        viewerPos = new Vector2(p.x, p.z);
        if (viewerPos != viewerPosOld)
        {
            foreach (TerrainChunk chunk in visibleTerrainChunks)
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
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
        for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
            visibleTerrainChunks[i].UpdateTerrainChunk();
        }
        int currentChunkCoordX = Mathf.RoundToInt(viewerPos.x / meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPos.y / meshWorldSize);

        for (int yOffset = -chunksInDistance; yOffset <= chunksInDistance; yOffset++)
        {
            for (int xOffset = -chunksInDistance; xOffset <= chunksInDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                {
                    if (chunkDictionary.TryGetValue(viewedChunkCoord, out TerrainChunk terrainChunk))
                    {
                        terrainChunk.UpdateTerrainChunk();
                    }
                    else
                    {
                        terrainChunk = new TerrainChunk(
                            viewedChunkCoord, 
                            heightMapSettings,
                            meshSettings,
                            detailLevels,
                            transform,
                            terrainMaterial, 
                            lodColliderIndex,
                            viewer);
                        chunkDictionary.Add(viewedChunkCoord, terrainChunk);
                        terrainChunk.OnVisibilityChanged += OnTerrainChunkVisibilityChanged;
                        terrainChunk.Load();
                    }
                }
            }
        }
    }

    private void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
    {
        if (isVisible)
        {
            visibleTerrainChunks.Add(chunk);
        }
        else
        {
            visibleTerrainChunks.Remove(chunk);
        }
    }
}
