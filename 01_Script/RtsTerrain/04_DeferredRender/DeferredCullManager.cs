using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Jobs;
using Fusion;

public class DeferredCullManager : MonoBehaviour
{
    public ComputeShader cshader;
    public Shader gshader;

    int ki_pvf;    

    int ki_pvf_vertex;    
    int ki_sphere_vertex;
    int ki_svf_vertex;

    int ki_pvf_cull_sphere;
    int ki_svf_cull_sphere;

    int ki_sphere_center;
    int ki_sphere_posV;

    int pass;
    int pass_pvf;    
    int pass_svf;
    int pass_sphere;

    int debugCullMode = 0;
    bool bDrawPvf = true;    
    bool bDrawSvf = true;
    bool bDrawSp = true;

    Camera mainCam;

    new Light light;
    Transform trLight;

    public void Init()
    {
        if (GameManager.unitCount > 0)
        {
            {
                light = LightManager.instance.light;
                trLight = LightManager.instance.transform;
            }

            {
                ki_pvf = cshader.FindKernel("CS_PVF");                

                ki_pvf_vertex = cshader.FindKernel("CS_PVF_Vertex");
                ki_svf_vertex = cshader.FindKernel("CS_SVF_Vertex");
                ki_sphere_vertex = cshader.FindKernel("CS_Sphere_Vertex");
                
                ki_pvf_cull_sphere = cshader.FindKernel("CS_PVF_Cull_Sphere");                

                ki_sphere_center = cshader.FindKernel("CS_Sphere_Center");
                ki_sphere_posV = cshader.FindKernel("CS_Sphere_PosV");

                ki_svf_cull_sphere = cshader.FindKernel("CS_SVF_Cull_Sphere");
            }

            {
                mainCam = Camera.main;
                csmAction = LightManager.instance.csm_action;
            }

            {

#if UNITY_EDITOR
                bCullDebug = true;
#else
            bCullDebug = false;
#endif
            }

            InitData();
            InitResource();
            InitDebugRender();
        }
    }

    public void Enable()
    {
        //if (!GameManager.bUnitSpawn)
        //{
        //    return;
        //}

        //if (GameManager.unitCount > 0)
        {
            //RenderGOM.Cull += Compute;
            RenderGOM_DF.OnRenderCamDebug += RenderDebug;
        }
    }
  

    public void Disable()
    {
        //if (GameManager.unitCount > 0)
        {
            //RenderGOM.Cull -= Compute;
            RenderGOM_DF.OnRenderCamDebug -= RenderDebug;
        }
    }

    UnitManager[] unitMans;    

    public static int[] spCounts;
    public static int spCount = 0;
    public static int[] cullOffsets { get; set; }

    int dpSpCount;

    int spVtxInCount;
    int vfVtxCount;

    int spVtxOutCount;
    int pvfVtxOutCount;    
    int svfVtxOutCount;

    Transform[] spTrs;
    NetworkTransform[] nt_spTrs;

    int pvfCount = 1;
    int planePvfCount;
    int groupPvfCount;
    int totalPvfCount;

    
    int totalSvfCount;
    int svfCount = 1;

    CSM_Action csmAction;
    //float4[] csmPos;
    //quaternion[] csmRot;
    //float4[] csmfi;

    public static bool bCull = true;
    bool bCullDebug = true;


    //NativeArray<float3> spOffset;
    TransformAccessArray spTraa;


    NativeArray<float4x4> spTrData;
    SphereTransform spTrans;
    int dpSpTrCount;


