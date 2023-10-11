Shader "Unlit/ArrowShader"
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
            Name "ArrowColor"

            HLSLPROGRAM
            #pragma require cubearray
            #pragma vertex VShader           
            #pragma fragment PShader      
            
            #include "Assets\01_Script\RtsTerrain\03_Arrow\01_GShader\ArrowColor.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ArrowColor_GBuffer"
             
            Stencil
            {
                Ref 1
                Comp always
                Pass replace
            }

            HLSLPROGRAM            
            //#pragma require cubearray
            #pragma vertex VShader           
            #pragma fragment PShader_GBuffer      

            #include "Assets\01_Script\RtsTerrain\03_Arrow\01_GShader\ArrowColor.hlsl"
            ENDHLSL
        }


        Pass
        {
            Name "ArrowDepth_CSM"
           
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex VShader           
            #pragma geometry GShader_CSM            
            #pragma fragment PShader_CSM      
            
            #include "Assets\01_Script\RtsTerrain\03_Arrow\01_GShader\ArrowDepth_GS.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ArrowDepth_CBM"
           
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex VShader           
            #pragma geometry GShader_CBM            
            #pragma fragment PShader_CBM      

            #include "Assets\01_Script\RtsTerrain\03_Arrow\01_GShader\ArrowDepth_GS.hlsl"
            ENDHLSL
        }
    }
}
