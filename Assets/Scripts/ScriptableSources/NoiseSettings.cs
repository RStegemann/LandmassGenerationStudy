using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Settings/NoiseSettings")]
public class NoiseSettings : ScriptableObject
{
    public NoiseGenerator.NormalizeMode normalizeMode;
    public float noiseScale;
    public int octaves; 
    [Range(0f, 1f)]
    public float persistence;
    public float lacunarity;
    public int seed;
    public Vector2 offset;

    public void OnValidate()
    {
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        if (octaves < 1)
        {
            octaves = 1;
        }
    }
}