    void InitData()
    {
        unitMans = GameManager.unitMan;                

        {
            int[] baseVtx;
            sMesh = RenderUtil.CreateSphereMeshWirePartsDetail_Normal(1.0f, 12, 6, out baseVtx);
            cMesh = RenderUtil.CreateBoxMeshWirePartsDetail_Normal(0.5f, out baseVtx);
        }

        spCount = 0;
        {
            spCounts = new int[unitMans.Length];
            cullOffsets = new int[unitMans.Length];
            //_cullOffsets = new int[unitMans.Length + 1];

            for (int i = 0; i < unitMans.Length; i++)
            {
                //_cullOffsets[i] = spCount;
                cullOffsets[i] = spCount;

                spCounts[i] = unitMans[i].count;
                spCount += spCounts[i];

                //unitMans[i].SetCullData(i);
            }            

            bCull = true;
            if (spCount == 0) bCull = false;

            dpSpCount = (spCount % 8 == 0) ? (spCount / 8) : (spCount / 8 + 1);
        }

        {
            spTrs = new Transform[spCount];
            nt_spTrs = new NetworkTransform[spCount];

            sphere = new float4[spCount];
         

            int start = 0;
            int i = 0;
            for (i = 0; i < unitMans.Length; i++)
            {
                for (int j = 0; j < unitMans[i].count; j++)
                {
                    spTrs[start + j] = unitMans[i].trs[j];
                    nt_spTrs[start + j] = unitMans[i].ntTrs[j];

                    {
                        sphere[start + j] = _sphereData[i]; 
                    }                        
                }
                start += unitMans[i].count;
            }           
        }
      

        {
            pvfCount = 1;            
        }

        {
            groupPvfCount = 1;            
            totalPvfCount = groupPvfCount * pvfCount;            
            planePvfCount = totalPvfCount * 6;            

            totalSvfCount = 1 * svfCount;
        }

        {
            spVtxInCount = sMesh.vertexCount;
            vfVtxCount = cMesh.vertexCount;

            spVtxOutCount = sMesh.vertexCount * spCount;
            pvfVtxOutCount = totalPvfCount * vfVtxCount;            
            svfVtxOutCount = totalSvfCount * sMesh.vertexCount;
        }

        {
            spTraa = new TransformAccessArray(spTrs);          
        }

        {
            spTrData = new NativeArray<float4x4>(spCount, Unity.Collections.Allocator.Persistent);

            spTrans = new SphereTransform();
            spTrans.spTr = spTrData;

            dpSpTrCount = (spCount % 64 == 0) ? (spCount / 64) : (spCount / 64 + 1);
        }      
    }
    ROBuffer<Info_VF> info_pvf_Buffer;    
    RWBuffer<float4> plane_pvf_Buffer;    

    ROBuffer<Vertex> vf_vertex_Buffer;
    RWBuffer<Vertex> pvf_vertex_Buffer;    
    RWBuffer<Vertex> svf_vertex_Buffer;

    ROBuffer<float4> sphere_Buffer;
    ROBuffer<Vertex> sphere_vertex_In_Buffer;
    RWBuffer<Vertex> sphere_vertex_Out_Buffer;

    ROBuffer<float4> sphere_In_Buffer;
    ROBuffer<float4x4> sphere_trM_Buffer;
    RWBuffer<float4> sphere_Out_Buffer;

    RWBuffer<float4x4> sphere_W_Buffer;
    RWBuffer<float4x4> sphere_Wn_Buffer;
    RWBuffer<float4> sphere_PosV_Buffer;

    RenderTexture cullResult_pvf_Texture;    

    RWBuffer<float> cullResult_svf_Buffer;

    float lightCamRdius = 10.0f;
    Vector4 lightCamSphere;
    float4 lightCamData;
    float4 lightCamDirW;


    //float4[] _sphere;

    public float4[] _sphereData;
    float4[] sphere;

    public static float[] cullResult_pvf
    {
        get; set;
    }

    public float[] _cullResult_pvf;

    public static float[] cullResult_svf
    {
        get; set;
    }

    public float[] _cullResult_svf;

