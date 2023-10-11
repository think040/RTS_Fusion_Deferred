Shader "Unlit/Torus"
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
            Name "Torus"

            Cull Back
            ZWrite On
            ZTest LEqual
            
            BlendOp Add
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM
            #pragma vertex VShader        
            #pragma fragment PShader      
            
            #include "Assets\01_Script\RtsTerrain\00_GameControl\01_GShader\Torus.hlsl"
            ENDHLSL            
        }
    }
}
