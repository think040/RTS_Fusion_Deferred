using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Unity.Mathematics;

using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
#endif

using Object = UnityEngine.Object;

[CreateAssetMenu(fileName = "CoreRPA", menuName = "CoreRPA")]
public class CoreRPA : RenderPipelineAsset
{
    protected override RenderPipeline CreatePipeline()
    {

#if UNITY_EDITOR
        var gdt = SystemInfo.graphicsDeviceType;
        string[] define = new string[3];

        if (gdt == GraphicsDeviceType.Direct3D11 || gdt == GraphicsDeviceType.Direct3D12)
        {
            define = new string[] { "DirectX", "OpenGL_Non", "Vulkan_Non", "FUSION_WEAVER" };
        }
        else if (gdt == GraphicsDeviceType.OpenGLCore || gdt == GraphicsDeviceType.OpenGLES3)
        {
            define = new string[] { "DirectX_Non", "OpenGL", "Vulkan_Non", "FUSION_WEAVER" };
        }
        else if (gdt == GraphicsDeviceType.Vulkan)
        {
            define = new string[] { "DirectX_Non", "OpenGL_Non", "Vulkan", "FUSION_WEAVER" };
        }

        //PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, "DirectX");
        //PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, "OpneGL");
        //PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, "Vulkan_Non");
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, 
            define);
        
#endif

        return new CoreRP();
    }
}

public class CoreRP : RenderPipeline
{
    protected override void Render(ScriptableRenderContext context, Camera[] cams)
    {
#if UNITY_EDITOR              
        if (EditorApplication.isPlaying == true)
        {
            RenderGOM.PreFrameRender(context, cams);
            BeginFrameRendering(context, cams);

            foreach (Camera cam in cams)
            {
                RenderGOM.PreCameraRender(context, cam);
                BeginCameraRendering(context, cam);

                RenderGOM.RenderCam(context, cam);

                EndCameraRendering(context, cam);
                RenderGOM.PostCameraRender(context, cam);
            }

            EndFrameRendering(context, cams);
            RenderGOM.PostFrameRender(context, cams);
        }
#else               
        {
            RenderGOM.PreFrameRender(context, cams);
            BeginFrameRendering(context, cams);
            
            foreach (Camera cam in cams)
            {
                RenderGOM.PreCameraRender(context, cam);
                BeginCameraRendering(context, cam);

                RenderGOM.RenderCam(context, cam);

                EndCameraRendering(context, cam);
                RenderGOM.PostCameraRender(context, cam);
            }
            
            EndFrameRendering(context, cams);
            RenderGOM.PostFrameRender(context, cams);
        }
#endif
    }
}

public static class RenderGOM
{
    static RenderGOM()
    {        
        perCam = new PerCamera();

        if ((RenderUtil.gdt == GraphicsDeviceType.Direct3D11) ||
            (RenderUtil.gdt == GraphicsDeviceType.Direct3D12) ||
            (RenderUtil.gdt == GraphicsDeviceType.Vulkan))
        {
            bBackCullingInvert = true;
        }
        else
        {
            bBackCullingInvert = false;
        }
    }

    public class PerCamera
    {
        public float4x4 V;
        public float4x4 C;
        public float4x4 S;
        public float4x4 CV;
        public float3 dirW_view;
        public float3 posW_view;
    }

    public static PerCamera perCam;   
    public static Color clearColor = new Color(0.75f, 0.75f, 0.75f, 1.0f);
    static bool bBackCullingInvert = false;

    public static Action<ScriptableRenderContext, CommandBuffer, Camera[]> BeginFrameRender
    {
        get; set;
    } = (context, cmd, cams) => { };

    public static Action<ScriptableRenderContext, CommandBuffer, Camera, PerCamera> BeginCameraRender
    {
        get; set;
    } = (context, cmd, cam, perCam) => { }; 

    public static Action<ScriptableRenderContext, CommandBuffer, Camera, PerCamera> OnRenderCam
    {
        get; set;
    } = (context, cmd, cam, perCam) => { };

    public static Action<ScriptableRenderContext, CommandBuffer, Camera, PerCamera> OnRenderCamAlpha
    {
        get; set;
    } = (context, cmd, cam, perCam) => { };

    public static Action<ScriptableRenderContext, CommandBuffer, Camera, PerCamera> OnRenderCamDebug
    {
        get; set;
    } = (context, cmd, cam, perCam) => { };

    public static Action<ScriptableRenderContext, CommandBuffer, Camera, PerCamera> EndCameraRender
    {
        get; set;
    } = (context, cmd, cam, perCam) => { };

