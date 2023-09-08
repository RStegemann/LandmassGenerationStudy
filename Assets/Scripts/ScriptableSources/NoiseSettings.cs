using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Settings/NoiseSettings")]
public class NoiseSettings : UpdatableScriptable
{
    public NoiseGenerator.NormalizeMode normalizeMode;
    public float globalNoiseScale;
    public float noiseScale;
    public int octaves; 
    [Range(0f, 1f)]
    public float persistence;
    public float lacunarity;
    public int seed;
    public Vector2 offset;

    #if UNITY_EDITOR
    public override void OnValidate()
    {
        base.OnValidate();
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        if (octaves < 1)
        {
            octaves = 1;
        }
        if(globalNoiseScale == 0)
        {
            globalNoiseScale = 1;
        }
    }
    #endif
}
