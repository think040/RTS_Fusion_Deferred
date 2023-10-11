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

[CreateAssetMenu(fileName = "CoreRPA_DF", menuName = "CoreRPA_DF")]
public class CoreRPA_DF : RenderPipelineAsset
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

        return new CoreRP_DF();
    }
}

public class CoreRP_DF : RenderPipeline
{
    protected override void Render(ScriptableRenderContext context, Camera[] cams)
    {
#if UNITY_EDITOR              
        if (EditorApplication.isPlaying == true)
        {
            RenderGOM_DF.PreFrameRender(context, cams);
            BeginFrameRendering(context, cams);

            foreach (Camera cam in cams)
            {
                RenderGOM_DF.PreCameraRender(context, cam);
                BeginCameraRendering(context, cam);

                RenderGOM_DF.RenderCam(context, cam);

                EndCameraRendering(context, cam);
                RenderGOM_DF.PostCameraRender(context, cam);
            }

            EndFrameRendering(context, cams);
            RenderGOM_DF.PostFrameRender(context, cams);
        }
#else               
        {
            RenderGOM_DF.PreFrameRender(context, cams);
            BeginFrameRendering(context, cams);
            
            foreach (Camera cam in cams)
            {
                RenderGOM_DF.PreCameraRender(context, cam);
                BeginCameraRendering(context, cam);
        
                RenderGOM_DF.RenderCam(context, cam);
        
                EndCameraRendering(context, cam);
                RenderGOM_DF.PostCameraRender(context, cam);
            }
            
            EndFrameRendering(context, cams);
            RenderGOM_DF.PostFrameRender(context, cams);
        }
#endif
    }
}

public static class RenderGOM_DF
{
    static RenderGOM_DF()
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
    //public static Color clearColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
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

    public static Action<ScriptableRenderContext, CommandBuffer, Camera, PerCamera> OnRenderCamViewport
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

            {
                UpdateCSM(context, cmd, cams);
                UpdateCBM(context, cmd, cams);
                Cull(context, cmd, cams);
            }

            //CSM
            {
                PreRenderCSM(context, cmd, cams);

                InvertBackFaceCulling(cmd, true);
                RenderCSM(context, cmd, cams);
                InvertBackFaceCulling(cmd, false);

                PostRenderCSM(context, cmd, cams);
            }

