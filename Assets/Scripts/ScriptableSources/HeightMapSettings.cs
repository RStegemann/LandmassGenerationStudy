using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Settings/NoiseSettings")]
public class HeightMapSettings : UpdatableScriptable
{
    [Expandable]
    public NoiseLayerSettings noiseSettings;
    [Header("Falloff")]
    public bool useFalloff;
    public AnimationCurve falloffCurve;
    [FormerlySerializedAs("meshHeightMultiplier")] public float heightMultiplier;
    [FormerlySerializedAs("meshHeightCurve")] public AnimationCurve heightCurve;

    public float MinHeight => heightMultiplier * heightCurve.Evaluate(0);
    public float MaxHeight => heightMultiplier * heightCurve.Evaluate(1);

    public override void OnValidate()
    {
        if (noiseSettings != null)
        {
            noiseSettings.OnValuesUpdated -= NotifyOfUpdate;
            noiseSettings.OnValuesUpdated += NotifyOfUpdate;
        }
        base.OnValidate();
    }
}
