using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPreview : MonoBehaviour
{
    public Renderer textureRenderer;
    public MeshFilter meshFilter;
    
    public enum DrawMode
    {
        NoiseMap,
        Mesh,
        FalloffMap
    }

    [Header("General")]
    public DrawMode drawMode = DrawMode.NoiseMap;
    [Expandable]
    public MeshSettings meshSettings;
    [Expandable]
    public HeightMapSettings heightMapSettings;
    [Expandable]
    public TextureSettings textureSettings;
    public Material terrainMaterial;
    
    [Header("Mesh")]
    [Range(0, MeshSettings.NumSupportedLoDs - 1)]
    public int editorPreviewLOD;

    public bool autoUpdate;

    private void OnTextureValuesUpdated()
    {
        textureSettings.ApplyToMaterial(terrainMaterial);
    }

    public void DrawMapInEditor()
    {
        textureSettings.ApplyToMaterial(terrainMaterial);
        textureSettings.UpdateMeshHeights(terrainMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(
            meshSettings.NumVertsPerLine,
            meshSettings.NumVertsPerLine, 
            heightMapSettings,
            Vector2.zero
        );
        switch (drawMode)
        {
            case DrawMode.NoiseMap:
                DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
                break;
            case DrawMode.Mesh:
                DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorPreviewLOD));
                break;
            case DrawMode.FalloffMap:
                DrawTexture(
                    TextureGenerator.TextureFromHeightMap(
                            new HeightMap(NoiseGenerator.GenerateFalloffMap(meshSettings.NumVertsPerLine, heightMapSettings.falloffCurve), 0, 1)
                        )
                    );
                break;
        }
    }
    
    public void DrawTexture(Texture2D texture)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / (10f);
        textureRenderer.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    public void DrawMesh(MeshData meshData)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        textureRenderer.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
    }
    
    private void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }
    
    private void OnValidate()
    {
        if (meshSettings != null)
        {
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (heightMapSettings != null)
        {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureSettings != null)
        {
            textureSettings.OnValuesUpdated -= OnTextureValuesUpdated;
            textureSettings.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }
}
