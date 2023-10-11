Shader "Unlit/Cull"
{
    Properties
    {

    }
    SubShader
    {
        //Tags { "RenderType" = "Opaque" }
        //LOD 100

        //Pass
        //{
        //    Name "Cull"
        //
        //    Cull Back
        //    ZWrite On
        //    ZTest LEqual
        //
        //    BlendOp Add
        //    Blend SrcAlpha OneMinusSrcAlpha
        //
        //    HLSLPROGRAM
        //    #pragma vertex VShader           
        //    #pragma fragment PShader      
        //    
        //    #include "Assets\01_Script\RTSmini\00_GameManagement\01_GShader\Cull.hlsl"
        //    ENDHLSL
        //}

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

            #include "Assets\01_Script\RtsTerrain\00_GameControl\01_GShader\Cull.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Cull_Ovf"

            Cull Back
            ZWrite On
            ZTest LEqual

            BlendOp Add
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex VShader_OVF           
            #pragma fragment PShader_OVF      

            #include "Assets\01_Script\RtsTerrain\00_GameControl\01_GShader\Cull.hlsl"
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

            #include "Assets\01_Script\RtsTerrain\00_GameControl\01_GShader\Cull.hlsl"
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

            #include "Assets\01_Script\RtsTerrain\00_GameControl\01_GShader\Cull.hlsl"
            ENDHLSL
        }
    }
}