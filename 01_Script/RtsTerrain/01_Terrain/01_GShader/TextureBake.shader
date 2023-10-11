Shader "Unlit/TextureBake"
{
    Properties
    {
       
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Name "TerrainTexBake"

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #include "Assets\01_Script\RtsTerrain\01_Terrain\01_GShader\TerrainTexBake.hlsl"
            #pragma vertex VShader
            #pragma fragment PShader
            
            ENDHLSL           
        }

        Pass
        {
            Name "NormalMap"

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #include "Assets\01_Script\RtsTerrain\01_Terrain\01_GShader\TerrainTexBake.hlsl"
            #pragma vertex VShader
            #pragma fragment PShader_NormalMap

            ENDHLSL
        }
    }
}
