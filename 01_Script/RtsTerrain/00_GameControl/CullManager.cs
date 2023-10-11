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

public class CullManager : MonoBehaviour
{
    public ComputeShader cshader;
    public Shader gshader;

    int ki_pvf;
    int ki_ovf;

    int ki_pvf_vertex;
    int ki_ovf_vertex;
    int ki_sphere_vertex;
    int ki_svf_vertex;

    int ki_pvf_cull_sphere;
    int ki_ovf_cull_sphere;
    int ki_svf_cull_sphere;

    int ki_sphere_center;

    int ki_unit_tessinfo;

    int pass;
    int pass_pvf;
    int pass_ovf;
    int pass_svf;
    int pass_sphere;

    int debugCullMode = 0;
    bool bDrawPvf = true;
    bool bDrawOvf = true;
    bool bDrawSvf = true;
    bool bDrawSp = true;

    Camera mainCam;

    public void Init()
    {
        if(GameManager.unitCount < 1)
        {
            return;
        }

        {
            ki_pvf = cshader.FindKernel("CS_PVF");
            ki_ovf = cshader.FindKernel("CS_OVF");

            ki_pvf_vertex = cshader.FindKernel("CS_PVF_Vertex");
            ki_ovf_vertex = cshader.FindKernel("CS_OVF_Vertex");
            ki_sphere_vertex = cshader.FindKernel("CS_Sphere_Vertex");
            ki_svf_vertex = cshader.FindKernel("CS_SVF_Vertex");

            ki_pvf_cull_sphere = cshader.FindKernel("CS_PVF_Cull_Sphere");
            ki_ovf_cull_sphere = cshader.FindKernel("CS_OVF_Cull_Sphere");
            ki_svf_cull_sphere = cshader.FindKernel("CS_SVF_Cull_Sphere");

            ki_sphere_center = cshader.FindKernel("CS_Sphere_Center");

            ki_unit_tessinfo = cshader.FindKernel("CS_Unit_TessInfo");
        }

        {
            mainCam = Camera.main;
        }

        {

#if UNITY_EDITOR
            bCullDebug = true;
            bTessDebug = true;
#else
            bCullDebug = false;
            bTessDebug = false;
#endif
        }

        {
            if (bTessDebug)
            {
                cshader.EnableKeyword("DEBUG_TESS");
            }
            else
            {
                cshader.DisableKeyword("DEBUG_TESS");
            }
        }

        InitData();
        InitResource();
        InitDebugRender();
    }

    public void Enable()
    {
        if (GameManager.unitCount < 1)
        {
            return;
        }

        //RenderGOM_DF.Cull += Compute;
        RenderGOM_DF.OnRenderCamDebug += Render_debug;
    }

    public void Disable()
    {
        if (GameManager.unitCount < 1)
        {
            return;
        }

        //RenderGOM_DF.Cull -= Compute;
        RenderGOM_DF.OnRenderCamDebug -= Render_debug;
    }

    UnitManager[] unitMans;
    ArrowManager arrowMans;
    TorusManager torusMan;
    HpbarManager hpbarMan;
    DeferredRenderManager dfMan;
   
    public static int[] spCounts;
    public static int spCount = 0;
    public static int spCount_unit = 0;
    public static int[] cullOffsets;
   
    int dpSpCount;
    int dpSpCount_unit;
    int tessInfoCount;

    int spVtxInCount;
    int vfVtxCount;

    int spVtxOutCount;
    int pvfVtxOutCount;
    int ovfVtxOutCount;
    int svfVtxOutCount;

    Transform[] spTrs;

    int pvfCount = 1;  //1
    int planePvfCount;
    int groupPvfCount;
    int totalPvfCount;

    int ovfCount;
    int planeOvfCount;
    int groupOvfCount;
    int totalOvfCount;

    int svfCount = 1;
    int totalSvfCount;
    

    public CSM_Action csmAction;
    float4[] csmPos;
    quaternion[] csmRot;
    float4[] csmfi;

    public CBM_Action cbmAction;
    float3[] cbmPos;
    quaternion[] cbmRot;
    float4[] cbmfi;

    public static bool bCull = true;
    bool bCullDebug = true;
    bool bTessDebug = false;
    
    NativeArray<float3> spOffset;    
    TransformAccessArray spTraa;
   

    NativeArray<float4x4> spTrData;
    SphereTransform spTrans;
    int dpSpTrCount;
    int dpSpTrCount_unit;

