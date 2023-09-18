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

    public float maxHeightScale;
    public float baseRoughness = 0;
    public float roughness;

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
        for (int i = 0; i < octaves; i++)
        {
            elevation += amplitude;
            amplitude *= persistence;
        }
        return elevation;
    }

    public override float Evaluate(float x, float y, Vector2 sampleCenter)
    {
        float maxPossibleValue = 0;
        float amplitude = 1;
        float elevation = 0;
        float frequency = baseRoughness;
        for (int i = 0; i < octaves; i++)
        {
            float2 sample = new float2((x + sampleCenter.x) * frequency, (y - sampleCenter.y) * frequency);
            float v = 1 - Mathf.Abs(noise.snoise(sample));
            elevation += v * amplitude;
            maxPossibleValue += 1 * amplitude;
            frequency *= roughness;
            amplitude *= persistence * elevation;
        }

        elevation = elevation / maxPossibleValue / maxHeightScale;
        
        return elevation;
    }
    
    public override void ValidateValues()
    {
        scale = Mathf.Max(scale, 0.01f);
        octaves = Mathf.Max(octaves, 1);
        persistence = Mathf.Clamp01(persistence);
    }
}
