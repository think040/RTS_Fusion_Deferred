using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Jobs;
using UnityEngine.UI;
using UnityEngine.AI;

using UserAnimSpace;

using Fusion;

public class UnitManager : MonoBehaviour
{
    public virtual void Init(int _count)
    {
        //count = size * size;
        count = _count;

        if (count > 0)
        {
            InitData();
            InitBone();
            InitSkinMeshShader();
            InitStaticMeshShader();
            InitCSM();
            InitRendering();
            InitArray();
        }
    }
    

    public virtual void Spawn<T>() where T : UnitActor
    {
        if (count > 0)
        {
            float3 s = unit.transform.localScale;
            float dx = 2.5f * s.x;
            float dz = 2.5f * s.z;
            float3 xaxis = math.rotate(transform.rotation, new float3(1.0f, 0.0f, 0.0f));
            float3 zaxis = math.rotate(transform.rotation, new float3(0.0f, 0.0f, 1.0f));
            float3 center = transform.position;

            int c = 8;
            //c = 16;       //256
            //c = 32;     //1024
            //c = 64;     //4096
            //c = 128;    //16384

            c = (int)math.ceil(math.sqrt((float)count));

            if (c > 1)
            {
                float h = (float)(c - 1) * (-0.5f);
                center = center + (xaxis * dx + zaxis * dz) * h;
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = new Vector3((float)(i % c) * dx, 0.0f, (float)(i / c) * dz);
                pos = center + xaxis * pos.x + zaxis * pos.z;

                units[i] = GameObject.Instantiate(unit, pos, transform.rotation);
                units[i].SetActive(true);


                trs[i] = units[i].transform;
                hitTrs[i] = units[i].GetComponentInChildren<HitActor>().transform;
                anims[i] = units[i].GetComponent<UserAnimation>();
                anims[i].Init1(bns0[i], dicCurves, i);

                unitActors[i] = units[i].GetComponent<UnitActor>();
                unitActors[i].unitIdx = unitIdx;
                unitActors[i].iid = i;
                unitActors[i].offsetIdx = offsetIdx;
                {
                    unitActors[i].vRadius = GameManager.viewRadiusDef[unitIdx];
                    unitActors[i].aRadius = GameManager.attackRadiusDef[unitIdx];
                }
                (unitActors[i] as T).Init(stNames, trM[i], hasStMesh);
                unitActors[i].unitMan = this;
            }

            {
                SendDataToGameManager();
            }

            {
                InitAnim();
                BakeAnimation();
            }
        }
    }

    //Fusion
    public virtual void Init_Fusion(int _count)
    {
        //count = size * size;
        count = _count;

        if (count > 0)
        {
            InitData();
            InitBone();
            InitSkinMeshShader();
            InitStaticMeshShader();
            InitLight();
            InitRendering();
            InitArray();
        }

        {
            //bMergeNormal_sk = false;
            //bMergeNormal_st = false;

            bMergeNormal_sk = true;
            bMergeNormal_st = true;
        }

        {
            bTess_sk = true;
            bTess_st = false;

            bTess_sk_csm = true;
            bTess_st_csm = false;

            bTess_sk_cbm = true;
            bTess_st_cbm = false;
        }

        {
            tFactor_sk = new float4(1.0f, 4.0f, 4.0f, 4.0f);
            tFactor_st = new float4(1.0f, 1.0f, 1.0f, 1.0f);
        }

        {
            bRenderWire = false;
        }
    }

    public virtual void Spawn_Fusion(NetworkRunner runner)
    {

    }

