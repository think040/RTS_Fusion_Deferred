Shader "Unlit/DeferredCull"
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
            Name "Cull_Pvf"

            Cull Back
            ZWrite On
            ZTest LEqual

            BlendOp Add
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex VShader_PVF           
            #pragma fragment PShader_PVF      

            #include "Assets\01_Script\RtsTerrain\04_DeferredRender\01_GShader\DeferredCull.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Cull_Svf"

            Cull Back
            ZWrite On
            ZTest LEqual

            BlendOp Add
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex VShader_SVF           
            #pragma fragment PShader_SVF      

            #include "Assets\01_Script\RtsTerrain\04_DeferredRender\01_GShader\DeferredCull.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Cull_Sphere"

            Cull Back
            ZWrite On
            ZTest LEqual

            BlendOp Add
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex VShader_Sphere           
            #pragma fragment PShader_Sphere      

            #include "Assets\01_Script\RtsTerrain\04_DeferredRender\01_GShader\DeferredCull.hlsl"
            ENDHLSL
        }
    }
}
