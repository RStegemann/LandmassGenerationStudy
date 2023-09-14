using System;
using UnityEngine;

public abstract class NoiseFilter : UpdatableScriptable
{
    public abstract float MinValue();
    public abstract float MaxValue();

    public abstract float Evaluate(float x, float y, Vector2 sampleCenter);

    public abstract void ValidateValues();
}