    void InitData()
    {
        unitMans = GameManager.unitMan;
        arrowMans = GameManager.arrowMan;
        torusMan = GameManager.torusMan;
        hpbarMan = GameManager.hpbarMan;
        dfMan = GameManager.dfMan;

        {
            int[] baseVtx;
            sMesh = RenderUtil.CreateSphereMeshWirePartsDetail_Normal(1.0f, 12, 6, out baseVtx);
            cMesh = RenderUtil.CreateBoxMeshWirePartsDetail_Normal(0.5f, out baseVtx);
        }

        spCount = 0;
        spCount_unit = 0;
        {
            spCounts = new int[unitMans.Length + 1];
            cullOffsets = new int[unitMans.Length + 1];
            //_cullOffsets = new int[unitMans.Length + 1];

            for (int i = 0; i < unitMans.Length; i++)
            {
                //_cullOffsets[i] = spCount;
                cullOffsets[i] = spCount;

                spCounts[i] = unitMans[i].count;
                spCount += spCounts[i];

                //unitMans[i].SetCullData(i);
            }

            {
                spCount_unit = spCount;
            }

            {
                int i = unitMans.Length;

                //_cullOffsets[i] = spCount;
                cullOffsets[i] = spCount;

                spCounts[i] = ArrowManager.cCount;
                spCount += ArrowManager.cCount;

                //arrowMans.SetCullData(i);
            }

            bCull = true;
            if (spCount == 0) bCull = false;

            dpSpCount = (spCount % 8 == 0) ? (spCount / 8) : (spCount / 8 + 1);           
        }

        {
            tessInfoCount = GameManager.unitCounts.Length;
        }

        {
            spTrs = new Transform[spCount];
            _sphere = new float4[spCount];
            spOffset = new NativeArray<float3>(spCount, Allocator.Persistent);
            //spCenter = new NativeArray<float3>(spCount, Allocator.Persistent);

            int start = 0;
            int i = 0;
            for (i = 0; i < unitMans.Length; i++)
            {
                for (int j = 0; j < unitMans[i].count; j++)
                {
                    spTrs[start + j] = unitMans[i].trs[j];
                    SphereCollider col = unitMans[i].unitActors[j].GetComponent<SphereCollider>();
                    _sphere[start + j] = new float4(0.0f, 0.0f, 0.0f, col.radius);
                    spOffset[start + j] = col.center;
                }
                start += unitMans[i].count;
            }

            {
                for (int j = 0; j < ArrowManager.cCount; j++)
                {
                    spTrs[start + j] = arrowMans.arrow[j].transform;
                    SphereCollider col = arrowMans.arrow[j].GetComponent<SphereCollider>();
                    _sphere[start + j] = new float4(0.0f, 0.0f, 0.0f, col.radius);
                    spOffset[start + j] = col.center;
                }
            }
        }

        {
            csmPos = csmAction.pos;
            csmRot = csmAction.rot;
            csmfi = csmAction.fi;
        }

        {
            cbmPos = cbmAction.pos;
            cbmRot = cbmAction.rot;
            cbmfi = cbmAction.fi;
        }


        {
            //pvfCount = 1;
            //ovfCount = csmAction.csmCount;

            pvfCount = 7;
            ovfCount = csmAction.csmCount + 1;  //apply CBM_box
        }

        {
            groupPvfCount = 1;
            groupOvfCount = 1;

            totalPvfCount = groupPvfCount * pvfCount;
            totalOvfCount = groupOvfCount * ovfCount;

            planePvfCount = totalPvfCount * 6;
            planeOvfCount = totalOvfCount * 6;

            totalSvfCount = 1 * svfCount;
        }

        {
            spVtxInCount = sMesh.vertexCount;
            vfVtxCount = cMesh.vertexCount;

            spVtxOutCount = sMesh.vertexCount * spCount;
            pvfVtxOutCount = totalPvfCount * vfVtxCount;
            ovfVtxOutCount = totalOvfCount * vfVtxCount;
            svfVtxOutCount = totalSvfCount * sMesh.vertexCount;
        }

        {
            spTraa = new TransformAccessArray(spTrs);

            //spAction = new SphereAction();

            //spAction.spOffset = spOffset;
            //spAction.spCenter = spCenter;
        }

        {
            spTrData = new NativeArray<float4x4>(spCount, Allocator.Persistent);

            spTrans = new SphereTransform();
            spTrans.spTr = spTrData;

            dpSpTrCount = (spCount % 64 == 0) ? (spCount / 64) : (spCount / 64 + 1);
            dpSpTrCount_unit = (spCount_unit % 64 == 0) ? (spCount_unit / 64) : (spCount_unit / 64 + 1);
        }
    }    
    ROBuffer<Info_VF> info_pvf_Buffer;
    ROBuffer<Info_VF> info_ovf_Buffer;
    RWBuffer<float4> plane_pvf_Buffer;
    RWBuffer<float4> plane_ovf_Buffer;