    protected void Spawn_Fusion_T<T>(NetworkRunner runner) where T : UnitActor
    {
        if(!runner.IsServer)
        {
            return;
        }

        if (count > 0)
        {            
            float3 s = unit.transform.localScale;
            float dx = 2.5f * s.x;
            float dz = 2.5f * s.z;
            float3 xaxis = math.rotate(transform.rotation, new float3(1.0f, 0.0f, 0.0f));
            float3 zaxis = math.rotate(transform.rotation, new float3(0.0f, 0.0f, 1.0f));
            float3 center = transform.position;

            int c = 8;
            //c = 16;       //256
            //c = 32;     //1024
            //c = 64;     //4096
            //c = 128;    //16384

            c = (int)math.ceil(math.sqrt((float)count));

            if (c > 1)
            {
                float h = (float)(c - 1) * (-0.5f);
                center = center + (xaxis * dx + zaxis * dz) * h;
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = new Vector3((float)(i % c) * dx, 0.0f, (float)(i / c) * dz);
                pos = center + xaxis * pos.x + zaxis * pos.z;

                Ray ray = new Ray(pos + new Vector3(0.0f, 100.0f, 0.0f), Vector3.down);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 500, LayerMask.GetMask("Terrain")))
                {
                    pos.y = hit.point.y;
                }

                runner.Spawn(unit, pos, transform.rotation);                
            }          
        }
    }

    public int idx_add = 0;

    public void AddUnit(GameObject unit)
    {
        units[idx_add] = unit;
        idx_add++;
    }

    public virtual void Spawn_Fusion_Post(NetworkRunner runner, int unitIdx)
    {

    }

    public PlayerRef player { get; set; } = PlayerRef.None;

    NetworkRunner runner;

    protected void Spawn_Fusion_Post_T<T>(NetworkRunner runner, int unitIdx) where T : UnitActor
    {
        this.runner = runner;

        if (count > 0)
        {
            //this.unitIdx = unitIdx;

            for (int i = 0; i < count; i++)
            {                
                units[i].SetActive(true);                

                trs[i] = units[i].transform;                
                hitTrs[i] = units[i].GetComponentInChildren<HitActor>().transform;
                anims[i] = units[i].GetComponent<UserAnimation>();
                anims[i].Init1(bns0[i], dicCurves, i);

                unitActors[i] = units[i].GetComponent<UnitActor>();
                //unitActors[i].unitIdx = unitIdx;
                unitActors[i].unitIdx = this.unitIdx;
                unitActors[i].iid = i;
                unitActors[i].offsetIdx = offsetIdx;
                {
                    unitActors[i].vRadius = GameManager.viewRadiusDef[unitIdx];
                    unitActors[i].aRadius = GameManager.attackRadiusDef[unitIdx];
                }
                (unitActors[i] as T).Init(stNames, trM[i], hasStMesh);
                unitActors[i].unitMan = this;

                ntTrs[i] = unitActors[i].GetBehaviour<NetworkTransform>();
            }
          

            {
                SendDataToGameManager();
            }

            {
                InitAnim();
                BakeAnimation();
            }
        }
    }



    public virtual void Begin()
    {
        if (count > 0)
        {
            BeginRendering();

            for (int i = 0; i < count; i++)
            {
                unitActors[i].Begin();
            }
        }
    }


    public void Enable()
    {
        if (count > 0)
        {
            RenderGOM_DF.BeginFrameRender += BeginFrameRender;

            RenderGOM_DF.RenderCSM += RenderCSM;
            RenderGOM_DF.RenderCBM += RenderCBM;
            //RenderGOM.OnRenderCamAlpha += Render;
            DeferredRenderManager.OnRender_GBuffer += Render_GBuffer;
            DeferredRenderManager.OnRender_Transparent += Render_Transparent;

            RenderGOM_DF.OnRenderCamAlpha += Render_Wire;

            EnableColRender();
        }
    }

    public void Disable()
    {
        if (count > 0)
        {
            RenderGOM_DF.BeginFrameRender -= BeginFrameRender;

            RenderGOM_DF.RenderCSM -= RenderCSM;
            RenderGOM_DF.RenderCBM -= RenderCBM;
            //RenderGOM.OnRenderCamAlpha -= Render;
            DeferredRenderManager.OnRender_GBuffer -= Render_GBuffer;
            DeferredRenderManager.OnRender_Transparent -= Render_Transparent;

            RenderGOM_DF.OnRenderCamAlpha -= Render_Wire;

            DisableColRender();
        }
    }

    public KeyCode key_spawn;

    //public void FixedUpdate()
    //{
    //    
    //}

    
    //public virtual void Update()
    //{
    //
    //}


    //
    //public void FixedUpdate()
    //public void UpdateBone()
    public virtual void Update()
    {
        if (!GameManager.bUpdate)
        {
            return;
        }
       
        if(GameManager.isPause)
        {
            return;
        }


        if (count > 0)
        {
            UpdateSampleAnim();

            UpdateRootM();
            {
                boneCompute.Compute();
            }
            UpdateStaticM();

            UpdateLight();
            UpdateAnimSpeed();


            //if(unitActors[0].Object.HasInputAuthority)
            //{
            //    if (Input.GetKeyDown(key_spawn))
            //    {
            //        //SpawnTerrain();
            //        StartCoroutine(SpawnTerrainRoutine());
            //    }
            //}
            //

            {
                UpdateTessFactor();
            }

            {
                UpdateTessMode();
            }
        }
    }

    void UpdateTessMode()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            bRenderWire = !bRenderWire;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (bTess_sk) { tFactor_sk -= 1.0f; tFactor_sk = math.clamp(tFactor_sk, 1.0f, 4.0f); }
            if (bTess_st) { tFactor_st -= 1.0f; tFactor_st = math.clamp(tFactor_st, 1.0f, 4.0f); }
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (bTess_sk) { tFactor_sk += 1.0f; tFactor_sk = math.clamp(tFactor_sk, 1.0f, 4.0f); }
            if (bTess_st) { tFactor_st += 1.0f; tFactor_st = math.clamp(tFactor_st, 1.0f, 4.0f); }
        }


        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            bTess_sk = !bTess_sk;
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            bTess_st = !bTess_st;
        }


        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            bTess_sk_csm = !bTess_sk_csm;
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            bTess_st_csm = !bTess_st_csm;
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            bTess_sk_cbm = !bTess_sk_cbm;
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            bTess_st_cbm = !bTess_st_cbm;
        }

    }

    public float tessY;
    public float _tessDist;

    void UpdateTessFactor()
    {
        _tessDist = tessDist;

        bool bTessDist = true;

        if(bTessDist)
        {
            float4 tinfo = GameManager.unitTessInfo;
            float xmin = tinfo.z;
            float xmax = tinfo.w;
            float ymin = tinfo.x;
            float ymax = tinfo.y;
        
            float k = (ymin - ymax) / (xmax - xmin);
            float x = math.clamp(tessDist, xmin, xmax);
            float y = math.clamp(k * (x - xmin) + ymax, ymin, ymax);
        
            //float x = tessDist;
            //float y = k * (x - xmin) + ymax;
        
            y = math.floor(y);
            tessY = y;
        
            tFactor_sk = y * new float4(1.0f, 1.0f, 1.0f, 1.0f);
            //tFactor_st = y * new float4(1.0f, 1.0f, 1.0f, 1.0f);
        }



        {
            skMpb.SetVector("tFactor", tFactor_sk);
        }

        if (hasStMesh)
        {
            stMpb.SetVector("tFactor", tFactor_st);
        }
    }

    void OnDestroy()
    {
        if (vtxStatic != null) vtxStatic.Dispose();
        if (idxBuffer != null) idxBuffer.Dispose();
        if (idxBuffers != null)
        {
            for (int i = 0; i < idxBuffers.Length; i++)
            {
                idxBuffers[i].Dispose();
            }
        }
        
        if (hasStMesh)
        {
            if (stVtxBuffer != null) stVtxBuffer.Dispose();
            if (stIdxBuffer != null) stIdxBuffer.Dispose();
        }
        
        {
            rootWJob.Dispose();
            if (traa_rootW.isCreated) traa_rootW.Dispose();
        }
        
        {
            ReleaseAnim();
        }
        
        {
            if (vtxCompute != null) vtxCompute.ReleaseCShader();
            if (vtxComputeSt != null) vtxComputeSt.ReleaseCShader();
        }
        
        {
            DestroyColRender();
        }
    }


    public bool bRender { get; set; }= true;
    int size = 1;
    public int count{ get; set; }
    public GameObject unit;    
    public Model model;
    public UserAnimClip[] clips;

    public Transform rootTr;
    public Shader gshader;
    public ComputeShader cshader;
   
    public ComputeShader cshader_col;
    public Shader gshader_col;

    public Transform trLight;
    public Texture2D[] texes;

    bool bMergeNormal_sk = false;
    bool bMergeNormal_st = false;

    bool bTess_sk = true;
    bool bTess_st = true;

    float4 tFactor_sk = new float4(4.0f, 4.0f, 4.0f, 4.0f);
    float4 tFactor_st = new float4(1.0f, 1.0f, 1.0f, 1.0f);

    bool bRenderWire = false;

    bool bTess_sk_csm = false;
    bool bTess_st_csm = false;

    bool bTess_sk_cbm = false;
    bool bTess_st_cbm = false;

    Material skMte;
    MaterialPropertyBlock skMpb;

    Material stMte;
    MaterialPropertyBlock stMpb;

    float4x4 rootMat;

    
    string[] skNames;
    
    Mesh[] skMeshes;
    int skCount;

    string[] boneNames;
    BoneNode[] rootBns;
    protected BoneNode[][] bns0;
    
    BoneNode[][] skBns;
       

    protected Dictionary<string, BoneCurve[]> dicCurves;
        
    float4x4[] bindpose;
    float4x4[] finalW;    

    int stCount;
    string[] stNames;
    Mesh[] stMeshes;
    BoneNode[][] bnsSt;
    float4x4[][] trM;

    public float animSpeed = 1.0f;
       
    public void SpawnTerrain()
    {
        if(count > 0)
        {
            for(int i = 0; i < count; i++)
            {
                UnitActor actor = unitActors[i];
                if(actor.state == UnitActor.ActionState.Sleep)
                {
                    actor.isActive = true;
                    actor.state = UnitActor.ActionState.Idle;
                    actor.nvAgent.enabled = true;
                    actor.bodyCollider.enabled = true;
                    actor.hitCollider.enabled = true;                    

                    actor.transform.position = transform.position;
                    actor.targetPos = transform.position;                   
                    actor.ClearAttackTr();
                    actor.Hp = actor.maxHp;
                    //actor.positionTr = null;

                    break;
                }
            }
        }
    }

    public IEnumerator SpawnTerrainRoutine0()
    {
        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                UnitActor actor = unitActors[i];
                if (actor.state == UnitActor.ActionState.Sleep)
                {
                    float3 sPos = transform.position;

                    Ray ray = new Ray(transform.position + new Vector3(0.0f, 50.0f, 0.0f), Vector3.down);
                    RaycastHit hit;
                    if(Physics.Raycast(ray, out hit, 100.0f, LayerMask.GetMask("Terrain")))
                    {
                        sPos.y = hit.point.y;
                    }
                    
                    actor.targetPos = sPos;
                    actor.stateData = (int)(UnitActor.ActionState.ReSpawn);

                    yield return new WaitForSeconds(1.0f);
                    //yield return null;

                    actor.transform.position = actor.targetPos;
                    actor.isActive = true;
                    actor.nvAgent.enabled = true;
                    actor.Hp = actor.maxHp;

                    actor.stateData = (int)(UnitActor.ActionState.Idle);
                   
                    actor.bodyCollider.enabled = true;
                    actor.hitCollider.enabled = true;
                                       
                    actor.ClearAttackTr();
                                        
                    break;
                }
            }
        }
    }


    public IEnumerator SpawnTerrainRoutine()
    {
        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                UnitActor actor = unitActors[i];
                if (actor.state == UnitActor.ActionState.Sleep)
                {
                    float3 sPos = transform.position;

                    Ray ray = new Ray(transform.position + new Vector3(0.0f, 50.0f, 0.0f), Vector3.down);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 100.0f, LayerMask.GetMask("Terrain")))
                    {
                        sPos.y = hit.point.y;
                    }

                    //yield return new WaitForSeconds(1.0f);

                    //actor.targetPos = sPos;
                    //actor.ntTransform.WritePosition(sPos);
                    //actor.ntTransform.TeleportToPosition(sPos);

                    //actor.stateData = (int)(UnitActor.ActionState.ReSpawn);
                    actor.RPC_SetStateData((int)(UnitActor.ActionState.ReSpawn));
                    //actor.RPC_Set_isActive(true);                   

                    yield return new WaitForSeconds(1.0f);
                    //yield return null;

                    actor.RPC_Set_Hp(actor.maxHp);                    
                    actor.RPC_Set_isActive(true);

                    //
                    //yield return new WaitForSeconds(1.0f);

                    //yield return new WaitForSeconds(1.0f);

                    //actor.transform.position = actor.targetPos;
                    //actor.transform.position = sPos;

                    //actor.targetPos = sPos;
                    actor.ntTransform.WritePosition(actor.targetPos);
                    //actor.ntTransform.TeleportToPosition(sPos);

                    actor.nvAgent.enabled = true;
                    //actor.Hp = actor.maxHp;                    
                   

                    //actor.stateData = (int)(UnitActor.ActionState.Idle);
                    actor.RPC_SetStateData((int)(UnitActor.ActionState.Idle));

                    actor.bodyCollider.enabled = true;
                    actor.hitCollider.enabled = true;
                    //actor.ntTransform.enabled = true;

                    actor.ClearAttackTr();

                    break;
                }
            }
        }
    }


    bool testPause = true;
    bool testReStart = true;

    //public Text txCountInfo;
    //public CSM_Action csm_action;
       
    IEnumerator TestAnim()
    {
        int id = 0;
        while (true)
        {
            for (int i = id; i < count; i++)
            {
                while (testPause)
                {
                    yield return null;

                    if (testReStart)
                    {
                        i = id = 0;
                    }
                }

                anims[i].PlayCross("Running");
                yield return new WaitForSeconds(0.25f);
            }

            yield return null;
            id = 0;
        }
    }
    

    public int offsetIdx
    {
        get { return GameManager.baseCount[unitIdx].x; }
    }   
    public int unitIdx
    {
        get; set;
    }
    

    public virtual void Spawn0<T>() where T : UnitActor
    {
        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                units[i] = GameObject.Instantiate(unit, float3.zero, quaternion.identity);

                trs[i] = units[i].transform;
                anims[i] = units[i].GetComponent<UserAnimation>();
                anims[i].Init1(bns0[i], dicCurves, i);

                units[i].SetActive(true);
                unitActors[i] = units[i].GetComponent<UnitActor>();
                unitActors[i].unitIdx = unitIdx;
                unitActors[i].iid = i;
                unitActors[i].offsetIdx = offsetIdx;
                (unitActors[i] as T).Init(stNames, trM[i], hasStMesh);

            }

            float3 s = unit.transform.localScale;
            float dx = 2.5f * s.x;
            float dz = 2.5f * s.z;
            float3 xaxis = math.rotate(transform.rotation, new float3(1.0f, 0.0f, 0.0f));
            float3 zaxis = math.rotate(transform.rotation, new float3(0.0f, 0.0f, 1.0f));
            float3 center = transform.position;

            int c = 8;
            //c = 16;       //256
            //c = 32;     //1024
            //c = 64;     //4096
            //c = 128;    //16384

            c = (int)math.ceil(math.sqrt((float)count));

            if (c > 1)
            {
                float h = (float)(c - 1) * (-0.5f);
                center = center + (xaxis * dx + zaxis * dz) * h;
            }

            for (int i = 0; i < count; i++)
            {

                Vector3 pos = new Vector3((float)(i % c) * dx, 0.0f, (float)(i / c) * dz);
                pos = center + xaxis * pos.x + zaxis * pos.z;
                units[i].transform.SetPositionAndRotation(pos, transform.rotation);
                units[i].SetActive(true);
            }

            {
                SendDataToGameManager();
            }

            {
                InitAnim();
                BakeAnimation();
            }
        }
    }

    void SendDataToGameManager()
    {
        int i0 = offsetIdx;

        for (int i = 0; i < count; i++)
        {
            GameManager.unitActors[i0 + i] = unitActors[i];
            GameManager.unitTrs[i0 + i] = unitActors[i].transform;
            GameManager.unitNtTrs[i0 + i] = ntTrs[i];
        }
    }


    int[] bCounts;
    int[] bBase;

    void InitData()
    {
        {
            skNames = model._skNames.ToArray();
            skCount = skNames.Length;


            skMeshes = new Mesh[skCount];
            bBase = new int[skCount + 1];
            bCounts = new int[skCount];

            bBase[0] = 0;
            for (int i = 0; i < skCount; i++)
            {
                skMeshes[i] = model.dicSkMesh[skNames[i]];
                bCounts[i] = model.dicSkBoneNames[skNames[i]].names.Length;
                bBase[i + 1] = bBase[i] + bCounts[i];
            }
            int _bCount = bBase[skCount];

            boneNames = new string[_bCount];
            for (int i = 0; i < skCount; i++)
            {
                for (int j = 0; j < bCounts[i]; j++)
                {
                    boneNames[bBase[i] + j] = model.dicSkBoneNames[skNames[i]].names[j];
                }
            }

        }

        if (model._stMesh.Count > 0)
        {
            hasStMesh = true;
        }

        if (hasStMesh)
        {
            stNames = model._stNames.ToArray();
            stMeshes = new Mesh[stNames.Length];
            for (int i = 0; i < stNames.Length; i++)
            {
                stMeshes[i] = model.dicStMesh[stNames[i]];
            }
            stCount = stNames.Length;
        }
    }

    float4x4[] orthoM;
    int[] boneIdx;   //21
    int[] boneIdx_st; //2
    float4[] boneSca_st;

    int boneCount;   //31
    int frameCount;  //16
    int clipCount;   //3

    int bmCount;     //21
    void InitBone()
    {
        rootBns = new BoneNode[count];
        for (int i = 0; i < count; i++)
        {
            rootBns[i] = new BoneNode();
        }

        {
            BoneNode.ConstructBones(rootTr, rootBns);
            bns0 = BoneNode.ToArrayArrayForCompute(rootBns);
            boneCount = bns0[0].Length;

            BoneNode.ToArrayForCompute(rootBns[0], out siblingCount, out depthCount, out bnCount, out idxMask, out idxParent, out idx, out idxMP);
        }

        {
            dicCurves = new Dictionary<string, BoneCurve[]>();
            BoneNode.BindBoneInfo(bns0[0]);
            for (int i = 0; i < clips.Length; i++)
            {
                string name = clips[i].name;
                BoneNode.BindBoneCurve(bns0[0], name, clips[i].curves);
                dicCurves[name] = BoneNode.ToBoneCurves(bns0[0], name);
            }

            frameCount = BoneCurve.dqc;
            clipCount = dicCurves.Count;
        }
      
        {
            skBns = new BoneNode[count][];
            for (int i = 0; i < count; i++)
            {
                skBns[i] = BoneNode.FindBones(bns0[i], boneNames, ref boneIdx);
            }

            bmCount = boneNames.Length;          
            bindpose = new float4x4[boneNames.Length];
            finalW = new float4x4[count * boneNames.Length];
         
            for (int i = 0; i < skCount; i++)
            {
                for (int j = 0; j < bCounts[i]; j++)
                {
                    bindpose[bBase[i] + j] = skMeshes[i].bindposes[j];
                }
            }

            //debug
            orthoM = BoneNode.CheckOrthoNormal(bindpose);

        }       

        if (hasStMesh)
        {

            bnsSt = new BoneNode[count][];
            trM = new float4x4[count][];
            for (int i = 0; i < count; i++)
            {
                bnsSt[i] = BoneNode.FindBones(bns0[i], stNames, ref boneIdx_st);
                trM[i] = new float4x4[bnsSt[i].Length];
                for (int j = 0; j < bnsSt[i].Length; j++)
                {
                    trM[i][j] = new float4x4();
                }
            }

            boneSca_st = new float4[stCount];
            for (int i = 0; i < stCount; i++)
            {
                if (bnsSt[0][i] != null)
                {
                    boneSca_st[i].xyz = bnsSt[0][i].transform.scaL;
                }
            }


            {
                stWCount = stCount * count;
                stW = new float4x4[stWCount];
                for (int i = 0; i < stW.Length; i++)
                {
                    stW[i] = float4x4.zero;
                }
            }
        }

        {
            rootMat = BoneNode.GetPfromC(rootTr.localPosition, rootTr.localRotation, rootTr.localScale);
        }

    }


    GraphicsBuffer[] idxBuffers;
    GraphicsBuffer vtxStatic;

    GraphicsBuffer idxBuffer;       

    SubMeshDescriptor[] smDescSk;
    int[][] sbIdxSk;
    VertexStatic[] vtxDataSk;

    void InitSkinMeshShader()
    {
        {
            skMte = new Material(gshader);
            skMpb = new MaterialPropertyBlock();
        }

        List<int> idxData = new List<int>();
        {
            SubMeshDescriptor[][] _smdesc = new SubMeshDescriptor[skCount][];
            int[][][] _idx = new int[skCount][][];

            int _baseVtx = 0;
            for (int i = 0; i < skCount; i++)
            {
                Mesh _mesh = skMeshes[i];
                _smdesc[i] = new SubMeshDescriptor[_mesh.subMeshCount];
                _idx[i] = new int[_mesh.subMeshCount][];
                for (int j = 0; j < _mesh.subMeshCount; j++)
                {
                    SubMeshDescriptor _smesh = _mesh.GetSubMesh(j);
                    _smdesc[i][j] = _smesh;
                    _idx[i][j] = _mesh.GetIndices(j);
                    for (int k = 0; k < _smesh.indexCount; k++)
                    {
                        int id = _baseVtx + _smesh.baseVertex + _idx[i][j][k];
                        //int id = _smesh.baseVertex + _idx[i][j][k];
                        idxData.Add(id);
                    }
                }
                _baseVtx += _mesh.vertexCount;
            }
        }

        List<VertexStatic> vtxData = new List<VertexStatic>();
        {
            for (int i = 0; i < skCount; i++)
            {
                Mesh _mesh = skMeshes[i];
                float4 _color = float4.zero;
                //if (_mesh.name == "WK_Horse" || _mesh.name == "WK_horse_Seat")
                if (_mesh.name == "WK_Horse")
                {
                    _color.x = 1.0f;
                }


                float2[][] uvs = new float2[2][];
                for (int j = 0; j < uvs.Length; j++)
                {
                    uvs[j] = new float2[_mesh.vertexCount];
                    for (int k = 0; k < _mesh.vertexCount; k++)
                    {
                        uvs[j][k] = float2.zero;
                    }
                }

                for (int j = 0; j < uvs.Length; j++)
                {
                    List<Vector2> _uvs = new List<Vector2>();
                    _mesh.GetUVs(j, _uvs);
                    for (int k = 0; k < _uvs.Count; k++)
                    {
                        uvs[j][k] = _uvs[k];
                    }
                }

                for (int j = 0; j < _mesh.vertexCount; j++)
                {
                    vtxData.Add(new VertexStatic { uv0 = uvs[0][j], uv1 = uvs[1][j], color = _color });
                }
            }
        }

        {
            idxBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, idxData.Count, sizeof(int));
            vtxStatic = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vtxData.Count, Marshal.SizeOf<VertexStatic>());

            idxBuffer.SetData(idxData.ToArray());
            vtxStatic.SetData(vtxData.ToArray());

            skMpb.SetBuffer("vtxStatic", vtxStatic);
        }

        {
            skMpb.SetInt("unitIdx", unitIdx);
        }
    }


    GraphicsBuffer stVtxBuffer;
    GraphicsBuffer stIdxBuffer;

    float4x4[] stW;
    int stWCount;

    bool hasStMesh = true;    

    SubMeshDescriptor[][] smDescSt;
    int[][][] sbIdxSt;
    VertexStatic[] vtxDataSt;

    void InitStaticMeshShader()
    {
        if (hasStMesh)
        {
            stMte = new Material(gshader);
            stMpb = new MaterialPropertyBlock();

            List<int> idxData = new List<int>();
            {
                SubMeshDescriptor[][] _smdesc = new SubMeshDescriptor[stCount][];
                int[][][] _idx = new int[stCount][][];

                int _baseVtx = 0;
                for (int i = 0; i < stCount; i++)
                {
                    Mesh _mesh = stMeshes[i];
                    _smdesc[i] = new SubMeshDescriptor[_mesh.subMeshCount];
                    _idx[i] = new int[_mesh.subMeshCount][];
                    for (int j = 0; j < _mesh.subMeshCount; j++)
                    {
                        SubMeshDescriptor _smesh = _mesh.GetSubMesh(j);
                        _smdesc[i][j] = _smesh;
                        _idx[i][j] = _mesh.GetIndices(j);
                        for (int k = 0; k < _smesh.indexCount; k++)
                        {
                            int id = _baseVtx + _smesh.baseVertex + _idx[i][j][k];
                            idxData.Add(id);
                        }
                    }
                    _baseVtx += _mesh.vertexCount;
                }
            }

            List<VertexStatic> vtxData = new List<VertexStatic>();
            {
                for (int i = 0; i < stCount; i++)
                {
                    Mesh _mesh = stMeshes[i];
                    float4 _color = float4.zero;
                    //if (_mesh.name == "WK_Horse" || _mesh.name == "WK_horse_Seat")
                    if (_mesh.name == "WK_Horse")
                    {
                        _color.x = 1.0f;
                    }

                    float2[][] uvs = new float2[2][];
                    for (int j = 0; j < uvs.Length; j++)
                    {
                        uvs[j] = new float2[_mesh.vertexCount];
                        for (int k = 0; k < _mesh.vertexCount; k++)
                        {
                            uvs[j][k] = float2.zero;
                        }
                    }

                    for (int j = 0; j < uvs.Length; j++)
                    {
                        List<Vector2> _uvs = new List<Vector2>();
                        _mesh.GetUVs(j, _uvs);
                        for (int k = 0; k < _uvs.Count; k++)
                        {
                            uvs[j][k] = _uvs[k];
                        }
                    }


                    for (int j = 0; j < _mesh.vertexCount; j++)
                    {
                        vtxData.Add(new VertexStatic { uv0 = uvs[0][j], uv1 = uvs[1][j], color = _color });
                    }
                }

                int a = 0;
            }

            {
                stIdxBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, idxData.Count, sizeof(int));
                stVtxBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vtxData.Count, Marshal.SizeOf<VertexStatic>());

                stIdxBuffer.SetData(idxData.ToArray());
                stVtxBuffer.SetData(vtxData.ToArray());

                stMpb.SetBuffer("vtxStatic", stVtxBuffer);
            }
        }

        {
            stMpb.SetInt("unitIdx", unitIdx);
        }
    }

    int pass_csm;
    int pass_cbm;

    int pass_tess_csm;
    int pass_tess_cbm;


    CSM_Action csm_action;
    CBM_Action cbm_action;

    void InitLight()
    {
        {
            csm_action = LightManager.instance.csm_action;
            cbm_action = LightManager.instance.cbm_action;
        }

        //
        {
            {
                pass_csm = skMte.FindPass("UnitAnimationDepth_CSM");
                pass_cbm = skMte.FindPass("UnitAnimationDepth_CBM");

                pass_tess_csm = skMte.FindPass("UnitAnimationDepth_Tess_CSM");
                pass_tess_cbm = skMte.FindPass("UnitAnimationDepth_Tess_CBM");
            }

            {
                csm_action.Bind_Data(skMpb);
                cbm_action.Bind_Data(skMpb);
            }

            if (hasStMesh)
            {
                csm_action.Bind_Data(stMpb);
                cbm_action.Bind_Data(stMpb);
            }
        }


        //{
        //    int _count = csm_action.csmCount;            
        //}
        //
        //{
        //    skMpb.SetBuffer("csmDataBuffer", csm_action.csmDataBuffer.value);
        //
        //    {
        //        skMpb.SetTexture("csmTexArray", csm_action.renTex);
        //    }
        //
        //    bool bArray = true;
        //    {
        //        if (bArray)
        //        {
        //            skMpb.SetInt("bArray", 1);
        //        }
        //        else
        //        {
        //            skMpb.SetInt("bArray", 0);
        //        }
        //
        //        skMpb.SetInt("csmWidth", csm_action.csmSize);
        //        skMpb.SetInt("csmHeight", csm_action.csmSize);
        //        skMpb.SetFloat("specularPow", 2.0f);
        //    }
        //}
        //
        //if (hasStMesh)
        //{
        //    stMpb.SetBuffer("csmDataBuffer", csm_action.csmDataBuffer.value);
        //
        //    {
        //
        //        stMpb.SetTexture("csmTexArray", csm_action.renTex);
        //    }
        //
        //    bool bArray = true;
        //    {
        //        if (bArray)
        //        {
        //            stMpb.SetInt("bArray", 1);
        //        }
        //        else
        //        {
        //            stMpb.SetInt("bArray", 0);
        //        }
        //
        //        stMpb.SetInt("csmWidth", csm_action.csmSize);
        //        stMpb.SetInt("csmHeight", csm_action.csmSize);
        //        stMpb.SetFloat("specularPow", 2.0f);
        //    }
        //}
    }
   

    void InitCSM()
    {
        {
            int _count = csm_action.csmCount;
            pass_csm = skMte.FindPass("UnitAnimationDepth");
        }

        {
            skMpb.SetBuffer("csmDataBuffer", csm_action.csmDataBuffer.value);

            {
                skMpb.SetTexture("csmTexArray", csm_action.renTex);
            }

            bool bArray = true;
            {
                if (bArray)
                {
                    skMpb.SetInt("bArray", 1);
                }
                else
                {
                    skMpb.SetInt("bArray", 0);
                }

                //skMpb.SetInt("csmWidth", csm_action.csmW);
                //skMpb.SetInt("csmHeight", csm_action.csmH);
                skMpb.SetFloat("specularPow", 2.0f);
            }
        }

        if (hasStMesh)
        {
            stMpb.SetBuffer("csmDataBuffer", csm_action.csmDataBuffer.value);

            {

                stMpb.SetTexture("csmTexArray", csm_action.renTex);
            }

            bool bArray = true;
            {
                if (bArray)
                {
                    stMpb.SetInt("bArray", 1);
                }
                else
                {
                    stMpb.SetInt("bArray", 0);
                }

                //stMpb.SetInt("csmWidth", csm_action.csmW);
                //stMpb.SetInt("csmHeight", csm_action.csmH);
                stMpb.SetFloat("specularPow", 2.0f);
            }
        }
    }

    public Transform[] trs
    {
        get; set;
    }

    public NetworkTransform[] ntTrs
    {
        get; set;
    }

    protected Transform[] hitTrs;
   
    public UserAnimation[] anims
    {
        get; set;
    }
    public GameObject[] units
    {
        get; set;
    }   
    public UnitActor[] unitActors
    {
        get; set;
    }

    public void InitArray()
    {
        units = new GameObject[count];
        trs = new Transform[count];
        hitTrs = new Transform[count];
        anims = new UserAnimation[count];
        //unitActors = new UnitActor[count];       
        unitActors = new UnitActor[count];

        ntTrs = new NetworkTransform[count];
    }


    int passColor;
    int pass_gbuffer;
    int pass_tess_gbuffer;
    int pass_tess_wire;
    int pass_tp;

    public int playerNum
    {
        get { return GameManager.playerNum[unitIdx]; }
    }

    public Color playerColor
    {
        get { return GameManager.playerColor[playerNum]; }
    }

   


    void InitRendering()
    {
        {
            passColor = skMte.FindPass("UnitAnimationColor");
            pass_gbuffer = skMte.FindPass("UnitAnimationColor_GBuffer");
            pass_tess_gbuffer = skMte.FindPass("UnitAnimationColor_Tess_GBuffer");
            pass_tess_wire = skMte.FindPass("UnitAnimationColor_Tess_Wire");
            pass_tp = skMte.FindPass("UnitAnimationColor_Tp");
        }

        {
            for (int i = 0; i < texes.Length; i++)
            {
                skMpb.SetTexture("tex_diffuse" + i.ToString(), texes[i]);
            }

            skMpb.SetBuffer("active_Buffer", GameManager.active_Buffer.value);
            skMpb.SetBuffer("state_Buffer", GameManager.state_Buffer.value);
            skMpb.SetInteger("offsetIdx", offsetIdx);            
            skMpb.SetVector("unitColor", (Vector4)playerColor);           
        }       

        if(hasStMesh)
        {
            for (int i = 0; i < texes.Length; i++)
            {                
                stMpb.SetTexture("tex_diffuse" + i.ToString(), texes[i]);
            }

            stMpb.SetBuffer("active_Buffer", GameManager.active_Buffer.value);
            stMpb.SetBuffer("state_Buffer", GameManager.state_Buffer.value);
            stMpb.SetInteger("offsetIdx", offsetIdx);
            stMpb.SetVector("unitColor", (Vector4)playerColor);
        }

        //RenderGOM.OnRenderCamAlpha += Render;
    }

    void BeginRendering()
    {        
        {            
            skMpb.SetVector("unitColor", (Vector4)playerColor);
        }

        if (hasStMesh)
        {           
            stMpb.SetVector("unitColor", (Vector4)playerColor);
        }

        //RenderGOM.OnRenderCamAlpha += Render;
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
        while(true)
        {
            if (colRenders != null)
            {
                for (int i = 0; i < colRenders.Length; i++)
                {
                    colRenders[i].bRender = GameManager.bColDebug[i];                                            
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
        if(count > 0)
        {
            colRenders = new ColliderRender[4];
            //bColRen = new bool[4];
    
            colRenders[0] = new ColliderRender(trs, ColliderRender.Type.Capsule);
            colRenders[0].Init(gshader_col, cshader_col);

            colRenders[1] = new ColliderRender(trs, ColliderRender.Type.Sphere);
            colRenders[1].Init(gshader_col, cshader_col);
            
            colRenders[2] = new ColliderRender(trs, ColliderRender.Type.Cylinder);
            colRenders[2].Init(gshader_col, cshader_col);
        }        
    }

    int cullIdx;

    public int cullOffset
    {
        get
        {
            return CullManager.cullOffsets[cullIdx];
        }
    }  

    public void SetCullData(int cullIdx, RenderTexture pvf_tex, RenderTexture ovf_tex)
    {
        if (count > 0)
        {
            this.cullIdx = cullIdx;

            {
                skMpb.SetInt("cullOffset", cullOffset);
                skMpb.SetTexture("cullResult_pvf_Texture", pvf_tex);
                skMpb.SetTexture("cullResult_ovf_Texture", ovf_tex);
            }

            if (hasStMesh)
            {
                stMpb.SetInt("cullOffset", cullOffset);
                stMpb.SetTexture("cullResult_pvf_Texture", pvf_tex);
                stMpb.SetTexture("cullResult_ovf_Texture", ovf_tex);
            }

            for(int i = 0; i < count; i++)
            {
                unitActors[i].cullOffset = cullOffset;
            }
        }
    }


    float4[] tessInfo;

    bool bApplyTess
    {
        get
        {
            return tessInfo[unitIdx].x == 1.0f ? true : false;
        }
    }

    float tessDist
    {
        get
        {
            return tessInfo[unitIdx].z;
        }
    }

    public void SetCullData(int cullIdx, RenderTexture pvf_tex, RenderTexture ovf_tex, RWBuffer<float4> tessinfo_buffer)
    {
        if (count > 0)
        {
            this.cullIdx = cullIdx;
            tessInfo = tessinfo_buffer.data;

            {
                skMpb.SetInt("cullOffset", cullOffset);
                skMpb.SetTexture("cullResult_pvf_Texture", pvf_tex);
                skMpb.SetTexture("cullResult_ovf_Texture", ovf_tex);
                skMpb.SetBuffer("unit_tess_Buffer", tessinfo_buffer.value);
            }

            if (hasStMesh)
            {
                stMpb.SetInt("cullOffset", cullOffset);
                stMpb.SetTexture("cullResult_pvf_Texture", pvf_tex);
                stMpb.SetTexture("cullResult_ovf_Texture", ovf_tex);
                stMpb.SetBuffer("unit_tess_Buffer", tessinfo_buffer.value);
            }

            for (int i = 0; i < count; i++)
            {
                unitActors[i].cullOffset = cullOffset;
            }
        }
    }

    RootWJob rootWJob;
    TransformAccessArray traa_rootW;  
   
    protected float4x4[,,] boneSampleDataIn;    

    ROBuffer<AnimPlayerData> animPlayer;
    Texture3D boneSample_input;
    RWBuffer<float4x4> boneSample_output;
    
    COBuffer<int> boneWTransform_mask;
    COBuffer<int> boneWTransform_parent;
    ROBuffer<float4x4> boneWTransform_root;
    RWBuffer<float4x4> boneWTransform_output;
    
    COBuffer<uint> boneOffset_idx;
    COBuffer<float4x4> boneOffset_input;
    RWBuffer<float4x4> bone;
    
    COBuffer<uint> boneStatic_idx;
    COBuffer<float4> boneSca_static;
    RWBuffer<float4x4> boneStatic_tr;
    RWBuffer<float4x4> boneStatic;
    RWBuffer<float4x4> boneStatic_IT;
  

    int boneSampleCount;
    int boneWTransformCount;
    int boneOffsetCount;
    int boneStaticCount;

    float4 countInfo_sample;
    float4 countInfo_wtransform;
    float4 countInfo_offset;
    float4 countInfo_static;

    BoneCompute boneCompute;
    VertexCompute vtxCompute;
    VertexComputeSt vtxComputeSt;

    void InitAnim()
    {
        {
            boneSampleCount = count * boneCount;
            boneWTransformCount = count * boneCount;
            boneOffsetCount = count * bmCount;
            boneStaticCount = count * stCount;
        }

        {
            rootWJob = new RootWJob();
            rootWJob.rootW = new NativeArray<float4x4>(count, Unity.Collections.Allocator.Persistent);
            rootWJob.ntTrs = new NativeArray<float4x4>(count, Unity.Collections.Allocator.Persistent);

            traa_rootW = new TransformAccessArray(trs);
        }

        {            
            boneSampleDataIn = new float4x4[clipCount, frameCount, boneCount];            
        }        

        {
            animPlayer          = new ROBuffer<AnimPlayerData>(count);
            boneSample_input    = new Texture3D(4 * boneCount, frameCount, clipCount, TextureFormat.RGBAFloat, false);
            boneSample_output   = new RWBuffer<float4x4>(boneSampleCount);

            boneWTransform_mask     = new COBuffer<int>(boneCount);
            boneWTransform_parent   = new COBuffer<int>(boneCount);
            boneWTransform_root     = new ROBuffer<float4x4>(count);
            boneWTransform_output   = new RWBuffer<float4x4>(boneWTransformCount);

            boneOffset_idx      = new COBuffer<uint>(bmCount);
            boneOffset_input    = new COBuffer<float4x4>(bmCount);
            bone                = new RWBuffer<float4x4>(boneOffsetCount);

            boneStatic_idx  = new COBuffer<uint>(stCount);
            boneSca_static  = new COBuffer<float4>(stCount);
            boneStatic_tr   = new RWBuffer<float4x4>(boneStaticCount);
            boneStatic      = new RWBuffer<float4x4>(boneStaticCount);
            boneStatic_IT   = new RWBuffer<float4x4>(boneStaticCount);
        }

        {
            //sample
            for (int i = 0; i < count; i++)
            {
                //animPlayerData[i] = new AnimPlayerData();
                anims[i].SetPlayerData(animPlayer.data);
            }

            //wTransform
            {
                var data = boneWTransform_mask.data;
                for (int i = 0; i < boneCount; i++)
                {
                   data[i] = idxMask[i];
                }
                boneWTransform_mask.Write();
            }

            {
                var data = boneWTransform_parent.data;
                for (int i = 0; i < boneCount; i++)
                {
                   data[i] = idxParent[i];
                }
                boneWTransform_parent.Write();
            }


            //skin
            {
                var data = boneOffset_idx.data;
                for (int i = 0; i < bmCount; i++)
                {
                    data[i] = (uint)boneIdx[i];
                }
                boneOffset_idx.Write();
            }

            {
                var data = boneOffset_input.data;
                for (int i = 0; i < bmCount; i++)
                {
                    data[i] = bindpose[i];
                }
                boneOffset_input.Write();
            }


            //static
            {
                var data = boneStatic_idx.data;
                for (int i = 0; i < stCount; i++)
                {
                    data[i] = (uint)boneIdx_st[i];
                }
                boneStatic_idx.Write();
            }

            {
                var data = boneSca_static.data;
                for (int i = 0; i < stCount; i++)
                {
                    data[i] = boneSca_st[i];
                }
                boneSca_static.Write();
            }            

            {
                countInfo_sample.x = boneCount;
                countInfo_wtransform.xy = new float2(boneCount, depthCount);
                countInfo_offset.x = bmCount;
                countInfo_static.x = stCount;
            }
        }

        {
            boneCompute = new BoneCompute();
            vtxCompute = new VertexCompute();
            vtxComputeSt = new VertexComputeSt();

            boneCompute.Init(cshader, this);           
            vtxCompute.Init(cshader, skMeshes, this, count, bBase);
            vtxComputeSt.Init(cshader, stMeshes, this, count);
        }

        {
            skMpb.SetBuffer("vtxDynamic", vtxCompute.vOut.value);
            skMpb.SetInt("vtxCount", vtxCompute.vertexCount);
        }

        if (hasStMesh)
        {
            stMpb.SetBuffer("vtxDynamic", vtxComputeSt.vOut.value);
            stMpb.SetInt("vtxCount", vtxComputeSt.vertexCount);
        }

        //Debug
        {
            //skVtxOut = vtxCompute.vOutData;
        }

    }

    //VertexCompute.VertexOut[] skVtxOut;

    void ReleaseAnim()
    {
        Action<ComputeBuffer> ReleaseCBuffer =
            (cbuffer) => { if (cbuffer != null) cbuffer.Release(); cbuffer = null; };

        BufferBase<AnimPlayerData>.Release(animPlayer);      
        BufferBase<float4x4>.Release(boneSample_output);
        BufferBase<int>.Release(boneWTransform_mask);
        BufferBase<int>.Release(boneWTransform_parent);
        BufferBase<float4x4>.Release(boneWTransform_root);
        BufferBase<float4x4>.Release(boneWTransform_output);
        BufferBase<uint>.Release(boneOffset_idx);
        BufferBase<float4x4>.Release(boneOffset_input);
        BufferBase<float4x4>.Release(bone);
        BufferBase<uint>.Release(boneStatic_idx);
        BufferBase<float4>.Release(boneSca_static);
        BufferBase<float4x4>.Release(boneStatic_tr);
        BufferBase<float4x4>.Release(boneStatic);
        BufferBase<float4x4>.Release(boneStatic_IT);
    }



    void BeginFrameRender(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {        
        {
            vtxCompute.ComputeVertex(context, cmd);
        }

        if (hasStMesh)
        {
            vtxComputeSt.ComputeVertex(context, cmd);
        }
    }

    bool bRigid = true;

    public void BakeAnimCurve(string clipName, int frameIdx)
    {
        if (count > 0)
        {
            var player = anims[0].player;
            player.bRigid = bRigid;
            anims[0].BakeAnimation(clipName, frameIdx);
        }
    }

    public void BakeAnimation()
    {
        for (int i = 0; i < clipCount; i++)
        {
            for (int j = 0; j < frameCount; j++)
            {
                BakeAnimCurve(clips[i].name, j);

                for (int k = 0; k < boneCount; k++)
                {
                    float4 real = bns0[0][k].transform.dqL.real;
                    float4 dual = bns0[0][k].transform.dqL.dual;

                    boneSampleDataIn[i, j, k].c0 = real;
                    boneSampleDataIn[i, j, k].c1 = dual;

                    boneSample_input.SetPixel(k * 4 + 0, j, i, (Vector4)real);
                    boneSample_input.SetPixel(k * 4 + 1, j, i, (Vector4)dual);
                }                       
            }
        }

        boneSample_input.Apply();       
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateSampleAnim()
    {
        float dt = Time.deltaTime;
        //float dt = runner.DeltaTime;


        Parallel.For(0, count,
            (i) =>
            {
                var player = anims[i].player;             
                player.cState.Sample_Total(dt);
            });
    
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void UpdateRootM0()
    {
        {
            rootWJob.Schedule<RootWJob>(traa_rootW).Complete();
        }      
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void UpdateRootM()
    {
        for(int i = 0; i < count; i++)
        {
            NetworkTransform ntTr = ntTrs[i];            

            float4x4 trs = float4x4.zero;

            trs.c0.xyz = ntTr.ReadPosition();
            trs.c1 = ((quaternion)(ntTr.ReadRotation())).value;

            rootWJob.ntTrs[i] = trs;
        }

        {
            rootWJob.Schedule<RootWJob>(traa_rootW).Complete();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void UpdateStaticM()
    {
        if (hasStMesh)
        {
            Parallel.For(0, count,
                (int i) =>
                {
                    for (int j = 0; j < stCount; j++)
                    {
                        //trM[i][j] = boneStaticData_tr[i * stCount + j];
                        trM[i][j] = boneStatic_tr.data[i * stCount + j];
                    }
                });

        }
    }

    float3 lightDir;
    void UpdateLight()
    {
        lightDir = -math.rotate(trLight.rotation, new float3(0.0f, 0.0f, 1.0f));
    }

    void UpdateAnimSpeed()
    {
        Parallel.For(0, count,
            (int i) =>
            {
                anims[i].player.speed = animSpeed;
            });      
    }

    #region Render

    void RenderCSM(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {
        if (LightManager.type == LightType.Directional)
        {
            {
                {
                    cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, skMte, pass_csm, MeshTopology.Triangles, idxBuffer.count, count, skMpb);
                }

                if (bApplyTess)
                {
                    cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, skMte, pass_tess_csm, MeshTopology.Triangles, idxBuffer.count, 1, skMpb);
                }
            }

            if (hasStMesh)
            {
                {
                    cmd.DrawProcedural(stIdxBuffer, Matrix4x4.identity, stMte, pass_csm, MeshTopology.Triangles, stIdxBuffer.count, count, stMpb);
                }

                if (bApplyTess)
                {
                    cmd.DrawProcedural(stIdxBuffer, Matrix4x4.identity, stMte, pass_tess_csm, MeshTopology.Triangles, stIdxBuffer.count, 1, stMpb);
                }
            }
        }
    }

    void RenderCBM(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {
        if (LightManager.type == LightType.Point || LightManager.type == LightType.Spot)
        {
            {
                cbm_action.Update_Data(skMpb);

                {
                    cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, skMte, pass_cbm, MeshTopology.Triangles, idxBuffer.count, count, skMpb);
                }

                if (bApplyTess)
                {
                    cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, skMte, pass_tess_cbm, MeshTopology.Triangles, idxBuffer.count, 1, skMpb);
                }
            }

            if (hasStMesh)
            {
                cbm_action.Update_Data(stMpb);

                {
                    cmd.DrawProcedural(stIdxBuffer, Matrix4x4.identity, stMte, pass_cbm, MeshTopology.Triangles, stIdxBuffer.count, count, stMpb);
                }

                if (bApplyTess)
                {
                    cmd.DrawProcedural(stIdxBuffer, Matrix4x4.identity, stMte, pass_tess_cbm, MeshTopology.Triangles, stIdxBuffer.count, 1, stMpb);
                }
            }
        }
    }

    private void Render_GBuffer(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        if (bRender)
        {
            //{
            //    float4x4 CV_cam = math.mul(RenderUtil.GetCfromV(cam, false), perCam.V);
            //
            //    {
            //        csm_action.Update_Data(skMpb, CV_cam);
            //    }
            //
            //    if (hasStMesh)
            //    {
            //        csm_action.Update_Data(stMpb, CV_cam);
            //    }
            //}

            {
                skMpb.SetMatrix("V", perCam.V);
                skMpb.SetMatrix("CV", perCam.CV);
                skMpb.SetVector("dirW_view", (Vector3)perCam.dirW_view);
                skMpb.SetVector("posW_view", (Vector3)perCam.posW_view);
                skMpb.SetVector("dirW_light", (Vector3)lightDir);

                {
                    cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, skMte, pass_gbuffer, MeshTopology.Triangles, idxBuffer.count, count, skMpb);
                }

                if (bApplyTess)
                {
                    cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, skMte, pass_tess_gbuffer, MeshTopology.Triangles, idxBuffer.count, 1, skMpb);
                }

            }

            if (hasStMesh)
            {
                stMpb.SetMatrix("V", perCam.V);
                stMpb.SetMatrix("CV", perCam.CV);
                stMpb.SetVector("dirW_view", (Vector3)perCam.dirW_view);
                stMpb.SetVector("posW_view", (Vector3)perCam.posW_view);
                stMpb.SetVector("dirW_light", (Vector3)lightDir);

                {
                    cmd.DrawProcedural(stIdxBuffer, Matrix4x4.identity, stMte, pass_gbuffer, MeshTopology.Triangles, stIdxBuffer.count, count, stMpb);
                }

                if (bApplyTess)
                {
                    cmd.DrawProcedural(stIdxBuffer, Matrix4x4.identity, stMte, pass_tess_gbuffer, MeshTopology.Triangles, stIdxBuffer.count, 1, stMpb);
                }
            }
        }
    }

    private void Render_Transparent(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        if (bRender)
        {
            {
                float4x4 CV_cam = math.mul(RenderUtil.GetCfromV(cam, false), perCam.V);

                {
                    csm_action.Update_Data(skMpb, CV_cam);
                }

                if (hasStMesh)
                {
                    csm_action.Update_Data(stMpb, CV_cam);
                }
            }

            {
                skMpb.SetMatrix("CV", perCam.CV);
                skMpb.SetVector("dirW_view", (Vector3)perCam.dirW_view);
                skMpb.SetVector("posW_view", (Vector3)perCam.posW_view);
                skMpb.SetVector("dirW_light", (Vector3)lightDir);

                cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, skMte, pass_tp, MeshTopology.Triangles, idxBuffer.count, count, skMpb);
            }

            if (hasStMesh)
            {
                stMpb.SetMatrix("CV", perCam.CV);
                stMpb.SetVector("dirW_view", (Vector3)perCam.dirW_view);
                stMpb.SetVector("posW_view", (Vector3)perCam.posW_view);
                stMpb.SetVector("dirW_light", (Vector3)lightDir);

                cmd.DrawProcedural(stIdxBuffer, Matrix4x4.identity, stMte, pass_tp, MeshTopology.Triangles, stIdxBuffer.count, count, stMpb);
            }
        }
    }

    private void Render_Wire(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        if (bRenderWire)
        {
            {
                skMpb.SetMatrix("CV", perCam.CV);
                skMpb.SetMatrix("S", perCam.S);

                cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, skMte, pass_tess_wire, MeshTopology.Triangles, idxBuffer.count, 1, skMpb);
            }

            if (hasStMesh)
            {
                stMpb.SetMatrix("CV", perCam.CV);
                stMpb.SetMatrix("S", perCam.S);

                cmd.DrawProcedural(stIdxBuffer, Matrix4x4.identity, stMte, pass_tess_wire, MeshTopology.Triangles, stIdxBuffer.count, 1, stMpb);
            }
        }
    }

    #endregion

    #region Render_Origial
    void RenderCSM0(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {
        if (LightManager.type == LightType.Directional)
        {
            {
                int pass = bTess_sk_csm ? pass_tess_csm : pass_csm;
                cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, skMte, pass, MeshTopology.Triangles, idxBuffer.count, count, skMpb);
            }

            if (hasStMesh)
            {
                int pass = bTess_st_csm ? pass_tess_csm : pass_csm;
                cmd.DrawProcedural(stIdxBuffer, Matrix4x4.identity, stMte, pass, MeshTopology.Triangles, stIdxBuffer.count, count, stMpb);
            }
        }
    }

    void RenderCBM0(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {
        if (LightManager.type == LightType.Point || LightManager.type == LightType.Spot)
        {
            {
                cbm_action.Update_Data(skMpb);
                int pass = bTess_sk_cbm ? pass_tess_cbm : pass_cbm;
                cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, skMte, pass, MeshTopology.Triangles, idxBuffer.count, count, skMpb);
            }

            if (hasStMesh)
            {
                cbm_action.Update_Data(stMpb);
                int pass = bTess_st_cbm ? pass_tess_cbm : pass_cbm;
                cmd.DrawProcedural(stIdxBuffer, Matrix4x4.identity, stMte, pass, MeshTopology.Triangles, stIdxBuffer.count, count, stMpb);
            }
        }
    }

    private void Render(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM.PerCamera perCam)
    {
        if (bRender)
        {
            {
                float4x4 CV_cam = math.mul(RenderUtil.GetCfromV(cam, false), perCam.V);

                {
                    csm_action.Update_Data(skMpb, CV_cam);
                }

                if (hasStMesh)
                {
                    csm_action.Update_Data(stMpb, CV_cam);
                }
            }

            {
                //skMpb.SetMatrix("CV_view", math.mul(RenderUtil.GetCfromV(cam, false), perCam.V));
                skMpb.SetMatrix("CV", perCam.CV);
                //skMpb.SetMatrixArray("TCV_light", csm_action.TCV_depth);
                //skMpb.SetFloatArray("endZ", csm_action.endZ);
                skMpb.SetVector("dirW_view", (Vector3)perCam.dirW_view);
                skMpb.SetVector("posW_view", (Vector3)perCam.posW_view);
                skMpb.SetVector("dirW_light", (Vector3)lightDir);

                cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, skMte, passColor, MeshTopology.Triangles, idxBuffer.count, count, skMpb);
            }

            if (hasStMesh)
            {
                //stMpb.SetMatrix("CV_view", math.mul(RenderUtil.GetCfromV(cam, false), perCam.V));
                stMpb.SetMatrix("CV", perCam.CV);
                //stMpb.SetMatrixArray("TCV_light", csm_action.TCV_depth);
                //stMpb.SetFloatArray("endZ", csm_action.endZ);
                stMpb.SetVector("dirW_view", (Vector3)perCam.dirW_view);
                stMpb.SetVector("posW_view", (Vector3)perCam.posW_view);
                stMpb.SetVector("dirW_light", (Vector3)lightDir);

                cmd.DrawProcedural(stIdxBuffer, Matrix4x4.identity, stMte, passColor, MeshTopology.Triangles, stIdxBuffer.count, count, stMpb);
            }
        }
    }

    private void Render_GBuffer0(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        if (bRender)
        {
            //{
            //    float4x4 CV_cam = math.mul(RenderUtil.GetCfromV(cam, false), perCam.V);
            //
            //    {
            //        csm_action.Update_Data(skMpb, CV_cam);
            //    }
            //
            //    if (hasStMesh)
            //    {
            //        csm_action.Update_Data(stMpb, CV_cam);
            //    }
            //}

            {
                skMpb.SetMatrix("V", perCam.V);
                skMpb.SetMatrix("CV", perCam.CV);
                skMpb.SetVector("dirW_view", (Vector3)perCam.dirW_view);
                skMpb.SetVector("posW_view", (Vector3)perCam.posW_view);
                skMpb.SetVector("dirW_light", (Vector3)lightDir);
                //skMpb.SetVector("tFactor", tFactor_sk);

                int pass = bTess_sk ? pass_tess_gbuffer : pass_gbuffer;
                cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, skMte, pass, MeshTopology.Triangles, idxBuffer.count, count, skMpb);
            }

            if (hasStMesh)
            {
                stMpb.SetMatrix("V", perCam.V);
                stMpb.SetMatrix("CV", perCam.CV);
                stMpb.SetVector("dirW_view", (Vector3)perCam.dirW_view);
                stMpb.SetVector("posW_view", (Vector3)perCam.posW_view);
                stMpb.SetVector("dirW_light", (Vector3)lightDir);
                //stMpb.SetVector("tFactor", tFactor_st);

                int pass = bTess_st ? pass_tess_gbuffer : pass_gbuffer;
                cmd.DrawProcedural(stIdxBuffer, Matrix4x4.identity, stMte, pass, MeshTopology.Triangles, stIdxBuffer.count, count, stMpb);
            }
        }
    }

    private void Render_Wire0(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        if (bRenderWire)
        {
            {
                skMpb.SetVector("tFactor", tFactor_sk);
                skMpb.SetMatrix("CV", perCam.CV);
                skMpb.SetMatrix("S", perCam.S);

                cmd.DrawProcedural(idxBuffer, Matrix4x4.identity, skMte, pass_tess_wire, MeshTopology.Triangles, idxBuffer.count, count, skMpb);
            }

            if (hasStMesh)
            {
                stMpb.SetVector("tFactor", tFactor_st);
                stMpb.SetMatrix("CV", perCam.CV);
                stMpb.SetMatrix("S", perCam.S);

                cmd.DrawProcedural(stIdxBuffer, Matrix4x4.identity, stMte, pass_tess_wire, MeshTopology.Triangles, stIdxBuffer.count, count, stMpb);
            }
        }
    }
    #endregion

    class BoneCompute
    {
        public void Compute()
        {
            WriteToResource();
            Dispatch();
            ReadFromResource();
        }

        UnitManager unitMan;
        ComputeShader cshader;
        int ki_sample;
        int ki_wtransform;
        int ki_offset;
        int ki_static;

        public void Init(ComputeShader cshader, UnitManager unitMan)
        {
            this.unitMan = unitMan;
            this.cshader = cshader;
            this.ki_sample = cshader.FindKernel("CS_BoneSample");
            this.ki_wtransform = cshader.FindKernel("CS_BoneWTransform");
            this.ki_offset = cshader.FindKernel("CS_BoneOffset_Dynamic");
            this.ki_static = cshader.FindKernel("CS_BoneOffset_Static");

            cshader.SetVector("countInfo_sample", unitMan.countInfo_sample);
            cshader.SetBuffer(ki_sample, "animPlayer", unitMan.animPlayer.value);          
            cshader.SetTexture(ki_sample, "boneSample_input", unitMan.boneSample_input);
            cshader.SetBuffer(ki_sample, "boneSample_output", unitMan.boneSample_output.value);

            cshader.SetVector("countInfo_wtransform", unitMan.countInfo_wtransform);
            cshader.SetBuffer(ki_wtransform, "boneWTransform_mask", unitMan.boneWTransform_mask.value);
            cshader.SetBuffer(ki_wtransform, "boneWTransform_parent", unitMan.boneWTransform_parent.value);
            cshader.SetBuffer(ki_wtransform, "boneWTransform_root", unitMan.boneWTransform_root.value);
            cshader.SetBuffer(ki_wtransform, "boneSample_output", unitMan.boneSample_output.value);
            cshader.SetBuffer(ki_wtransform, "boneWTransform_output", unitMan.boneWTransform_output.value);


            cshader.SetVector("countInfo_offset", unitMan.countInfo_offset);          
            cshader.SetBuffer(ki_offset, "boneWTransform_output", unitMan.boneWTransform_output.value);
            cshader.SetBuffer(ki_offset, "boneOffset_idx", unitMan.boneOffset_idx.value);
            cshader.SetBuffer(ki_offset, "boneOffset_input", unitMan.boneOffset_input.value);
            cshader.SetBuffer(ki_offset, "bone", unitMan.bone.value);

            cshader.SetVector("countInfo_static", unitMan.countInfo_static);
            cshader.SetBuffer(ki_static, "boneStatic_idx", unitMan.boneStatic_idx.value);
            cshader.SetBuffer(ki_static, "boneSca_static", unitMan.boneSca_static.value);           
            cshader.SetBuffer(ki_static, "boneWTransform_output", unitMan.boneWTransform_output.value);
            cshader.SetBuffer(ki_static, "boneStatic_tr", unitMan.boneStatic_tr.value);
            cshader.SetBuffer(ki_static, "bone", unitMan.boneStatic.value);
            cshader.SetBuffer(ki_static, "bone_IT", unitMan.boneStatic_IT.value);
        }

        public void WriteToResource()
        {
            {
                unitMan.animPlayer.Write();
            }          

            {
                var data = unitMan.boneWTransform_root.data;
                for (int i = 0; i < unitMan.count; i++)
                {                   
                    data[i] = unitMan.rootWJob.rootW[i];
                }
                unitMan.boneWTransform_root.Write();
            }
        }

        public void Dispatch()
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            {
                cmd.SetComputeVectorParam(cshader, "countInfo_sample", unitMan.countInfo_sample);
                cmd.SetComputeBufferParam(cshader, ki_sample, "animPlayer", unitMan.animPlayer.value);
                cmd.SetComputeTextureParam(cshader, ki_sample, "boneSample_input", unitMan.boneSample_input);
                cmd.SetComputeBufferParam(cshader, ki_sample, "boneSample_output", unitMan.boneSample_output.value);

                cmd.SetComputeVectorParam(cshader, "countInfo_wtransform", unitMan.countInfo_wtransform);
                cmd.SetComputeBufferParam(cshader, ki_wtransform, "boneWTransform_mask", unitMan.boneWTransform_mask.value);
                cmd.SetComputeBufferParam(cshader, ki_wtransform, "boneWTransform_parent", unitMan.boneWTransform_parent.value);
                cmd.SetComputeBufferParam(cshader, ki_wtransform, "boneWTransform_root", unitMan.boneWTransform_root.value);
                cmd.SetComputeBufferParam(cshader, ki_wtransform, "boneSample_output", unitMan.boneSample_output.value);
                cmd.SetComputeBufferParam(cshader, ki_wtransform, "boneWTransform_output", unitMan.boneWTransform_output.value);

                cmd.SetComputeVectorParam(cshader, "countInfo_offset", unitMan.countInfo_offset);
                cmd.SetComputeBufferParam(cshader, ki_offset, "boneWTransform_output", unitMan.boneWTransform_output.value);
                cmd.SetComputeBufferParam(cshader, ki_offset, "boneOffset_idx", unitMan.boneOffset_idx.value);
                cmd.SetComputeBufferParam(cshader, ki_offset, "boneOffset_input", unitMan.boneOffset_input.value);
                cmd.SetComputeBufferParam(cshader, ki_offset, "bone", unitMan.bone.value);

                cmd.SetComputeVectorParam(cshader, "countInfo_static", unitMan.countInfo_static);
                cmd.SetComputeBufferParam(cshader, ki_static, "boneStatic_idx", unitMan.boneStatic_idx.value);
                cmd.SetComputeBufferParam(cshader, ki_static, "boneSca_static", unitMan.boneSca_static.value);
                cmd.SetComputeBufferParam(cshader, ki_static, "boneWTransform_output", unitMan.boneWTransform_output.value);
                cmd.SetComputeBufferParam(cshader, ki_static, "boneStatic_tr", unitMan.boneStatic_tr.value);
                cmd.SetComputeBufferParam(cshader, ki_static, "bone", unitMan.boneStatic.value);
                cmd.SetComputeBufferParam(cshader, ki_static, "bone_IT", unitMan.boneStatic_IT.value);
            }

            cmd.DispatchCompute(cshader, ki_sample, unitMan.count, 1, 1);
            cmd.DispatchCompute(cshader, ki_wtransform, unitMan.count, 1, 1);
            cmd.DispatchCompute(cshader, ki_offset, unitMan.count, 1, 1);
            cmd.DispatchCompute(cshader, ki_static, unitMan.count, 1, 1);

            Graphics.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void ReadFromResource()
        {
            bool bDebug = false;

            if (bDebug)
            {
                CommandBuffer cmd = CommandBufferPool.Get();

                unitMan.boneSample_output.Read(cmd);
                unitMan.boneWTransform_output.Read(cmd);
                unitMan.bone.Read(cmd);
                unitMan.boneStatic.Read(cmd);
                unitMan.boneStatic_IT.Read(cmd);

                Graphics.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            {
                unitMan.boneStatic_tr.Read();                     
            }
        }     

        public void Release()
        {

        }
    }

    public class VertexCompute
    {
        public void ComputeVertex(ScriptableRenderContext context, CommandBuffer cmd)
        {
            WriteToCShader(context, cmd);
            ExecuteCShaderVertex(context, cmd);
            //ReadFromCShader(context, cmd);
        }
        UnitManager unitMan;
        ComputeShader cshader;
        int kindex;

        VertexIn[] vInData;                 

        COBuffer<VertexIn> vIn;
        public RWBuffer<VertexOut> vOut { get; private set; }

        public int vertexCount;
        int boneCount;
        int insCount;
        int vtxCountOut;

        int vgCount;
        int vtCount = 1024;
        int dpCount;
        public int dvCount;

        Vector4 countInfo;
       
        Mesh skMesh;

        int skCount;
        Mesh[] skMeshes;
        int bCount;
        int[] bBase;

        public void Init(ComputeShader cshader, Mesh[] skMeshes, UnitManager unitMan, int insCount, int[] bBase)
        {
            this.unitMan = unitMan;
            this.cshader = cshader;
            this.kindex = cshader.FindKernel("CS_BoneVertex_Dynamic");
           
            this.skMeshes = skMeshes;
            this.insCount = insCount;

            skCount = skMeshes.Length;
            this.vertexCount = 0;
            this.bCount = 0;
            for (int i = 0; i < skCount; i++)
            {
                this.vertexCount += skMeshes[i].vertexCount;
                this.bCount += skMeshes[i].bindposes.Length;
            }

            this.boneCount = insCount * this.bCount;

            this.vgCount = (vertexCount % vtCount == 0) ? (vertexCount / vtCount) : (vertexCount / vtCount + 1);
            this.dpCount = insCount * vgCount;
            this.dvCount = vtCount * vgCount;           
            this.vtxCountOut = insCount * vertexCount;

            countInfo = new Vector4();           

            {
                countInfo[0] = vertexCount;
                countInfo[1] = this.bCount;
            }

            this.bBase = bBase;

            InitCShader();
        }

        public void InitCShader0()
        {
            List<VertexIn> vtxData = new List<VertexIn>();
            {
                for (int i = 0; i < skCount; i++)
                {
                    Mesh _mesh = skMeshes[i];
                    _mesh.RecalculateNormals();
                    _mesh.RecalculateTangents();

                    List<Vector3> posList = new List<Vector3>();
                    List<Vector3> normalList = new List<Vector3>();
                    List<Vector4> tangetList = new List<Vector4>();
                    List<BoneWeight> bwList = new List<BoneWeight>();
                    _mesh.GetVertices(posList);
                    _mesh.GetNormals(normalList);
                    _mesh.GetTangents(tangetList);
                    _mesh.GetBoneWeights(bwList);

                    for (int j = 0; j < _mesh.vertexCount; j++)
                    {
                        VertexIn vtx;

                        vtx.posL = posList[j];
                        vtx.normalL = normalList[j];
                        vtx.tangentL = tangetList[j];

                        BoneWeight bw = bwList[j];
                        vtx.boneI = new int4(bBase[i]) + new int4(bw.boneIndex0, bw.boneIndex1, bw.boneIndex2, bw.boneIndex3);
                        vtx.boneW = new float4(bw.weight0, bw.weight1, bw.weight2, bw.weight3);

                        vtxData.Add(vtx);
                    }
                }
            }

            {
                vIn = new COBuffer<VertexIn>(vertexCount);
                vInData = vtxData.ToArray();
                var data = vIn.data;
                for (int i = 0; i < vertexCount; i++)
                {
                    data[i] = vInData[i];
                }
                vIn.Write();

                cshader.SetBuffer(kindex, "vIn", vIn.value);
            }

            {
                vOut = new RWBuffer<VertexOut>(vtxCountOut);
                cshader.SetBuffer(kindex, "vOut", vOut.value);
            }

        }

        public void InitCShader()
        {
            List<VertexIn> vtxData = new List<VertexIn>();
            {
                for (int i = 0; i < skCount; i++)
                {
                    Mesh _mesh = skMeshes[i];
                    _mesh.RecalculateNormals();
                    _mesh.RecalculateTangents();

                    List<Vector3> posList = new List<Vector3>();
                    List<Vector3> normalList = new List<Vector3>();
                    List<Vector4> tangetList = new List<Vector4>();
                    List<BoneWeight> bwList = new List<BoneWeight>();
                    _mesh.GetVertices(posList);
                    _mesh.GetNormals(normalList);
                    _mesh.GetTangents(tangetList);
                    _mesh.GetBoneWeights(bwList);

                    for (int j = 0; j < _mesh.vertexCount; j++)
                    {
                        VertexIn vtx;

                        vtx.posL = posList[j];
                        vtx.normalL = normalList[j];
                        vtx.tangentL = tangetList[j];

                        BoneWeight bw = bwList[j];
                        vtx.boneI = new int4(bBase[i]) + new int4(bw.boneIndex0, bw.boneIndex1, bw.boneIndex2, bw.boneIndex3);
                        vtx.boneW = new float4(bw.weight0, bw.weight1, bw.weight2, bw.weight3);

                        vtxData.Add(vtx);
                    }
                }


            }

            VertexIn[] vtxData1 = new VertexIn[vertexCount];
            if (unitMan.bMergeNormal_sk)
            {
                Parallel.For(0, vertexCount,
                    (i) =>
                    {
                        var vtx0 = vtxData[i];
                        vtxData1[i] = vtx0;
                        float3 nom = float3.zero;
                        int c = 0;
                        for (int j = 0; j < vertexCount; j++)
                        {
                            var vtx1 = vtxData[j];
                            float e = 0.01f;
                            if (math.distance(vtx0.posL, vtx1.posL) < e)
                            {
                                nom += math.normalize(vtx1.normalL);
                                c++;
                            }
                        }
                        nom /= c;
                        vtx0.normalL = nom;
                        vtxData1[i] = vtx0;
                    });
            }

            {
                vIn = new COBuffer<VertexIn>(vertexCount);
                vInData = unitMan.bMergeNormal_sk ? vtxData1 : vtxData.ToArray();
                var data = vIn.data;
                for (int i = 0; i < vertexCount; i++)
                {
                    data[i] = vInData[i];
                }
                vIn.Write();

                cshader.SetBuffer(kindex, "vIn", vIn.value);
            }

            {
                vOut = new RWBuffer<VertexOut>(vtxCountOut);
                cshader.SetBuffer(kindex, "vOut", vOut.value);
            }

        }

        public void WriteToCShader(ScriptableRenderContext context, CommandBuffer cmd)
        {
           
        }

        public void ExecuteCShaderVertex(ScriptableRenderContext context, CommandBuffer cmd)
        {            
            cmd.SetComputeVectorParam(cshader, "countInfo_vertex", countInfo);
            cmd.SetComputeBufferParam(cshader, kindex, "bone", unitMan.bone.value);
            cmd.SetComputeBufferParam(cshader, kindex, "vIn", vIn.value);
            cmd.SetComputeBufferParam(cshader, kindex, "vOut", vOut.value);
        
            cmd.DispatchCompute(cshader, kindex, insCount, vgCount, 1);          
        }

        public void ReadFromCShader(ScriptableRenderContext context, CommandBuffer cmd)
        {            
            {
                vOut.Read();
            }         
        }

        public void SetCullTexture(RenderTexture testRt, int cullOffset)
        {
            cshader.SetTexture(kindex, "testCull", testRt);
            cshader.SetInt("cullOffset", cullOffset);
        }

        public void ReleaseCShader()
        {
            if (vIn != null) vIn.Release();           
            if (vOut != null) vOut.Release();
        }

        struct VertexIn
        {
            public float3 posL;
            public float3 normalL;
            public float4 tangentL;
            public int4 boneI;
            public float4 boneW;
        };
        
        [System.Serializable]
        public struct VertexOut
        {
            public float3 posW;
            public float3 normalW;
            public float4 tangentW;
        };
    }

    class VertexComputeSt
    {
        public void ComputeVertex(ScriptableRenderContext context, CommandBuffer cmd)
        {
            WriteToCShader(context, cmd);
            ExecuteCShaderVertex(context, cmd);
            //ReadFromCShader(context, cmd);
        }
        UnitManager unitMan;
        ComputeShader cshader;
        int kindex;

        VertexIn[] vInData;              

        COBuffer<VertexIn> vIn;
        public RWBuffer<VertexOut> vOut { get; private set; }

        public int vertexCount;
        int boneCount;
        int insCount;
        int stCount;
        int vtxCountOut;

        int vgCount;
        int vtCount = 1024;
        int dpCount;
        public int dvCount;

        Vector4 countInfo;
        
        Mesh stMesh;
        Mesh[] stMeshes;

        public void Init(ComputeShader cshader, Mesh[] stMeshes, UnitManager unitMan, int insCount)
        {
            this.unitMan = unitMan;
            this.cshader = cshader;
            this.kindex = cshader.FindKernel("CS_BoneVertex_Static");
           
            this.stMeshes = stMeshes;
            this.insCount = insCount;
            this.stCount = stMeshes.Length;

            this.vertexCount = 0;
            for (int i = 0; i < stMeshes.Length; i++)
            {
                this.vertexCount += stMeshes[i].vertexCount;
            }

            this.boneCount = insCount * stCount;

            this.vgCount = (vertexCount % vtCount == 0) ? (vertexCount / vtCount) : (vertexCount / vtCount + 1);
            this.dpCount = insCount * vgCount;
            this.dvCount = vtCount * vgCount;        
            this.vtxCountOut = insCount * vertexCount;

            countInfo = new Vector4();           

            {
                countInfo[0] = vertexCount;
                countInfo[1] = stCount;
            }

            InitCShader();
        }

        public void InitCShader0()
        {
            {
                vInData = new VertexIn[vertexCount];
                vIn = new COBuffer<VertexIn>(vertexCount); ;
                cshader.SetBuffer(kindex, "vIn", vIn.value);

                int k = 0;
                for (int i = 0; i < stCount; i++)
                {
                    Mesh mesh = stMeshes[i];
                    mesh.RecalculateNormals();
                    mesh.RecalculateTangents();

                    List<Vector3> pos = new List<Vector3>();
                    List<Vector3> normal = new List<Vector3>();
                    List<Vector4> tangent = new List<Vector4>();

                    mesh.GetVertices(pos);
                    mesh.GetNormals(normal);
                    mesh.GetTangents(tangent);

                    for (int j = 0; j < mesh.vertexCount; j++)
                    {
                        VertexIn vtx = new VertexIn();
                        vtx.posL = pos[j];
                        vtx.normalL = normal[j];
                        vtx.tangentL = tangent[j];
                        vtx.boneI.x = i;

                        vInData[k] = vtx;
                        k++;
                    }
                }

                var data = vIn.data;
                for (int i = 0; i < vertexCount; i++)
                {
                    data[i] = vInData[i];
                }
                vIn.Write();
            }

            {
                vOut = new RWBuffer<VertexOut>(vtxCountOut);
                cshader.SetBuffer(kindex, "vOut", vOut.value);
            }

        }

        public void InitCShader()
        {
            {
                vInData = new VertexIn[vertexCount];
                vIn = new COBuffer<VertexIn>(vertexCount); ;
                cshader.SetBuffer(kindex, "vIn", vIn.value);

                int k = 0;
                for (int i = 0; i < stCount; i++)
                {
                    Mesh mesh = stMeshes[i];
                    mesh.RecalculateNormals();
                    mesh.RecalculateTangents();

                    List<Vector3> pos = new List<Vector3>();
                    List<Vector3> normal = new List<Vector3>();
                    List<Vector4> tangent = new List<Vector4>();

                    mesh.GetVertices(pos);
                    mesh.GetNormals(normal);
                    mesh.GetTangents(tangent);

                    for (int j = 0; j < mesh.vertexCount; j++)
                    {
                        VertexIn vtx = new VertexIn();
                        vtx.posL = pos[j];
                        vtx.normalL = normal[j];
                        vtx.tangentL = tangent[j];
                        vtx.boneI.x = i;

                        vInData[k] = vtx;
                        k++;
                    }
                }

                VertexIn[] vInData1 = new VertexIn[vertexCount];
                if (unitMan.bMergeNormal_st)
                {
                    Parallel.For(0, vertexCount,
                        (i) =>
                        {
                            var vtx0 = vInData[i];
                            vInData1[i] = vtx0;
                            float3 nom = float3.zero;
                            int c = 0;
                            for (int j = 0; j < vertexCount; j++)
                            {
                                var vtx1 = vInData1[j];
                                float e = 0.01f;
                                if (math.distance(vtx0.posL, vtx1.posL) < e)
                                {
                                    nom += math.normalize(vtx1.normalL);
                                    c++;
                                }
                            }
                            nom /= c;
                            vtx0.normalL = nom;
                            vInData1[i] = vtx0;
                        });
                }

                var data = vIn.data;
                var data1 = unitMan.bMergeNormal_st ? vInData1 : vInData;
                for (int i = 0; i < vertexCount; i++)
                {
                    data[i] = data1[i];
                }
                vIn.Write();
            }

            {
                vOut = new RWBuffer<VertexOut>(vtxCountOut);
                cshader.SetBuffer(kindex, "vOut", vOut.value);
            }

        }


        public void WriteToCShader(ScriptableRenderContext context, CommandBuffer cmd)
        {
            
        }

        public void ExecuteCShaderVertex(ScriptableRenderContext context, CommandBuffer cmd)
        {         
            cmd.SetComputeVectorParam(cshader, "countInfo_vertex_static", countInfo);
            cmd.SetComputeBufferParam(cshader, kindex, "vIn", vIn.value);
            cmd.SetComputeBufferParam(cshader, kindex, "bone", unitMan.boneStatic.value);
            cmd.SetComputeBufferParam(cshader, kindex, "bone_IT", unitMan.boneStatic_IT.value);
            cmd.SetComputeBufferParam(cshader, kindex, "vOut", vOut.value);
         
            cmd.DispatchCompute(cshader, kindex, insCount, vgCount, 1);           
        }


        public void ReadFromCShader(ScriptableRenderContext context, CommandBuffer cmd)
        {            
            {
                vOut.Read();
            }           
        }

        public void SetCullTexture(RenderTexture testRt, int cullOffset)
        {
            cshader.SetTexture(kindex, "testCull", testRt);
            cshader.SetInt("cullOffset", cullOffset);
        }

        public void ReleaseCShader()
        {
            if (vIn != null) vIn.Release();           
            if (vOut != null) vOut.Release();
        }

        struct VertexIn
        {
            public float3 posL;
            public float3 normalL;
            public float4 tangentL;
            public int4 boneI;
            public float4 boneW;
        };     

        public struct VertexOut
        {
            public float3 posW;
            public float3 normalW;
            public float4 tangentW;
        };
    }


    struct RootWJob : IJobParallelForTransform
    {
        [ReadOnly]
        public NativeArray<float4x4> ntTrs;

        [WriteOnly]
        public NativeArray<float4x4> rootW;

        public void Execute0(int i, TransformAccess tra)
        {
            rootW[i] = BoneNode.GetPfromC(tra.position, tra.rotation, tra.localScale);           
        }

        public void Execute(int i, TransformAccess tra)
        {
            float4x4 trs = ntTrs[i];

            float3 pos = trs.c0.xyz;
            quaternion rot = trs.c1;
            float3 sca = tra.localScale;

            rootW[i] = BoneNode.GetPfromC(pos, rot, sca);
        }

        public void Dispose()
        {
            if (rootW.IsCreated) rootW.Dispose();
            if (ntTrs.IsCreated) ntTrs.Dispose();
        }
    }



    struct VertexStatic
    {
        public float2 uv0;
        public float2 uv1;
        public float4 color;
    }


    //Debug
    int[] siblingCount;
    int depthCount;
    int bnCount;
    int[] idxMask;
    int[] idxParent;
    int[] idx;
    int2[] idxMP;

    BoneNode[] bns;


    //Test
    void TestBns()
    {
        //rootBns = new BoneNode[count];
        //for (int i = 0; i < count; i++)
        //{
        //    rootBns[i] = new BoneNode();
        //}

        {
            //BoneNode.ConstructBones(rootTr, rootBns);
            bns = BoneNode.ToArrayForCompute(rootBns[0], out siblingCount, out depthCount, out bnCount, out idxMask, out idxParent, out idx, out idxMP);
        }
    }
}