    void InitResource()
    {
        {
            info_pvf_Buffer = new ROBuffer<Info_VF>(totalPvfCount);            
            plane_pvf_Buffer = new RWBuffer<float4>(planePvfCount);            

            vf_vertex_Buffer = new ROBuffer<Vertex>(vfVtxCount);
            pvf_vertex_Buffer = new RWBuffer<Vertex>(pvfVtxOutCount);            
            svf_vertex_Buffer = new RWBuffer<Vertex>(svfVtxOutCount);

            sphere_Buffer = new ROBuffer<float4>(spCount);
            sphere_vertex_In_Buffer = new ROBuffer<Vertex>(spVtxInCount);
            sphere_vertex_Out_Buffer = new RWBuffer<Vertex>(spVtxOutCount);

            sphere_In_Buffer = new ROBuffer<float4>(spCount);
            sphere_trM_Buffer = new ROBuffer<float4x4>(spCount);
            sphere_Out_Buffer = new RWBuffer<float4>(spCount);

            sphere_W_Buffer = new RWBuffer<float4x4>(spCount);
            sphere_Wn_Buffer = new RWBuffer<float4x4>(spCount);

            sphere_PosV_Buffer = new RWBuffer<float4>(spCount);
        }

        {
            cullResult_svf_Buffer = new RWBuffer<float>(spCount);
            _cullResult_svf = cullResult_svf = cullResult_svf_Buffer.data;        
        }

        {
            RenderTextureDescriptor rtd = new RenderTextureDescriptor();
            {
                rtd.msaaSamples = 1;
                rtd.depthBufferBits = 0;
                rtd.enableRandomWrite = true;

                rtd.colorFormat = RenderTextureFormat.RFloat;
                rtd.dimension = TextureDimension.Tex3D;
                rtd.width = spCount;
            }

            {
                rtd.height = pvfCount;
                rtd.volumeDepth = groupPvfCount;
                cullResult_pvf_Texture = new RenderTexture(rtd);
            }            
        }

        {
            cshader.SetInt("spCount", spCount);

            cshader.SetBuffer(ki_pvf, "info_pvf_Buffer", info_pvf_Buffer.value);
            cshader.SetBuffer(ki_pvf, "plane_pvf_Buffer", plane_pvf_Buffer.value);
            

            cshader.SetBuffer(ki_pvf_vertex, "info_pvf_Buffer", info_pvf_Buffer.value);
            cshader.SetBuffer(ki_pvf_vertex, "vf_vertex_Buffer", vf_vertex_Buffer.value);
            cshader.SetBuffer(ki_pvf_vertex, "pvf_vertex_Buffer", pvf_vertex_Buffer.value);
            

            cshader.SetBuffer(ki_sphere_vertex, "sphere_Buffer", sphere_Buffer.value);
            cshader.SetBuffer(ki_sphere_vertex, "sphere_Out_Buffer", sphere_Out_Buffer.value);
            cshader.SetBuffer(ki_sphere_vertex, "sphere_vertex_In_Buffer", sphere_vertex_In_Buffer.value);
            cshader.SetBuffer(ki_sphere_vertex, "sphere_vertex_Out_Buffer", sphere_vertex_Out_Buffer.value);

            cshader.SetBuffer(ki_svf_vertex, "sphere_vertex_In_Buffer", sphere_vertex_In_Buffer.value);
            cshader.SetBuffer(ki_svf_vertex, "svf_vertex_Buffer", svf_vertex_Buffer.value);

            cshader.SetBuffer(ki_pvf_cull_sphere, "sphere_Out_Buffer", sphere_Out_Buffer.value);
            cshader.SetBuffer(ki_pvf_cull_sphere, "sphere_Buffer", sphere_Buffer.value);
            cshader.SetBuffer(ki_pvf_cull_sphere, "plane_pvf_Buffer", plane_pvf_Buffer.value);
           

            cshader.SetTexture(ki_pvf_cull_sphere, "cullResult_pvf_Texture", cullResult_pvf_Texture);           

            cshader.SetBuffer(ki_sphere_center, "sphere_In_Buffer", sphere_In_Buffer.value);
            cshader.SetBuffer(ki_sphere_center, "sphere_trM_Buffer", sphere_trM_Buffer.value);
            cshader.SetBuffer(ki_sphere_center, "sphere_Out_Buffer", sphere_Out_Buffer.value);
            cshader.SetBuffer(ki_sphere_center, "sphere_W_Buffer", sphere_W_Buffer.value);
            cshader.SetBuffer(ki_sphere_center, "sphere_Wn_Buffer", sphere_Wn_Buffer.value);

            //cshader.SetBuffer(ki_sphere_center, "cullResult_svf_Buffer", cullResult_svf_Buffer.value);

            cshader.SetBuffer(ki_sphere_posV, "sphere_Out_Buffer", sphere_Out_Buffer.value);
            cshader.SetBuffer(ki_sphere_posV, "sphere_PosV_Buffer", sphere_PosV_Buffer.value);

           
            cshader.SetBuffer(ki_svf_cull_sphere, "sphere_Out_Buffer", sphere_Out_Buffer.value);            
            cshader.SetBuffer(ki_svf_cull_sphere, "cullResult_svf_Buffer", cullResult_svf_Buffer.value);
        }

        {
            _cullResult_pvf = cullResult_pvf = new float[spCount * totalPvfCount];            
        }

        {
            List<Vector3> pos = new List<Vector3>();
            List<Vector3> nom = new List<Vector3>();

            cMesh.GetVertices(pos);
            cMesh.GetNormals(nom);

            var data = vf_vertex_Buffer.data;
            for (int i = 0; i < vfVtxCount; i++)
            {
                Vertex vtx = new Vertex();
                vtx.position = pos[i];
                vtx.normal = nom[i];
                data[i] = vtx;
            }
            vf_vertex_Buffer.Write();
        }

        {
            List<Vector3> pos = new List<Vector3>();
            List<Vector3> nom = new List<Vector3>();

            sMesh.GetVertices(pos);
            sMesh.GetNormals(nom);

            var data = sphere_vertex_In_Buffer.data;
            for (int i = 0; i < spVtxInCount; i++)
            {
                Vertex vtx = new Vertex();
                vtx.position = pos[i];
                vtx.normal = nom[i];
                data[i] = vtx;
            }
            sphere_vertex_In_Buffer.Write();
        }
    }

