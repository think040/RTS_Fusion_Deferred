using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Rendering;
using Utility_JSB;

public class CSM_Action : MonoBehaviour
{
    LightManager lightMan;
    new Light light;
    Camera mainCam;
    int igdt;
    public bool bDepth = true;
    public bool bDebug = true;
    //int csmSize = 800;
    //int csmSize = 800;
    [HideInInspector]
    public Rect csmRect;
    public float[] csmRange = new float[] { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };

    private void Awake()
    {

    }

    public void Enable()
    {
        //RenderGOM.UpdateCSM += UpdateCSM;
        RenderGOM_DF.PreRenderCSM += PreRenderCSM;
        RenderGOM_DF.PostRenderCSM += PostRenderCSM;
    }

    public void Disable()
    {
        //RenderGOM.UpdateCSM -= UpdateCSM;
        RenderGOM_DF.PreRenderCSM -= PreRenderCSM;
        RenderGOM_DF.PostRenderCSM -= PostRenderCSM;
    }

    public void Init()
    {
        mainCam = Camera.main;
        lightMan = GameManager.lightMan;
        light = GetComponent<Light>();

        StartCSM_Job(csmRange);
        InitData();
        InitRenderTex();
    }

    void InitData()
    {
        {
            csm_data_Buffer = new ROBuffer<CSM_depth_data>(csmCount);
        }
    }

    public RenderTexture renTex { get; set; }
    RenderTargetIdentifier rti;
    public Texture2D[] tex2D;
    public int csmIdx { get; set; } = 0;

    public int csmSize = 4096;





    void InitRenderTex()
    {
        {
            //csmRect = new Rect(0.0f, 0.0f, csmSize, csmSize);
            csmRect = new Rect(0.0f, 0.0f, csmSize, csmSize);
        }

        {
            RenderTextureDescriptor rtd = new RenderTextureDescriptor();
            {
                rtd.msaaSamples = 1;
                rtd.depthBufferBits = 24;
                rtd.enableRandomWrite = false;

                if (bDepth)
                {
                    rtd.colorFormat = RenderTextureFormat.RFloat;
                }
                else
                {
                    rtd.colorFormat = RenderTextureFormat.ARGB32;
                }
                rtd.dimension = TextureDimension.Tex2DArray;
                rtd.width = csmSize;
                rtd.height = csmSize;
                rtd.volumeDepth = csmCount;
            }
            renTex = new RenderTexture(rtd);
            rti = new RenderTargetIdentifier(renTex, 0, CubemapFace.Unknown, -1);
        }

        {
            tex2D = new Texture2D[csmCount];
            for (int i = 0; i < csmCount; i++)
            {
                if (bDepth)
                {
                    tex2D[i] = new Texture2D(csmSize, csmSize, TextureFormat.RFloat, false);
                }
                else
                {
                    tex2D[i] = new Texture2D(csmSize, csmSize, TextureFormat.ARGB32, false);
                }
            }
        }

        RenderGOM.InitCSMData(csmCount);
    }

    public void Bind_Data(MaterialPropertyBlock mpb)
    {
        mpb.SetBuffer("light_Buffer", LightManager.light_Buffer.value);
        mpb.SetFloat("csmSize", (float)csmSize);
        mpb.SetBuffer("csm_data_Buffer", csm_data_Buffer.value);

        mpb.SetTexture("csmTexture", renTex);
    }

    public void Update_Data(MaterialPropertyBlock mpb, float4x4 CV_cam)
    {
        mpb.SetMatrix("CV_cam", CV_cam);
        mpb.SetMatrixArray("TCV_csm", TCV_depth);
        mpb.SetFloatArray("endZ_csm", endZ);
    }

    Color clearColor = new Color(0.75f, 0.75f, 0.75f, 1.0f);

