using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;

using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

using Fusion;

public class HpbarManager : MonoBehaviour
{
    public void Enable()
    {        
        if (unitCount > 0)
        {
            DeferredRenderManager.OnRender_Transparent += Render;
        }      
    }

    public void Disable()
    {       
        if (unitCount > 0)
        {
            DeferredRenderManager.OnRender_Transparent -= Render;
        }        
    }

    void OnDestroy()
    {       
        if (unitCount > 0)
        {
            ReleaseResource();
        }          
    }

    void Update()
    {
        if (!GameManager.bUpdate)
        {
            return;
        }

        //if (GameManager.isPause)
        //{
        //    return;
        //}

        if (unitCount > 0)
        {
            Compute();
        }      
    }

    void Compute()
    {
        WriteToResource();
        DispatchCompute();
        ReadFromResource();
    }     

    public ComputeShader cshader;
    public Shader gshader;

    public CSM_Action csmAction;

    int count;

    

    int cullOffset
    {
        get
        {
            return 0;
        }
    }

    public void SetCullData(RenderTexture pvf_tex)
    {
        int count = GameManager.unitCount;
        if (count > 0)
        {            
            {
                mpb.SetInt("cullOffset", cullOffset);
                mpb.SetTexture("cullResult_pvf_Texture", pvf_tex);
            }
        }
    }

    public void Init()
    {
        unitCount = GameManager.unitCount;

        if(unitCount > 0)
        {
            InitData();
            InitCompute();
            InitRendering();
            InitResource();
        }            
    }

    public void Begin()
    {

    }            

    //public float[] _maxHp;
    //public float[] _hitHp;
    //public float[] _healHp;

    //public float[] _hp;

    int unitCount;
    int[] unitCounts;
    Transform[] unitTrs;
    UnitActor[] unitActors;

    NetworkTransform[] ntTrs;


    public float[] _offset;
    float3[] offsetVec;

    public float3[] _tScale;
    float3[] tScales;

    
    //public static float[] maxHp
    //{
    //    get; set;
    //}
    float[] maxHp;
    float[] maxHps;

    float[] hp;

    int dpTrWCount;    

    Transform mainCamTr;
   

    static HpbarManager()
    {       
       
    }


    void InitData()
    {        
        unitCounts = GameManager.unitCounts;
        unitTrs = GameManager.unitTrs;
        unitActors = GameManager.unitActors;        

        offsetVec = new float3[unitCount];
        tScales = new float3[unitCount];
        maxHps = new float[unitCount];
        hp = GameManager.hp; 

       
        {
            maxHp = GameManager.maxHp;       
        }        
       
        
        int start = 0;
        for (int i = 0; i < unitCounts.Length; i++)
        {
            for (int j = 0; j < unitCounts[i]; j++)
            {
                int idx = start + j;
                offsetVec[idx] = new float3(0.0f, _offset[i], 0.0f);
                tScales[idx] = _tScale[i];
                //maxHps[idx] = _maxHp[i];
                maxHps[idx] = maxHp[i];
                hp[idx] = maxHps[idx];
            }
            start += unitCounts[i];
        }

        dpTrWCount = (unitCount % 64 == 0) ? (unitCount / 64) : (unitCount / 64 + 1);

        mainCamTr = Camera.main.transform;

        {
            ntTrs = new NetworkTransform[unitCount];

            for(int i = 0; i < unitCount; i++)
            {
                ntTrs[i] = unitActors[i].GetBehaviour<NetworkTransform>();
            }
        }
    }

    int ki_trW;
    int ki_Vertex;

    void InitCompute()
    {
        ki_trW = cshader.FindKernel("CS_trW");
        ki_Vertex = cshader.FindKernel("CS_Vertex");
    }

    public Mesh cubeMesh;
    Material mte;
    MaterialPropertyBlock mpb;
    int pass;
    GraphicsBuffer idxBuffer;


    int vtxInCount;
    int vtxOutCount;
    
    void InitRendering()
    {       
        mte = new Material(gshader);
        mpb = new MaterialPropertyBlock();

        vtxInCount = cubeMesh.vertexCount;
        vtxOutCount = unitCount * vtxInCount;

        idxBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, (int)cubeMesh.GetIndexCount(0), sizeof(int));
        idxBuffer.SetData(cubeMesh.GetIndices(0));

        pass = mte.FindPass("Hpbar");

