using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;
using Utility_JSB;

public class DecalManager : MonoBehaviour
{
    public void Init()
    {
        InitData();
        InitCompute();
        InitResource();

        InitRendering();
        InitRendering_Debug();

        InitTestDecal();
    }


    public void Enable()
    {        
        {
            RenderGOM_DF.Cull += Compute;
            //DeferredRenderManager.OnRender_DBuffer += Render;
            DeferredRenderManager.OnRender_DBuffer += Render_DBuffer;
            DeferredRenderManager.OnRender_SSD += Render_SSD;
        }

#if UNITY_EDITOR
        {            
            RenderGOM_DF.OnRenderCamDebug += Render_Debug;           
        }
#endif
    }

    public void Disable()
    {
        {
            RenderGOM_DF.Cull -= Compute;
            //DeferredRenderManager.OnRender_DBuffer -= Render;
            DeferredRenderManager.OnRender_DBuffer -= Render_DBuffer;
            DeferredRenderManager.OnRender_SSD -= Render_SSD;
        }


#if UNITY_EDITOR
        {
            RenderGOM_DF.OnRenderCamDebug -= Render_Debug;
        }
#endif
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    public int count = 4;
    public int pvfCount = 1;
    public ComputeShader cshader;
    public Shader gshader;

    public GameObject prefab_decal;
    public Mesh boxMesh;
    Camera mainCam;

    public bool bDebug = true;

    void InitData()
    {
        mainCam = Camera.main;

        {
            boxTrs = new Transform[count];
            pvfTrs = new Transform[pvfCount];
        }

        for(int i = 0; i < count; i++)
        {
            boxTrs[i] = GameObject.Instantiate(prefab_decal, float3.zero, quaternion.identity).transform;
        }

        {
            pvfTrs[0] = mainCam.transform;
        }

    }

    int ki_bone;
    int ki_worldVertex;

    int ki_pvf_vertex;
    int ki_pvf_vertex_wire;
    int ki_pvf_cull;
    
    void InitCompute()
    {        
        ki_bone             = cshader.FindKernel("CS_Bone");
        ki_worldVertex      = cshader.FindKernel("CS_WorldVertex");

        ki_pvf_vertex       = cshader.FindKernel("CS_PVF_Vertex");
        ki_pvf_vertex_wire  = cshader.FindKernel("CS_PVF_Vertex_Wire");
        ki_pvf_cull         = cshader.FindKernel("CS_PVF_Cull");
    }

    //bone
    ROBuffer<float4x4> box_trM_Buffer;
    RWBuffer<float4x4> W_box_Buffer;
    RWBuffer<float4x4> Wn_box_Buffer;
    RWBuffer<float4x4> Wi_box_Buffer;

    ROBuffer<float4x4> pvf_trM_Buffer;
    RWBuffer<float4x4> W_pvf_Buffer;
    RWBuffer<float4x4> Wn_pvf_Buffer;
    RWBuffer<float4x4> Wi_pvf_Buffer;

    Transform[] boxTrs;
    Transform[] pvfTrs;

    JobTransform box_jobTr;
    JobTransform pvf_jobTr;

    TransformAccessArray boxTraa;
    TransformAccessArray pvfTraa;

    NativeArray<float4x4> na_box_tr;
    NativeArray<float4x4> na_pvf_tr;

    int dpBoxCount;
    int dpPvfCount;

    //pvf_vertex
    HHCollider box;

    float4[] fisData;
    Vector4[] bPos; //[24]  //[8] wire
    Vector4[] bNormal; //[24]
    Vector4[] bCenter; //[1]
    Vector4[] bPlane;  //[12]


    float4[] pCenterData;   //float4    
    float4[] pPlaneData;
    float4[] pPosData;
    float4[] pNormalData;

    ROBuffer<float4> fis_Buffer;  //float4
    RWBuffer<float4> pCenter_Buffer;   //float4
    RenderTexture pPlane_Tex;
    RenderTexture pPos_Tex;
    RenderTexture pNormal_Tex;

    //pvf_vertex_wire
    Vector4[] bPosWire; //[8]

    float4[] pPosWireData;

    RenderTexture pPosWire_Tex;

    //cull    
    public float[] TestCullPVFData;    

    float4x4[] pvfM;
    
    COBuffer<int> hhIndex_Buffer;
    ROBuffer<float4x4> pvfM_Buffer;
    //ROBuffer<float4x4> boxData_Buffer;

    RenderTexture TestCullPVF_Tex;
    

    void InitResource()
    {
        //CS_Bone
        {
            box_trM_Buffer = new ROBuffer<float4x4>(count);
            W_box_Buffer  = new RWBuffer<float4x4>(count);
            Wn_box_Buffer = new RWBuffer<float4x4>(count);
            Wi_box_Buffer = new RWBuffer<float4x4>(count);
        }

        {
            pvf_trM_Buffer = new ROBuffer<float4x4>(pvfCount);
            W_pvf_Buffer = new RWBuffer<float4x4> (pvfCount);
            Wn_pvf_Buffer = new RWBuffer<float4x4>(pvfCount);
            Wi_pvf_Buffer = new RWBuffer<float4x4>(pvfCount);
        }

        {
            boxTraa = new TransformAccessArray(boxTrs);
            na_box_tr = new NativeArray<float4x4>(count, Allocator.Persistent);

            box_jobTr = new JobTransform();
            box_jobTr.naTr = na_box_tr;

            dpBoxCount = (count % 64 == 0) ? (count / 64) : (count / 64 + 1);
        }

        {
            pvfTraa = new TransformAccessArray(pvfTrs);
            na_pvf_tr = new NativeArray<float4x4>(pvfCount, Allocator.Persistent);

            pvf_jobTr = new JobTransform();
            pvf_jobTr.naTr = na_pvf_tr;

            dpPvfCount = (pvfCount % 64 == 0) ? (pvfCount / 64) : (pvfCount / 64 + 1);
        }

      

        //CS_PVF_Vertex && CS_PVF_Vertex_Wire
        {
            //solid
            fisData = new float4[pvfCount];
            bPos = new Vector4[24];
            bNormal = new Vector4[24];
            bCenter = new Vector4[1];
            bPlane = new Vector4[12];

            pCenterData = new float4[pvfCount];
            pPlaneData = new float4[12 * pvfCount];
            pPosData = new float4[24 * pvfCount];
            pNormalData = new float4[24 * pvfCount];

            //wire
            bPosWire = new Vector4[8];

            pPosWireData = new float4[8 * pvfCount];
        }


        {
            box = new HHCollider();
            box.InitBox();

            for (int i = 0; i < 24; i++)
            {
                bPos[i] = new float4(box.pos[i], 0.0f);
                bNormal[i] = new float4(box.nom[i], 0.0f);
            }

            for (int i = 0; i < 1; i++)
            {
                bCenter[i] = new float4(box.center, 0.0f);
            }

            for (int i = 0; i < 6; i++)
            {
                float3x2 plane = box.planes[i];
                bPlane[2 * i + 0] = new float4(plane.c0, 0.0f); //normal
                bPlane[2 * i + 1] = new float4(plane.c1, 0.0f); //position
            }

            //wire
            for (int i = 0; i < BoxWire.vtxCount; i++)
            {
                bPosWire[i] = new float4(BoxWire.sPos[i], 0.0f);
            }
        }

        {
            fis_Buffer = new ROBuffer<float4>(pvfCount);  //float4
            pCenter_Buffer = new RWBuffer<float4>(pvfCount);    //float4

            RenderTextureDescriptor rtd = new RenderTextureDescriptor();
            {
                rtd.colorFormat = RenderTextureFormat.ARGBFloat;
                rtd.msaaSamples = 1;
                rtd.depthBufferBits = 0;
                rtd.enableRandomWrite = true;

                rtd.dimension = TextureDimension.Tex2D;
                rtd.width = pvfCount;
                rtd.volumeDepth = 1;
            }

            {
                rtd.height = 12;
                pPlane_Tex = new RenderTexture(rtd);
            }

            {
                rtd.height = 24;
                pPos_Tex = new RenderTexture(rtd);
                pNormal_Tex = new RenderTexture(rtd);
            }

            //wire
            {
                rtd.height = 8;
                pPosWire_Tex = new RenderTexture(rtd);
            }
        }

        {
            cshader.SetVectorArray("bPos", bPos);
            cshader.SetVectorArray("bNormal", bNormal);
            cshader.SetVectorArray("bCenter", bCenter);
            cshader.SetVectorArray("bPlane", bPlane);

            cshader.SetBuffer(ki_pvf_vertex, "fis_Buffer", fis_Buffer.value);
            cshader.SetBuffer(ki_pvf_vertex, "pCenter_Buffer", pCenter_Buffer.value);
            cshader.SetTexture(ki_pvf_vertex, "pPlane_Tex", pPlane_Tex);
            cshader.SetTexture(ki_pvf_vertex, "pPos_Tex", pPos_Tex);
            cshader.SetTexture(ki_pvf_vertex, "pNormal_Tex", pNormal_Tex);

            //wire
            cshader.SetVectorArray("bPosWire", bPosWire);
            cshader.SetBuffer(ki_pvf_vertex_wire, "fis_Buffer", fis_Buffer.value);
            cshader.SetTexture(ki_pvf_vertex_wire, "pPosWire_Tex", pPosWire_Tex);
        }

        //Cull
        {
            int _count = count;
            TestCullPVFData = new float[_count * pvfCount];
           

            pvfM = new float4x4[pvfCount];            
        }

        {
            hhIndex_Buffer = new COBuffer<int>(box.indices.Length);
            hhIndex_Buffer.data = box.indices;
            hhIndex_Buffer.Write();

            pvfM_Buffer = new ROBuffer<float4x4>(pvfCount);
            
            RenderTextureDescriptor rtd = new RenderTextureDescriptor();
            {
                rtd.colorFormat = RenderTextureFormat.RFloat;
                rtd.msaaSamples = 1;
                rtd.depthBufferBits = 0;
                rtd.enableRandomWrite = true;

                rtd.dimension = TextureDimension.Tex3D;
                rtd.width = count;
                rtd.volumeDepth = 1;
            }

            {
                rtd.height = pvfCount;
                TestCullPVF_Tex = new RenderTexture(rtd);
            }           
        }
       
        {
            //pvf
            cshader.SetBuffer(ki_pvf_cull, "boxData_Buffer", box_trM_Buffer.value);
            cshader.SetBuffer(ki_pvf_cull, "hhIndex_Buffer", hhIndex_Buffer.value);
            cshader.SetBuffer(ki_pvf_cull, "pvfM_Buffer", pvfM_Buffer.value);
            cshader.SetBuffer(ki_pvf_cull, "pCenter_Buffer", pCenter_Buffer.value);
            cshader.SetTexture(ki_pvf_cull, "pPlane_Tex", pPlane_Tex);
            cshader.SetTexture(ki_pvf_cull, "pPos_Tex", pPos_Tex);
            cshader.SetTexture(ki_pvf_cull, "TestCullPVF_Tex", TestCullPVF_Tex);           
        }
    }

    MeshInfo mesh_quad;
    MeshInfo mesh_decal;

    Material mte;
    MaterialPropertyBlock mpb;

    int pass_decal;
    int pass_decal_blend;

    public Texture2D[] decalTex;

    public struct DecalInfo
    {
        public int texId;
        public int useLight;
        public int bAlphaControl;
        public float alpha;
    }

    public static ROBuffer<DecalInfo> decalInfo_Buffer { get; set; }

    void InitRendering()
    {
        {
            {
                mesh_decal = new MeshInfo(boxMesh, count);                
            }

            {
                mesh_quad = new MeshInfo(RenderUtil.CreateNDCquadMesh(), 1);
            }
        }

        {
            mte = new Material(gshader);
            mpb = new MaterialPropertyBlock();

            pass_decal = mte.FindPass("SSD");
            pass_decal_blend = mte.FindPass("SSD_Blend");
        }

        {
            perViewBuffer = new COBuffer<ViewData>(1);
            mpb.SetBuffer("camera", perViewBuffer.value);
        }

        {
            decalInfo_Buffer = new ROBuffer<DecalInfo>(count);
            //mpb.SetBuffer("decalInfo_Buffer", decalInfo_Buffer.value);
        }

        {
            mpb.SetTexture("TestCullPVF_Tex", TestCullPVF_Tex);
            mpb.SetInt("vfIdx", 0);
        }
        
    }

    Material pMte;
    Material bMte;

    MaterialPropertyBlock pMpb;
    MaterialPropertyBlock bMpb;

    int pass_debug;

    Texture2D Box_Vtx_Tex;
    GraphicsBuffer BoxWireIdx_Buffer;

    void InitRendering_Debug()
    {        
        pMte = new Material(gshader);
        bMte = new Material(gshader);

        pMpb = new MaterialPropertyBlock();
        bMpb = new MaterialPropertyBlock();

        pass_debug = pMte.FindPass("Decal_Debug");

        {
            Box_Vtx_Tex = new Texture2D(1, BoxWire.vtxCount, TextureFormat.RGBAFloat, false);
            for (int i = 0; i < BoxWire.vtxCount; i++)
            {
                Vector4 pos = new float4(BoxWire.sPos[i], 1.0f);
                Box_Vtx_Tex.SetPixel(0, i, new Color(pos.x, pos.y, pos.z, pos.w));
            }
            Box_Vtx_Tex.Apply();

            BoxWireIdx_Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, BoxWire.idxCount, sizeof(int));
            BoxWireIdx_Buffer.SetData(BoxWire.sIndices);            
        }

        {
            pMpb.SetInt("boxCount", count);            
            bMpb.SetInt("boxCount", count);

            pMpb.SetInt("type", 0);            
            bMpb.SetInt("type", 1);

            pMpb.SetTexture("Pvf_Vtx_Tex", pPosWire_Tex);            
            bMpb.SetTexture("Box_Vtx_Tex", Box_Vtx_Tex);

            pMpb.SetTexture("TestCullPVF_Tex", TestCullPVF_Tex);          
            bMpb.SetTexture("TestCullPVF_Tex", TestCullPVF_Tex);

            pMpb.SetBuffer("W_pvf_Buffer", W_pvf_Buffer.value);
            bMpb.SetBuffer("W_box_Buffer", W_box_Buffer.value);
        }
    }