    ROBuffer<Vertex> vf_vertex_Buffer;
    RWBuffer<Vertex> pvf_vertex_Buffer;
    RWBuffer<Vertex> ovf_vertex_Buffer;
    RWBuffer<Vertex> svf_vertex_Buffer;

    ROBuffer<float4> sphere_Buffer;
    ROBuffer<Vertex> sphere_vertex_In_Buffer;
    RWBuffer<Vertex> sphere_vertex_Out_Buffer;  
    
    ROBuffer<float4> sphere_In_Buffer;
    ROBuffer<float4x4> sphere_trM_Buffer;
    RWBuffer<float4> sphere_Out_Buffer;

    RenderTexture cullResult_pvf_Texture;
    RenderTexture cullResult_ovf_Texture;

    RWBuffer<float> cullResult_svf_Buffer;

    public float audioCamRdius = 10.0f;
    Vector4 audioCamSphere;

    ROBuffer<float> cullOffset_Buffer;
    RWBuffer<float4> unit_tess_Buffer; //float4(isIn, idx, dist, count)
    RWBuffer<float4> unit_tess_debug_Buffer;

    float4[] _sphere;   

    public static float[] cullResult_pvf
    {
        get; set;
    }

    public static float[] cullResult_ovf
    {
        get; set;
    }

    public static float[] cullResult_svf
    {
        get; set;
    }

    public float[] _cullResult_svf;

