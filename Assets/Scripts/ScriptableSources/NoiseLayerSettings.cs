using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Settings/NoiseLayerSettings")]
public class NoiseLayerSettings : UpdatableScriptable
{
    public float globalNoiseHeightScale;
    public NoiseLayer[] noiseLayers;

#if UNITY_EDITOR
    public override void OnValidate()
    {
        foreach (NoiseLayer noiseLayer in noiseLayers)
        {
            noiseLayer.noise.OnValuesUpdated -= OnValuesUpdated;
            noiseLayer.noise.OnValuesUpdated += OnValuesUpdated;
            noiseLayer.noise.ValidateValues();
        }
        base.OnValidate();
    }
#endif
    
}

[System.Serializable]
public class NoiseLayer
{
    public enum NoiseAlgorithm
    {
        Perlin
    }
    public bool useFirstLayerAsMask;
    [Range(0, 1)]
    public float strength;
    public bool enabled;
    public NoiseFilter noise;
}
