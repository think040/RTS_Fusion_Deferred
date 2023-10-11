Shader "Unlit/ColliderRender"
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
            Name "Capsule_Test"
            Cull Back
            ZWrite On
            ZTest LEqual
           
            BlendOp Add
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex VShader                       
            #pragma fragment PShader

            #include "Assets\01_Script\Utility\01_GShader\ColliderRender.hlsl"
            ENDHLSL
        }
    }
}
