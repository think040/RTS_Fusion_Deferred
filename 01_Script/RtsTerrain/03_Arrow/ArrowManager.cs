using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using Utility_JSB;

using Fusion;

public class ArrowManager : NetworkBehaviour
{
    public ComputeShader cshader;
    public Shader gshader;
    public GameObject arrowPrefab;

    public ComputeShader cshader_col;
    public Shader gshader_col;
    public static int cCount
    { get; set; } = 1024;

    public static ArrowConst[] arrowConstData;
    public ArrowBoneOut[] bonesOutData;

    public GameObject[] arrow;
    ArrowActor[] arrowActor;

    [HideInInspector]
    public GameObject[] target;
    [HideInInspector]
    public GameObject[] shooter;

    public static Transform[] arrowTr;
    public static Transform[] shootTr;
    public static Transform[] targetTr;
    public static UnitActor[] sActor;
    public static UnitActor[] tActor;

    public float3 aSca = new float3(0.05f, 0.05f, 0.25f);

    ArrowCompute arrowCom;

    //static public ArrowManager instance { get; set; }

    void Awake()
    {
        //Init();
    }

    public void Enable()
    {
        if (cCount > 0)
        {         
            RenderGOM_DF.RenderCSM += RenderCSM;
            RenderGOM_DF.RenderCBM += RenderCBM;           

            DeferredRenderManager.OnRender_GBuffer += Render_GBuffer;

            EnableColRender();
        }
    }

    public void Disable()
    {
        if (cCount > 0)
        {
            RenderGOM_DF.RenderCSM -= RenderCSM;
            RenderGOM_DF.RenderCBM -= RenderCBM;

            DeferredRenderManager.OnRender_GBuffer -= Render_GBuffer;

            DisableColRender();

            StopCoroutine(ac);            
        }

        if (cCount > 0)
        {
            if (arrowCom != null) arrowCom.ReleaseCShader();
            if (idxBuffer != null) idxBuffer.Dispose();

            BufferBase<float4>.Release(arrowColor_Buffer);
            BufferBase<int>.Release(pid_Buffer);

            DestroyColRender();

            DestroyArrow();
        }
    }

    void OnDestroy()
    {
        //if (cCount > 0)
        //{
        //    if (arrowCom != null) arrowCom.ReleaseCShader();
        //    if (idxBuffer != null) idxBuffer.Dispose();
        //
        //    BufferBase<float4>.Release(arrowColor_Buffer);
        //    BufferBase<int>.Release(pid_Buffer);
        //
        //    DestroyColRender();            
        //}
    }


    void DestroyArrow()
    {
        if(cCount > 0)
        {
            for(int i = 0; i < cCount; i++)
            {
                DestroyImmediate(arrow[i]);
            }
        }
    }

