Shader "Unlit/TerrainQT"
{
    Properties
    {

    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100
        
        Pass
        {
            Name "Terrain_CSM_Color"

            //Cull Front
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex VShader
            #pragma hull HShader
            #pragma domain DShader
            #pragma geometry GShader_CSM
            #pragma fragment PShader_Light_Debug

            #include "Assets\01_Script\RtsTerrain\01_Terrain\01_GShader\TerrainQT.hlsl"
            ENDHLSL
        }

         Pass
        {
            Name "Terrain_CBM_Color"

            //Cull Front
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex VShader
            #pragma hull HShader
            #pragma domain DShader
            #pragma geometry GShader_CBM
            #pragma fragment PShader_Light_Debug

            #include "Assets\01_Script\RtsTerrain\01_Terrain\01_GShader\TerrainQT.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Terrain_CSM"

            //Cull Front
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex VShader
            #pragma hull HShader
            #pragma domain DShader
            #pragma geometry GShader_CSM
            #pragma fragment PShader_CSM

            #include "Assets\01_Script\RtsTerrain\01_Terrain\01_GShader\TerrainQT.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Terrain_CBM"
        
            //Cull Front
            Cull Back
            ZWrite On
            ZTest LEqual
        
            HLSLPROGRAM
            #pragma vertex VShader
            #pragma hull HShader
            #pragma domain DShader
            #pragma geometry GShader_CBM
            #pragma fragment PShader_CBM
        
            #include "Assets\01_Script\RtsTerrain\01_Terrain\01_GShader\TerrainQT.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Terrain_CAM"

            Cull Back
            ZWrite On
            ZTest LEqual

            BlendOp Add
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex VShader
            #pragma hull HShader
            #pragma domain DShader
            #pragma geometry GShader_CAM
            #pragma fragment PShader_CAM

            #pragma multi_compile_local LAYERED_NORMAL_MAP BAKED_NORMAL_MAP NONE_NORMAL_MAP
            #pragma multi_compile_local LAYERED_DIFFUSE_TEX BAKED_DIFFUSE_TEX                        

            #include "Assets\01_Script\RtsTerrain\01_Terrain\01_GShader\TerrainQT.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Terrain_CAM_GBuffer"

            Cull Back
            ZWrite On
            ZTest LEqual           

            Stencil
            {
                Ref 1
                Comp always
                Pass replace
            }

            HLSLPROGRAM
            #pragma vertex VShader
            #pragma hull HShader
            #pragma domain DShader
            #pragma geometry GShader_CAM
            #pragma fragment PShader_CAM_GBuffer

            #pragma multi_compile_local LAYERED_NORMAL_MAP BAKED_NORMAL_MAP NONE_NORMAL_MAP
            #pragma multi_compile_local LAYERED_DIFFUSE_TEX BAKED_DIFFUSE_TEX                        

            #include "Assets\01_Script\RtsTerrain\01_Terrain\01_GShader\TerrainQT.hlsl"
            ENDHLSL
        }


        Pass
        {
            Name "Terrain_CSM_Wire"

            //Cull Front
            Cull Back
            ZWrite On
            ZTest LEqual

            BlendOp Add
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex VShader
            #pragma hull HShader
            #pragma domain DShader
            #pragma geometry GShader_CSM_Wire
            #pragma fragment PShader_Wire

            #include "Assets\01_Script\RtsTerrain\01_Terrain\01_GShader\TerrainQT.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Terrain_CBM_Wire"

            //Cull Front
            Cull Back
            ZWrite On
            ZTest LEqual

            BlendOp Add
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex VShader
            #pragma hull HShader
            #pragma domain DShader
            #pragma geometry GShader_CBM_Wire
            #pragma fragment PShader_Wire

            #include "Assets\01_Script\RtsTerrain\01_Terrain\01_GShader\TerrainQT.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Terrain_CAM_Wire"

            Cull Back
            ZWrite On
            ZTest LEqual

            BlendOp Add
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex VShader
            #pragma hull HShader
            #pragma domain DShader
            #pragma geometry GShader_CAM_Wire
            #pragma fragment PShader_Wire

            #include "Assets\01_Script\RtsTerrain\01_Terrain\01_GShader\TerrainQT.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Terrain_CAM_Line"

            Cull Back
            ZWrite On
            ZTest LEqual

            BlendOp Add
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex VShader
            #pragma hull HShader
            #pragma domain DShader          
            #pragma geometry GShader_CAM_Line
            #pragma fragment PShader_Line

            #include "Assets\01_Script\RtsTerrain\01_Terrain\01_GShader\TerrainQT.hlsl"
            ENDHLSL
    }

        Pass
        {
            Name "Terrain_CSM_Debug"

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex VShader           
            #pragma geometry GShader_CSM
            #pragma fragment PShader

            #include "Assets\01_Script\RtsTerrain\01_Terrain\01_GShader\TerrainQT_Debug.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Terrain_CBM_Debug"

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex VShader           
            #pragma geometry GShader_CBM
            #pragma fragment PShader

            #include "Assets\01_Script\RtsTerrain\01_Terrain\01_GShader\TerrainQT_Debug.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Terrain_CAM_Debug"

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex VShader           
            #pragma geometry GShader_CAM
            #pragma fragment PShader

            #include "Assets\01_Script\RtsTerrain\01_Terrain\01_GShader\TerrainQT_Debug.hlsl"
            ENDHLSL
        }
    }
}
