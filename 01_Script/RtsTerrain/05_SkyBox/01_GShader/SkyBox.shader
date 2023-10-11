Shader "Unlit/SkyBox"
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
            Name "SkyBox"

            Stencil
            {
                Ref 0
                Comp equal
            }

            Cull Back
            //ZWrite On
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex VShader              
            #pragma fragment PShader      

            #include "Assets\01_Script\RtsTerrain\05_SkyBox\01_GShader\SkyBox.hlsl"

            ENDHLSL
        }
    }
}
