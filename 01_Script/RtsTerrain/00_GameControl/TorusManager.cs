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

public class TorusManager : MonoBehaviour
{         
    public ComputeShader cshader;
    public Shader gshader;

    public CSM_Action csmAction;

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
        if(!GameManager.bUpdate)
        {
            return;
        }

        if (GameManager.isPause)
        {
            return;
        }


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

    int unitCount;
    int[] unitCounts;
    Transform[] unitTrs;

    NetworkTransform[] ntTrs;

    public float[] _offset;
    float3[] offsetVec;
    
    public float[] _tScale;
    float3[] tScales;

    float3 tSca;

    int dpTrWCount;

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

    void InitData()
    {        
        unitCounts = GameManager.unitCounts;
        unitTrs = GameManager.unitTrs;
        
        offsetVec = new float3[unitCount];
        tScales = new float3[unitCount];
        
        int start = 0;
        for (int i = 0; i < unitCounts.Length; i++)
        {            
            for (int j = 0; j < unitCounts[i]; j++)
            {
                offsetVec[start + j] = new float3(0.0f, _offset[i], 0.0f);
                tScales[start + j] = new float3(1.0f, 1.0f, 1.0f) * _tScale[i];
            }
            start += unitCounts[i];
        }

        dpTrWCount = (unitCount % 64 == 0) ? (unitCount / 64) : (unitCount / 64 + 1);

        {
            ntTrs = new NetworkTransform[unitCount];

            for (int i = 0; i < unitCount; i++)
            {
                ntTrs[i] = GameManager.unitActors[i].GetBehaviour<NetworkTransform>();
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

    Mesh torusMesh;
    Material mte;
    MaterialPropertyBlock mpb;
    int pass;
    GraphicsBuffer idxBuffer;

    
    int vtxInCount;
    int vtxOutCount;

    public float4 torusInfo = new float4(1.0f, 2.0f, 24.0f, 24.0f);

    void InitRendering()
    {       
        torusMesh = RenderUtil.CreateTorusMesh(torusInfo.x, torusInfo.y, (int)torusInfo.z, (int)torusInfo.w);
        mte = new Material(gshader);
        mpb = new MaterialPropertyBlock();

        vtxInCount = torusMesh.vertexCount;
        vtxOutCount = unitCount * vtxInCount;

        idxBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, (int)torusMesh.GetIndexCount(0), sizeof(int));
        idxBuffer.SetData(torusMesh.GetIndices(0));

        pass = mte.FindPass("Torus");
    }
   

    ROBuffer<float4x4> trW_In_Buffer;
    RWBuffer<float4x4> trW_Out_Buffer;
    ROBuffer<Vertex> vertex_In_Buffer;
    RWBuffer<Vertex> vertex_Out_Buffer;
    

    void InitResource()
    {
        {
            trW_In_Buffer =     new ROBuffer<float4x4>(unitCount);
            trW_Out_Buffer =    new RWBuffer<float4x4>(unitCount);
            vertex_In_Buffer =  new ROBuffer<Vertex>(vtxInCount) ;
            vertex_Out_Buffer = new RWBuffer<Vertex>(vtxOutCount);
        }        

        {
            cshader.SetBuffer(ki_trW, "trW_In_Buffer", trW_In_Buffer.value);
            cshader.SetBuffer(ki_trW, "trW_Out_Buffer", trW_Out_Buffer.value);

            cshader.SetBuffer(ki_Vertex, "trW_Out_Buffer", trW_Out_Buffer.value);
            cshader.SetBuffer(ki_Vertex, "vertex_In_Buffer", vertex_In_Buffer.value);
            cshader.SetBuffer(ki_Vertex, "vertex_Out_Buffer", vertex_Out_Buffer.value);
        }

        {
            cshader.SetVector("t1_t0", TerrainManager.t1_t0);
            cshader.SetTexture(ki_trW, "normalHeight_Tex", TerrainManager.normalHeight_Tex);
        }

        {
            {
                var data = trW_In_Buffer.data;
                for (int i = 0; i < unitCount; i++)
                {
                    float4x4 mat = float4x4.zero;
                    //mat.c2 = new float4(tSca, 0.0f);
                    mat.c2 = new float4(tScales[i], 0.0f);
                    mat.c3 = new float4(offsetVec[i], 0.0f);
                  
                    data[i] = mat;
                }
                trW_In_Buffer.Write();
            }

            {
                List<Vector3> pos = new List<Vector3>();
                List<Vector3> nom = new List<Vector3>();

                torusMesh.GetVertices(pos);
                torusMesh.GetNormals(nom);

                var data = vertex_In_Buffer.data;
                for(int i = 0; i < vtxInCount; i++)
                {
                    Vertex vtx;
                    vtx.pos = pos[i];
                    vtx.nom = nom[i];
                    data[i] = vtx;
                }

                vertex_In_Buffer.Write();
            }
        }
      
        {
            mpb.SetBuffer("vertex_Out_Buffer", vertex_Out_Buffer.value);
            mpb.SetInt("dvCount", vtxInCount);

            mpb.SetBuffer("isSelect_Buffer", GameManager.select_Buffer.value);
            mpb.SetBuffer("has_input_Buffer", GameManager.has_input_Buffer.value);
        }       
    }

   

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteToResource0()
    {
        {
            var data = trW_In_Buffer.data;
            for (int i = 0; i < unitCount; i++)
            {
                float4x4 mat = data[i];
                Transform tr = unitTrs[i];
                mat.c0.xyz = tr.position;
                mat.c1 = ((quaternion)(tr.rotation)).value;

                data[i] = mat;
            }
            trW_In_Buffer.Write();
        }

        {
            cshader.SetMatrix("T", TerrainManager.matT);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteToResource()
    {
        {
            var data = trW_In_Buffer.data;
            for (int i = 0; i < unitCount; i++)
            {
                float4x4 mat = data[i];
                NetworkTransform ntTr = ntTrs[i];
                mat.c0.xyz = ntTr.ReadPosition();
                mat.c1 = ((quaternion)(ntTr.ReadRotation())).value;

                data[i] = mat;
            }
            trW_In_Buffer.Write();
        }

        {
            cshader.SetMatrix("T", TerrainManager.matT);
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
    }
 
    
    void Render(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        {
            mpb.SetMatrix("CV", perCam.CV);
            mpb.SetVector("dirW_light", -csmAction.dirW);
        }

        {
            cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, mte, pass, MeshTopology.Triangles, idxBuffer.count, unitCount, mpb);
        }        
    }


    void ReleaseResource()
    {
        BufferBase<float4x4>.Release(trW_In_Buffer);        
        BufferBase<float4x4>.Release(trW_Out_Buffer);
        BufferBase<Vertex>.Release(vertex_In_Buffer);
        BufferBase<Vertex>.Release(vertex_Out_Buffer);
       
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