    public void Init()
    {
        {
            var mainLight = GameObject.FindGameObjectWithTag("MainLight");

            csm_action = mainLight.GetComponent<CSM_Action>();
            light = mainLight.GetComponent<Light>();
        }


        cCount = 4 * (
            GameManager.unitCounts[0] +
            GameManager.unitCounts[1] +
            GameManager.unitCounts[4] +
            GameManager.unitCounts[5]);

        if (cCount > 0)
        {
            arrowConstData = new ArrowConst[cCount];
            arrow = new GameObject[cCount];
            target = new GameObject[cCount];
            shooter = new GameObject[cCount];
            aes = new IEnumerator[cCount];

            arrowTr = new Transform[cCount];
            shootTr = new Transform[cCount];
            targetTr = new Transform[cCount];
            sActor = new UnitActor[cCount];
            tActor = new UnitActor[cCount];

            arrowActor = new ArrowActor[cCount];

            {
                float3 xaxis = math.rotate(transform.rotation, new float3(1.0f, 0.0f, 0.0f));
                float3 zaxis = math.rotate(transform.rotation, new float3(0.0f, 0.0f, 1.0f));
                float3 center = transform.position;

                float3 pos;
                quaternion rot = quaternion.identity;
                float dz = 2.0f;
                float dx = 1.0f;
                //float z0 = -8.0f;
                float z0 = -16.0f;
                //float x0 = -15.0f;
                float x0 = -30.0f;

                float z1 = +10.0f;
                //float x1 = +25.0f;
                float x1 = +30.0f;
                int uNum = 128;


                x0 = 0;
                z0 = 0;
                for (int i = 0; i < cCount; i++)
                {
                    //pos = new float3((float)i * 2.0f, 0.0f, 0.0f);
                    pos = new float3(x0 - (float)(i / uNum) * dx, 0.0f, z0 + (float)(i % uNum) * dz);
                    pos = center + xaxis * pos.x + zaxis * pos.z;

                    arrow[i] = GameObject.Instantiate(arrowPrefab, pos, rot);
                    arrow[i].name = "arrow" + i.ToString();
                    arrowTr[i] = arrow[i].transform;
                    arrowActor[i] = arrow[i].GetComponent<ArrowActor>();
                }

                //for (int i = 0; i < cCount; i++)
                //{
                //    //pos = new float3((float)i * 2.0f, 0.0f, 0.0f);
                //    pos = new float3(x0 - (float)(i / uNum) * dx, 0.0f, z0 + (float)(i % uNum) * dz);                    
                //    pos = center + xaxis * pos.x + zaxis * pos.z;
                //
                //    shooter[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //    shooter[i].transform.position = pos;
                //    shooter[i].transform.rotation = rot;
                //    shooter[i].name = "shooter" + i.ToString();
                //}
                //
                //for (int i = 0; i < cCount; i++)
                //{
                //    //pos = new float3((float)i * 2.0f, 0.0f, 10.0f);
                //    pos = new float3(x1, 0.0f, z0 + (float)(i % uNum) * dz);
                //    pos = center + xaxis * pos.x + zaxis * pos.z;
                //
                //    target[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //    target[i].transform.position = pos;
                //    target[i].transform.rotation = rot;
                //    target[i].name = "target" + i.ToString();
                //}

                for (int i = 0; i < cCount; i++)
                {
                    ArrowConst ad = new ArrowConst();
                    //ad.p0 = shooter[i].transform.position;
                    //ad.p1 = target[i].transform.position;
                    //ad.pi = arrow[i].transform.position;

                    ad.sca = aSca;
                    ad.u = 0.0f;
                    ad.active = false;
                    //ad.active = true;
                    arrowConstData[i] = ad;

                }
            }

            {
                arrowCom = new ArrowCompute();
                arrowCom.Init(cshader, arrowConstData);
            
                ac = arrowCom.Compute();
            
                StartCoroutine(ac);
            }

            ////Compute_Fixed
            //{
            //    arrowCom = new ArrowCompute();
            //    arrowCom.Init(cshader, arrowConstData);
            //    arrowCom.InitCShader();                
            //}

            {
                for (int i = 0; i < cCount; i++)
                {
                    aes[i] = ArrowAction(i);
                }

                arrowCom.PreCompute +=
                    () =>
                    {
                        for (int i = 0; i < cCount; i++)
                        {
                            aes[i].MoveNext();
                        }
                    };
            }

            InitRenderArrow();

            {
                InitColliderRendering();
                StartCoroutine(UpdateColRender());
            }
        }
    }

    public void Begin()
    {
        for (int i = 0; i < cCount; i++)
        {
            arrowActor[i].Begin();
        }
    }

    //public override void FixedUpdateNetwork()
    //{
    //    if(!GameManager.bUpdate)
    //    {
    //        return;
    //    }
    //
    //    if(ac != null)
    //    {
    //        ac.MoveNext();
    //    }
    //
    //    //arrowCom.Compute_Fixed();
    //
    //}

    //private void FixedUpdate()
    //{
    //    if(!GameManager.bUpdate)
    //    {
    //        return;
    //    }
    //    
    //    if(ac != null)
    //    {
    //        ac.MoveNext();
    //    }
    //    
    //}




