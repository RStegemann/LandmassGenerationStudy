using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public static class NoiseGenerator
{
    public static float[][] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseLayerSettings settings, Vector2 sampleCenter)
    {
        float[][] values = JaggedArray.CreateJaggedArray<float[][]>(mapWidth, mapHeight);
        if (settings.noiseLayers.Length > 0)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    float elevation = 0;
                    float firstLayerElevation = settings.noiseLayers[0].noise.Evaluate(x, y, sampleCenter);
                    if (settings.noiseLayers[0].enabled)
                    {
                        elevation = firstLayerElevation * settings.noiseLayers[0].strength;
                    }
                    
                    for (int i = 1; i < settings.noiseLayers.Length; i++)
                    {
                        NoiseLayer layer = settings.noiseLayers[i];
                        if (layer.enabled)
                        {
                            float mask = layer.useFirstLayerAsMask ? firstLayerElevation : 1;
                            float v = settings.noiseLayers[i].noise.Evaluate(x, y, sampleCenter) * mask;
                            elevation += v * settings.noiseLayers[i].strength;
                        }
                    }
                    values[x][y] = elevation / settings.noiseLayers.Length / settings.globalNoiseHeightScale;  
                }
            }
        }
        return values;
    }

    public static float[][] GenerateFalloffMap(int size, AnimationCurve curve)
    {
        float[][] map = JaggedArray.CreateJaggedArray<float[][]>(size, size);

        for(int row = 0; row < size; row++)
        {
            for(int col = 0;  col < size; col++)
            {
                float x = col / (float)size * 2 - 1;
                float y = row / (float)size * 2 - 1;
                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[row][col] = curve.Evaluate(value);
            }
        }

        return map;
    }
}