        {
            mpb.SetBuffer("active_Buffer", GameManager.active_Buffer.value);
            mpb.SetBuffer("state_Buffer", GameManager.state_Buffer.value);
        }
    }

    ROBuffer<float4x4> trW_In_Buffer;
    COBuffer<float4x4> trW_Const_Buffer;
    RWBuffer<float4x4> trW_Out_Buffer;
    COBuffer<Vertex> vertex_In_Buffer;
    RWBuffer<Vertex> vertex_Out_Buffer;  

    COBuffer<int>   player_Num_Buffer;    

    void InitResource()
    {
        {
            trW_In_Buffer =     new ROBuffer<float4x4>(unitCount);
            trW_Out_Buffer =    new RWBuffer<float4x4>(unitCount);
            trW_Const_Buffer =  new COBuffer<float4x4>(unitCount);
            vertex_In_Buffer =  new COBuffer<Vertex>(vtxInCount);
            vertex_Out_Buffer = new RWBuffer<Vertex>(vtxOutCount);
        }      

        {
            cshader.SetBuffer(ki_trW, "trW_In_Buffer", trW_In_Buffer.value);
            cshader.SetBuffer(ki_trW, "trW_Const_Buffer", trW_Const_Buffer.value);
            cshader.SetBuffer(ki_trW, "trW_Out_Buffer", trW_Out_Buffer.value);
            cshader.SetBuffer(ki_trW, "refHp_Buffer", GameManager.refHp_Buffer.value);

            cshader.SetBuffer(ki_Vertex, "trW_Out_Buffer", trW_Out_Buffer.value);
            cshader.SetBuffer(ki_Vertex, "vertex_In_Buffer", vertex_In_Buffer.value);
            cshader.SetBuffer(ki_Vertex, "vertex_Out_Buffer", vertex_Out_Buffer.value);
        }

        {            
            {
                var data = trW_Const_Buffer.data;
                for (int i = 0; i < unitCount; i++)
                {
                    float4x4 mat = float4x4.zero;
                    mat.c0 = new float4(tScales[i], 0.0f);
                    mat.c1 = new float4(offsetVec[i], 0.0f);
                    mat.c2 = new float4(maxHps[i], 0.0f, 0.0f, 0.0f);
                    data[i] = mat;                    
                }

                trW_Const_Buffer.Write();
            }

            {
                List<Vector3> pos = new List<Vector3>();
                List<Vector3> nom = new List<Vector3>();

                cubeMesh.GetVertices(pos);
                cubeMesh.GetNormals(nom);

                var data = vertex_In_Buffer.data;
                for (int i = 0; i < vtxInCount; i++)
                {
                    Vertex vtx;
                    vtx.pos = pos[i];
                    vtx.nom = nom[i];
                    data[i] = vtx;
                }
                vertex_In_Buffer.Write();
            }
        }


        //     
        {
            player_Num_Buffer = new COBuffer<int>(unitCount);           
        }

        {
            var data = player_Num_Buffer.data;
            for (int i = 0; i < unitCount; i++)
            {
                data[i] = unitActors[i].pNum;
            }

            player_Num_Buffer.Write();
        }

        {
            mpb.SetBuffer("vertex_Out_Buffer", vertex_Out_Buffer.value);
            mpb.SetInt("dvCount", vtxInCount);
            mpb.SetInt("cullOffset", 0);

            mpb.SetBuffer("player_Num_Buffer", player_Num_Buffer.value);
            mpb.SetBuffer("pColor_Buffer", GameManager.playerColor_Buffer.value);
        }       
    }


   


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteToResource0()
    {        
        {
            float3x3 R = new float3x3(mainCamTr.rotation);
            R.c0 *= -1.0f;
            R.c2 *= -1.0f;

            var data = trW_In_Buffer.data;
            for (int i = 0; i < unitCount; i++)
            {
                float4x4 mat = float4x4.zero;
                Transform tr = unitTrs[i];
                mat.c0 = new float4(R.c0, 0.0f);
                mat.c1 = new float4(R.c1, 0.0f);
                mat.c2 = new float4(R.c2, 0.0f);
                mat.c3 = new float4(tr.position, hp[i]);

                data[i] = mat;
            }
            trW_In_Buffer.Write();
        }      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteToResource()
    {
        {
            float3x3 R = new float3x3(mainCamTr.rotation);
            R.c0 *= -1.0f;
            R.c2 *= -1.0f;

            var data = trW_In_Buffer.data;
            for (int i = 0; i < unitCount; i++)
            {
                float4x4 mat = float4x4.zero;
                float3 ntPos = ntTrs[i].ReadPosition();
                mat.c0 = new float4(R.c0, 0.0f);
                mat.c1 = new float4(R.c1, 0.0f);
                mat.c2 = new float4(R.c2, 0.0f);
                mat.c3 = new float4(ntPos, hp[i]);

                data[i] = mat;
            }
            trW_In_Buffer.Write();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void DispatchCompute()
    {
        CommandBuffer cmd = CommandBufferPool.Get();

        cmd.DispatchCompute(cshader, ki_trW, dpTrWCount, 1, 1);
        cmd.DispatchCompute(cshader, ki_Vertex, unitCount, 1, 1);
    
        Graphics.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    bool bDebug = false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ReadFromResource()
    { 
        if(bDebug)
        {
            trW_Out_Buffer.Read();
            vertex_Out_Buffer.Read();
        }

        {
            GameManager.refHp_Buffer.Read();
        }
    }

    
    
    void Render(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        {
            mpb.SetMatrix("CV", perCam.CV);
            mpb.SetVector("dirW_light", csmAction.dirW);
        }

        {
            cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, mte, pass, MeshTopology.Triangles, idxBuffer.count, unitCount, mpb);
        }
    }


    void ReleaseResource()
    {
        BufferBase<float4x4>.Release(trW_In_Buffer    );
        BufferBase<float4x4>.Release(trW_Out_Buffer   );
        BufferBase<float4x4>.Release(trW_Const_Buffer );
        BufferBase<Vertex>.Release(vertex_In_Buffer );
        BufferBase<Vertex>.Release(vertex_Out_Buffer);
        BufferBase<int>.Release(player_Num_Buffer);       

        ReleaseGBuffer(idxBuffer);
    }

    void ReleaseGBuffer(GraphicsBuffer gBuffer)
    {
        if (gBuffer != null) gBuffer.Release();
    }

    [System.Serializable]
    public struct Vertex
    {
        public float3 pos;
        public float3 nom;
    };


}