    void InitResource()
    {        
        {
            info_pvf_Buffer =   new ROBuffer<Info_VF>(totalPvfCount);
            info_ovf_Buffer =   new ROBuffer<Info_VF>(totalOvfCount);
            plane_pvf_Buffer =  new RWBuffer<float4>(planePvfCount);
            plane_ovf_Buffer =  new RWBuffer<float4>(planeOvfCount);

            vf_vertex_Buffer =  new ROBuffer<Vertex>(vfVtxCount);
            pvf_vertex_Buffer = new RWBuffer<Vertex>(pvfVtxOutCount );
            ovf_vertex_Buffer = new RWBuffer<Vertex>(ovfVtxOutCount );
            svf_vertex_Buffer = new RWBuffer<Vertex>(svfVtxOutCount);

            sphere_Buffer =             new ROBuffer<float4>(spCount);
            sphere_vertex_In_Buffer =   new ROBuffer<Vertex>(spVtxInCount   );
            sphere_vertex_Out_Buffer =  new RWBuffer<Vertex>(spVtxOutCount  );

            sphere_In_Buffer =  new ROBuffer<float4>(spCount);
            sphere_trM_Buffer = new ROBuffer<float4x4>(spCount);
            sphere_Out_Buffer = new RWBuffer<float4>(spCount);
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

            {
                rtd.height = ovfCount;
                rtd.volumeDepth = groupOvfCount;
                cullResult_ovf_Texture = new RenderTexture(rtd);
            }
        }

        {
            cullResult_svf_Buffer = new RWBuffer<float>(spCount_unit);
            _cullResult_svf = cullResult_svf = cullResult_svf_Buffer.data;

            GameManager.audioCull_Buffer = cullResult_svf_Buffer;
            GameManager.audioCull = cullResult_svf_Buffer.data;
            //GameManager.instance._audioCull = GameManager.audioCull;
        }

        {
            cullOffset_Buffer = new ROBuffer<float>(cullOffsets.Length);
            unit_tess_Buffer = new RWBuffer<float4>(unitMans.Length);
            unit_tess_debug_Buffer = new RWBuffer<float4>(64 * unitMans.Length);
        }

        {
            cshader.SetInt("spCount", spCount);
            cshader.SetInt("pvfCount", pvfCount);
            cshader.SetInt("ovfCount", ovfCount);

            cshader.SetBuffer(ki_pvf, "info_pvf_Buffer",    info_pvf_Buffer.value);
            cshader.SetBuffer(ki_pvf, "plane_pvf_Buffer",   plane_pvf_Buffer.value);
            cshader.SetBuffer(ki_ovf, "info_ovf_Buffer",    info_ovf_Buffer.value);
            cshader.SetBuffer(ki_ovf, "plane_ovf_Buffer",   plane_ovf_Buffer.value);

            cshader.SetBuffer(ki_pvf_vertex, "info_pvf_Buffer", info_pvf_Buffer.value );
            cshader.SetBuffer(ki_pvf_vertex, "vf_vertex_Buffer", vf_vertex_Buffer.value );
            cshader.SetBuffer(ki_pvf_vertex, "pvf_vertex_Buffer", pvf_vertex_Buffer.value );
            cshader.SetBuffer(ki_ovf_vertex, "info_ovf_Buffer", info_ovf_Buffer.value );
            cshader.SetBuffer(ki_ovf_vertex, "vf_vertex_Buffer", vf_vertex_Buffer.value );
            cshader.SetBuffer(ki_ovf_vertex, "ovf_vertex_Buffer", ovf_vertex_Buffer.value );

            cshader.SetBuffer(ki_sphere_vertex, "sphere_Buffer", sphere_Buffer.value);
            cshader.SetBuffer(ki_sphere_vertex, "sphere_Out_Buffer", sphere_Out_Buffer.value);
            cshader.SetBuffer(ki_sphere_vertex, "sphere_vertex_In_Buffer", sphere_vertex_In_Buffer.value);
            cshader.SetBuffer(ki_sphere_vertex, "sphere_vertex_Out_Buffer", sphere_vertex_Out_Buffer.value);

            cshader.SetBuffer(ki_svf_vertex, "sphere_vertex_In_Buffer", sphere_vertex_In_Buffer.value);
            cshader.SetBuffer(ki_svf_vertex, "svf_vertex_Buffer", svf_vertex_Buffer.value);

            cshader.SetBuffer(ki_pvf_cull_sphere, "sphere_Out_Buffer", sphere_Out_Buffer.value);
            cshader.SetBuffer(ki_pvf_cull_sphere, "sphere_Buffer", sphere_Buffer.value);
            cshader.SetBuffer(ki_pvf_cull_sphere, "plane_pvf_Buffer", plane_pvf_Buffer.value);
            cshader.SetBuffer(ki_ovf_cull_sphere, "sphere_Out_Buffer", sphere_Out_Buffer.value);
            cshader.SetBuffer(ki_ovf_cull_sphere, "sphere_Buffer", sphere_Buffer.value);
            cshader.SetBuffer(ki_ovf_cull_sphere, "plane_ovf_Buffer", plane_ovf_Buffer.value);

            cshader.SetTexture(ki_pvf_cull_sphere, "cullResult_pvf_Texture", cullResult_pvf_Texture);
            cshader.SetTexture(ki_ovf_cull_sphere, "cullResult_ovf_Texture", cullResult_ovf_Texture);

            cshader.SetBuffer(ki_sphere_center, "sphere_In_Buffer", sphere_In_Buffer.value);
            cshader.SetBuffer(ki_sphere_center, "sphere_trM_Buffer", sphere_trM_Buffer.value);
            cshader.SetBuffer(ki_sphere_center, "sphere_Out_Buffer", sphere_Out_Buffer.value);

            cshader.SetBuffer(ki_svf_cull_sphere, "sphere_Out_Buffer", sphere_Out_Buffer.value);
            cshader.SetBuffer(ki_svf_cull_sphere, "cullResult_svf_Buffer", cullResult_svf_Buffer.value);
        }

        {            
            cshader.SetBuffer(ki_unit_tessinfo, "sphere_Out_Buffer", sphere_Out_Buffer.value);
            cshader.SetBuffer(ki_unit_tessinfo, "cullOffset_Buffer",  cullOffset_Buffer.value);
            cshader.SetBuffer(ki_unit_tessinfo, "unit_tess_Buffer", unit_tess_Buffer.value);
            cshader.SetBuffer(ki_unit_tessinfo, "unit_tess_debug_Buffer", unit_tess_debug_Buffer.value);
        }

        {           
            cullResult_pvf = new float[spCount * totalPvfCount];
            cullResult_ovf = new float[spCount * totalOvfCount];            
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

        {
            var data = cullOffset_Buffer.data;
            for(int i = 0; i < cullOffsets.Length; i++)
            {
                data[i] = (float)cullOffsets[i];
            }
            cullOffset_Buffer.Write();
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
        pass_ovf = mte.FindPass("Cull_Ovf");
        pass_svf = mte.FindPass("Cull_Svf");
        pass_sphere = mte.FindPass("Cull_Sphere");

        idxVf_Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, (int)cMesh.GetIndexCount(0), sizeof(int));
        idxSp_Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, (int)sMesh.GetIndexCount(0), sizeof(int));

        idxVf_Buffer.SetData(cMesh.GetIndices(0));
        idxSp_Buffer.SetData(sMesh.GetIndices(0));

        mpb.SetTexture("cullResult_pvf_Texture", cullResult_pvf_Texture);
        mpb.SetTexture("cullResult_ovf_Texture", cullResult_ovf_Texture);
        mpb.SetBuffer("cullResult_svf_Buffer", cullResult_svf_Buffer.value);

        mpb.SetBuffer("sphere_vertex_Out_Buffer", sphere_vertex_Out_Buffer.value);
        mpb.SetBuffer("pvf_vertex_Buffer", pvf_vertex_Buffer.value);
        mpb.SetBuffer("ovf_vertex_Buffer", ovf_vertex_Buffer.value);
        mpb.SetBuffer("svf_vertex_Buffer", svf_vertex_Buffer.value);

        mpb.SetInt("dvCount_sp", sMesh.vertexCount);
        mpb.SetInt("dvCount_vf", cMesh.vertexCount);

        mpb.SetInt("spCount_unit", spCount_unit);
    }   