    void InitTestDecal()
    {
        {
            float3 center = transform.position;
            float3 xaxis = math.rotate(transform.rotation, new float3(1.0f, 0.0f, 0.0f));
            float3 yaxis = new float3(0.0f, 1.0f, 0.0f);
            float3 zaxis = math.rotate(transform.rotation, new float3(0.0f, 0.0f, 1.0f));

            float ds;
            ds = 50.0f;
            //ds = 20.0f;
            float dp = ds * 1.25f;
            for (int i = 0; i < count; i++)
            {
                var tr = boxTrs[i];
                //tr.position = new float3((float)(i % 8) * dp, 0.0f, (float)(i / 8) * dp);
                tr.position = center + xaxis * (float)(i % 8) * dp + yaxis * (0.5f * ds - 1.0f) + zaxis * (float)(i / 8) * dp;
                tr.rotation = quaternion.identity;
                tr.localScale = new float3(ds, ds, ds);
            }
        }

        {
            var data = decalInfo_Buffer.data;
            for(int i = 0; i < count; i++)
            {
                data[i].texId = i % 2;
                //data[i].texId = (i / 2 + 1) % 2;
                //data[i].useLight = i % 2;
                //data[i].useLight = i / 2;
                //data[i].useLight = (i + 1) % 2;
                data[i].useLight = (i / 2 + 1) % 2;
                //data[i].useLight = 1;
                //data[i].bAlphaControl = 1;
                data[i].bAlphaControl = data[i].useLight == 1 ? 1 : 0;
                data[i].alpha = data[i].useLight == 1 ? 0.5f : 0.5f;
            }
            decalInfo_Buffer.Write();
        }
               

    }