    public static Action<ScriptableRenderContext, CommandBuffer, Camera[]> EndFrameRender
    {
        get; set;
    } = (context, cmd, cams) => { };


    public static void SetPerCamPorperty(Camera cam, bool toTex = false)
    {
        float4x4 V = float4x4.zero;
        float4x4 C = float4x4.zero;
        float4x4 S = float4x4.zero;
        float4x4 CV = float4x4.zero;

        //if(cam.cameraType == CameraType.SceneView)
        //{
        //    cam.farClipPlane = 1000.0f;
        //}

        {
            V = RenderUtil.GetVfromW(cam);

            C = RenderUtil.GetCfromV(cam, toTex);
            S = RenderUtil.GetSfromN(cam, toTex);
            //C = RenderUtil.GetCfromV(cam, true, toTex);
            //S = RenderUtil.GetSfromN(cam, true, toTex);

            CV = math.mul(C, V);
            float3 dirW_view = -math.rotate(cam.transform.rotation, new float3(0.0f, 0.0f, 1.0f));
            float3 posW_view = cam.transform.position;

            perCam.V = V;
            perCam.C = C;
            perCam.S = S;
            perCam.CV = CV;
            perCam.dirW_view = dirW_view;
            perCam.posW_view = posW_view;
        }
    }

