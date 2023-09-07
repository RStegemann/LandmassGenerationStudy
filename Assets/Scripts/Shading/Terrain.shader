Shader "Unlit/Terrain"
{
    Properties
    {
        [Header(Surface Options)]
        [MainColor] _ColorTint("Tint", Color) = (1, 1, 1, 1)
        [MainTexture] _ColorMap("Color", 2D) = "white" {}
        _Smoothness("Smoothness", Float) = 0
    }
    
    SubShader{
        Tags{"RenderPipeline" = "UniversalPipeline"}
        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}
            
            HLSLPROGRAM
            #define _SPECULAR_COLOR
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #pragma vertex vertex
            #pragma fragment fragment

            #include "TerrainLitForwardPass.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}
            
            HLSLPROGRAM
            #pragma vertex vertex
            #pragma fragment fragment
            #include "TerrainLitShadowCasterPass.hlsl"
            ENDHLSL            
        }
    }
}
