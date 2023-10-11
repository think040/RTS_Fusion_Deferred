Shader "Unlit/UnitAnimation"
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
            Name "UnitAnimationDepth_CSM"

            Cull Back
            ZWrite On
            ZTest LEqual         

            HLSLPROGRAM
            #pragma vertex VShader  
            #pragma geometry GShader_CSM
            #pragma fragment PShader_CSM      

            #include "Assets\01_Script\RtsTerrain\02_UnitAnimation\01_GShader\UnitAnimationDepth.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "UnitAnimationDepth_CBM"

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex VShader  
            #pragma geometry GShader_CBM
            #pragma fragment PShader_CBM      

            #include "Assets\01_Script\RtsTerrain\02_UnitAnimation\01_GShader\UnitAnimationDepth.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "UnitAnimationColor"

            Cull Back
            ZWrite On
            ZTest LEqual

            BlendOp Add
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM            
            #pragma require cubearray
            #pragma vertex VShader           
            #pragma fragment PShader      
                      
            #include "Assets\01_Script\RtsTerrain\02_UnitAnimation\01_GShader\UnitAnimationColor.hlsl"            

            ENDHLSL
        }        

        Pass
        {
            Name "UnitAnimationColor_GBuffer"

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
            #pragma vertex VShader_GBuffer           
            #pragma fragment PShader_GBuffer      

            #include "Assets\01_Script\RtsTerrain\02_UnitAnimation\01_GShader\UnitAnimationColor.hlsl"            

            ENDHLSL
        }

        Pass
        {
            Name "UnitAnimationColor_Tess_GBuffer"

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
            #pragma hull  HShader
            #pragma domain DShader
            #pragma geometry GShader
            #pragma fragment PShader_GBuffer      

            #include "Assets\01_Script\RtsTerrain\02_UnitAnimation\01_GShader\UnitAnimationColor_Tess.hlsl"            

            ENDHLSL
        }

        Pass
        {
            Name "UnitAnimationColor_Tess_Wire"

            Cull Back
            ZWrite On
            ZTest LEqual

            BlendOp Add
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM            

            #pragma vertex VShader     
            #pragma hull  HShader
            #pragma domain DShader
            #pragma geometry GShader_Wire
            #pragma fragment PShader_Wire      

            #include "Assets\01_Script\RtsTerrain\02_UnitAnimation\01_GShader\UnitAnimationColor_Tess.hlsl"            

            ENDHLSL
        }

        Pass
        {
            Name "UnitAnimationDepth_Tess_CSM"
        
            Cull Back
            ZWrite On
            ZTest LEqual
        
            HLSLPROGRAM
            #pragma vertex VShader  
            #pragma hull  HShader
            #pragma domain DShader
            #pragma geometry GShader_CSM
            #pragma fragment PShader_CSM      
        
            #include "Assets\01_Script\RtsTerrain\02_UnitAnimation\01_GShader\UnitAnimationDepth_Tess.hlsl"
        
            ENDHLSL
        }
        
        Pass
        {
            Name "UnitAnimationDepth_Tess_CBM"
        
            Cull Back
            ZWrite On
            ZTest LEqual
        
            HLSLPROGRAM
            #pragma vertex VShader
            #pragma hull  HShader
            #pragma domain DShader
            #pragma geometry GShader_CBM
            #pragma fragment PShader_CBM      
        
            #include "Assets\01_Script\RtsTerrain\02_UnitAnimation\01_GShader\UnitAnimationDepth_Tess.hlsl"
        
            ENDHLSL
        }


        Pass
        {
            Name "UnitAnimationColor_Tp" //Transparent

            Cull Back
            ZWrite On
            ZTest LEqual

            BlendOp Add
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma require cubearray
            #pragma vertex VShader           
            #pragma fragment PShader_Tp      

            #include "Assets\01_Script\RtsTerrain\02_UnitAnimation\01_GShader\UnitAnimationColor.hlsl"            

            ENDHLSL
        }
    }
}
