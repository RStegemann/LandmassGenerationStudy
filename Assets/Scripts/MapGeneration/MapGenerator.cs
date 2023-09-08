using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        NoiseMap,
        Mesh,
        FalloffMap
    }

    [Header("General")]
    public DrawMode drawMode = DrawMode.NoiseMap;
    [Expandable]
    public TerrainSettings terrainSettings;
    [Expandable]
    public NoiseSettings noiseSettings;
    [Expandable]
    public TextureSettings textureSettings;

    public Material terrainMaterial;
    [Range(0, MeshGenerator.NumSupportedChunkSizes - 1)]
    public int chunkSizeIndex;
    [Range(0, MeshGenerator.NumSupportedFlatShadedChunkSizes - 1)]
    public int flatShadedChunkSizeIndex;
    
    
    [Header("Mesh")]
    [Range(0, MeshGenerator.NumSupportedLoDs - 1)]
    public int editorPreviewLOD;

    public bool autoUpdate;
    private float[,] falloffMap;

    Queue<MapThreadInfo<MapData>> mapThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    private void Awake()
    {
        textureSettings.ApplyToMaterial(terrainMaterial);
        textureSettings.UpdateMeshHeights(terrainMaterial, terrainSettings.MinHeight, terrainSettings.MaxHeight);
    }

    private void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    private void OnTextureValuesUpdated()
    {
        textureSettings.ApplyToMaterial(terrainMaterial);
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
            if(falloffMap == null)
            {
                falloffMap = NoiseGenerator.GenerateFalloffMap(MapChunkSize() + 2, terrainSettings.falloffCurve);
            }
            for (int y = 0; y < MapChunkSize() + 2; y++)
            {
                for(int x = 0; x < MapChunkSize() + 2; x++)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }
            }
        }
        return new MapData(noiseMap);
    }

    public int MapChunkSize()
    {
        return terrainSettings.useFlatShading ? 
            MeshGenerator.SupportedFlatShadedChunkSizes[flatShadedChunkSizeIndex] - 1:
            MeshGenerator.SupportedChunkSizes[chunkSizeIndex] - 1;
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
        textureSettings.UpdateMeshHeights(terrainMaterial, terrainSettings.MinHeight, terrainSettings.MaxHeight);
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        switch (drawMode)
        {
            case DrawMode.NoiseMap:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
                break;
            case DrawMode.Mesh:
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainSettings.meshHeightMultiplier, terrainSettings.meshHeightCurve, editorPreviewLOD, terrainSettings.useFlatShading));
                break;
            case DrawMode.FalloffMap:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(NoiseGenerator.GenerateFalloffMap(MapChunkSize() + 2, terrainSettings.falloffCurve)));
                break;
        }
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
        if (textureSettings != null)
        {
            textureSettings.OnValuesUpdated -= OnTextureValuesUpdated;
            textureSettings.OnValuesUpdated += OnTextureValuesUpdated;
        }
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

public struct MapData
{
    public readonly float[,] heightMap;

    public MapData(float[,] heightMap)
    {
        this.heightMap = heightMap;
    }
}