    public static void ShootArrow(UnitActor sactor, Transform sTr, Transform tTr)
    {
        for (int i = 0; i < cCount; i++)
        {
            if (arrowConstData[i].active == false)
            {
                arrowConstData[i].active = true;
                shootTr[i] = sTr;
                targetTr[i] = tTr;
                sActor[i] = sactor;
                tActor[i] = tTr.GetComponent<UnitActor>();

                pid_Buffer.data[i] = sactor.pNum;
                break;
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShootArrow(NetworkId sId, NetworkId tId)
    {
        var sObject = Runner.FindObject(sId);
        var tObject = Runner.FindObject(tId);

        if (sObject == null || tObject ==null)
        {
            return;
        }

        for (int i = 0; i < cCount; i++)
        {
            if (arrowConstData[i].active == false)
            {
                arrowConstData[i].active = true;
                sActor[i] = sObject.GetComponent<UnitActor>();
                tActor[i] = tObject.GetComponent<UnitActor>();
                shootTr[i] = sActor[i].shootTr;
                targetTr[i] = tObject.transform;
               

                pid_Buffer.data[i] = sActor[i].pNum;
                break;
            }
        }
    }

    IEnumerator ArrowAction0(int i)
    {
        float u; ;
        float da_du = 8.0f;
        float arcLength = 10.0f;

        float yOffset = 1.5f;
        Transform tTr = null;

        while (true)
        {
            if (arrowConstData[i].active)
            {
                //{
                //    ArrowConst ad = arrowConstData[i];
                //    ad.p0 = shooter[i].transform.position;
                //    ad.p1 = target[i].transform.position;
                //    ad.pi = arrow[i].transform.position;
                //    u = ad.u;
                //    arrowConstData[i] = ad;
                //}

                {
                    tTr = targetTr[i];
                
                    ArrowConst ad = arrowConstData[i];
                    ad.p0 = shootTr[i].position;
                    ad.p1 = tTr.position + new Vector3(0.0f, tTr.localScale.y * yOffset, 0.0f);
                    ad.pi = arrowTr[i].position;
                    u = ad.u;
                    arrowConstData[i] = ad;
                }

                while (0.0f <= u && u <= 1.0f)
                {
                    yield return null;

                    //{
                    //    ArrowOut ao = arrowCom.arrowData[i];
                    //    arrow[i].transform.position = ao.pos.xyz;
                    //    arrow[i].transform.rotation = new quaternion(ao.rot);
                    //    arcLength = ao.pos.w;
                    //
                    //    ArrowConst ad = arrowConstData[i];
                    //    ad.p1 = target[i].transform.position;
                    //    ad.pi = arrow[i].transform.position;
                    //
                    //    u = u + da_du * Time.deltaTime / arcLength;
                    //    ad.u = u;
                    //
                    //    arrowConstData[i] = ad;
                    //}

                    {
                        ArrowOut ao = arrowCom.arrowData[i];
                        arrowTr[i].position = ao.pos.xyz;
                        arrowTr[i].rotation = new quaternion(ao.rot);
                        arcLength = ao.pos.w;
                    
                        ArrowConst ad = arrowConstData[i];
                        ad.p1 = tTr.position + new Vector3(0.0f, tTr.localScale.y * yOffset, 0.0f);
                        ad.pi = arrowTr[i].position;

                        //u = u + da_du * Time.deltaTime / arcLength;
                        u = u + da_du * Runner.DeltaTime / arcLength;                                                

                        ad.u = u;
                    
                        arrowConstData[i] = ad;
                    }
                }
                u = 0.0f;

                //if ((tActor[i].isActive || tActor[i].stateData < 3 ) && arrowConstData[i].active )
                if (tActor[i].isActive && arrowConstData[i].active)
                {
                    //tActor[i].DamageHp(sActor[i].hitHp);
                    
                    if(Object.HasStateAuthority)
                    {
                        tActor[i].RPC_DamageHp(sActor[i].hitHp);
                    }        
                    
                    //arrowActor[i].AudioPlay();
                }

                {
                    ArrowConst ad = arrowConstData[i];
                    ad.u = u;
                    ad.active = false;
                    arrowConstData[i] = ad;
                }
            }

            yield return null;
        }
    }

    IEnumerator ArrowAction1(int i)
    {
        float u; ;
        float da_du = 8.0f;
        float arcLength = 10.0f;

        float yOffset = 1.5f;
        Transform tTr = null;

        while (true)
        {
            if (arrowConstData[i].active)
            {
                //{
                //    ArrowConst ad = arrowConstData[i];
                //    ad.p0 = shooter[i].transform.position;
                //    ad.p1 = target[i].transform.position;
                //    ad.pi = arrow[i].transform.position;
                //    u = ad.u;
                //    arrowConstData[i] = ad;
                //}

                {
                    tTr = targetTr[i];

                    ArrowConst ad = arrowConstData[i];
                    ad.p0 = shootTr[i].position;
                    ad.p1 = tTr.position + new Vector3(0.0f, tTr.localScale.y * yOffset, 0.0f);
                    ad.pi = arrowTr[i].position;
                    u = ad.u;
                    arrowConstData[i] = ad;
                }

                while (0.0f <= u && u <= 1.0f)
                {
                    yield return null;

                    //{
                    //    ArrowOut ao = arrowCom.arrowData[i];
                    //    arrow[i].transform.position = ao.pos.xyz;
                    //    arrow[i].transform.rotation = new quaternion(ao.rot);
                    //    arcLength = ao.pos.w;
                    //
                    //    ArrowConst ad = arrowConstData[i];
                    //    ad.p1 = target[i].transform.position;
                    //    ad.pi = arrow[i].transform.position;
                    //
                    //    u = u + da_du * Time.deltaTime / arcLength;
                    //    ad.u = u;
                    //
                    //    arrowConstData[i] = ad;
                    //}

                    if(tActor[i].isActive)
                    {
                        ArrowOut ao = arrowCom.arrowData[i];
                        arrowTr[i].position = ao.pos.xyz;
                        arrowTr[i].rotation = new quaternion(ao.rot);
                        arcLength = ao.pos.w;

                        ArrowConst ad = arrowConstData[i];
                        ad.p1 = tTr.position + new Vector3(0.0f, tTr.localScale.y * yOffset, 0.0f);
                        ad.pi = arrowTr[i].position;

                        //u = u + da_du * Time.deltaTime / arcLength;
                        u = u + da_du * Runner.DeltaTime / arcLength;

                        ad.u = u;

                        arrowConstData[i] = ad;
                    }
                    else
                    {
                        ArrowConst ad = arrowConstData[i];
                        ad.active = false;
                        ad.u = 0.0f;
                        arrowConstData[i] = ad;

                        u = 1.5f;
                        break;
                    }
                }
                u = 0.0f;

                //if ((tActor[i].isActive || tActor[i].stateData < 3 ) && arrowConstData[i].active )
                if (tActor[i].isActive && arrowConstData[i].active)
                {
                    //tActor[i].DamageHp(sActor[i].hitHp);

                    if (Object.HasStateAuthority)
                    {
                        tActor[i].RPC_DamageHp(sActor[i].hitHp);
                    }

                    //arrowActor[i].AudioPlay();
                }

                {
                    ArrowConst ad = arrowConstData[i];
                    ad.u = u;
                    ad.active = false;
                    arrowConstData[i] = ad;
                }
            }

            yield return null;
        }
    }

    IEnumerator ArrowAction(int i)
    {
        float u; ;
        float da_du = 8.0f;
        float arcLength = 10.0f;
        //da_du *= ((1.0f / Runner.DeltaTime) / 60.0f);
        //da_du *= (1.0f / 60.0f);
        //da_du *= ((Runner.DeltaTime) * (Runner.DeltaTime * 60.0f));

        float yOffset = 1.5f;
        Transform tTr = null;
        bool bTarget_lost = false;

        while (true)
        {
            if (arrowConstData[i].active)
            {
                //{
                //    ArrowConst ad = arrowConstData[i];
                //    ad.p0 = shooter[i].transform.position;
                //    ad.p1 = target[i].transform.position;
                //    ad.pi = arrow[i].transform.position;
                //    u = ad.u;
                //    arrowConstData[i] = ad;
                //}
                
                {
                    tTr = targetTr[i];

                    ArrowConst ad = arrowConstData[i];
                    ad.p0 = shootTr[i].position;
                    ad.p1 = tTr.position + new Vector3(0.0f, tTr.localScale.y * yOffset, 0.0f);
                    ad.pi = arrowTr[i].position;
                    u = ad.u;
                    arrowConstData[i] = ad;
                }

                while (0.0f <= u && u <= 1.0f)
                {
                    yield return null;

                    //{
                    //    ArrowOut ao = arrowCom.arrowData[i];
                    //    arrow[i].transform.position = ao.pos.xyz;
                    //    arrow[i].transform.rotation = new quaternion(ao.rot);
                    //    arcLength = ao.pos.w;
                    //
                    //    ArrowConst ad = arrowConstData[i];
                    //    ad.p1 = target[i].transform.position;
                    //    ad.pi = arrow[i].transform.position;
                    //
                    //    u = u + da_du * Time.deltaTime / arcLength;
                    //    ad.u = u;
                    //
                    //    arrowConstData[i] = ad;
                    //}

                    if(tActor[i].state == UnitActor.ActionState.Die)
                    {
                        bTarget_lost = true;
                    }

                    //if (tActor[i].isActive)
                    {
                        ArrowOut ao = arrowCom.arrowData[i];
                        
                        arrowTr[i].position = ao.pos.xyz;
                        arrowTr[i].rotation = new quaternion(ao.rot);
                        arcLength = ao.pos.w;

                        ArrowConst ad = arrowConstData[i];

                        if(!bTarget_lost)                        
                        {
                            ad.p1 = tTr.position + new Vector3(0.0f, tTr.localScale.y * yOffset, 0.0f);
                        }
                        
                        ad.pi = arrowTr[i].position;

                        u = u + da_du * Time.deltaTime / arcLength;
                        //u = u + da_du * Runner.DeltaTime / arcLength;
                        //u = u + da_du * Time.fixedDeltaTime / arcLength;
                        //u = u + da_du / arcLength; 

                        ad.u = u;

                        arrowConstData[i] = ad;
                    }                   
                   
                }
                u = 0.0f;

                //if ((tActor[i].isActive || tActor[i].stateData < 3 ) && arrowConstData[i].active )
                if (tActor[i].isActive && arrowConstData[i].active && !bTarget_lost)
                {
                    //tActor[i].DamageHp(sActor[i].hitHp);

                    if (Object.HasStateAuthority)
                    {
                        tActor[i].RPC_DamageHp(sActor[i].hitHp);
                        tActor[i].RPC_Play_EffectAudio((int)UnitActor.Type_Audio.Arrow);
                    }

                    //arrowActor[i].AudioPlay();
                }

                {
                    ArrowConst ad = arrowConstData[i];
                    ad.u = u;
                    ad.active = false;
                    arrowConstData[i] = ad;

                    bTarget_lost = false;
                }
            }

            yield return null;
        }
    }

    void Start()
    {
        
    }

    COBuffer<float4> arrowColor_Buffer;
    static ROBuffer<int> pid_Buffer;

    void InitRenderArrow()
    {               
        {
            mte = new Material(gshader);
            mpb = new MaterialPropertyBlock();          

            Mesh mesh = arrowCom.mesh;
            List<int> idx = new List<int>();
            mesh.GetIndices(idx, 0);
            idxCount = idx.Count;

            idxBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, idxCount, sizeof(int));
            idxBuffer.SetData(idx.ToArray());

            mpb.SetBuffer("vtxDynamic", arrowCom.vOutBuffer.value);
            mpb.SetInt("dvCount", arrowCom.dvCount);
            mpb.SetInt("vtxCount", arrowCom.vtxCount);
            mpb.SetBuffer("arrowConst", arrowCom.arrowConstBuffer.value);
            mpb.SetColor("arrowColor", new Color(1.0f, 0.0f, 0.0f, 1.0f));
        }

        {
            int _count = GameManager.playerColor.Length;
            arrowColor_Buffer = new COBuffer<float4>(_count);
            for(int i = 0; i <_count; i++)
            {
                arrowColor_Buffer.data[i] = (float4)((Vector4)(GameManager.playerColor[i]));
            }
            arrowColor_Buffer.Write();

            mpb.SetBuffer("arrowColor_Buffer", arrowColor_Buffer.value);
        }

        {
            pid_Buffer = new ROBuffer<int>(cCount);
            for(int i = 0; i < cCount; i++)
            {
                pid_Buffer.data[i] = 0;
            }
            pid_Buffer.Write();

            mpb.SetBuffer("pid_Buffer", pid_Buffer.value);            
        }

        //passCSM = mte.FindPass("ArrowDepth_GS");
        passColor = mte.FindPass("ArrowColor");
        pass_gbuffer = mte.FindPass("ArrowColor_GBuffer");


        InitLightData();
    }

    CSM_Action csm_action;
    CBM_Action cbm_action;

    int pass_csm;
    int pass_cbm;

    void InitLightData()
    {
        {
            csm_action = LightManager.instance.csm_action;
            cbm_action = LightManager.instance.cbm_action;
        }

        {
            {
                pass_csm = mte.FindPass("ArrowDepth_CSM");
                pass_cbm = mte.FindPass("ArrowDepth_CBM");
            }

            {
                csm_action.Bind_Data(mpb);
                cbm_action.Bind_Data(mpb);
            }
        }



        {
            int _count = csm_action.csmCount;
        }

        {
            mpb.SetBuffer("csmDataBuffer", csm_action.csmDataBuffer.value);

            {
                mpb.SetTexture("csmTexArray", csm_action.renTex);
            }

            bool bArray = true;
            {
                if (bArray)
                {
                    mpb.SetInt("bArray", 1);
                }
                else
                {
                    mpb.SetInt("bArray", 0);
                }

                mpb.SetInt("csmWidth", csm_action.csmSize);
                mpb.SetInt("csmHeight", csm_action.csmSize);
                mpb.SetFloat("specularPow", 2.0f);
            }
        }
    }


    protected ColliderRender[] colRenders;


    void EnableColRender()
    {
        if (colRenders != null)
        {
            for (int i = 0; i < colRenders.Length; i++)
            {
                {
                    colRenders[i].Enable();
                }
            }
        }
    }

    void DisableColRender()
    {
        if (colRenders != null)
        {
            for (int i = 0; i < colRenders.Length; i++)
            {
                {
                    colRenders[i].Disable();
                }
            }
        }
    }

    protected IEnumerator UpdateColRender()
    {
        while (true)
        {
            if (colRenders != null)
            {                
                {
                    colRenders[0].bRender = GameManager.bColDebug[1];
                }
            }

            yield return null;
        }
    }

    void DestroyColRender()
    {
        if (colRenders != null)
        {
            for (int i = 0; i < colRenders.Length; i++)
            {
                {
                    colRenders[i].Destroy();
                }
            }
        }
    }

    protected virtual void InitColliderRendering()
    {
        {
            colRenders = new ColliderRender[1];
                        
            colRenders[0] = new ColliderRender(arrowTr, ColliderRender.Type.Sphere);
            colRenders[0].Init(gshader_col, cshader_col);           
        }
    }

    int cullIdx;

    int cullOffset
    {
        get
        {
            return CullManager.cullOffsets[cullIdx];
        }
    }    

    public void SetCullData(int cullIdx, RenderTexture pvf_tex, RenderTexture ovf_tex)
    {
        if (cCount > 0)
        {
            this.cullIdx = cullIdx;

            {
                mpb.SetInt("cullOffset", cullOffset);
                mpb.SetTexture("cullResult_pvf_Texture", pvf_tex);
                mpb.SetTexture("cullResult_ovf_Texture", ovf_tex);
            }
        }
    }

    private void RenderCSM(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {
        if (LightManager.type == LightType.Directional)
        {
            cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, mte, pass_csm, MeshTopology.Triangles, idxBuffer.count, cCount, mpb);
        }
    }

    private void RenderCBM(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {
        if (LightManager.type == LightType.Point || LightManager.type == LightType.Spot)
        {
            cbm_action.Update_Data(mpb);
            cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, mte, pass_cbm, MeshTopology.Triangles, idxBuffer.count, cCount, mpb);
        }
    }

    private void Render_GBuffer(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        //{
        //    float4x4 CV_cam = math.mul(RenderUtil.GetCfromV(cam, false), perCam.V);
        //
        //    {
        //        csm_action.Update_Data(mpb, CV_cam);
        //    }
        //}

        //{
        //    mpb.SetBuffer("arrowConst", arrowCom.arrowConstBuffer.value);
        //}


        perCam.V = RenderUtil.GetVfromW(cam);
        {
            mpb.SetMatrix("V", perCam.V);
            mpb.SetMatrix("CV", perCam.CV);
            mpb.SetVector("dirW_view", (Vector3)perCam.dirW_view);
            mpb.SetVector("posW_view", (Vector3)perCam.posW_view);
            mpb.SetVector("dirW_light", -(Vector3)csm_action.dirW.xyz);

            pid_Buffer.Write();

            cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, mte, pass_gbuffer, MeshTopology.Triangles, idxBuffer.count, cCount, mpb);
        }
    }




    private void Render0(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM.PerCamera perCam)
    {
        perCam.V = RenderUtil.GetVfromW(cam);
        {
            mpb.SetMatrix("CV_view", math.mul(RenderUtil.GetCfromV(cam, false), perCam.V));
            mpb.SetMatrix("CV", perCam.CV);
            mpb.SetMatrixArray("TCV_light", csm_action.TCV_depth);
            mpb.SetFloatArray("endZ", csm_action.endZ);
            mpb.SetVector("dirW_view", (Vector3)perCam.dirW_view);
            mpb.SetVector("posW_view", (Vector3)perCam.posW_view);
            mpb.SetVector("dirW_light", -(Vector3)csm_action.dirW.xyz);

            pid_Buffer.Write();

            cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, mte, passColor, MeshTopology.Triangles, idxBuffer.count, cCount, mpb);
        }
    }