    public static void PreFrameRender(ScriptableRenderContext context, Camera[] cams)
    {
        //{
        //    CommandBuffer cmd = CommandBufferPool.Get();
        //    BeginFrameRender(context, cmd, cams);
        //    context.ExecuteCommandBuffer(cmd);
        //    CommandBufferPool.Release(cmd);
        //}
        
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            BeginFrameRender(context, cmd, cams);

            UpdateCSM(context, cmd, cams);
            Cull(context, cmd, cams);

            PreRenderCSM(context, cmd, cams);

            InvertBackFaceCulling(cmd, true);
            RenderCSM(context, cmd, cams);
            InvertBackFaceCulling(cmd, false);

            PostRenderCSM(context, cmd, cams);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);           
        }
    }

    public static void PreCameraRender(ScriptableRenderContext context, Camera cam)
    {        
        {
            SetPerCamPorperty(cam, true);                
        }

        {
            CommandBuffer cmd = CommandBufferPool.Get();
            BeginCameraRender(context, cmd, cam, perCam);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        //{
        //    Debug.Log("Camera Name : " + cam.name);
        //    Debug.Log("Instance ID : " + cam.GetInstanceID().ToString());            
        //}
    }    

    public static void RenderCam(ScriptableRenderContext context, Camera cam)
    {
        {
            SetPerCamPorperty(cam, true);
            RenTexInfo.Update(cam);

            CommandBuffer cmd = CommandBufferPool.Get();

            {
                cmd.SetRenderTarget(RenTexInfo.rti_frame_now, RenTexInfo.rti_depth_now);
                cmd.ClearRenderTarget(true, true, clearColor);
            }

            {
                InvertBackFaceCulling(cmd, true);

                {
                    OnRenderCam(context, cmd, cam, perCam);
                    OnRenderCamAlpha(context, cmd, cam, perCam);
#if UNITY_EDITOR
                    if (cam.cameraType == CameraType.SceneView)
                    {
                        OnRenderCamDebug(context, cmd, cam, perCam);
                    }
#endif 
                }

                InvertBackFaceCulling(cmd, false);
            }

            {
                cmd.Blit(RenTexInfo.rti_frame_now, cam.targetTexture);
            }            

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);            
        }
    }


    public static void PostCameraRender(ScriptableRenderContext context, Camera cam)
    {        
        {
            SetPerCamPorperty(cam, true);            
        }

        {
            CommandBuffer cmd = CommandBufferPool.Get();
            EndCameraRender(context, cmd, cam, perCam);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public static void PostFrameRender(ScriptableRenderContext context, Camera[] cams)
    {               
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            EndFrameRender(context, cmd, cams);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        context.Submit();
    }   

    //    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void InvertBackFaceCulling0(CommandBuffer cmd, bool value)
    {
        if (bBackCullingInvert)
        {
            cmd.SetInvertCulling(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvertBackFaceCulling(CommandBuffer cmd, bool value)
    {
#if (DirectX || Vulkan)
            cmd.SetInvertCulling(value);
#endif       
    }


    //CSM
    public static int csmCount;

    public static Action<ScriptableRenderContext, CommandBuffer, Camera[]> UpdateCSM
    {
        get; set;
    } = (context, cmd, cam) => { };

    public static Action<ScriptableRenderContext, CommandBuffer, Camera[]> Cull
    {
        get; set;
    } = (context, cmd, cam) => { };

    public static Action<ScriptableRenderContext, CommandBuffer, Camera> CullDebug
    {
        get; set;
    } = (context, cmd, cam) => { };

    public static Action<ScriptableRenderContext, CommandBuffer, Camera[]> PreRenderCSM
    {
        get; set;
    } = (context, cmd, cam) => { };

    public static Action<ScriptableRenderContext, CommandBuffer, Camera[]> RenderCSM
    {
        get; set;
    } = (context, cmd, cam) => { };

    public static Action<ScriptableRenderContext, CommandBuffer, Camera[]> PostRenderCSM
    {
        get; set;
    } = (context, cmd, cam) => { };



    public static void InitCSMData(int csmCount)
    {
        RenderGOM.csmCount = csmCount;
        //PreRenderCSM_GS = (context, cam) => { };
        //RenderCSM_GS = (context, cam) => { };        
    }

}

class RenTexInfo
{   
    public static RenderTexture renTex_frame_now;
    public static RenderTargetIdentifier rti_frame_now;

    public static RenderTexture renTex_depth_now;
    public static RenderTargetIdentifier rti_depth_now;

    public static Dictionary<Camera, RenTexInfo> dic_renInfo;

    static RenTexInfo()
    {
        dic_renInfo = new Dictionary<Camera, RenTexInfo>();        
    }

    public RenderTexture renTex_frame;
    public RenderTargetIdentifier rti_frame;

    public RenderTexture renTex_depth;
    public RenderTargetIdentifier rti_depth;

    public Rect preCamRect = new Rect(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f));


    public static void Update(Camera cam)
    {
        RenTexInfo ren_info;
        if(dic_renInfo.TryGetValue(cam, out ren_info))
        {
            if (ren_info.TestSize(cam))
            {
                ren_info.UpdateData(cam.pixelRect);
            }
        }
        else
        {
            ren_info = new RenTexInfo();
            dic_renInfo.Add(cam, ren_info);
            {
                ren_info.UpdateData(cam.pixelRect);
            }            
        }

        SetNow(ren_info);
    }

    public static void SetNow(RenTexInfo ren_info)
    {
        renTex_frame_now = ren_info.renTex_frame;
        rti_frame_now = ren_info.rti_frame;

        renTex_depth_now = ren_info.renTex_depth;
        rti_depth_now = ren_info.rti_depth;
    }

    public bool TestSize(Camera cam)
    {
        if (preCamRect.width != cam.pixelRect.width || preCamRect.height != cam.pixelRect.height)
        {
            UnityEngine.Debug.Log("ScreenSize Changed!");
            return true;
        }

        return false;
    }   

    public void UpdateData(Rect pixelRect)
    {
        int dw = (int)pixelRect.width;
        int dh = (int)pixelRect.height;

        preCamRect = pixelRect;


        {
            ReleaseTex(renTex_frame);

            RenderTextureDescriptor rtd = new RenderTextureDescriptor();
            {
                rtd.msaaSamples = 4;
                rtd.depthBufferBits = 0;
                rtd.enableRandomWrite = false;

                rtd.colorFormat = RenderTextureFormat.ARGB32;
                rtd.dimension = TextureDimension.Tex2D;
                rtd.width = dw;
                rtd.height = dh;
                rtd.volumeDepth = 1;
            }

            renTex_frame = new RenderTexture(rtd);
            rti_frame = new RenderTargetIdentifier(renTex_frame);
        }

        {
            ReleaseTex(renTex_depth);

            RenderTextureDescriptor rtd = new RenderTextureDescriptor();
            {
                rtd.msaaSamples = 4;
                rtd.depthBufferBits = 24;
                rtd.enableRandomWrite = false;

                rtd.colorFormat = RenderTextureFormat.Depth;
                rtd.dimension = TextureDimension.Tex2D;
                rtd.width = dw;
                rtd.height = dh;
                rtd.volumeDepth = 1;
            }

            renTex_depth = new RenderTexture(rtd);
            rti_depth = new RenderTargetIdentifier(renTex_depth);
        }
    }


    public void ReleaseTex(RenderTexture tex)
    {
        if (tex != null)
        {
            tex.Release(); tex = null;
        }
    }

    public void DestroyTex(Texture tex)
    {
        if (tex != null)
        {
            Object.DestroyImmediate(tex); tex = null;
        }
    }
}





