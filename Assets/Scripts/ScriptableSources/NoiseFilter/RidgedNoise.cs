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
    public int seed = 15;

    private SimplexNoise noise = new SimplexNoise();

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
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for(int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100_000, 100_000) + sampleCenter.x;
            float offsetY = prng.Next(-100_000, 100_000) - sampleCenter.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }
        
        float amplitude = 1;
        float elevation = 0;
        float frequency = baseRoughness;
        float weight = 1;
        for (int i = 0; i < octaves; i++)
        {
            //float sampleX = (x + octaveOffsets[i].x) / scale * frequency;
            //float sampleY = (y + octaveOffsets[i].y) / scale * frequency;
            //float v = 1 - Mathf.PerlinNoise(sampleX, sampleY);
            float v = 1 - Mathf.Abs(noise.Evaluate(new Vector3(x, y, 0) / scale * frequency));
            v *= v;
            v *= weight;
            weight = v;
            elevation += v * amplitude;
            frequency *= roughness;
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