    void Render_debug(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        if(GameManager.cullTestMode != 0)
        {
            return;
        }

        {
            mpb.SetInt("cullMode", debugCullMode);
            mpb.SetMatrix("CV", perCam.CV);
            mpb.SetVector("dirW_light", csmAction.dirW);
            mpb.SetVector("posW_view", cam.transform.position);
        }

        int _pvfOffset = 0;
        int _ovfOffset = 0;
        int _svfOffset = 0;
        int _pvfCount = 1;
        int _ovfCount = 1;
        int _svfCount = 1;

        int _mode = debugCullMode;
        int lmode = LightManager.type == LightType.Directional ? 1 : 0;
        
        if(_mode == 0)
        {
            _pvfOffset = 0;
            _ovfOffset = 0;
            _pvfCount = 1;
            _ovfCount = 0;
        }
        else if(0 < _mode && _mode < 5)
        {
            _pvfOffset = 0;
            _ovfOffset = _mode - 1;
            _pvfCount = 0;
            _ovfCount = lmode == 0 ? 0 : 1;            
        }
        else if (_mode == 5)
        {
            _pvfOffset = 0;
            _ovfOffset = 0;
            _pvfCount = lmode == 0 ? 0 : 1;
            _ovfCount = lmode == 0 ? 0 : 4;
        }
        else if (5 < _mode && _mode < 12)
        {
            _pvfOffset = 1;
            _ovfOffset = 0;
            _pvfCount = lmode == 1 ? 0 : 6;
            _ovfCount = 0;
        }
        else if (_mode == 12)
        {
            _pvfOffset = 0;
            _ovfOffset = 4;
            _pvfCount = 0;
            _ovfCount = lmode == 1 ? 0:  1;
        }
        else if (_mode == 13)
        {
            _svfOffset = 0;                        
            _svfCount = 1;
        }

        {
            mpb.SetInt("pvfOffset", _pvfOffset);
            mpb.SetInt("ovfOffset", _ovfOffset);
            mpb.SetInt("svfOffset", _svfOffset);
            mpb.SetInt("lmode", lmode);
        }

        {
            {
                if (bDrawSp)
                {
                    cmd.DrawProcedural(idxSp_Buffer, Matrix4x4.identity, mte, pass_sphere, MeshTopology.Lines, idxSp_Buffer.count, spCount, mpb);
                }
            }

            {
                if (bDrawPvf && _pvfCount > 0)
                {
                    cmd.DrawProcedural(idxVf_Buffer, Matrix4x4.identity, mte, pass_pvf, MeshTopology.Lines, idxVf_Buffer.count, _pvfCount, mpb);
                }

                if (bDrawOvf && _ovfCount > 0)
                {
                    cmd.DrawProcedural(idxVf_Buffer, Matrix4x4.identity, mte, pass_ovf, MeshTopology.Lines, idxVf_Buffer.count, _ovfCount, mpb);
                }

                if (bDrawSvf && _svfCount > 0)
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
        if (GameManager.unitCount < 1)
        {
            return;
        }

        ReleaseResource();
        ReleaseDebugResource();
    }

    void ReleaseResource()
    {
        BufferBase<Info_VF>.Release(info_pvf_Buffer);
        BufferBase<Info_VF>.Release(info_ovf_Buffer);
        BufferBase<float4>.Release(plane_pvf_Buffer);
        BufferBase<float4>.Release(plane_ovf_Buffer);
        BufferBase<Vertex>.Release(vf_vertex_Buffer);
        BufferBase<Vertex>.Release(pvf_vertex_Buffer);
        BufferBase<Vertex>.Release(ovf_vertex_Buffer);
        BufferBase<Vertex>.Release(svf_vertex_Buffer);
        BufferBase<float4>.Release(sphere_Buffer);
        BufferBase<Vertex>.Release(sphere_vertex_In_Buffer);
        BufferBase<Vertex>.Release(sphere_vertex_Out_Buffer);
        BufferBase<float4>.Release(sphere_In_Buffer);
        BufferBase<float4x4>.Release(sphere_trM_Buffer);
        BufferBase<float4>.Release(sphere_Out_Buffer);
        BufferBase<float>.Release(cullResult_svf_Buffer);

        BufferBase<float>.Release(cullOffset_Buffer);
        BufferBase<float4>.Release(unit_tess_Buffer);
        BufferBase<float4>.Release(unit_tess_debug_Buffer);

        ReleaseRenTexture(cullResult_pvf_Texture);
        ReleaseRenTexture(cullResult_ovf_Texture);

        DisposeNa<float3>(spOffset);        
        DisposeTraa(spTraa);

        DisposeNa<float4x4>(spTrData);
    }

    public void Begin()
    {
        {
            for (int i = 0; i < unitMans.Length; i++)
            {
                unitMans[i].SetCullData(i, cullResult_pvf_Texture, cullResult_ovf_Texture, unit_tess_Buffer);
            }
        
            {
                int i = unitMans.Length;
                arrowMans.SetCullData(i, cullResult_pvf_Texture, cullResult_ovf_Texture);
            }
        
            {
                torusMan.SetCullData(cullResult_pvf_Texture);
                hpbarMan.SetCullData(cullResult_pvf_Texture);
            }

            //{
            //    dfMan.SetCullData(cullResult_pvf_Texture);
            //}
        }
    }


    void Start()
    {

    }
    

    void Update()
    {
        if(!GameManager.bUpdate)
        {
            return;
        }

        if (GameManager.unitCount < 1)
        {
            return;
        }

        {
            Compute();
        }

        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            debugCullMode++;
            if (debugCullMode > 13)
            {
                debugCullMode = 0;
            }

            //debugCullMode = (++debugCullMode) % 6;
        }

        if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            debugCullMode--;
            if (debugCullMode < 0)
            {
                debugCullMode = 13;
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
                
                if(i == 0)
                {
                    Transform camTr = mainCam.transform;

                    ivf.fi = new float4(mainCam.fieldOfView, mainCam.aspect, mainCam.nearClipPlane, mainCam.farClipPlane);
                    ivf.pos = camTr.position;
                    ivf.rot = ((quaternion)(camTr.rotation)).value;
                }
                else //(i > 0)
                {
                    ivf.fi = cbmfi[0];
                    ivf.pos = cbmPos[0].xyz;
                    ivf.rot = cbmRot[i - 1].value;
                }

                data[i] = ivf;
            }
            info_pvf_Buffer.Write();
        }

        {
            var data = info_ovf_Buffer.data;
            for (int i = 0; i < ovfCount; i++)
            {
                Info_VF ivf = new Info_VF();

                if (i < 4)
                {                    
                    ivf.fi = csmfi[i];
                    ivf.pos = csmPos[i].xyz;
                    ivf.rot = csmRot[i].value;                    
                }
                else if(i == 4)
                {
                    ivf.fi = cbmAction.fiBox;
                    ivf.pos = cbmAction.posBox;
                    ivf.rot = cbmAction.rotBox.value;
                }

                data[i] = ivf;
            }
            info_ovf_Buffer.Write();
        }

        {
            spTrans.Schedule<SphereTransform>(spTraa).Complete();

            {
                var data = sphere_In_Buffer.data;
                for (int i = 0; i < spCount; i++)
                {
                    _sphere[i].xyz = spOffset[i];
                    data[i] = _sphere[i];
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
            //audioCamSphere = new float4((float3)(mainCam.transform.position), audioCamRdius);
            Vector3 c = mainCam.transform.position;
            audioCamSphere = new Vector4(c.x, c.y, c.z, audioCamRdius);

            cshader.SetVector("audioCamSphere", audioCamSphere);
            cshader.SetVector("audioCamRot", ((quaternion)mainCam.transform.rotation).value);
        }


        {
            float maxTessDist = GameManager.unitTessInfo.w;
            float3 pos;

            //{
            //    float2 pc = mainCam.pixelRect.size * 0.5f;
            //    Ray ray = mainCam.ScreenPointToRay(new float3(pc, 0.0f));
            //    pos = ray.origin + 1.0f * ray.direction;
            //}

            {
                pos = mainCam.transform.position;
            }           
            
            cshader.SetVector("mainCamInfo", new float4(pos, maxTessDist));
        }

        {
            float maxcos = 0.0f;

            //{
            //    const float maxTessAngle = 15.0f;
            //    maxcos = math.cos(math.radians(maxTessAngle));
            //}

            float3 dir = math.rotate(mainCam.transform.rotation, new float3(0.0f, 0.0f, 1.0f));
            
            cshader.SetVector("mainCamDir", 
                new float4(dir, maxcos));
        }
    }
       
    void DispatchCompute()
    {
        int _pvfCount = 1;
        int _ovfCount = 1;
        int _svfCount = 1;
        int _pvfOffset = 0;
        int _ovfOffset = 0;
        int _svfOffset = 0;

        if (LightManager.type == LightType.Directional)
        {
            _pvfOffset = 0;
            _ovfOffset = 0;
            _pvfCount = 1;
            _ovfCount = 4;
        }
        else if (LightManager.type == LightType.Point || LightManager.type == LightType.Spot)
        {
            _pvfOffset = 0;
            _ovfOffset = 4;
            _pvfCount = 7;
            _ovfCount = 1;
        }

        CommandBuffer cmd = CommandBufferPool.Get();

        {
            cmd.SetComputeIntParam(cshader, "pvfOffset", _pvfOffset);
            cmd.SetComputeIntParam(cshader, "ovfOffset", _ovfOffset);
        }        

        cmd.DispatchCompute(cshader, ki_pvf, 1, _pvfCount, 1);
        cmd.DispatchCompute(cshader, ki_ovf, 1, _ovfCount, 1);

        cmd.DispatchCompute(cshader, ki_sphere_center, dpSpTrCount, 1, 1);

        if (bCullDebug)
        {
            cmd.DispatchCompute(cshader, ki_pvf_vertex, _pvfCount, 1, 1);
            cmd.DispatchCompute(cshader, ki_ovf_vertex, _ovfCount, 1, 1);
            cmd.DispatchCompute(cshader, ki_svf_vertex, _svfCount, 1, 1);
            cmd.DispatchCompute(cshader, ki_sphere_vertex, spCount, 1, 1);
        }

        cmd.DispatchCompute(cshader, ki_pvf_cull_sphere, dpSpCount, _pvfCount, 1);
        cmd.DispatchCompute(cshader, ki_ovf_cull_sphere, dpSpCount, _ovfCount, 1);
        cmd.DispatchCompute(cshader, ki_svf_cull_sphere, dpSpTrCount_unit, 1, 1);

        {
            cmd.DispatchCompute(cshader, ki_unit_tessinfo, tessInfoCount, 1, 1);
        }

        Graphics.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    void ReadFromResource()
    {
        bool bReadDebug = false;

        {
            cullResult_svf_Buffer.Read();
        }

        {
            unit_tess_Buffer.Read();            
        }

        if(bTessDebug)
        {
            unit_tess_debug_Buffer.Read();
        }

        if (bReadDebug)
        {
            {
                CommandBuffer cmd = CommandBufferPool.Get();

                {
                    plane_pvf_Buffer.Read();
                }

                {
                    plane_ovf_Buffer.Read();
                }

                {
                    pvf_vertex_Buffer.Read();
                }

                {
                    ovf_vertex_Buffer.Read();
                }

                {
                    sphere_vertex_Out_Buffer.Read();
                }

                {
                    sphere_Out_Buffer.Read();
                }


                Graphics.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
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

            {
                var read = AsyncGPUReadback.Request(cullResult_ovf_Texture);
                read.WaitForCompletion();

                for (int i = 0; i < groupOvfCount; i++)
                {
                    var na = read.GetData<float>(i);
                    for (int j = 0; j < ovfCount; j++)
                    {
                        for (int k = 0; k < spCount; k++)
                        {
                            cullResult_ovf[i * ovfCount * spCount + j * spCount + k] = na[j * spCount + k];
                        }
                    }
                }
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


    //Test
    void Update0()
    {
        if (GameManager.unitCount < 1)
        {
            return;
        }

        {
            Compute();
        }

        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            debugCullMode++;
            if (debugCullMode > 5)
            {
                debugCullMode = 0;
            }

            //debugCullMode = (++debugCullMode) % 6;
        }

        if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            debugCullMode--;
            if (debugCullMode < 0)
            {
                debugCullMode = 5;
            }
        }
    }

    void Update00()
    {
        if (GameManager.unitCount < 1)
        {
            return;
        }

        {
            Compute();
        }

        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            debugCullMode++;
            if (debugCullMode > 11)
            {
                debugCullMode = 0;
            }

            //debugCullMode = (++debugCullMode) % 6;
        }

        if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            debugCullMode--;
            if (debugCullMode < 0)
            {
                debugCullMode = 11;
            }
        }
    }

    void WriteToResource0()
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
            var data = info_ovf_Buffer.data;
            for (int i = 0; i < ovfCount; i++)
            {
                Info_VF ivf = new Info_VF();

                ivf.fi = csmfi[i];
                ivf.pos = csmPos[i].xyz;
                ivf.rot = csmRot[i].value;

                data[i] = ivf;
            }
            info_ovf_Buffer.Write();
        }

        {
            spTrans.Schedule<SphereTransform>(spTraa).Complete();

            {
                var data = sphere_In_Buffer.data;
                for (int i = 0; i < spCount; i++)
                {
                    _sphere[i].xyz = spOffset[i];
                    data[i] = _sphere[i];
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
    }

    void WriteToResource00()
    {
        {
            var data = info_pvf_Buffer.data;
            for (int i = 0; i < pvfCount; i++)
            {
                Info_VF ivf = new Info_VF();

                if (i == 0)
                {
                    Transform camTr = mainCam.transform;

                    ivf.fi = new float4(mainCam.fieldOfView, mainCam.aspect, mainCam.nearClipPlane, mainCam.farClipPlane);
                    ivf.pos = camTr.position;
                    ivf.rot = ((quaternion)(camTr.rotation)).value;
                }
                else //(i > 0)
                {
                    ivf.fi = cbmfi[0];
                    ivf.pos = cbmPos[0].xyz;
                    ivf.rot = cbmRot[i - 1].value;
                }

                data[i] = ivf;
            }
            info_pvf_Buffer.Write();
        }

        {
            var data = info_ovf_Buffer.data;
            for (int i = 0; i < ovfCount; i++)
            {
                Info_VF ivf = new Info_VF();

                ivf.fi = csmfi[i];
                ivf.pos = csmPos[i].xyz;
                ivf.rot = csmRot[i].value;

                data[i] = ivf;
            }
            info_ovf_Buffer.Write();
        }

        {
            spTrans.Schedule<SphereTransform>(spTraa).Complete();

            {
                var data = sphere_In_Buffer.data;
                for (int i = 0; i < spCount; i++)
                {
                    _sphere[i].xyz = spOffset[i];
                    data[i] = _sphere[i];
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
    }

    void DispatchCompute0()
    {
        CommandBuffer cmd = CommandBufferPool.Get();

        cmd.DispatchCompute(cshader, ki_pvf, totalPvfCount, 1, 1);
        cmd.DispatchCompute(cshader, ki_ovf, totalOvfCount, 1, 1);

        cmd.DispatchCompute(cshader, ki_sphere_center, dpSpTrCount, 1, 1);

        if (bCullDebug)
        {
            cmd.DispatchCompute(cshader, ki_pvf_vertex, totalPvfCount, 1, 1);
            cmd.DispatchCompute(cshader, ki_ovf_vertex, totalOvfCount, 1, 1);
            cmd.DispatchCompute(cshader, ki_sphere_vertex, spCount, 1, 1);
        }

        cmd.DispatchCompute(cshader, ki_pvf_cull_sphere, dpSpCount, pvfCount, 1);
        cmd.DispatchCompute(cshader, ki_ovf_cull_sphere, dpSpCount, ovfCount, 1);

        Graphics.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    void Render_debug0(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        {
            mpb.SetInt("cullMode", debugCullMode);
            mpb.SetMatrix("CV", perCam.CV);
            mpb.SetVector("dirW_light", csmAction.dirW);
            mpb.SetVector("posW_view", cam.transform.position);
        }

        {
            {
                if (bDrawSp)
                {
                    cmd.DrawProcedural(idxSp_Buffer, Matrix4x4.identity, mte, pass_sphere, MeshTopology.Lines, idxSp_Buffer.count, spCount, mpb);
                }
            }

            {
                if (bDrawPvf)
                {
                    cmd.DrawProcedural(idxVf_Buffer, Matrix4x4.identity, mte, pass_pvf, MeshTopology.Lines, idxVf_Buffer.count, pvfCount, mpb);
                }

                if (bDrawOvf)
                {
                    cmd.DrawProcedural(idxVf_Buffer, Matrix4x4.identity, mte, pass_ovf, MeshTopology.Lines, idxVf_Buffer.count, ovfCount, mpb);
                }
            }
        }

    }

}