    private void Render(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM.PerCamera perCam)
    {
        {
            float4x4 CV_cam = math.mul(RenderUtil.GetCfromV(cam, false), perCam.V);

            {
                csm_action.Update_Data(mpb, CV_cam);
            }
        }


        perCam.V = RenderUtil.GetVfromW(cam);
        {
            //mpb.SetMatrix("CV_view", math.mul(RenderUtil.GetCfromV(cam, false), perCam.V));
            mpb.SetMatrix("CV", perCam.CV);
            //mpb.SetMatrixArray("TCV_light", csm_action.TCV_depth);
            //mpb.SetFloatArray("endZ", csm_action.endZ);
            mpb.SetVector("dirW_view", (Vector3)perCam.dirW_view);
            mpb.SetVector("posW_view", (Vector3)perCam.posW_view);
            mpb.SetVector("dirW_light", -(Vector3)csm_action.dirW.xyz);

            pid_Buffer.Write();

            cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, mte, passColor, MeshTopology.Triangles, idxBuffer.count, cCount, mpb);
        }
    }

    




    // Update is called once per frame
    void Update()
    {
        
    }
    
    IEnumerator ac;
    IEnumerator[] aes;   

    public static float da_du;
   

    int pass;
    Material mte;
    MaterialPropertyBlock mpb;

    GraphicsBuffer idxBuffer;
    int idxCount;

    public Light light;
    

    //CSMAction csmAction;
    int csmCount;
    RenderTexture drenTexs;
    Texture2DArray drenTexArray;
    int passCSM;
    int passColor;
    int pass_gbuffer;

    int dw;
    int dh;   
    float specularPow;   

    class ArrowCompute
    {
        public IEnumerator Compute()
        {
            InitCShader();
            while (true)
            {
                //PreCompute().MoveNext();
                //if (GamePlay.isResume)
                while (GameManager.isPause)
                {
                    yield return null;
                }


                {
                    PreCompute();

                    WriteToCShader();
                    ExecuteCShader();
                    ReadFromCShader();

                    PostCompute();
                }

                yield return null;
                
                //yield return new WaitForFixedUpdate();
            }
            ReleaseCShader();
        }

        public void Compute_Fixed()
        {
            {
                PreCompute();

                WriteToCShader();
                ExecuteCShader();
                ReadFromCShader();

                PostCompute();
            }
        }

        //public Func<IEnumerator> PreCompute;
        public Action PreCompute = () => { };
        public Action PostCompute = () => { };


        ComputeShader cshader;
        int ki_curve;
        int ki_bone;
        int ki_vertex;

        public Mesh mesh
        {
            get; private set;
        }

        public int bCount
        {
            get; private set;
        }
        int cCount;
        public int vtxCount
        {
            get; private set;
        }

        int boCount;

        const int vtCount = 1024;
        int vgCount;
        int dpCount;
        public int dvCount
        {
            get; private set;
        }
        int voCount;

        Vector4 countInfo;        

        public ROBuffer<ArrowConst> arrowConstBuffer
        {
            get; private set;
        }
        COBuffer<float> boneZBuffer;
        COBuffer<VertexIn> vInBuffer;

        RWBuffer<ArrowCurveOut> curvesBuffer;
        RWBuffer<ArrowBoneOut> bonesBuffer;
        RWBuffer<ArrowOut> arrowDataBuffer;

        public RWBuffer<VertexOut> vOutBuffer
        {
            get; private set;
        }


        public ArrowConst[] arrowConstData
        {
            get; private set;
        }
        float[] boneZData;
       

        public ArrowOut[] arrowData
        {
            get; private set;
        }       

        public void Init(ComputeShader cshader, ArrowConst[] arrowConstData)
        {
            this.cshader = cshader;
            ki_curve = cshader.FindKernel("ArrowCurveCompute");
            ki_bone = cshader.FindKernel("ArrowBoneCompute");
            ki_vertex = cshader.FindKernel("ArrowVertexCompute");

            this.cCount = arrowConstData.Length;

            List<float> bz;
            mesh = RenderUtil.CreateSphereMesh_ForArrow(1.0f, 12, 24, out bz);
            //mesh = RenderUtil.CreateSphereMesh_ForArrow(1.0f, 6, 6, out bz);
            this.boneZData = bz.ToArray();

            this.bCount = boneZData.Length;
            this.vtxCount = mesh.vertexCount;

            this.boCount = bCount * cCount;

            this.vgCount = (vtxCount % vtCount == 0) ? (vtxCount / vtCount) : (vtxCount / vtCount + 1);
            //this.dvCount = vgCount * vtCount;
            //this.dpCount = vgCount * cCount;
            this.voCount = vtxCount * cCount;

            countInfo = new Vector4();           
            countInfo[0] = vtxCount;            
            countInfo[1] = bCount;        

            {
                arrowConstBuffer    = new ROBuffer<ArrowConst>(cCount);
                boneZBuffer         = new COBuffer<float>(bCount);
                vInBuffer           = new COBuffer<VertexIn>(vtxCount);

                curvesBuffer        = new RWBuffer<ArrowCurveOut>(cCount);
                bonesBuffer         = new RWBuffer<ArrowBoneOut>(boCount);
                arrowDataBuffer     = new RWBuffer<ArrowOut>(cCount      );
                vOutBuffer          = new RWBuffer<VertexOut>(voCount    );
            }

            {
                this.arrowConstData = arrowConstData;               
                arrowData = arrowDataBuffer.data;     
            }
        }

        public void InitCShader()
        {
            {                
                boneZBuffer.data = boneZData;
                boneZBuffer.Write();
            }

            {
                List<Vector3> posList = new List<Vector3>();
                List<Vector3> normalList = new List<Vector3>();
                List<Vector4> tangentList = new List<Vector4>();
                List<Vector4> biList = new List<Vector4>();
                mesh.GetVertices(posList);
                mesh.GetNormals(normalList);
                mesh.GetTangents(tangentList);
                mesh.GetUVs(4, biList);

                var data = vInBuffer.data;
                for (int i = 0; i < vtxCount; i++)
                {
                    data[i].posL = posList[i];
                    data[i].normalL = normalList[i];
                    data[i].tangentL = tangentList[i];
                    data[i].boneI = new int4((int)(biList[i].x), 0, 0, 0);
                }

                vInBuffer.Write();
            }

            {
                var data = bonesBuffer.data;
                for (int i = 0; i < cCount; i++)
                {
                    for (int j = 0; j < bCount; j++)
                    {
                        data[i * bCount + j].cIndex = i;
                        data[i * bCount + j].bIndex = j;
                    }
                }

                bonesBuffer.Write();
            }

            {
                cshader.SetBuffer(ki_curve, "arrowConst",   arrowConstBuffer.value    );
                cshader.SetBuffer(ki_curve, "arrowData",    arrowDataBuffer .value    );
                cshader.SetBuffer(ki_curve, "curves",       curvesBuffer    .value    );

                cshader.SetBuffer(ki_bone, "arrowConst",    arrowConstBuffer.value);
                cshader.SetBuffer(ki_bone, "curves",        curvesBuffer    .value);
                cshader.SetBuffer(ki_bone, "boneZ",         boneZBuffer     .value);
                cshader.SetBuffer(ki_bone, "arrowData",     arrowDataBuffer .value);
                cshader.SetBuffer(ki_bone, "bones",         bonesBuffer     .value);

                cshader.SetBuffer(ki_vertex, "arrowConst",  arrowConstBuffer.value);
                cshader.SetBuffer(ki_vertex, "bones",       bonesBuffer     .value);
                cshader.SetBuffer(ki_vertex, "vIn",         vInBuffer       .value);
                cshader.SetBuffer(ki_vertex, "vOut",        vOutBuffer      .value);
            }
        }

        void WriteToCShader()
        {
            {
                var data = arrowConstBuffer.data;
                for (int i = 0; i < cCount; i++)
                {
                    data[i] = arrowConstData[i];
                }
                arrowConstBuffer.Write();
            }
        }       

        void ExecuteCShader()
        {
            cshader.SetVector("countInfo", countInfo);

            CommandBuffer cmd = CommandBufferPool.Get();

            cmd.DispatchCompute(cshader, ki_curve, cCount, 1, 1);
            cmd.DispatchCompute(cshader, ki_bone, cCount, 1, 1);
            cmd.DispatchCompute(cshader, ki_vertex, cCount, vgCount, 1);

            Graphics.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void ReadFromCShader()
        {                  
            bool bDebug = false;
            if(bDebug)
            {
                CommandBuffer cmd = CommandBufferPool.Get();

                curvesBuffer        .Read(cmd);
                bonesBuffer         .Read(cmd);
                vOutBuffer          .Read(cmd);
                arrowDataBuffer     .Read(cmd);

                Graphics.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            {                
                arrowDataBuffer.Read();
            }          
        }

        public void ReleaseCShader()
        {
            if (arrowConstBuffer != null) arrowConstBuffer.Release();
            if (boneZBuffer != null) boneZBuffer.Release();
            if (vInBuffer != null) vInBuffer.Release();
            if (curvesBuffer != null) curvesBuffer.Release();
            if (bonesBuffer != null) bonesBuffer.Release();
            if (arrowDataBuffer != null) arrowDataBuffer.Release();
            if (vOutBuffer != null) vOutBuffer.Release();
        }

    }
    

    [System.Serializable]
    public struct ArrowConst
    {
        public bool active;

        public float u;
        public float3 sca;

        public float3 pi;
        public float3 p0;
        public float3 p1;
    }

    
    struct ArrowCurveOut
    {
        public float3 dp0;
        public float3 dp1;

        public float arcLength;
        public float4x4 L;
    }

    [System.Serializable]
    public struct ArrowBoneOut
    {
        public int cIndex;
        public int bIndex;

        public float4x4 bone;
        public float4x4 boneIT;

        public float3 pos;
        public float4 rot;
    }

    struct ArrowOut
    {
        public float4 pos;
        public float4 rot;
    };

    struct VertexIn
    {
        public float3 posL;
        public float3 normalL;
        public float4 tangentL;
        public int4 boneI;
        public float4 boneW;
    }

    struct VertexOut
    {
        public float3 posW;
        public float3 normalW;
        public float4 tangentW;
        public int4 boneI;
    }
   
}

