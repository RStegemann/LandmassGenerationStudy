#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

float _smoothness;
float min_height;
float max_height;
const static float epsilon = 1E-4;

const static int max_layer_count = 8;
float3 base_colours[max_layer_count];
float base_start_heights[max_layer_count];
float base_blends[max_layer_count];
float base_colour_strengths[max_layer_count];
float base_texture_scales[max_layer_count];
float layer_count;
Texture2DArray base_textures;
SAMPLER(sampler_base_textures);

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

float3 triplanar(float3 world_pos, float scale, float3 blend_axes, int texture_index)
{
    float3 scaled_world_pos = world_pos / scale;
    float3 xProjection = SAMPLE_TEXTURE2D_ARRAY(base_textures, sampler_base_textures, float2(scaled_world_pos.y, scaled_world_pos.z), texture_index) * blend_axes.x;
    float3 yProjection = SAMPLE_TEXTURE2D_ARRAY(base_textures, sampler_base_textures, float2(scaled_world_pos.x, scaled_world_pos.z), texture_index) * blend_axes.y;
    float3 zProjection = SAMPLE_TEXTURE2D_ARRAY(base_textures, sampler_base_textures, float2(scaled_world_pos.x, scaled_world_pos.y), texture_index) * blend_axes.z;
    return xProjection + yProjection + zProjection;
}

float4 fragment(Interpolators input) : SV_TARGET{
    float height_percent = inverseLerp(min_height, max_height, input.position_ws.y);
    float3 blend_axes = abs(input.normal_ws);
    blend_axes /= blend_axes.x + blend_axes.y + blend_axes.z;
    
    float3 col = 1;
    for(int i = 0; i < layer_count; i++)
    {
        float drawStrength = inverseLerp(-base_blends[i]/2 - epsilon,
                base_blends[i]/2,
                height_percent - base_start_heights[i]);
        float3 base_colour = base_colours[i] * base_colour_strengths[i];
        float3 texture_colour = triplanar(input.position_ws, base_texture_scales[i], blend_axes, i) * (1 - base_colour_strengths[i]);
        
        col = col * (1 - drawStrength) + (base_colour + texture_colour) * drawStrength;
    }
    
    InputData lightingInput = (InputData)0;
    lightingInput.normalWS = input.normal_ws;
    lightingInput.positionWS = input.position_ws;
    lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.position_ws);
    lightingInput.shadowCoord = TransformWorldToShadowCoord(input.position_ws);

    SurfaceData surfaceInput = (SurfaceData)0;
    surfaceInput.albedo = col.rgb;
    surfaceInput.alpha = 1;
    surfaceInput.specular = 0;
    surfaceInput.smoothness = 1 - _smoothness;
    
    return UniversalFragmentBlinnPhong(lightingInput, surfaceInput);
}