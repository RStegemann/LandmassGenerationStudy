using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        NoiseMap,
        ColourMap,
        Mesh,
        FalloffMap
    }

    public const int mapChunkSize = 239;

    [Header("General")]
    public DrawMode drawMode = DrawMode.NoiseMap;
    [Expandable]
    public TerrainSettings terrainSettings;
    [Expandable]
    public NoiseSettings noiseSettings;

    [Header("Mesh")]
    [Range(0, 6)]
    public int editorPreviewLOD;

    [Header("Colouring")]
    public TerrainType[] regions;

    public bool autoUpdate;
    private float[,] falloffMap;

    Queue<MapThreadInfo<MapData>> mapThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    private MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = NoiseGenerator.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, 
            noiseSettings.seed, noiseSettings.noiseScale, noiseSettings.octaves, 
            noiseSettings.persistence, noiseSettings.lacunarity, center + noiseSettings.offset, noiseSettings.normalizeMode);
        if (terrainSettings.useFalloff)
        {
            for(int y = 0; y < mapChunkSize + 2; y++)
            {
                for(int x = 0; x < mapChunkSize + 2; x++)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }
            }
        }
        Color[] colourMap = CreateColourMap(noiseMap);
        return new MapData(noiseMap, colourMap);
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start();
    }

    private void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);
        lock (mapThreadInfoQueue)
        {
            mapThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    private void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainSettings.meshHeightMultiplier, terrainSettings.meshHeightCurve, lod);
        lock (meshThreadInfoQueue)
        {
            meshThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update()
    {
        if(mapThreadInfoQueue.Count > 0)
        {
            for(int i = 0; i < mapThreadInfoQueue.Count; i++)
            {
                lock (mapThreadInfoQueue)
                {
                    MapThreadInfo<MapData> threadInfo = mapThreadInfoQueue.Dequeue();
                    threadInfo.callback.Invoke(threadInfo.parameter);
                }
            }
        }

        if (meshThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshThreadInfoQueue.Count; i++)
            {
                lock (meshThreadInfoQueue)
                {
                    MapThreadInfo<MeshData> threadInfo = meshThreadInfoQueue.Dequeue();
                    threadInfo.callback.Invoke(threadInfo.parameter);
                }
            }
        }
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        switch (drawMode)
        {
            case DrawMode.NoiseMap:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
                break;
            case DrawMode.ColourMap:
                display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
                break;
            case DrawMode.Mesh:
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainSettings.meshHeightMultiplier, terrainSettings.meshHeightCurve, editorPreviewLOD), 
                    TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
                break;
            case DrawMode.FalloffMap:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(NoiseGenerator.GenerateFalloffMap(mapChunkSize, terrainSettings.falloffCurve)));
                break;
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
                    if (currentHeight >= type.height)
                    {
                        colourMap[y * mapChunkSize + x] = type.colour;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        return colourMap;
    }

    private void OnValidate()
    {
        falloffMap = NoiseGenerator.GenerateFalloffMap(mapChunkSize + 2, terrainSettings.falloffCurve);
    }

    private struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color colour;
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}
