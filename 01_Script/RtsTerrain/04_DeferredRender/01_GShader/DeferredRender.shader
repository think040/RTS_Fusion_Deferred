Shader "Unlit/DeferredRender"
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
            Name "LBuffer"

            Cull Front
            ZWrite On
            ZTest LEqual

            BlendOp Add
            Blend One One

            HLSLPROGRAM
            #pragma vertex VShader              
            #pragma fragment PShader      

            #include "Assets\01_Script\RtsTerrain\04_DeferredRender\01_GShader\00_LBuffer.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Final"

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma require cubearray
            #pragma vertex VShader              
            #pragma fragment PShader      

            #include "Assets\01_Script\RtsTerrain\04_DeferredRender\01_GShader\01_Final.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Fxaa"

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex VShader              
            #pragma fragment PShader  
            #pragma target 5.0

            #include "Assets\01_Script\RtsTerrain\04_DeferredRender\01_GShader\02_Fxaa.hlsl"

            ENDHLSL
        }


        Pass
        {
            Name "Debug"

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex VShader              
            #pragma fragment PShader   


            #include "Assets\01_Script\RtsTerrain\04_DeferredRender\01_GShader\03_Debug.hlsl"

            ENDHLSL
        }
    }
}
