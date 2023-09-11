#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

float _Smoothness;
float min_height;
float max_height;
const static int max_colour_count = 8;
float4 base_colours[max_colour_count];
float base_start_heights[max_colour_count];
float base_colour_count;

struct Attributes
{
    float3 position_oj: POSITION;
    float2 uv: TEXCOORD0;
    float3 normal_os: NORMAL;
};

struct Interpolators
{
    float4 position_cs: SV_POSITION;
    float3 position_ws: TEXCOORD2;
    float3 normal_ws: TEXCOORD1;
};

Interpolators vertex(const Attributes input)
{
    Interpolators output;
    const VertexPositionInputs position_inputs = GetVertexPositionInputs(input.position_oj);
    output.position_cs = position_inputs.positionCS;
    output.normal_ws = normalize(GetVertexNormalInputs(input.normal_os).normalWS);
    output.position_ws = position_inputs.positionWS;
    return output;
}

float inverseLerp(float a, float b, float value)
{
    return saturate((value-a)/(b-a));
}

float4 fragment(Interpolators input) : SV_TARGET{
    float height_percent = inverseLerp(min_height, max_height, input.position_ws.y);
    float4 col;
    for(int i = 0; i < base_colour_count; i++)
    {
        if(height_percent >= base_start_heights[i])
        {
            col = lerp(base_colours[max(0, i - 1)], base_colours[i], inverseLerp(base_start_heights[i], base_start_heights[min(i+1, max_colour_count)], height_percent));
        }
    }
    
    InputData lightingInput = (InputData)0;
    lightingInput.normalWS = input.normal_ws;
    lightingInput.positionWS = input.position_ws;
    lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.position_ws);
    lightingInput.shadowCoord = TransformWorldToShadowCoord(input.position_ws);

    SurfaceData surfaceInput = (SurfaceData)0;
    surfaceInput.albedo = col.rgb;
    surfaceInput.alpha = col.w;
    surfaceInput.specular = 1;
    surfaceInput.smoothness = _Smoothness;
    
    return UniversalFragmentBlinnPhong(lightingInput, surfaceInput);
}