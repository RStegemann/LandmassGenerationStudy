using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        NoiseMap,
        ColourMap,
        Mesh
    }

    const int mapChunkSize = 241;

    [Header("General")]
    public DrawMode drawMode = DrawMode.NoiseMap;
    public float noiseScale;
    public int octaves;
    [Range(0f, 1f)]
    public float persistence;
    public float lacunarity;
    public int seed;
    public Vector2 offset;

    [Header("Colouring")]
    public TerrainType[] regions;

    [Header("Mesh")]
    [Range(0, 6)]
    public int levelOfDetail;
    public float meshHeightMultiplier;
    public AnimationCurve mapHeightCurve;

    public bool autoUpdate;

    public void GenerateMap()
    {
        float[,] noiseMap = NoiseGenerator.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistence, lacunarity, offset);


        MapDisplay display = FindObjectOfType<MapDisplay>();

        if(drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        }
        else if(drawMode == DrawMode.ColourMap)
        {
            Color[] colourMap = CreateColourMap(noiseMap);
            display.DrawTexture(TextureGenerator.TextureFromColorMap(colourMap, mapChunkSize, mapChunkSize));
        }
        else if(drawMode == DrawMode.Mesh)
        {
            Color[] colourMap = CreateColourMap(noiseMap);
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, mapHeightCurve, levelOfDetail), TextureGenerator.TextureFromColorMap(colourMap, mapChunkSize, mapChunkSize));
        }
    }

    private Color[] CreateColourMap(float[,] noiseMap)
    {
        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x, y];
                foreach (TerrainType type in regions)
                {
                    if (currentHeight <= type.height)
                    {
                        colourMap[y * mapChunkSize + x] = type.colour;
                        break;
                    }
                }
            }
        }
        return colourMap;
    }

    private void OnValidate()
    {
        if(lacunarity < 1)
        {
            lacunarity = 1;
        }
        if(octaves < 1)
        {
            octaves = 1;
        }
    }

    [System.Serializable]
    public struct TerrainType
    {
        public string name;
        public float height;
        public Color colour;
    }
}
