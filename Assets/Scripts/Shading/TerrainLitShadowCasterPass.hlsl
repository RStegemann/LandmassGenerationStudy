#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

float3 _LightDirection;

struct Attributes
{
    float3 position_os: POSITION;
    float3 normal_os: NORMAL;
};

struct Interpolators
{
    float4 position_cs: SV_POSITION;
};

float4 GetShadowCasterPositionCS(float3 positionWS, float3 normalWS)
{
    float3 lightDirectionWS = _LightDirection;
    float4 position_cs = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
#if UNITY_REVERSED_Z
    position_cs.z = min(position_cs.z, UNITY_NEAR_CLIP_VALUE);    
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif
    return position_cs;
}

Interpolators vertex(Attributes input)
{
    Interpolators output;
    VertexPositionInputs position_inputs = GetVertexPositionInputs(input.position_os);
    output.position_cs = position_inputs.positionCS;
    return output;
}

float4 fragment(Interpolators input): SV_TARGET
{
    return 0;
}