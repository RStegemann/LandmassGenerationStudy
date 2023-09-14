using UnityEngine;

[CreateAssetMenu(menuName = "Noise/PerlinNoise")]
public class PerlinNoise : NoiseFilter
{
    public float scale = 50;
    public int octaves = 6; 
    [Range(0f, 1f)]
    public float persistence = 0.6f;
    public float lacunarity = 2;
    public int seed;
    public Vector2 offset;

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
        float maxValue = 0;
        float amplitude = 1;
        for (int i = 0; i < octaves; i++)
        {
            maxValue += amplitude;
            amplitude *= persistence;
        }
        return maxValue;
    }

    public override float Evaluate(float x, float y, Vector2 sampleCenter)
    {
        System.Random prng = new System.Random(seed);
        
        float amplitude = 1;
        float frequency = 1;
        float elevation = 0;
        
        Vector2[] octaveOffsets = new Vector2[octaves];
        for(int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100_000, 100_000) + offset.x + sampleCenter.x;
            float offsetY = prng.Next(-100_000, 100_000) - offset.y - sampleCenter.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
            amplitude *= persistence;
        }
        amplitude = 1;
        
        for(int i = 0; i < octaves; i++)
        {
            float sampleX = (x + octaveOffsets[i].x) / scale * frequency;
            float sampleY = (y + octaveOffsets[i].y) / scale * frequency;
            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
            elevation += perlinValue * amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }
        return elevation;
    }
    
    public override void ValidateValues()
    {
        scale = Mathf.Max(scale, 0.01f);
        octaves = Mathf.Max(octaves, 1);
        lacunarity = Mathf.Max(lacunarity, 1);
        persistence = Mathf.Clamp01(persistence);
    }
}
