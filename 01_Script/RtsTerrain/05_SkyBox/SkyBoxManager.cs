using System.Collections;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.Mathematics;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

public class SkyBoxManager : MonoBehaviour
{   
    MeshInfo mesh_quad;

    public Transform trMLight;

    public ComputeShader cshader;
    int ki_worldVertex;

    public Shader gshader;
    int pass_SkyBox;


    Material mte;
    MaterialPropertyBlock mpb;

    void Awake()
    {              

    }

    public void Init()
    {
        {
            ki_worldVertex = cshader.FindKernel("CS_WorldVertex");
        }

        {
            mte = new Material(gshader);
            mpb = new MaterialPropertyBlock();
           
            pass_SkyBox = mte.FindPass("SkyBox");
        }


        {
            CreateMesh();
            CreateCameraData();
            CreateLightData();
        }      
    }

    void CreateMesh()
    {
        {
            mesh_quad = new MeshInfo(RenderUtil.CreateNDCquadMesh(), 1);
        }
    }
        

    void CreateCameraData()
    {
        perViewBuffer = new COBuffer<ViewData>(1);
        mpb.SetBuffer("camera", perViewBuffer.value);
    }

    void CreateLightData()
    {
        {
            mLightData_Buffer = new COBuffer<LightData>(1);
        }

        {
            mpb.SetBuffer("mLightData_Buffer", mLightData_Buffer.value);
        }

        {
            mpb.SetBuffer("light_Buffer", LightManager.light_Buffer.value);
        }
    }

    public Cubemap cubemap;    
   
    public void Enable()
    {
        RenderGOM_DF.BeginFrameRender += Compute;        
        DeferredRenderManager.OnRender_SkyBox += Render;
    }

    public void Disable()
    {
        RenderGOM_DF.BeginFrameRender -= Compute;        
        DeferredRenderManager.OnRender_SkyBox -= Render;
    }

    void OnDestroy()
    {      

        if (mesh_quad != null)
        {
            mesh_quad.ReleaseResource();
        }

        BufferBase<LightData>.Release(mLightData_Buffer);

        BufferBase<ViewData>.Release(perViewBuffer);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(!GameManager.bUpdate)
        {
            return;
        }


        UpdateRootW();
        UpdateLightData();
    }

    unsafe void UpdateRootW()
    {
        float t = Time.time;        

        {
            MeshInfo mesh = mesh_quad;

            for (int i = 0; i < mesh.insCount; i++)
            {
                float4x4 W = float4x4.identity;
                float4x4 W_IT = float4x4.identity;

                float3 pos = float3.zero;
                quaternion rot = quaternion.identity;
                float3 sca = new float3(1.0f, 1.0f, 1.0f);

                MeshInfo.Get_W_Wn(pos, rot, sca, out W, out W_IT);

                mesh.wBone.data[i] = W;
                mesh.wBoneIT.data[i] = W_IT;
            }

            mesh.wBone.Write();
            mesh.wBoneIT.Write();
        }

        int a = 0;
    }

    unsafe void UpdateCameraData(Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        fixed (ViewData* data = &(perViewBuffer.data[0]))
        {
            data->V = perCam.V;

            data->C = perCam.C;
            data->CV = perCam.CV;

            data->_C_I = RenderUtil.GetVfromC(cam, false);
            data->V_I = RenderUtil.GetWfromV(cam);

            data->posW = new float4((float3)cam.transform.position, 0.0f);
            data->dirW = new float4(math.rotate(cam.transform.rotation, new float3(0.0f, 0.0f, 1.0f)), 0.0f);
            data->data = float4.zero;
        }


        perViewBuffer.Write();

        {
            mpb.SetBuffer("camera", perViewBuffer.value);
        }
    }

    unsafe void UpdateLightData()
    {

        float t = Time.time;

        //MainLight        
        {
            {
                trMLight.rotation = math.mul(
                    quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), 2.0f * t),
                    quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.radians(45.0f))
                    );
            }

            {
                var data = mLightData_Buffer.data;

                for (int n = 0; n < 1; n++)
                {
                    float sFactor = 10.0f;

                    data[n].dirW = new float4(-math.rotate(trMLight.rotation, new float3(0.0f, 0.0f, +1.0f)), 0.0f);
                    data[n].posW = new float4(0.0f, 2.5f, 0.0f, 1.0f);
                    data[n].dirW.w = sFactor;

                    data[n].posV = float4.zero;
                    data[n].dirV = float4.zero;
                    data[n].color = new float4(1.0f, 1.0f, 1.0f, 1.0f);
                    data[n].data = float4.zero;
                }

                mLightData_Buffer.Write();
            }
        }
    }

    void Compute(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {
        bool bDebug = false;       

        {
            var mesh = mesh_quad;

            cmd.SetComputeVectorParam(cshader, "countInfo", mesh.ci);
            cmd.SetComputeBufferParam(cshader, ki_worldVertex, "vIn", mesh.vtxIn_cs.value);
            cmd.SetComputeBufferParam(cshader, ki_worldVertex, "bone", mesh.wBone.value);
            cmd.SetComputeBufferParam(cshader, ki_worldVertex, "boneIT", mesh.wBoneIT.value);
            cmd.SetComputeBufferParam(cshader, ki_worldVertex, "vOut", mesh.vtxIn.value);

            cmd.DispatchCompute(cshader, ki_worldVertex, mesh.insCount, mesh.vgCount, 1);
        }

        if (bDebug)
        {
            var mesh = mesh_quad;

            mesh.vtxIn.Read();
        }

    }
    

    private void Render(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        UpdateCameraData(cam, perCam);

        //{
        //    cmd.ClearRenderTarget(RTClearFlags.Stencil, Color.black, 1.0f, 0);
        //}

        {
            var mesh = mesh_quad;
            var pass = pass_SkyBox;

            {
                float4x4 M = RenderUtil.GetM_Unity();
                mpb.SetMatrix("M", M);
            }

            mpb.SetVector("countInfo", mesh.ci);
            mpb.SetBuffer("vtxBuffer", mesh.vtxIn.value);
            mpb.SetBuffer("vtxBuffer_St", mesh.vtxIn_st.value);
            mpb.SetBuffer("vtxBuffer_Local", mesh.vtxIn_cs.value);
            mpb.SetTexture("cubemap", cubemap);

            cmd.DrawProcedural(mesh.idxBuffer, Matrix4x4.identity, mte, pass, MeshTopology.Triangles, mesh.idxCount, mesh.insCount, mpb);
        }
    }           

    struct ViewData
    {
        public float4x4 V;

        public float4x4 C;
        public float4x4 CV;

        public float4x4 _C_I;
        public float4x4 V_I;

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