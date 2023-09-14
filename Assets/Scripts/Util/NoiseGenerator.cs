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
            float maxPossibleHeight = 0;
            for(int i = 0; i < settings.noiseLayers.Length; i++)
            {
                if (settings.noiseLayers[i].enabled)
                {
                    maxPossibleHeight += settings.noiseLayers[i].noise.MaxValue();
                }
            }
        
            for (int index = 0; index < mapWidth; index++)
            {
                values[index] = new float[mapHeight];
            }

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    float firstLayerElevation = settings.noiseLayers[0].noise.Evaluate(x, y, sampleCenter);
                    float elevation = firstLayerElevation;
                    for (int i = 1; i < settings.noiseLayers.Length; i++)
                    {
                        if (settings.noiseLayers[i].enabled)
                        {
                            elevation += settings.noiseLayers[i].noise.Evaluate(x, y, sampleCenter);
                        }
                    }
                    values[x][y] = elevation;  
                    // normalize elevation
                    elevation = (values[x][y] + 1) / (maxPossibleHeight/settings.globalNoiseHeightScale);
                    values[x][y] = Mathf.Clamp(elevation, 0, int.MaxValue);
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