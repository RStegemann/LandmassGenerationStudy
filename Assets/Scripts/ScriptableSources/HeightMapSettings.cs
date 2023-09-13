using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Settings/NoiseSettings")]
public class HeightMapSettings : UpdatableScriptable
{
    public NoiseSettings noiseSettings;
    [Header("Falloff")]
    public bool useFalloff;
    public AnimationCurve falloffCurve;
    [FormerlySerializedAs("meshHeightMultiplier")] public float heightMultiplier;
    [FormerlySerializedAs("meshHeightCurve")] public AnimationCurve heightCurve;

    public float MinHeight => heightMultiplier * heightCurve.Evaluate(0);
    public float MaxHeight => heightMultiplier * heightCurve.Evaluate(1);

    #if UNITY_EDITOR
    public override void OnValidate()
    {
        noiseSettings.ValidateValues();
        base.OnValidate();
    }
    #endif
}
