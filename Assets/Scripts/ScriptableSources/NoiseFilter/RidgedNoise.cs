using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Noise/RidgedNoise")]
public class RidgedNoise : NoiseFilter
{
    public float scale = 50;
    public int octaves = 6; 
    [Range(0f, 1f)]
    public float persistence = 0.6f;
    public float baseRoughness = 0;

    public override void OnValidate()
    {
        base.OnValidate();
        ValidateValues();
    }
    
    public override float MinValue()
    {
        return 0;
    }

    public override float MaxValue()
    {
        float amplitude = 1;
        float elevation = 0;
        float weight = 1;
        for (int i = 0; i < octaves; i++)
        {
            float v = 1;
            v *= weight;
            weight = v;
            elevation = v * amplitude;
            amplitude *= persistence;
        }
        return elevation;
    }

    public override float Evaluate(float x, float y, Vector2 sampleCenter)
    {
        float amplitude = 1;
        float elevation = 0;
        float weight = 1;
        for (int i = 0; i < octaves; i++)
        {
            float v = 1 - Mathf.Sin(Mathf.PerlinNoise(x + sampleCenter.x + baseRoughness, y + sampleCenter.y + baseRoughness));
            v *= v;
            v *= weight;
            weight = v;
            elevation = v * amplitude;
            amplitude *= persistence;
        }

        return elevation;
    }
    
    public override void ValidateValues()
    {
        scale = Mathf.Max(scale, 0.01f);
        octaves = Mathf.Max(octaves, 1);
        persistence = Mathf.Clamp01(persistence);
    }
}