    Material mte;
    MaterialPropertyBlock mpb;

    Mesh cMesh;
    Mesh sMesh;

    GraphicsBuffer idxVf_Buffer;
    GraphicsBuffer idxSp_Buffer;



    void InitDebugRender()
    {
        mte = new Material(gshader);
        mpb = new MaterialPropertyBlock();

        pass = mte.FindPass("Cull");
        pass_pvf = mte.FindPass("Cull_Pvf");        
        pass_svf = mte.FindPass("Cull_Svf");
        pass_sphere = mte.FindPass("Cull_Sphere");

        idxVf_Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, (int)cMesh.GetIndexCount(0), sizeof(int));
        idxSp_Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, (int)sMesh.GetIndexCount(0), sizeof(int));

        idxVf_Buffer.SetData(cMesh.GetIndices(0));
        idxSp_Buffer.SetData(sMesh.GetIndices(0));       

        mpb.SetBuffer("sphere_vertex_Out_Buffer", sphere_vertex_Out_Buffer.value);
        mpb.SetBuffer("pvf_vertex_Buffer", pvf_vertex_Buffer.value);        
        mpb.SetBuffer("svf_vertex_Buffer", svf_vertex_Buffer.value);

        mpb.SetInt("dvCount_sp", sMesh.vertexCount);
        mpb.SetInt("dvCount_vf", cMesh.vertexCount);

        mpb.SetTexture("cullResult_pvf_Texture", cullResult_pvf_Texture);
        mpb.SetBuffer("cullResult_svf_Buffer", cullResult_svf_Buffer.value);
    }


    public void Bind_GShader(MaterialPropertyBlock mpb)
    {       
        mpb.SetBuffer("sphere_PosW_Buffer", sphere_Out_Buffer.value);
        mpb.SetBuffer("sphere_PosV_Buffer", sphere_PosV_Buffer.value);

        mpb.SetBuffer("cullResult_svf_Buffer", cullResult_svf_Buffer.value);
        mpb.SetTexture("cullResult_pvf_Texture", cullResult_pvf_Texture);
    }

    public void Bind_CShader(CommandBuffer cmd, ComputeShader cshader, int ki)
    {
        cmd.SetComputeBufferParam(cshader, ki, "bone", sphere_W_Buffer.value);
        cmd.SetComputeBufferParam(cshader, ki, "boneIT", sphere_Wn_Buffer.value);
    }

    void RenderDebug(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        if(GameManager.unitCount < 1) { return; }


        if (GameManager.cullTestMode != 1)
        {
            return;
        }

        if (light.type == LightType.Directional)
        {
            return;
        }

        {
            mpb.SetInt("cullMode", debugCullMode);
            mpb.SetMatrix("CV", perCam.CV);
            mpb.SetVector("dirW_light", csmAction.dirW);            
            mpb.SetVector("posW_view", cam.transform.position);
        }

        {
            {
                //if (bDrawSp)
                {
                    cmd.DrawProcedural(idxSp_Buffer, Matrix4x4.identity, mte, pass_sphere, MeshTopology.Lines, idxSp_Buffer.count, spCount, mpb);
                }
            }

            {
                //if (bDrawPvf)
                {
                    cmd.DrawProcedural(idxVf_Buffer, Matrix4x4.identity, mte, pass_pvf, MeshTopology.Lines, idxVf_Buffer.count, pvfCount, mpb);
                }
                

                //if (bDrawSvf)
                {
                    cmd.DrawProcedural(idxSp_Buffer, Matrix4x4.identity, mte, pass_svf, MeshTopology.Lines, idxSp_Buffer.count, svfCount, mpb);
                }
            }
        }

    }

    void ReleaseDebugResource()
    {
        ReleaseGBuffer(idxVf_Buffer);
        ReleaseGBuffer(idxSp_Buffer);
    }

    void OnDestroy()
    {
        ReleaseResource();
        ReleaseDebugResource();
    }

    void ReleaseResource()
    {
        BufferBase<Info_VF>.Release(info_pvf_Buffer);        
        BufferBase<float4>.Release(plane_pvf_Buffer);        
        BufferBase<Vertex>.Release(vf_vertex_Buffer);
        BufferBase<Vertex>.Release(pvf_vertex_Buffer);        
        BufferBase<Vertex>.Release(svf_vertex_Buffer);
        BufferBase<float4>.Release(sphere_Buffer);
        BufferBase<Vertex>.Release(sphere_vertex_In_Buffer);
        BufferBase<Vertex>.Release(sphere_vertex_Out_Buffer);
        BufferBase<float4>.Release(sphere_In_Buffer);
        BufferBase<float4x4>.Release(sphere_trM_Buffer);
        BufferBase<float4>.Release(sphere_Out_Buffer);
        BufferBase<float4x4>.Release(sphere_W_Buffer);
        BufferBase<float4x4>.Release(sphere_Wn_Buffer);
        BufferBase<float4>.Release(sphere_PosV_Buffer);
        BufferBase<float>.Release(cullResult_svf_Buffer);

        ReleaseRenTexture(cullResult_pvf_Texture);        

        //DisposeNa<float3>(spOffset);
        DisposeTraa(spTraa);

        DisposeNa<float4x4>(spTrData);
    }

    public void Begin()
    {
        {
           
        }
    }


    void Start()
    {

    }


    void Update()
    {
        //if (!GameManager.bUpdate)
        //{
        //    return;
        //}

        //if (GameManager.isPause)
        //{
        //    return;
        //}

        if (!GameManager.bUpdate)
        {
            return;
        }

        if (GameManager.unitCount > 0)
        {
            if(light.type == LightType.Directional)
            {
                return;
            }

            {
                Compute();
            }

            if (Input.GetKeyDown(KeyCode.PageUp))
            {
                debugCullMode++;
               
                if (debugCullMode > 1)
                {
                    debugCullMode = 0;
                }             
            }

            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                debugCullMode--;
                if (debugCullMode < 0)
                {                  
                    debugCullMode = 1;
                }
            }
        }
    }

    void Compute()
    {
        {
            WriteToResource();
            DispatchCompute();
            ReadFromResource();
        }
    }

    void WriteToResource()
    {
        {
            var data = info_pvf_Buffer.data;
            for (int i = 0; i < pvfCount; i++)
            {
                Info_VF ivf = new Info_VF();
                Transform camTr = mainCam.transform;

                ivf.fi = new float4(mainCam.fieldOfView, mainCam.aspect, mainCam.nearClipPlane, mainCam.farClipPlane);
                ivf.pos = camTr.position;
                ivf.rot = ((quaternion)(camTr.rotation)).value;

                data[i] = ivf;
            }
            info_pvf_Buffer.Write();
        }


        {
            int start = 0;
            int i = 0;
            for (i = 0; i < unitMans.Length; i++)
            {
                for (int j = 0; j < unitMans[i].count; j++)
                {
                    spTrs[start + j] = unitMans[i].trs[j];                    

                    {
                        sphere[start + j] = _sphereData[i];
                    }
                }
                start += unitMans[i].count;
            }
        }

        {
            spTrans.Schedule<SphereTransform>(spTraa).Complete();

            {
                var data = sphere_In_Buffer.data;
                for (int i = 0; i < spCount; i++)
                {
                    data[i] = sphere[i];
                }
                sphere_In_Buffer.Write();
            }

            {
                var data = sphere_trM_Buffer.data;
                for (int i = 0; i < spCount; i++)
                {
                    data[i] = spTrData[i];
                }
                sphere_trM_Buffer.Write();
            }
        }

        {
            //lightCamSphere = new float4((float3)(mainCam.transform.position), lightCamRdius);
            
            Vector3 c = trLight.position;
            lightCamRdius = light.range;
            lightCamSphere = new Vector4(c.x, c.y, c.z, lightCamRdius);
            lightCamData = new float4((float)(LightManager.type), math.radians(light.spotAngle * 0.5f), 0.0f, 0.0f);
            lightCamDirW = new float4(LightManager.dirW, 0.0f);

            cshader.SetVector("lightCamSphere", lightCamSphere);
            cshader.SetVector("lightCamRot", ((quaternion)trLight.rotation).value);            
            cshader.SetVector("lightCamData", lightCamData);           
            cshader.SetVector("lightCamDirW", lightCamDirW);
        }
    }

    void WriteToResource1()
    {
        {
            var data = info_pvf_Buffer.data;
            for (int i = 0; i < pvfCount; i++)
            {
                Info_VF ivf = new Info_VF();
                Transform camTr = mainCam.transform;

                ivf.fi = new float4(mainCam.fieldOfView, mainCam.aspect, mainCam.nearClipPlane, mainCam.farClipPlane);
                ivf.pos = camTr.position;
                ivf.rot = ((quaternion)(camTr.rotation)).value;

                data[i] = ivf;
            }
            info_pvf_Buffer.Write();
        }


        {
            int start = 0;
            int i = 0;
            for (i = 0; i < unitMans.Length; i++)
            {
                for (int j = 0; j < unitMans[i].count; j++)
                {
                    spTrs[start + j] = unitMans[i].trs[j];

                    {
                        sphere[start + j] = _sphereData[i];
                    }
                }
                start += unitMans[i].count;
            }
        }

        {
            //spTrans.Schedule<SphereTransform>(spTraa).Complete();

            {
                var data = sphere_In_Buffer.data;
                for (int i = 0; i < spCount; i++)
                {
                    data[i] = sphere[i];
                }
                sphere_In_Buffer.Write();
            }

            {
                var data = sphere_trM_Buffer.data;
                for (int i = 0; i < spCount; i++)
                {
                    var nt_Tr = nt_spTrs[i];
                    var tr = spTrs[i];
                    data[i].c0 = new float4(nt_Tr.ReadPosition(), 0.0f);
                    data[i].c1 = ((quaternion)(nt_Tr.ReadRotation())).value;
                    data[i].c2 = new float4(tr.localScale, 0.0f);
                }
                sphere_trM_Buffer.Write();
            }
        }

        {
            //lightCamSphere = new float4((float3)(mainCam.transform.position), lightCamRdius);

            Vector3 c = trLight.position;
            lightCamRdius = light.range;
            lightCamSphere = new Vector4(c.x, c.y, c.z, lightCamRdius);
            lightCamData = new float4((float)(LightManager.type), math.radians(light.spotAngle * 0.5f), 0.0f, 0.0f);
            lightCamDirW = new float4(LightManager.dirW, 0.0f);

            cshader.SetVector("lightCamSphere", lightCamSphere);
            cshader.SetVector("lightCamRot", ((quaternion)trLight.rotation).value);
            cshader.SetVector("lightCamData", lightCamData);
            cshader.SetVector("lightCamDirW", lightCamDirW);
        }
    }

    void DispatchCompute()
    {
        CommandBuffer cmd = CommandBufferPool.Get();

        cmd.DispatchCompute(cshader, ki_pvf, totalPvfCount, 1, 1);        

        cmd.DispatchCompute(cshader, ki_sphere_center, dpSpTrCount, 1, 1);

        if (bCullDebug)
        {
            cmd.DispatchCompute(cshader, ki_pvf_vertex, totalPvfCount, 1, 1);            
            cmd.DispatchCompute(cshader, ki_svf_vertex, totalSvfCount, 1, 1);
            cmd.DispatchCompute(cshader, ki_sphere_vertex, spCount, 1, 1);
        }

        cmd.DispatchCompute(cshader, ki_pvf_cull_sphere, dpSpCount, pvfCount, 1);
        cmd.DispatchCompute(cshader, ki_svf_cull_sphere, dpSpTrCount, 1, 1);        
       

        Graphics.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    void ReadFromResource()
    {
        {
            cullResult_svf_Buffer.Read();
        }

        {
            var read = AsyncGPUReadback.Request(cullResult_pvf_Texture);
            read.WaitForCompletion();

            for (int i = 0; i < groupPvfCount; i++)
            {
                var na = read.GetData<float>(i);
                for (int j = 0; j < pvfCount; j++)
                {
                    for (int k = 0; k < spCount; k++)
                    {
                        cullResult_pvf[i * pvfCount * spCount + j * spCount + k] = na[j * spCount + k];
                    }
                }
            }
        }

        bool bReadDebug = false;

        if (bReadDebug)
        {
            {
                CommandBuffer cmd = CommandBufferPool.Get();

                {
                    plane_pvf_Buffer.Read();
                }
              
                {
                    pvf_vertex_Buffer.Read();
                }
                
                {
                    svf_vertex_Buffer.Read();
                }

                {
                    sphere_vertex_Out_Buffer.Read();
                }           

                {
                    sphere_Out_Buffer.Read();
                }

                {
                    sphere_W_Buffer.Read();
                }

                {
                    sphere_Wn_Buffer.Read();
                }


                Graphics.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }                     
        }
    }

    public void Read_PvfCullData()
    {
        {
            var read = AsyncGPUReadback.Request(cullResult_pvf_Texture);
            read.WaitForCompletion();

            int size0 = pvfCount * spCount;
            for (int i = 0; i < groupPvfCount; i++)
            {
                var na = read.GetData<float>(i);
                for (int j = 0; j < pvfCount; j++)
                {
                    for (int k = 0; k < spCount; k++)
                    {
                        cullResult_pvf[i * size0 + j * spCount + k] = na[j * spCount + k];
                    }
                }
            }
        }
    }




    public void Compute_PosV(float4x4 V)
    {
        {
            WriteToResource_PosV(V);
            DispatchCompute_PosV();
            ReadFromResource_PosV();
        }
    }

    void WriteToResource_PosV(float4x4 V)
    {
        cshader.SetMatrix("camV", V);
    }

    void DispatchCompute_PosV()
    {
        CommandBuffer cmd = CommandBufferPool.Get();
       
        cmd.DispatchCompute(cshader, ki_sphere_posV, dpSpTrCount, 1, 1);        

        Graphics.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    void ReadFromResource_PosV()
    {
        bool bReadDebug = false;

        if (bReadDebug)
        {
            {
                CommandBuffer cmd = CommandBufferPool.Get();

                sphere_PosV_Buffer.Read();

                Graphics.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }            
        }
    }

    void ReleaseGBuffer(GraphicsBuffer gbuffer)
    {
        if (gbuffer != null) gbuffer.Release();
    }

    void ReleaseRenTexture(RenderTexture tex)
    {
        if (tex != null) tex.Release();
    }

    void DisposeNa<T>(NativeArray<T> na) where T : struct
    {
        if (na.IsCreated) na.Dispose();
    }

    void DisposeTraa(TransformAccessArray traa)
    {
        if (traa.isCreated) traa.Dispose();
    }

    [System.Serializable]
    public struct Info_VF
    {
        public float4 fi;
        public float3 pos;
        public float4 rot;
    }

    [System.Serializable]
    public struct Vertex
    {
        public float3 position;
        public float3 normal;
    }

    [BurstCompile]
    struct SphereTransform : IJobParallelForTransform
    {
        public NativeArray<float4x4> spTr;

        public void Execute(int i, TransformAccess traa)
        {
            float4x4 tr = float4x4.zero;

            tr.c0.xyz = traa.localPosition;
            tr.c1 = ((quaternion)traa.localRotation).value;
            tr.c2.xyz = traa.localScale;

            spTr[i] = tr;
        }
    }

}
