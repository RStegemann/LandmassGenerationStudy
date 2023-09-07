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
    private static MapGenerator instance;

    Queue<MapThreadInfo<MapData>> mapThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    private void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    private MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap;
        if(noiseSettings.normalizeMode == NoiseGenerator.NormalizeMode.Local)
        {
            noiseMap = NoiseGenerator.GenerateLocalizedNoiseMap(MapChunkSize() + 2, MapChunkSize() + 2,
                noiseSettings.seed, noiseSettings.noiseScale, noiseSettings.octaves,
                noiseSettings.persistence, noiseSettings.lacunarity, center + noiseSettings.offset);
        }
        else
        {
            noiseMap = NoiseGenerator.GenerateGlobalNoiseMap(MapChunkSize() + 2, MapChunkSize() + 2,
                noiseSettings.seed, noiseSettings.noiseScale, noiseSettings.octaves,
                noiseSettings.persistence, noiseSettings.lacunarity, center + noiseSettings.offset, noiseSettings.globalNoiseScale);
        }
        if (terrainSettings.useFalloff)
        {
            for(int y = 0; y < MapChunkSize() + 2; y++)
            {
                for(int x = 0; x < MapChunkSize() + 2; x++)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }
            }
        }
        Color[] colourMap = CreateColourMap(noiseMap);
        return new MapData(noiseMap, colourMap);
    }

    public static int MapChunkSize()
    {
        if (instance == null) instance = GameObject.FindObjectOfType<MapGenerator>();
        return instance.terrainSettings.useFlatShading ? 95 : 239;
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
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainSettings.meshHeightMultiplier, terrainSettings.meshHeightCurve, lod, terrainSettings.useFlatShading);
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
                display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, MapChunkSize(), MapChunkSize()));
                break;
            case DrawMode.Mesh:
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainSettings.meshHeightMultiplier, terrainSettings.meshHeightCurve, editorPreviewLOD, terrainSettings.useFlatShading), 
                    TextureGenerator.TextureFromColorMap(mapData.colorMap, MapChunkSize(), MapChunkSize()));
                break;
            case DrawMode.FalloffMap:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(NoiseGenerator.GenerateFalloffMap(MapChunkSize(), terrainSettings.falloffCurve)));
                break;
        }
    }

    private Color[] CreateColourMap(float[,] noiseMap)
    {
        Color[] colourMap = new Color[MapChunkSize() * MapChunkSize()];
        for (int y = 0; y < MapChunkSize(); y++)
        {
            for (int x = 0; x < MapChunkSize(); x++)
            {
                float currentHeight = noiseMap[x, y];
                foreach (TerrainType type in regions)
                {
                    if (currentHeight >= type.height)
                    {
                        colourMap[y * MapChunkSize() + x] = type.colour;
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
        if (terrainSettings != null)
        {
            terrainSettings.OnValuesUpdated -= OnValuesUpdated;
            terrainSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (noiseSettings != null)
        {
            noiseSettings.OnValuesUpdated -= OnValuesUpdated;
            noiseSettings.OnValuesUpdated += OnValuesUpdated;
        }
        falloffMap = NoiseGenerator.GenerateFalloffMap(MapChunkSize() + 2, terrainSettings.falloffCurve);
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
