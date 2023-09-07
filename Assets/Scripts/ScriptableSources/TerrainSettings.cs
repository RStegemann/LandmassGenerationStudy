using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Settings/TerrainSettings")]
public class TerrainSettings : UpdatableScriptable
{
    public float uniformScale = 5f;
    [Header("Falloff")]
    public bool useFalloff;
    public bool useFlatShading;
    public AnimationCurve falloffCurve;
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
}