    // Update is called once per frame
    void Update()
    {
        
    }

    void Compute(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {
        WriteToResource(context, cmd);
        DispatchCompute(context, cmd);
        ReadFromResource(context, cmd);

        ComputeVtx(context, cmd, cams);
    }

    void WriteToResource(ScriptableRenderContext context, CommandBuffer cmd)
    {
        {
            box_jobTr.Schedule<JobTransform>(boxTraa).Complete();
            pvf_jobTr.Schedule<JobTransform>(pvfTraa).Complete();
        }

        {
            {
                var data = box_trM_Buffer.data;
                for (int i = 0; i < count; i++)
                {
                    data[i] = na_box_tr[i];
                }
                box_trM_Buffer.Write();
            }

            {
                var data = pvf_trM_Buffer.data;
                for (int i = 0; i < pvfCount; i++)
                {
                    data[i] = na_pvf_tr[i];
                }
                pvf_trM_Buffer.Write();
            }
        }


        {
            fisData[0] = new float4(mainCam.fieldOfView, mainCam.aspect, mainCam.nearClipPlane, mainCam.farClipPlane);            

            var data = fis_Buffer.data;
            for (int i = 0; i < pvfCount; i++)
            {
                data[i] = fisData[0];
            }
            fis_Buffer.Write();
        }

        {
            float4x4 M0 = float4x4.zero;
            Transform camTr = mainCam.transform;
            M0.c0 = new float4(camTr.position, 0.0f);
            M0.c1 = ((quaternion)camTr.rotation).value;
            M0.c2 = new float4(camTr.localScale, 0.0f);           

            var data = pvfM_Buffer.data;
            for (int i = 0; i < pvfCount; i++)
            {
                data[i] = M0;
            }
            pvfM_Buffer.Write();
        }
    }

    void DispatchCompute(ScriptableRenderContext context, CommandBuffer cmd)
    {

        //Bone
        {
            {
               cmd.SetComputeBufferParam(cshader, ki_bone, "trM_Buffer", box_trM_Buffer.value);
               cmd.SetComputeBufferParam(cshader, ki_bone, "W_Buffer", W_box_Buffer.value);
               cmd.SetComputeBufferParam(cshader, ki_bone, "Wn_Buffer", Wn_box_Buffer.value);
               cmd.SetComputeBufferParam(cshader, ki_bone, "Wi_Buffer", Wi_box_Buffer.value);                
            }
                        
            cmd.DispatchCompute(cshader, ki_bone, dpBoxCount, 1, 1);

            {
                cmd.SetComputeBufferParam(cshader,ki_bone, "trM_Buffer", pvf_trM_Buffer.value);
                cmd.SetComputeBufferParam(cshader,ki_bone, "W_Buffer",  W_pvf_Buffer.value);
                cmd.SetComputeBufferParam(cshader,ki_bone, "Wn_Buffer", Wn_pvf_Buffer.value);
                cmd.SetComputeBufferParam(cshader,ki_bone, "Wi_Buffer", Wi_pvf_Buffer.value);
            }

            cmd.DispatchCompute(cshader, ki_bone, dpPvfCount, 1, 1);
        }


        //Pvf_Cull
        {            
            {
                cmd.SetComputeIntParam(cshader, "pvfOffset", 0);                
            }

            {
                cmd.DispatchCompute(cshader, ki_pvf_vertex, pvfCount, 1, 1);
                cmd.DispatchCompute(cshader, ki_pvf_cull, count, pvfCount, 1);
            }                       
        }


        if (bDebug)
        {
            cmd.DispatchCompute(cshader, ki_pvf_vertex_wire, pvfCount, 1, 1);
        }
    }

    void ReadFromResource(ScriptableRenderContext context, CommandBuffer cmd)
    {
        bool debug = false;

        if(debug)
        {
            //Bone
            {
                W_box_Buffer.Read(cmd);
                Wn_box_Buffer.Read(cmd);
                Wi_box_Buffer.Read(cmd);

                W_pvf_Buffer.Read(cmd);
                Wn_pvf_Buffer.Read(cmd);
                Wi_pvf_Buffer.Read(cmd);
            }


            //Pvf_vertex && Pvf_vertex_wire
            {
                pCenter_Buffer.Read(cmd);

                cmd.RequestAsyncReadback(pPlane_Tex,
                    (read) =>
                    {
                        var na = read.GetData<float4>(0);
                        for (int i = 0; i < read.width; i++)
                        {
                            for (int j = 0; j < read.height; j++)
                            {
                            //pPlaneData[i * read.width + j] = na[i * read.width + j];
                            pPlaneData[i * read.height + j] = na[j * read.width + i];
                            }
                        }
                    });

                cmd.RequestAsyncReadback(pPos_Tex,
                    (read) =>
                    {
                        var na = read.GetData<float4>(0);
                        for (int i = 0; i < read.width; i++)
                        {
                            for (int j = 0; j < read.height; j++)
                            {
                            //pPosData[i * read.width + j] = na[i * read.width + j];
                            pPosData[i * read.height + j] = na[j * read.width + i];
                            }
                        }
                    });

                cmd.RequestAsyncReadback(pNormal_Tex,
                    (read) =>
                    {
                        var na = read.GetData<float4>(0);
                        for (int i = 0; i < read.width; i++)
                        {
                            for (int j = 0; j < read.height; j++)
                            {
                            //pNormalData[i * read.width + j] = na[i * read.width + j];
                            pNormalData[i * read.height + j] = na[j * read.width + i];
                            }
                        }
                    });

                //wire
                cmd.RequestAsyncReadback(pPosWire_Tex,
                    (read) =>
                    {
                        var na = read.GetData<float4>(0);
                        for (int i = 0; i < read.width; i++)
                        {
                            for (int j = 0; j < read.height; j++)
                            {
                            //pPosWireData[i * read.width + j] = na[i * read.width + j];
                            pPosWireData[i * read.height + j] = na[j * read.width + i];
                            }
                        }
                    });

            }
        }

        {
            //Cull_Pvf
            {
                cmd.RequestAsyncReadback(TestCullPVF_Tex,
                    (read) =>
                    {
                        for (int i = 0; i < read.depth; i++)
                        {
                            var na = read.GetData<float>(i);

                            for (int j = 0; j < read.height; j++)
                            {
                                for (int k = 0; k < read.width; k++)
                                {
                                    TestCullPVFData[j * read.depth * read.width + i * read.width + k] = na[j * read.width + k];
                                }
                            }
                        }
                    });

            }
        }        
    }

    void ComputeVtx(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {
        bool debug = false;

        for (int i = 0; i < count; i++)
        {
            var mesh = mesh_decal;

            cmd.SetComputeVectorParam(cshader, "countInfo", mesh.ci);
            cmd.SetComputeBufferParam(cshader, ki_worldVertex, "vIn", mesh.vtxIn_cs.value);
            //cmd.SetComputeBufferParam(cshader, ki_worldVertex, "bone", mesh.wBone.value);
            //cmd.SetComputeBufferParam(cshader, ki_worldVertex, "boneIT", mesh.wBoneIT.value);
            cmd.SetComputeBufferParam(cshader, ki_worldVertex, "bone", W_box_Buffer.value);
            cmd.SetComputeBufferParam(cshader, ki_worldVertex, "boneIT", Wn_box_Buffer.value);
            cmd.SetComputeBufferParam(cshader, ki_worldVertex, "vOut", mesh.vtxIn.value);

            cmd.DispatchCompute(cshader, ki_worldVertex, mesh.insCount, mesh.vgCount, 1);
        }

        if (debug)
        {
            for (int i = 0; i < count; i++)
            {
                var mesh = mesh_decal;

                mesh.vtxIn.Read();
            }
        }


        {
            var mesh = mesh_quad;

            cmd.SetComputeVectorParam(cshader, "countInfo", mesh.ci);
            cmd.SetComputeBufferParam(cshader, ki_worldVertex, "vIn", mesh.vtxIn_cs.value);
            cmd.SetComputeBufferParam(cshader, ki_worldVertex, "bone", mesh.wBone.value);
            cmd.SetComputeBufferParam(cshader, ki_worldVertex, "boneIT", mesh.wBoneIT.value);
            cmd.SetComputeBufferParam(cshader, ki_worldVertex, "vOut", mesh.vtxIn.value);

            cmd.DispatchCompute(cshader, ki_worldVertex, mesh.insCount, mesh.vgCount, 1);
        }

        if (debug)
        {
            var mesh = mesh_quad;

            mesh.vtxIn.Read();
        }

    }


    //Render
    float4x4 GetDecalMatrix()
    {
        float4x4 D = float4x4.identity;

        //D.c3 = new float4(0.5f, 0.5f, 0.0f, 1.0f);
        D.c3 = new float4(0.5f, 0.0f, 0.5f, 1.0f);

        return D;
    }

    void UpdateTexture()
    {
        mpb.SetTexture("depthTex", RenTexInfo_DF.frame_gbuffer1.tex);
    }

    unsafe void UpdateCameraData(Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        fixed (ViewData* data = &(perViewBuffer.data[0]))
        {
            data->V = perCam.V;

            data->C = perCam.C;
            data->CV = perCam.CV;

            data->_C = RenderUtil.GetCfromV(cam, false);
            data->_CV = math.mul(data->_C, data->V);

            data->T_C = math.mul(RenderUtil.GetTfromN(), data->_C);

            data->_C_I = RenderUtil.GetVfromC(cam, false);
            data->V_I = RenderUtil.GetWfromV(cam);

            data->D = GetDecalMatrix();

            data->posW = new float4((float3)cam.transform.position, 0.0f);
            data->dirW = new float4(math.rotate(cam.transform.rotation, new float3(0.0f, 0.0f, 1.0f)), 0.0f);
            data->data = float4.zero;
        }


        perViewBuffer.Write();

        {
            mpb.SetBuffer("camera", perViewBuffer.value);
        }
    }

    bool isCull_decal(int idx)
    {
        return TestCullPVFData[idx] == 0.0f ? true : false;
    }

    void Render(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        {
            UpdateTexture();
            UpdateCameraData(cam, perCam);
        }

        {
            float4 pixelSize = new float4(cam.pixelWidth, cam.pixelHeight, 0.0f, 0.0f);
            mpb.SetVector("pixelSize", pixelSize);
        }

        {
            var mesh = mesh_decal;
            mpb.SetVector("countInfo", mesh.ci);
            mpb.SetBuffer("vtxBuffer", mesh.vtxIn.value);
            mpb.SetBuffer("vtxBuffer_St", mesh.vtxIn_st.value);
            //mpb.SetBuffer("W_I", mesh.wBoneI.value);
            mpb.SetBuffer("W_I", Wi_box_Buffer.value);

            for (int i = 0; i < count; i++)
            {
                if (isCull_decal(i))
                {
                    continue;
                }

                cmd.ClearRenderTarget(true, false, new Color(0.0f, 0.0f, 0.0f, 0.0f));            

                var data = decalInfo_Buffer.data;

                //mpb.SetTexture("decalTex", decalTex[i]);
                //mpb.SetTexture("decalTex", decalTex[0]);
                mpb.SetTexture("decalTex", decalTex[data[i].texId]);
                mpb.SetInteger("decal_idx", i);
                cmd.DrawProcedural(mesh.idxBuffer, Matrix4x4.identity, mte, pass_decal, MeshTopology.Triangles, mesh.idxCount, 1, mpb);
            }
        }        
    }

    void Render_DBuffer(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        {
            UpdateTexture();
            UpdateCameraData(cam, perCam);
        }

        {
            float4 pixelSize = new float4(cam.pixelWidth, cam.pixelHeight, 0.0f, 0.0f);
            mpb.SetVector("pixelSize", pixelSize);
        }

        var mesh = mesh_decal;
        {
            mpb.SetVector("countInfo", mesh.ci);
            mpb.SetBuffer("vtxBuffer", mesh.vtxIn.value);
            mpb.SetBuffer("vtxBuffer_St", mesh.vtxIn_st.value);
            mpb.SetBuffer("W_I", Wi_box_Buffer.value);
        }

        {                      
            for (int i = 0; i < count; i++)
            {
                if (isCull_decal(i))
                {
                    continue;
                }

                cmd.ClearRenderTarget(true, false, new Color(0.0f, 0.0f, 0.0f, 0.0f));

                var data = decalInfo_Buffer.data;

                if(data[i].useLight == 1)
                {                   
                    mpb.SetTexture("decalTex", decalTex[data[i].texId]);
                    mpb.SetInteger("decal_idx", i);
                    mpb.SetInteger("bAlphaControl", data[i].bAlphaControl);
                    mpb.SetFloat("alpha", data[i].alpha);
                    cmd.DrawProcedural(mesh.idxBuffer, Matrix4x4.identity, mte, pass_decal_blend, MeshTopology.Triangles, mesh.idxCount, 1, mpb);
                    //cmd.DrawProcedural(mesh.idxBuffer, Matrix4x4.identity, mte, pass_decal, MeshTopology.Triangles, mesh.idxCount, 1, mpb);
                }                               
            }
        }
    }

    void Render_SSD(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        //{
        //    UpdateTexture();
        //    UpdateCameraData(cam, perCam);
        //}
        //
        //{
        //    float4 pixelSize = new float4(cam.pixelWidth, cam.pixelHeight, 0.0f, 0.0f);
        //    mpb.SetVector("pixelSize", pixelSize);
        //}

        var mesh = mesh_decal;
        //{            
        //    mpb.SetVector("countInfo", mesh.ci);
        //    mpb.SetBuffer("vtxBuffer", mesh.vtxIn.value);
        //    mpb.SetBuffer("vtxBuffer_St", mesh.vtxIn_st.value);
        //    mpb.SetBuffer("W_I", Wi_box_Buffer.value);
        //}

        {           
            for (int i = 0; i < count; i++)
            {
                if (isCull_decal(i))
                {
                    continue;
                }

                cmd.ClearRenderTarget(true, false, new Color(0.0f, 0.0f, 0.0f, 0.0f));

                var data = decalInfo_Buffer.data;

                if (data[i].useLight == 0)
                {                    
                    mpb.SetTexture("decalTex", decalTex[data[i].texId]);
                    mpb.SetInteger("decal_idx", i);
                    mpb.SetInteger("bAlphaControl", data[i].bAlphaControl);
                    mpb.SetFloat("alpha", data[i].alpha);
                    cmd.DrawProcedural(mesh.idxBuffer, Matrix4x4.identity, mte, pass_decal_blend, MeshTopology.Triangles, mesh.idxCount, 1, mpb);
                }
            }
        }
    }


    //Render_Debug
    void Render_Debug(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        if (GameManager.cullTestMode != 2)
        {
            return;
        }


#if UNITY_EDITOR
        if (bDebug)
        {
            {
                pMpb.SetMatrix("CV", perCam.CV);                
                bMpb.SetMatrix("CV", perCam.CV);
                
                pMpb.SetInt("vfIdx", 0);                
                bMpb.SetInt("vfIdx", 0);
            }         

            //Pvf
            {               
                cmd.DrawProcedural(BoxWireIdx_Buffer, Matrix4x4.identity, pMte, pass_debug, MeshTopology.Lines, BoxWire.idxCount, 1, pMpb);
            }

            //Box           
            {
                int _count = count;
                cmd.DrawProcedural(BoxWireIdx_Buffer, Matrix4x4.identity, bMte, pass_debug, MeshTopology.Lines, BoxWire.idxCount, _count, bMpb);
            }
        }
#endif

    }




    void ReleaseResource()
    {
        BufferBase<float4x4>.Release(box_trM_Buffer);
        BufferBase<float4x4>.Release(W_box_Buffer);
        BufferBase<float4x4>.Release(Wn_box_Buffer);
        BufferBase<float4x4>.Release(Wi_box_Buffer);

        BufferBase<float4x4>.Release(pvf_trM_Buffer);
        BufferBase<float4x4>.Release(W_pvf_Buffer);
        BufferBase<float4x4>.Release(Wn_pvf_Buffer);
        BufferBase<float4x4>.Release(Wi_pvf_Buffer);

        BufferBase<float4>.Release(fis_Buffer);
        BufferBase<float4>.Release(pCenter_Buffer);
        BufferBase<int>.Release(hhIndex_Buffer);
        BufferBase<float4x4>.Release(pvfM_Buffer);        
    }

    void ReleaseTexture(RenderTexture tex)
    {
        if (tex != null)
        {
            tex.Release();
            tex = null;
        }
    }

    void DisposeNa<T>(NativeArray<T> na) where T : struct
    {
        if (na.IsCreated) na.Dispose();
    }

    void DisposeTraa(TransformAccessArray traa)
    {
        if (traa.isCreated) traa.Dispose();
    }

    void OnDestroy()
    {
        {
            ReleaseResource();
        }

        {           
            ReleaseTexture(pPlane_Tex);
            ReleaseTexture(pPos_Tex);
            ReleaseTexture(pNormal_Tex);
            ReleaseTexture(pPosWire_Tex);

            ReleaseTexture(TestCullPVF_Tex);            
        }

        {
            DisposeNa<float4x4>(na_box_tr);
            DisposeNa<float4x4>(na_pvf_tr);

            DisposeTraa(boxTraa);
            DisposeTraa(pvfTraa);
        }

        {
            if (BoxWireIdx_Buffer != null) { BoxWireIdx_Buffer.Release(); BoxWireIdx_Buffer = null; }
        }

        {
            if (mesh_decal != null)
            {
                mesh_decal.ReleaseResource();
            }

            if (mesh_quad != null)
            {
                mesh_quad.ReleaseResource();
            }

            BufferBase<LightData>.Release(mLightData_Buffer);
            BufferBase<ViewData>.Release(perViewBuffer);
        }

        {
            BufferBase<DecalInfo>.Release(decalInfo_Buffer);
        }
    }

    [BurstCompile]
    struct JobTransform : IJobParallelForTransform
    {
        public NativeArray<float4x4> naTr;

        public void Execute(int i, TransformAccess traa)
        {
            float4x4 tr = float4x4.zero;

            tr.c0.xyz = traa.localPosition;
            tr.c1 = ((quaternion)traa.localRotation).value;
            tr.c2.xyz = traa.localScale;

            naTr[i] = tr;
        }
    }

    struct ViewData
    {
        public float4x4 V;

        public float4x4 C;
        public float4x4 CV;

        public float4x4 _C;
        public float4x4 _CV;

        public float4x4 T_C;

        public float4x4 _C_I;
        public float4x4 V_I;

        public float4x4 D;

        public float4 posW;
        public float4 dirW;
        public float4 data;
    };

    COBuffer<ViewData> perViewBuffer;

    struct LightData
    {
        public float4 posW;
        public float4 dirW;
        public float4 posV;
        public float4 dirV;

        public float4 color;
        public float4 data;
    };

    COBuffer<LightData> mLightData_Buffer;


    [Serializable]
    public class MeshInfo
    {
        public Mesh mesh;

        public int insCount;
        public int idxCount;
        public int vtxCount;

        public int vgCount;
        const int vtCount = 1024;

        public GraphicsBuffer idxBuffer;

        public int layer = 0;
        public MeshInfo(Mesh mesh, int insCount, int layer = 0)
        {
            this.mesh = mesh;
            this.insCount = insCount;
            this.layer = layer;

            Init();
        }

        public void Init()
        {
            {
                idxCount = mesh.GetIndices(0).Length;
                vtxCount = mesh.vertexCount;
            }

            {
                vgCount = vtxCount % vtCount == 0 ? vtxCount / vtCount : vtxCount / vtCount + 1;
            }

            {
                ci = new float4(vtxCount, 0.0f, 0.0f, 0.0f);
            }


            {
                idxBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, idxCount, sizeof(int));
            }

            {
                vtxIn_cs = new ROBuffer<VertexCSIn>(vtxCount);
                vtxIn_st = new ROBuffer<VertexSt>(vtxCount);

                vtxIn = new RWBuffer<VertexVSIn>(insCount * vtxCount);

                wBone = new ROBuffer<float4x4>(insCount);
                wBoneIT = new ROBuffer<float4x4>(insCount);

                wBoneI = new ROBuffer<float4x4>(insCount);
            }


            {
                idxBuffer.SetData(mesh.GetIndices(0));
            }

            {
                List<Vector3> posList = new List<Vector3>();
                List<Vector3> nomList = new List<Vector3>();
                List<Vector4> tanList = new List<Vector4>();
                List<BoneWeight> bwList = new List<BoneWeight>();
                List<Vector2> uvList = new List<Vector2>();

                mesh.GetVertices(posList);
                mesh.GetNormals(nomList);
                mesh.GetTangents(tanList);
                mesh.GetBoneWeights(bwList);
                mesh.GetUVs(0, uvList);

                bool usePos = posList.Count > 0;
                bool useNom = nomList.Count > 0;
                bool useTan = tanList.Count > 0;
                bool useBW = bwList.Count > 0;
                bool useUV = uvList.Count > 0;

                {
                    var data = vtxIn_cs.data;
                    for (int i = 0; i < vtxCount; i++)
                    {
                        VertexCSIn vtx;

                        vtx.posL = float4.zero;
                        vtx.nomL = float4.zero;
                        vtx.tanL = float4.zero;
                        vtx.boneI = float4.zero;
                        vtx.boneW = float4.zero;

                        if (usePos) { vtx.posL = new float4((float3)posList[i], 0.0f); }
                        if (useNom) { vtx.nomL = new float4((float3)nomList[i], 0.0f); }
                        if (useTan) { vtx.tanL = new float4((float4)tanList[i]); }
                        if (useBW)
                        {
                            BoneWeight bw = bwList[i];
                            vtx.boneI = new float4(bw.boneIndex0, bw.boneIndex1, bw.boneIndex2, bw.boneIndex3);
                            vtx.boneW = new float4(bw.weight0, bw.weight1, bw.weight2, bw.weight3);
                        };

                        data[i] = vtx;
                    }
                    vtxIn_cs.Write();
                }

                {
                    var data = vtxIn_st.data;
                    for (int i = 0; i < vtxCount; i++)
                    {
                        VertexSt vtx;

                        vtx.uv = float4.zero;

                        if (useUV) { vtx.uv = new float4((float2)uvList[i], 0.0f, 0.0f); }

                        data[i] = vtx;
                    }
                    vtxIn_st.Write();
                }
            }

            int a = 0;
        }

        public static void Get_W_Wn(float3 pos, quaternion rot, float3 sca, out float4x4 W, out float4x4 Wn)
        {
            float3x3 R = new float3x3(rot);

            W.c0 = new float4(sca.x * R.c0, 0.0f);
            W.c1 = new float4(sca.y * R.c1, 0.0f);
            W.c2 = new float4(sca.z * R.c2, 0.0f);
            W.c3 = new float4(pos, 1.0f);

            float3 rs = 1.0f / sca;
            Wn.c0 = new float4(rs.x * R.c0, 0.0f);
            Wn.c1 = new float4(rs.y * R.c1, 0.0f);
            Wn.c2 = new float4(rs.z * R.c2, 0.0f);
            Wn.c3 = float4.zero;
        }

        public Texture2D testTex;

        public void CreateTestTexture(float4 color)
        {
            int iw = 512;
            int ih = 512;

            //unsafe
            {
                {
                    testTex = new Texture2D(iw, ih, TextureFormat.ARGB32, false);
                }

                float4[] image = new float4[iw * ih];

                int cu = 8;
                int cv = 8;

                float kx = (float)cu / (float)iw;
                float ky = (float)cv / (float)ih;

                for (int i = 0; i < ih; i++)
                {
                    for (int j = 0; j < iw; j++)
                    {
                        int m = (int)((float)j * kx);
                        int n = (int)((float)i * ky);

                        int mn = (int)(math.pow(-1, m) * math.pow(-1, n));

                        float4 c = float4.zero;
                        if (mn == 1)
                        {
                            c = color;
                        }
                        else if (mn == -1)
                        {
                            c = new float4(1.0f, 1.0f, 1.0f, 1.0f);
                        }

                        image[i * iw + j] = c;
                        //image[i * iw + j] = new flaot4(0.0f, 0.0f, 1.0f, 1.0f);

                        testTex.SetPixel(i, j, new Color(c.x, c.y, c.z, c.w));
                    }
                }

                testTex.Apply();
            }
        }


        public void ReleaseResource()
        {
            if (idxBuffer != null) { idxBuffer.Release(); idxBuffer = null; }

            BufferBase<VertexCSIn>.Release(vtxIn_cs);
            BufferBase<VertexSt>.Release(vtxIn_st);
            BufferBase<VertexVSIn>.Release(vtxIn);
            BufferBase<float4x4>.Release(wBone);
            BufferBase<float4x4>.Release(wBoneIT);
            BufferBase<float4x4>.Release(wBoneI);
        }

        public struct VertexCSIn
        {
            public float4 posL;
            public float4 nomL;
            public float4 tanL;
            public float4 boneI;
            public float4 boneW;
        };

        public struct VertexSt
        {
            public float4 uv;
        };

        public struct VertexVSIn
        {
            public float4 posW;
            public float4 nomW;
            public float4 tanW;
        };



        public float4 ci;

        public ROBuffer<VertexCSIn> vtxIn_cs;
        public ROBuffer<VertexSt> vtxIn_st;

        public RWBuffer<VertexVSIn> vtxIn;

        public ROBuffer<float4x4> wBone;
        public ROBuffer<float4x4> wBoneIT;

        public ROBuffer<float4x4> wBoneI;
    }

    struct TrObject
    {
        public float3 pos;
        public quaternion rot;
        public float3 sca;

        public static unsafe void GetW_Wn(TrObject* tr, float4x4* W, float4x4* Wn)
        {
            float3* posL = &(tr->pos);
            quaternion* rotL = &(tr->rot);
            float3* scaL = &(tr->sca);

            {
                *W = float4x4.identity;
                *Wn = float4x4.identity;
                float3x3 R = new float3x3(*rotL);

                {
                    W->c0 = new float4(scaL->x * R.c0, 0.0f);
                    W->c1 = new float4(scaL->y * R.c1, 0.0f);
                    W->c2 = new float4(scaL->z * R.c2, 0.0f);
                    W->c3 = new float4(*posL, 1.0f);
                }

                float3 rsca = new float3(1.0f, 1.0f, 1.0f) / *scaL;

                {
                    Wn->c0 = new float4(rsca.x * R.c0, 0.0f);
                    Wn->c1 = new float4(rsca.y * R.c1, 0.0f);
                    Wn->c2 = new float4(rsca.z * R.c2, 0.0f);
                    Wn->c3 = float4.zero;
                }
            }

            return;
        }

        public static unsafe void GetW_Wn(TrObject* tr, float4x4* W, float4x4* Wn, float4x4* Wi)
        {
            float3* posL = &(tr->pos);
            quaternion* rotL = &(tr->rot);
            float3* scaL = &(tr->sca);

            {
                *W = float4x4.identity;
                *Wn = float4x4.identity;
                *Wi = float4x4.identity;

                float3x3 R = new float3x3(*rotL);
                {
                    W->c0 = new float4(scaL->x * R.c0, 0.0f);
                    W->c1 = new float4(scaL->y * R.c1, 0.0f);
                    W->c2 = new float4(scaL->z * R.c2, 0.0f);
                    W->c3 = new float4(*posL, 1.0f);
                }

                float3 rsca = new float3(1.0f, 1.0f, 1.0f) / *scaL;
                {
                    Wn->c0 = new float4(rsca.x * R.c0, 0.0f);
                    Wn->c1 = new float4(rsca.y * R.c1, 0.0f);
                    Wn->c2 = new float4(rsca.z * R.c2, 0.0f);
                    Wn->c3 = float4.zero;
                }

                float3 t = -new float3(math.dot((Wn->c0).xyz, *posL), math.dot((Wn->c1).xyz, *posL), math.dot((Wn->c2).xyz, *posL));
                {
                    Wi->c0 = new float4((Wn->c0).xyz, t.x);
                    Wi->c1 = new float4((Wn->c1).xyz, t.y);
                    Wi->c2 = new float4((Wn->c2).xyz, t.z);
                    Wi->c3 = new float4(0.0f, 0.0f, 0.0f, 1.0f);

                    *Wi = math.transpose(*Wi);
                }

            }

            return;
        }

    };
}