            //CBM
            {
                PreRenderCBM(context, cmd, cams);

                //InvertBackFaceCulling(cmd, true);
                RenderCBM(context, cmd, cams);
                //InvertBackFaceCulling(cmd, false);

                PostRenderCBM(context, cmd, cams);
            }

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
        if (cam.gameObject.tag != "MainLight")
        {
            SetPerCamPorperty(cam, true);
            RenTexInfo_DF.Update(cam);

            CommandBuffer cmd = CommandBufferPool.Get();

            {
                cmd.SetRenderTarget(RenTexInfo_DF.frame_now.view, RenTexInfo_DF.depth_now.view);
                cmd.ClearRenderTarget(true, true, clearColor);
            }

            {
                InvertBackFaceCulling(cmd, true);

                {
                    OnRenderCam(context, cmd, cam, perCam);
                }

                { 
                    cmd.SetRenderTarget(RenTexInfo_DF.frame_now.view, RenTexInfo_DF.depth_now.view);
                    cmd.ClearRenderTarget(true, false, clearColor);

                    OnRenderCamAlpha(context, cmd, cam, perCam);
#if UNITY_EDITOR
                    if (cam.cameraType == CameraType.SceneView)
                    {
                        OnRenderCamDebug(context, cmd, cam, perCam);
                    }
#endif

                    OnRenderCamViewport(context, cmd, cam, perCam);

                }

                InvertBackFaceCulling(cmd, false);
            }

            {
                cmd.Blit(RenTexInfo_DF.frame_now.view, cam.targetTexture);
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



    //CBM
    public static Action<ScriptableRenderContext, CommandBuffer, Camera[]> UpdateCBM
    {
        get; set;
    } = (context, cmd, cam) => { };

    public static Action<ScriptableRenderContext, CommandBuffer, Camera[]> PreRenderCBM
    {
        get; set;
    } = (context, cmd, cam) => { };

    public static Action<ScriptableRenderContext, CommandBuffer, Camera[]> RenderCBM
    {
        get; set;
    } = (context, cmd, cam) => { };

    public static Action<ScriptableRenderContext, CommandBuffer, Camera[]> PostRenderCBM
    {
        get; set;
    } = (context, cmd, cam) => { };
}

class RenTexInfo_DF
{    
    public static Dictionary<Camera, RenTexInfo_DF> dic_renInfo;

    #region Info
    public class Info
    {
        public enum Type
        {
            frame_float, frame_color, _depth
        }

        public Type type;
        public bool msaa;       

        public RenderTexture tex;
        public RenderTargetIdentifier view;
    }

    static int count_frame = 7;
    static int count_depth = 3;

    static int final_idx_frame;
    static int final_idx_depth;

    Info[] _frame;
    Info[] _depth;

    public static Info frame_now;
    public static Info depth_now;

    public static Info[] frame;
    public static Info[] depth;

    public static Info frame_gbuffer0;
    public static Info frame_gbuffer1;
    public static Info frame_gbuffer2;
    public static Info frame_gbuffer3;

    public static Info frame_lbuffer;       //light
    public static Info frame_dbuffer;       //decal
    public static Info frame_final;
    public static Info frame_fxaa;


    public static RenderTargetIdentifier[] rti_gbuffer;
    public static RenderTargetIdentifier rti_gbuffer_depth;

    public static Rect[] vRects;

    #endregion


    static RenTexInfo_DF()
    {
        dic_renInfo = new Dictionary<Camera, RenTexInfo_DF>();

        {
            //count_frame = 7;
            count_frame = 8;
            count_depth = 3;
        }
        
        {
            final_idx_frame = count_frame - 1;
            final_idx_depth = count_depth - 1;
        }

        {
            frame = new Info[count_frame];
            depth = new Info[count_depth];
        }

        {
            rti_gbuffer = new RenderTargetIdentifier[4];
        }

        {
            vRects = new Rect[count_frame - 1];
        }
    }

    RenTexInfo_DF()
    {                
        {
            _frame = new Info[count_frame];
            _depth = new Info[count_depth];
        }

        for (int i = 0; i < count_frame; i++)
        {
            _frame[i] = new Info();
            _frame[i].type = Info.Type.frame_float;
        }

        for (int i = 0; i < count_depth; i++)
        {
            _depth[i] = new Info();
            _depth[i].type = Info.Type._depth;
        }

        {
            _frame[0].type = Info.Type.frame_float;
            _frame[1].type = Info.Type.frame_float;
            _frame[2].type = Info.Type.frame_float;
            _frame[3].type = Info.Type.frame_float;

            _frame[4].type = Info.Type.frame_float;
            _frame[5].type = Info.Type.frame_float;

            _frame[6].type = Info.Type.frame_color;
        }

        {
            _depth[0].type = Info.Type._depth;
            _depth[1].type = Info.Type._depth;
            _depth[2].type = Info.Type._depth;
        }       
    }

    public Rect preCamRect = new Rect(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f));


    public static void Update(Camera cam)
    {
        RenTexInfo_DF ren_info;
        if (dic_renInfo.TryGetValue(cam, out ren_info))
        {
            if (ren_info.TestSize(cam))
            {
                ren_info.UpdateData(cam.pixelRect);
            }
        }
        else
        {
            ren_info = new RenTexInfo_DF();
            dic_renInfo.Add(cam, ren_info);
            {
                ren_info.UpdateData(cam.pixelRect);
            }
        }

        SetNow(ren_info);
        SetViewPort(cam.pixelRect);
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

        Action<Info> CreateFrameTex = 
            (Info info) => 
            {
                ReleaseTex(info.tex);

                RenderTextureDescriptor rtd = new RenderTextureDescriptor();
                {
                    rtd.msaaSamples = 1;
                    rtd.depthBufferBits = 0;
                    rtd.enableRandomWrite = false;
                    
                    if(info.type == Info.Type.frame_float)
                    {
                        rtd.colorFormat = RenderTextureFormat.ARGBFloat;
                    }
                    else
                    if(info.type == Info.Type.frame_color)
                    {
                        rtd.colorFormat = RenderTextureFormat.ARGB32;
                    }                   
                    rtd.dimension = TextureDimension.Tex2D;
                    rtd.width = dw;
                    rtd.height = dh;
                    rtd.volumeDepth = 1;
                }
                
                info.msaa = false;                
                info.tex = new RenderTexture(rtd);
                info.view = new RenderTargetIdentifier(info.tex);
            };        

        Action<Info> CreateDepthTex =
            (Info info) =>
            {
                ReleaseTex(info.tex);

                RenderTextureDescriptor rtd = new RenderTextureDescriptor();
                {
                    rtd.msaaSamples = 1;
                    rtd.depthBufferBits = 24;
                    rtd.enableRandomWrite = false;

                    rtd.colorFormat = RenderTextureFormat.Depth;
                    rtd.dimension = TextureDimension.Tex2D;
                    rtd.width = dw;
                    rtd.height = dh;
                    rtd.volumeDepth = 1;
                }

                info.type = Info.Type._depth;
                info.msaa = false;
                info.tex = new RenderTexture(rtd);
                info.view = new RenderTargetIdentifier(info.tex);
            };


        for(int i = 0; i < count_frame; i++)
        {
            CreateFrameTex(_frame[i]);
        }

        for (int i = 0; i < count_depth; i++)
        {
            CreateDepthTex(_depth[i]);
        }

        int a = 0;
    }

    public static void SetNow(RenTexInfo_DF ren_info)
    {
        {
            frame_now = ren_info._frame[final_idx_frame];
            depth_now = ren_info._depth[final_idx_depth];
        }

        for(int i = 0; i < count_frame; i++)
        {
            frame[i] = ren_info._frame[i];            
        }

        for (int i = 0; i < count_depth; i++)
        {            
            depth[i] = ren_info._depth[i];
        }

        //{
        //    frame_gbuffer0 = ren_info._frame[0];
        //    frame_gbuffer1 = ren_info._frame[1];
        //    frame_gbuffer2 = ren_info._frame[2];
        //    frame_gbuffer3 = ren_info._frame[3];
        //
        //    frame_lbuffer   = ren_info._frame[4];
        //    frame_final     = ren_info._frame[5];
        //    frame_fxaa      = ren_info._frame[6];
        //}

        {
            frame_gbuffer0 = ren_info._frame[0];
            frame_gbuffer1 = ren_info._frame[1];
            frame_gbuffer2 = ren_info._frame[2];
            frame_gbuffer3 = ren_info._frame[3];

            frame_lbuffer = ren_info._frame[4];
            frame_dbuffer = ren_info._frame[5];
            frame_final   = ren_info._frame[6];
            frame_fxaa    = ren_info._frame[7];
        }


        {
            rti_gbuffer[0] = ren_info._frame[0].view;
            rti_gbuffer[1] = ren_info._frame[1].view;
            rti_gbuffer[2] = ren_info._frame[2].view;
            rti_gbuffer[3] = ren_info._frame[3].view;
        }

        {
            rti_gbuffer_depth = ren_info._depth[0].view;
        }
    }

    public static void SetViewPort(Rect pixelRect)
    {
        float w = pixelRect.width;
        float h = pixelRect.height;
              
        {
            float r = 0.25f;
            float size = r * h;
        
            vRects[0] = new Rect(0.0f * size, 0.0f, size, size);
            vRects[1] = new Rect(1.0f * size, 0.0f, size, size);
            vRects[2] = new Rect(2.0f * size, 0.0f, size, size);
            vRects[3] = new Rect(3.0f * size, 0.0f, size, size);
            vRects[4] = new Rect(4.0f * size, 0.0f, size, size);
            vRects[5] = new Rect(5.0f * size, 0.0f, size, size);
            vRects[6] = new Rect(6.0f * size, 0.0f, size, size);
        }

        //{
        //    float r = 0.25f;
        //    float size = r * h;
        //
        //    vRects[0] = new Rect(0.0f * size, 0.0f, size, size);
        //    vRects[1] = new Rect(1.0f * size, 0.0f, size, size);
        //    vRects[2] = new Rect(2.0f * size, 0.0f, size, size);
        //    vRects[3] = new Rect(3.0f * size, 0.0f, size, size);
        //    vRects[4] = new Rect(4.0f * size, 0.0f, size, size);
        //    vRects[5] = new Rect(5.0f * size, 0.0f, size, size);
        //}

        //{
        //    float r = 0.5f;
        //    float size = r * h;
        //
        //    vRects[0] = new Rect(0.0f * size, 0.0f * size, size, size);
        //    vRects[1] = new Rect(1.0f * size, 0.0f * size, size, size);
        //    vRects[2] = new Rect(0.0f * size, 1.0f * size, size, size);
        //    vRects[3] = new Rect(1.0f * size, 1.0f * size, size, size);           
        //}
    }    

    public static void ReleaseTex(RenderTexture tex)
    {
        if (tex != null)
        {
            tex.Release(); tex = null;
        }
    }   
}
