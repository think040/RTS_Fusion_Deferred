Shader "Unlit/DecalShader"
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
            Name "SSD"

            Cull Front            
            ZWrite On            
            ZTest LEqual         

            HLSLPROGRAM
            #pragma vertex VShader              
            #pragma fragment PShader_Vspace
            //#pragma fragment PShader_Nspace      

            #include "Assets\01_Script\RtsTerrain\06_Decal\01_GShader\00_ScreenSpaceDecal.hlsl"

            ENDHLSL
        }   

        Pass
        {
            Name "SSD_Blend"

            Cull Front                    
            ZWrite On            
            ZTest LEqual

            BlendOp Add
            //Blend One One       //src dst
            //Blend One Zero
            //Blend SrcAlpha OneMinusSrcAlpha
            Blend SrcAlpha OneMinusSrcAlpha, One Zero  //Blend (RGB_src RGB_dst), (Alpha_src Alpha_dst)
           

            HLSLPROGRAM
            #pragma vertex VShader              
            #pragma fragment PShader_Vspace
            //#pragma fragment PShader_Nspace      

            #include "Assets\01_Script\RtsTerrain\06_Decal\01_GShader\00_ScreenSpaceDecal.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Decal_Debug"

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex VShader           
            #pragma geometry GShader
            #pragma fragment PShader

            #include "Assets\01_Script\RtsTerrain\06_Decal\01_GShader\01_DecalDebug.hlsl"
            ENDHLSL
        }
    }
}