    void PreRenderCSM(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {
        //CommandBuffer cmd = CommandBufferPool.Get();
        cmd.SetRenderTarget(rti, 0, CubemapFace.Unknown, -1);

        if (bDepth)
        {
            cmd.ClearRenderTarget(true, true, Color.white, 1.0f);
        }
        else
        {
            cmd.ClearRenderTarget(true, true, clearColor, 1.0f);
        }


        //context.ExecuteCommandBuffer(cmd);
        //CommandBufferPool.Release(cmd);
    }

    void PostRenderCSM(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {

#if UNITY_EDITOR
        if (bDebug && !bDepth)
        {
            if (LightManager.type == LightType.Directional)
            {
                for (int i = 0; i < csmCount; i++)
                {
                    cmd.CopyTexture(rti, i, tex2D[i], 0);
                }

                for (int i = 0; i < cams.Length; i++)
                {
                    if (cams[i].gameObject.tag == "MainLight")
                    {
                        var cam = cams[i];

                        cmd.SetRenderTarget(cam.targetTexture);
                        cmd.ClearRenderTarget(true, true, RenderGOM.clearColor);

                        if (csmIdx < 0)
                        {
                            return;
                        }

                        cam.transform.position = posW[csmIdx];
                        cam.transform.rotation = transform.rotation;

                        cam.orthographic = true;
                        //cam.orthographicSize = 2.0f * frustumInfo[csmIdx].x;
                        cam.orthographicSize = frustumInfo[csmIdx].x;
                        cam.aspect = frustumInfo[csmIdx].y;
                        cam.nearClipPlane = frustumInfo[csmIdx].z;
                        cam.farClipPlane = frustumInfo[csmIdx].w;

                        cmd.Blit(tex2D[csmIdx], cam.targetTexture);
                    }
                }
            }
        }
#endif

    }


    void Update()
    {
        if (!GameManager.bUpdate)
        {
            return;
        }

        //UpdateCSM_Job(mainCam);

        //if(Input.GetKeyDown(KeyCode.KeypadPlus))
        //{
        //    csmIdx = (++csmIdx) % csmCount;
        //}

        //if (Input.GetKeyDown(KeyCode.KeypadMinus))
        //{
        //    csmIdx = (--csmIdx) < 0 ? csmCount - 1 : csmIdx;
        //}
    }

    private void OnDestroy()
    {
        if (enabled)
        {
            DispoesCSM_Job();

            BufferBase<CSMdata>.Release(csmDataBuffer);
            BufferBase<CSM_depth_data>.Release(csm_data_Buffer);
        }
    }


    NativeArray<CfromW_Job.CBuffer> cBuffer;
    NativeArray<CfromW_Job.Input> input;
    NativeArray<CfromW_Job.Output> output;

    public Matrix4x4[] CV { get; set; }
    public Matrix4x4[] CV_depth { get; set; }
    public Matrix4x4[] TCV_depth { get; set; }
    public float[] endZ { get; set; }
    public int csmCount { get; set; } = 4;
    public float4 dirW { get; set; }
    public Matrix4x4[] E_Cull { get; set; }
    public Matrix4x4[] W_Cull { get; set; }
    public Matrix4x4[] Wn_Cull { get; set; }
    public float3[] posW { get; set; }
    public float4[] frustumInfo { get; set; }


    public float4[] pos { get; set; }
    public quaternion[] rot { get; set; }
    public float4[] fi { get; set; }

    void SetIGDT()
    {
        GraphicsDeviceType gdt = SystemInfo.graphicsDeviceType;
        if (gdt == GraphicsDeviceType.Direct3D11 || gdt == GraphicsDeviceType.Direct3D12)
        {
            igdt = 0;
        }
        else if (gdt == GraphicsDeviceType.OpenGLCore)
        {
            igdt = 1;
        }
        else if (gdt == GraphicsDeviceType.Vulkan)
        {
            igdt = 2;
        }
    }

    public struct CSMdata
    {
        public float4x4 CV;
        public float4x4 CV_depth;
    };

    public ROBuffer<CSMdata> csmDataBuffer
    {
        get; set;
    }

    public void StartCSM_Job(float[] csmRange)
    {
        SetIGDT();

        this.csmRange = csmRange;
        csmCount = csmRange.Length - 1;
        cBuffer = new NativeArray<CfromW_Job.CBuffer>(1, Allocator.Persistent);
        input = new NativeArray<CfromW_Job.Input>(csmCount, Allocator.Persistent);
        output = new NativeArray<CfromW_Job.Output>(csmCount, Allocator.Persistent);

        CV = new Matrix4x4[csmCount];
        CV_depth = new Matrix4x4[csmCount];
        TCV_depth = new Matrix4x4[csmCount];
        endZ = new float[csmCount];
        E_Cull = new Matrix4x4[csmCount];
        W_Cull = new Matrix4x4[csmCount];
        Wn_Cull = new Matrix4x4[csmCount];
        posW = new float3[csmCount];
        frustumInfo = new float4[csmCount];
        //RenderGOM.CV = CV;
        //RenderGOM.CV_depth = CV_depth;
        //RenderGOM.TCV_depth = TCV_depth;
        //RenderGOM.endZ = endZ;

        {
            pos = new float4[csmCount];
            rot = new quaternion[csmCount];
            fi = new float4[csmCount];
        }

        {
            csmDataBuffer = new ROBuffer<CSMdata>(csmCount);
        }
    }

    public struct CSM_depth_data
    {
        public float4x4 CV;
        public float4x4 CV_depth;
    }

    public ROBuffer<CSM_depth_data> csm_data_Buffer;

    public void UpdateCSM(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {
        if (light.type == LightType.Directional)
        {

        }

        {
            CfromW_Job.CBuffer cb = new CfromW_Job.CBuffer();
            cb.vfov_2 = mainCam.fieldOfView * 0.5f;
            cb.aspect = mainCam.aspect;
            cb.near = mainCam.nearClipPlane;
            cb.far = mainCam.farClipPlane;
            cb.lightCamRot = transform.rotation;
            cb.W = RenderUtil.GetWfromV(mainCam);
            cb.C = RenderUtil.GetCfromV(mainCam, false);
            cb.igdt = igdt;
            cBuffer[0] = cb;

            float e = 0.00f; // 0.05f;
            for (int i = 0; i < csmCount; i++)
            {
                CfromW_Job.Input ip = new CfromW_Job.Input();
                ip.zn = csmRange[0];
                //ip.zn = csmRange[i];
                ip.zf = csmRange[i + 1] + e;
                input[i] = ip;
            }

            CfromW_Job job = new CfromW_Job();
            job.cbuffer = cBuffer;
            job.input = input;
            job.output = output;
            JobHandle handle = job.Schedule<CfromW_Job>(csmCount, 1);
            handle.Complete();


            for (int i = 0; i < csmCount; i++)
            {
                CV[i] = job.output[i].CV;
                CV_depth[i] = job.output[i].CV_depth;
                TCV_depth[i] = math.mul(RenderUtil.GetTfromN(), CV_depth[i]);
                endZ[i] = job.output[i].endZ;
                E_Cull[i] = job.output[i].E_Cull;
                W_Cull[i] = job.output[i].W_Cull;
                Wn_Cull[i] = job.output[i].Wn_Cull;
                posW[i] = job.output[i].posW;
                frustumInfo[i] = job.output[i].frustumInfo;

                pos[i] = new float4(posW[i], 1.0f);
                rot[i] = transform.rotation;
                fi[i] = frustumInfo[i];
            }
            dirW = new float4(job.output[0].dirW, 0.0f);

            for (int i = 0; i < csmCount; i++)
            {
                csmDataBuffer.data[i].CV = CV[i];
                csmDataBuffer.data[i].CV_depth = CV_depth[i];
            }
            csmDataBuffer.Write();

            for (int i = 0; i < csmCount; i++)
            {
                csm_data_Buffer.data[i].CV = CV[i];
                csm_data_Buffer.data[i].CV_depth = CV_depth[i];
            }
            csm_data_Buffer.Write();
        }
    }

    public void UpdateCSM()
    {
        if (light.type == LightType.Directional)
        {

        }

        {
            CfromW_Job.CBuffer cb = new CfromW_Job.CBuffer();
            cb.vfov_2 = mainCam.fieldOfView * 0.5f;
            cb.aspect = mainCam.aspect;
            cb.near = mainCam.nearClipPlane;
            cb.far = mainCam.farClipPlane;
            cb.lightCamRot = transform.rotation;
            cb.W = RenderUtil.GetWfromV(mainCam);
            cb.C = RenderUtil.GetCfromV(mainCam, false);
            cb.igdt = igdt;
            cBuffer[0] = cb;

            float e = 0.00f; // 0.05f;
            for (int i = 0; i < csmCount; i++)
            {
                CfromW_Job.Input ip = new CfromW_Job.Input();
                ip.zn = csmRange[0];
                //ip.zn = csmRange[i];
                ip.zf = csmRange[i + 1] + e;
                input[i] = ip;
            }

            CfromW_Job job = new CfromW_Job();
            job.cbuffer = cBuffer;
            job.input = input;
            job.output = output;
            JobHandle handle = job.Schedule<CfromW_Job>(csmCount, 1);
            handle.Complete();


            for (int i = 0; i < csmCount; i++)
            {
                CV[i] = job.output[i].CV;
                CV_depth[i] = job.output[i].CV_depth;
                TCV_depth[i] = math.mul(RenderUtil.GetTfromN(), CV_depth[i]);
                endZ[i] = job.output[i].endZ;
                E_Cull[i] = job.output[i].E_Cull;
                W_Cull[i] = job.output[i].W_Cull;
                Wn_Cull[i] = job.output[i].Wn_Cull;
                posW[i] = job.output[i].posW;
                frustumInfo[i] = job.output[i].frustumInfo;

                pos[i] = new float4(posW[i], 1.0f);
                rot[i] = transform.rotation;
                fi[i] = frustumInfo[i];
            }
            dirW = new float4(job.output[0].dirW, 0.0f);

            for (int i = 0; i < csmCount; i++)
            {
                csmDataBuffer.data[i].CV = CV[i];
                csmDataBuffer.data[i].CV_depth = CV_depth[i];
            }
            csmDataBuffer.Write();

            for (int i = 0; i < csmCount; i++)
            {
                csm_data_Buffer.data[i].CV = CV[i];
                csm_data_Buffer.data[i].CV_depth = CV_depth[i];
            }
            csm_data_Buffer.Write();
        }
    }

    public void UpdateCSM_Job(Camera vcam)
    {

        if (vcam.cameraType == CameraType.SceneView)
        {
            Camera camera = vcam;
            vcam.fieldOfView = mainCam.fieldOfView;
            vcam.aspect = mainCam.aspect;
            vcam.nearClipPlane = mainCam.nearClipPlane;
            //vcam.farClipPlane = mainCam.farClipPlane;
        }

        if (light.type == LightType.Directional)
        {
            CfromW_Job.CBuffer cb = new CfromW_Job.CBuffer();
            cb.vfov_2 = vcam.fieldOfView * 0.5f;
            cb.aspect = vcam.aspect;
            cb.near = vcam.nearClipPlane;
            cb.far = vcam.farClipPlane;
            cb.lightCamRot = transform.rotation;
            cb.W = RenderUtil.GetWfromV(vcam);
            cb.C = RenderUtil.GetCfromV(vcam, false);
            cb.igdt = igdt;
            cBuffer[0] = cb;

            float e = 0.00f; // 0.05f;
            for (int i = 0; i < csmCount; i++)
            {
                CfromW_Job.Input ip = new CfromW_Job.Input();
                ip.zn = csmRange[0];
                //ip.zn = csmRange[i];
                ip.zf = csmRange[i + 1] + e;
                input[i] = ip;
            }

            CfromW_Job job = new CfromW_Job();
            job.cbuffer = cBuffer;
            job.input = input;
            job.output = output;
            JobHandle handle = job.Schedule<CfromW_Job>(csmCount, 1);
            handle.Complete();


            for (int i = 0; i < csmCount; i++)
            {
                CV[i] = job.output[i].CV;
                CV_depth[i] = job.output[i].CV_depth;
                TCV_depth[i] = math.mul(RenderUtil.GetTfromN(), CV_depth[i]);
                endZ[i] = job.output[i].endZ;
                E_Cull[i] = job.output[i].E_Cull;
                W_Cull[i] = job.output[i].W_Cull;
                Wn_Cull[i] = job.output[i].Wn_Cull;
                posW[i] = job.output[i].posW;
                frustumInfo[i] = job.output[i].frustumInfo;
            }
            dirW = new float4(job.output[0].dirW, 0.0f);
        }
    }

    public void DispoesCSM_Job()
    {
        if (cBuffer.IsCreated) cBuffer.Dispose();
        if (input.IsCreated) input.Dispose();
        if (output.IsCreated) output.Dispose();
    }

    [BurstCompile]
    struct CfromW_Job : IJobParallelFor
    {
        public struct CBuffer
        {
            public float vfov_2;
            public float aspect;
            public float near;
            public float far;
            public quaternion lightCamRot;
            public float4x4 W;
            public float4x4 C;
            public int igdt;
        }
        public struct Input
        {
            public float zn;
            public float zf;
        }
        public struct Output
        {
            public float4x4 CV;
            public float4x4 CV_depth;
            public float3 dirW;
            public float endZ;

            public float4x4 E_Cull;
            public float4x4 W_Cull;
            public float4x4 Wn_Cull;

            public float3 posW;
            public float4 frustumInfo;
        }

        [ReadOnly]
        public NativeArray<CBuffer> cbuffer;
        [ReadOnly]
        public NativeArray<Input> input;
        public NativeArray<Output> output;

        [BurstCompile]
        public void Execute(int i)
        {
            Output result = new Output();

            float vfov_2 = cbuffer[0].vfov_2;
            float aspect = cbuffer[0].aspect;
            float near = cbuffer[0].near;
            float far = cbuffer[0].far;
            float fn = far - near;

            float2 zRatio = new float2(input[i].zn, input[i].zf);
            zRatio = (new float2(near, near) + fn * zRatio);
            float4 posC_view = math.mul(cbuffer[0].C, new float4(0.0f, 0.0f, zRatio.y, 1.0f));
            result.endZ = posC_view.z / posC_view.w;
            zRatio /= new float2(near, near);
            //float zn = (near + fn * input[i].zn) / near;
            //float zf = (near + fn * input[i].zf) / near;
            float zn = near;
            float zf = far;

            float yn = math.tan(math.radians(vfov_2)) * zn;
            float xn = aspect * yn;

            //            
            float4x4 nP = float4x4.identity;
            float4x4 fP = float4x4.identity;
            nP.c0 = new float4(new float3(+xn, +yn, +zn) * zRatio.x, 1.0f);
            nP.c1 = new float4(new float3(-xn, +yn, +zn) * zRatio.x, 1.0f);
            nP.c2 = new float4(new float3(-xn, -yn, +zn) * zRatio.x, 1.0f);
            nP.c3 = new float4(new float3(+xn, -yn, +zn) * zRatio.x, 1.0f);
            fP.c0 = new float4(new float3(+xn, +yn, +zn) * zRatio.y, 1.0f);
            fP.c1 = new float4(new float3(-xn, +yn, +zn) * zRatio.y, 1.0f);
            fP.c2 = new float4(new float3(-xn, -yn, +zn) * zRatio.y, 1.0f);
            fP.c3 = new float4(new float3(+xn, -yn, +zn) * zRatio.y, 1.0f);
            float3 center = (
                nP.c0.xyz + nP.c1.xyz + nP.c2.xyz + nP.c3.xyz +
                fP.c0.xyz + fP.c1.xyz + fP.c2.xyz + fP.c3.xyz) / 8.0f;
            center = math.mul(cbuffer[0].W, new float4(center, 1.0f)).xyz;

            //
            float4x4 V = float4x4.identity;
            V = RenderUtil.GetVfromW(center, cbuffer[0].lightCamRot);
            float4x4 V2V = float4x4.identity;
            V2V = math.mul(V, cbuffer[0].W);
            nP = math.mul(V2V, nP);
            fP = math.mul(V2V, fP);

            // 
            float size = 0.0f;
            float maxZ = 0.0f;
            float minZ = 0.0f;

            unsafe
            {
                float* narrx = stackalloc float[8];
                float* narry = stackalloc float[8];
                float* narrz = stackalloc float[8];

                narrx[0] = math.abs(nP.c0.x); narry[0] = math.abs(nP.c0.y);
                narrx[1] = math.abs(nP.c1.x); narry[1] = math.abs(nP.c1.y);
                narrx[2] = math.abs(nP.c2.x); narry[2] = math.abs(nP.c2.y);
                narrx[3] = math.abs(nP.c3.x); narry[3] = math.abs(nP.c3.y);
                narrx[4] = math.abs(fP.c0.x); narry[4] = math.abs(fP.c0.y);
                narrx[5] = math.abs(fP.c1.x); narry[5] = math.abs(fP.c1.y);
                narrx[6] = math.abs(fP.c2.x); narry[6] = math.abs(fP.c2.y);
                narrx[7] = math.abs(fP.c3.x); narry[7] = math.abs(fP.c3.y);
                size = 1.2f * math.max(Max(narrx, 8), Max(narry, 8));

                narrz[0] = nP.c0.z;
                narrz[1] = nP.c1.z;
                narrz[2] = nP.c2.z;
                narrz[3] = nP.c3.z;
                narrz[4] = fP.c0.z;
                narrz[5] = fP.c1.z;
                narrz[6] = fP.c2.z;
                narrz[7] = fP.c3.z;
                maxZ = Max(narrz, 8);
                minZ = Min(narrz, 8);
            }

            float4x4 C = float4x4.identity;
            float4x4 C_depth = float4x4.identity;
            quaternion rot = cbuffer[0].lightCamRot;
            float3 axisz = math.rotate(rot, new float3(0.0f, 0.0f, 1.0f));
            result.dirW = axisz;
            float3 pos = center + minZ * axisz;
            V = RenderUtil.GetVfromW(pos, rot);
            float outFar = 1.2f * (maxZ - minZ);
            float outNear = 0.0f;
            outNear = -outFar;
            float outAspect = 1.0f;
            float outSize = size;
            //C = RenderUtil.GetCfromV_Ortho(outSize, outAspect, outNear, outFar, true);
            //C_depth = RenderUtil.GetCfromV_Ortho(outSize, outAspect, outNear, outFar, false);

            RenderUtil.GetCfromV_Ortho_Optimized(outSize, outAspect, outNear, outFar, out C, out C_depth);

            result.CV = math.mul(C, V);
            result.CV_depth = math.mul(C_depth, V);

            float4x4 inTr = float4x4.identity;
            inTr.c0 = new float4(pos, 1.0f);
            inTr.c1 = rot.value;
            inTr.c2 = new float4(1.0f, 1.0f, 1.0f, 0.0f);
            float4 inFrus = new float4(outSize, outAspect, outNear, outFar);
            float4x4 E_cull;
            float4x4 W_cull;
            float4x4 Wn_cull;
            OVFTransform.ChangeBoxToElement_Matrix(inTr, inFrus, out E_cull, out W_cull, out Wn_cull);
            result.E_Cull = E_cull;
            result.W_Cull = W_cull;
            result.Wn_Cull = Wn_cull;

            result.posW = pos;
            result.frustumInfo = inFrus;

            output[i] = result;

        }

        [BurstCompile]
        unsafe float Max(float* a, int num)
        {
            float max = a[0];
            for (int i = 1; i < num; i++)
            {
                if (a[i] > max)
                {
                    max = a[i];
                }
            }

            return max;
        }

        [BurstCompile]
        unsafe float Min(float* a, int num)
        {
            float min = a[0];
            for (int i = 1; i < num; i++)
            {
                if (a[i] < min)
                {
                    min = a[i];
                }
            }

            return min;
        }
    }
}