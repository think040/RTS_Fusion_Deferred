using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;

using UnityEngine;
using UnityEngine.AI;

using UserAnimSpace;

using Fusion;

public class UnitActor : NetworkBehaviour
{    
    public virtual void Init(string[] stNames, float4x4[] stM, bool hasStMesh)
    {        
        this.hasStMesh = hasStMesh;
        if (hasStMesh)
        {
            this.stNames = stNames;
            this.stM = stM;

            this.stCount = transform.childCount;
            stTr = new Transform[stCount];
            for (int i = 0; i < stCount; i++)
            {
                stTr[i] = transform.GetChild(i);
                stTr[i].gameObject.name = stNames[i];
            }
        }

        InitAnim();
    }

    IEnumerator et_die;

    public NetworkTransform ntTransform;
    public float3 ntPos;
    public quaternion ntRot;
   
    float3 nvData;

    public virtual void Begin()
    {
        {
            targetPos = transform.position;
        }

        {
            attackTr = new Transform[3];
            ClearAttackTr();
        }

        {
            bodyRadius = nvAgent.radius * 1.10f;
        }

        {
            et_die = EndTime(2.0f);
        }

        {
            ntTransform = GetBehaviour<NetworkTransform>();
        }
        
        if(Object.HasStateAuthority)
        {
            nvAgent.enabled = true;           
        }

        //if (Runner.IsClient && !Runner.IsServer)
        //{
        //    //bodyCollider.enabled = false;
        //    hitCollider.enabled = false;
        //    nvAgent.enabled = false;
        //}
        
        if(Object.HasStateAuthority)
        {            
            nvData = new float3(nvAgent.speed, nvAgent.acceleration, nvAgent.angularSpeed);
        }
    }

    private void OnEnable()
    {

    }

    void Start()
    {

    }

    public float _vRadius;
    public float _aRadius;

    public Transform shootTr;
   


    public virtual void Update()
    {

        //UpdateBehave();

        //if (Object.HasInputAuthority)
        //{
        //    if (Input.GetKeyDown(KeyCode.Q))
        //    {
        //        RPC_Set_unit_move_stop(true);
        //    }
        //
        //    if (Input.GetKeyUp(KeyCode.Q))
        //    {
        //        RPC_Set_unit_move_stop(false);
        //    }
        //}

        //if (!GameManager.bUpdate)
        //{
        //    return;
        //}
        //
        //{
        //    UpdateBone();
        //}
        //
        //if (ntTransform != null)
        //{
        //    ntPos = ntTransform.ReadPosition();
        //    ntRot = ntTransform.ReadRotation();
        //}
        //
        //if (Object.HasStateAuthority)
        //{
        //    Behave();
        //}


        //{
        //    stateData = (int)state;
        //}
    }

    public int _offsetIdx;
    public int _iid;

    private void FixedUpdate()
    {
        if (GameManager.isPause)
        {
            return;
        }


        if (Object.HasInputAuthority)
        {
            if (isSelected)
            {
                RPC_Set_isSelected(true);
            }
            else
            {
                RPC_Set_isSelected(false);
            }
        }
        //
        //if (Object.HasInputAuthority)
        //{
        //    MoveOrAttack();
        //}

    }



    
    //public override void Render()
    //public void UpdateBehave()
    public override void FixedUpdateNetwork()
    {       
        if (!GameManager.bUpdate)
        {
            return;
        }

        if (GameManager.isPause)
        {
            //nvAgent.isStopped = true;            

            return;
        }

        {
            _isCull_Audio = isCull_Audio;
        }

        //if(HasStateAuthority)
        //{           
        //    if(SceneTranslation.fps > 60.0f)
        //    {
        //        float radio = SceneTranslation.fps / 60.0f;
        //        float3 _nvData = radio * nvData;
        //
        //        nvAgent.speed = _nvData.x;
        //        nvAgent.acceleration = _nvData.y;
        //        nvAgent.angularSpeed = _nvData.z;
        //    }        
        //}

        //if(!Runner.IsRunning)
        //{
        //    return;
        //}

        //if(!Object.IsValid)
        //{
        //    return;
        //}

        //if(!Runner.isActiveAndEnabled)
        //{
        //    return;
        //}


        //if(Object.HasInputAuthority)
        //{
        //    if(Input.GetKeyDown(KeyCode.Q))
        //    {
        //        RPC_Set_unit_move_stop(true);
        //    }
        //
        //    if(Input.GetKeyUp(KeyCode.Q))
        //    {
        //        RPC_Set_unit_move_stop(false);
        //    }            
        //}

        if (Object.HasInputAuthority)
        {
            if (GetInput(out NetworkInputData data))
            {
                if (data.button0 == 1)
                {
                    RPC_Set_unit_move_stop(true);
        
                    //unit_move_stop = true;
                    //DebugInfo.Log($"Q Button is Down! / name : {name} / player : {Object.InputAuthority}");
                }
        
                if (data.button0 == 0)
                {
                    RPC_Set_unit_move_stop(false);
        
                    //unit_move_stop = false;
                    //DebugInfo.Log("Q Button is Released!");
                }
            }
        }


        //if (Object.HasInputAuthority)
        //{
        //    if (isSelected)
        //    {
        //        RPC_Set_isSelected(true);
        //    }
        //    else
        //    {
        //        RPC_Set_isSelected(false);
        //    }
        //}
        //
        //if (Object.HasInputAuthority)
        //{
        //    MoveOrAttack();
        //}


        {
            UpdateBone();        
        }
                        
        if(ntTransform != null)
        {
            ntPos = ntTransform.ReadPosition();
            ntRot = ntTransform.ReadRotation();
        }
           
        if (Object.HasStateAuthority)
        {
            Behave();
        }
       
    
        //if (Object.HasStateAuthority)
        //{
        //    RPC_SetStateData((int)state);
        //}
    
        //{
        //    _iid = iid;
        //    _offsetIdx = offsetIdx;
        //}
    }

    

    //public override void Render()
    //{
    //    //if (Object.HasStateAuthority)
    //    //{
    //    //    Behave();
    //    //}
    //
    //    //if (Object.HasInputAuthority)
    //    //{
    //    //    if (isSelected)
    //    //    {
    //    //        RPC_Set_isSelected(true);
    //    //    }
    //    //    else
    //    //    {
    //    //        RPC_Set_isSelected(false);
    //    //    }
    //    //}
    //    //
    //    //if (Object.HasInputAuthority)
    //    //{
    //    //    MoveOrAttack();
    //    //}
    //}

    void SetStateData(int state)
    {
        if(stateData == state)
        {
            return;
        }
        
        {
            RPC_SetStateData(state);
        }
    }


    bool paused = false;
    private void OnApplicationPause(bool pause)
    {
        this.paused = pause;

        //DebugInfo.Log($"OnApplicationPause : {pause}");
    }

    bool focus = true;
    private void OnApplicationFocus(bool focus)
    {
        this.focus = focus;
    }


    public bool bMove { get; set; } = false;
    public bool bAttack { get; set; } = false;

    void MoveOrAttack()
    {
        if (bAttack)
        {
            Transform htr = attackTr[0];
            UnitActor hactor = htr.GetComponent<UnitActor>();

            RPC_Attack(hactor.Object.Id, 0);
            bAttack = false;
        }

        if (bMove)
        {
            RPC_MoveTo(targetPos);
            bMove = false;
        }        
    }




    //[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_AnimPlayCross0(int clip)
    {       
        if (anim != null)
        {
            switch (clip)
            {
                case 0:
                    anim.PlayCross("Idle");
                    break;
                case 1:
                    anim.PlayCross("Running");
                    break;
                case 2:
                    anim.PlayCross("Attacking");
                    break;
            }
        }
    }



    [Rpc(RpcSources.StateAuthority, RpcTargets.All, 
        Channel = RpcChannel.Unreliable, HostMode = RpcHostMode.SourceIsHostPlayer, 
        InvokeLocal = true, InvokeResim = false, TickAligned = true)]    
    //[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_AnimPlayCross(int clip, RpcInfo info = default)
    {                                      
        //if(!info.Source.IsValid)
        //{
        //    return;
        //}

        if (anim != null)
        {
            switch (clip)
            {
                case 0:
                    anim.PlayCross("Idle");
                    break;
                case 1:
                    anim.PlayCross("Running");
                    break;
                case 2:
                    anim.PlayCross("Attacking");
                    break;
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All,
       Channel = RpcChannel.Unreliable, HostMode = RpcHostMode.SourceIsHostPlayer,
       InvokeLocal = true, InvokeResim = false, TickAligned = true)]
    //[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetStateData(int state, RpcInfo info = default)
    {
        //if (!info.Source.IsValid)
        //{
        //    return;
        //}

        stateData = state;
        this.state = (ActionState)state;
    }

    bool unit_move_stop { get; set; } = false;

    [Rpc(RpcSources.InputAuthority, RpcTargets.All,
        Channel = RpcChannel.Unreliable, HostMode = RpcHostMode.SourceIsHostPlayer,
        InvokeLocal = true, InvokeResim = false, TickAligned = true)]
    //[Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    void RPC_Set_unit_move_stop(bool value, RpcInfo info = default)
    {
        //if (!info.Source.IsValid)
        //{
        //    return;
        //}

        unit_move_stop = value;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_SetStateData1(int state)
    {       
        this.state = (ActionState)state;
    }

    //[Networked(OnChanged = nameof(OnNtVarChanged))]
    //public int stateData_nt { get; set; }
    //
    //static void OnNtVarChanged(Changed<UnitActor> changed)
    //{
    //    changed.Behaviour.stateData = changed.Behaviour.stateData_nt;
    //
    //}

    //[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    //void RPC_SetStateData_Id(int state, NetworkId id)
    //{
    //    var unit = Runner.FindObject(id).GetComponent<UnitActor>();
    //
    //    unit.stateData = state;
    //}

   

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_DamageHp(float dHp)
    {
        float hp = Hp - dHp;
        if (hp < 0.0f)
        {
            hp = 0.0f;
            //hp = maxHp;
        }

        //Debug
        Hp = hp;  
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Set_Hp(float hp)
    {
        Hp = hp;
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.All,
        Channel = RpcChannel.Unreliable, HostMode = RpcHostMode.SourceIsHostPlayer,
        InvokeLocal = true, InvokeResim = false, TickAligned = true)]
    //[Rpc(RpcSources.InputAuthority, RpcTargets.All)]    
    public void RPC_Set_positionTr_Null(bool bNull)
    {
        positionTr = bNull ? null : transform;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All,
         Channel = RpcChannel.Unreliable, HostMode = RpcHostMode.SourceIsHostPlayer,
         InvokeLocal = true, InvokeResim = false, TickAligned = true)]
    //[Rpc(RpcSources.InputAuthority, RpcTargets.All)]    
    public void RPC_ClearAttackTr()
    {
        if (attackTr != null)
        {
            int count = attackTr.Length;
            for (int i = 0; i < count; i++)
            {
                attackTr[i] = null;
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All,
        Channel = RpcChannel.Unreliable, HostMode = RpcHostMode.SourceIsHostPlayer,
        InvokeLocal = true, InvokeResim = false, TickAligned = true)]
    //[Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_SetAttackTr(NetworkId id, int order)
    {
        var target = Runner.FindObject(id);

        if (attackTr != null && target != null)
        {
            if (order < attackTr.Length)
            {
                attackTr[order] = target.transform;
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All,
         Channel = RpcChannel.Unreliable, HostMode = RpcHostMode.SourceIsHostPlayer,
         InvokeLocal = true, InvokeResim = false, TickAligned = true)]
    //[Rpc(RpcSources.InputAuthority, RpcTargets.All)]    
    public void RPC_Set_targetPos(Vector3 pos)
    {
        targetPos = pos;
    }    


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority,
         Channel = RpcChannel.Reliable, HostMode = RpcHostMode.SourceIsHostPlayer,
         InvokeLocal = true, InvokeResim = true, TickAligned = false)]
    //[Rpc(RpcSources.InputAuthority, RpcTargets.All)]    
    public void RPC_Attack(NetworkId id, int order)
    {
        //uactor.RPC_SetAttackTr(hactor.Object.Id, 0);
        //uactor.RPC_Set_positionTr_Null(true);

        {
            var target = Runner.FindObject(id);

            if (attackTr != null && target != null)
            {
                if (order < attackTr.Length)
                {
                    attackTr[order] = target.transform;
                }
            }
        }

        {
            positionTr = null;
        }        
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority,
         Channel = RpcChannel.Reliable, HostMode = RpcHostMode.SourceIsHostPlayer,
         InvokeLocal = true, InvokeResim = true, TickAligned = false)]
    //[Rpc(RpcSources.InputAuthority, RpcTargets.All)]    
    public void RPC_MoveTo(Vector3 pos)
    {
        //uactor.RPC_ClearAttackTr();
        //uactor.RPC_Set_positionTr_Null(false);
        //uactor.RPC_Set_targetPos(movePos);

        ClearAttackTr();

        {
            positionTr = transform;
        }

        targetPos = pos;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Set_isActive(bool value)
    {
        isActive = value;
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.All,
         Channel = RpcChannel.Unreliable, HostMode = RpcHostMode.SourceIsHostPlayer,
         InvokeLocal = false, InvokeResim = false, TickAligned = true)]
    //[Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_Set_isSelected(bool value, RpcInfo info = default)
    {        
        isSelected = value;        
    }



    public UserAnimation anim;
    protected UserAnimPlayer player;
    protected Dictionary<string, UserAnimState> dicStates;
    public NavMeshAgent nvAgent;
    public Collider bodyCollider;
    public Collider hitCollider;

    protected string[] stNames;
    protected float4x4[] stM;
    protected Transform[] stTr;
    protected int stCount;
    protected bool hasStMesh;

    public UnitManager unitMan
    {
        get; set;
    }

    public int offsetIdx
    {
        get; set;
    }
    public int iid
    {
        get; set;
    }
    public int unitIdx
    {
        get; set;
    }

    public int pNum_nt;

    public int pNum
    {
        get
        {
            return GameManager.playerNum[unitIdx];
        }
    }

    public float vRadiusDef
    {
        get
        {
            return GameManager.viewRadiusDef[unitIdx];
        }
    }
    
    public float aRadiusDef
    {
        get
        {
            return GameManager.attackRadiusDef[unitIdx];
        }
    }

    public float vRadius
    {
        get
        {
            return GameManager.viewRadius[offsetIdx + iid];
        }
        set
        {
            GameManager.viewRadius[offsetIdx + iid] = value;
        }
    } 

    public float aRadius
    {
        get
        {
            return GameManager.attackRadius[offsetIdx + iid];
        }
        set
        {
            GameManager.attackRadius[offsetIdx + iid] = value;
        }
    }

    public float Hp
    {
        get { return GameManager.hp[offsetIdx + iid]; }
        set { GameManager.hp[offsetIdx + iid] = value; }
    }

    public float maxHp
    {
        get { return GameManager.maxHp[unitIdx]; }        
    }

    public float rHp
    {
        get { return Hp / GameManager.maxHp[unitIdx]; }
    }

    public float hitHp
    {
        get { return GameManager.hitHp[unitIdx]; }
    }

    public bool isSelected0
    {
        get { return GameManager.selectData[offsetIdx + iid] == 1 ? true : false; }
        set { GameManager.selectData[offsetIdx + iid] = value ? 1 : 0; }
    }

    public bool isSelected
    {
        get 
        {            
            return GameManager.selectData[offsetIdx + iid] == 1 ? true : false; 
        }
        set 
        { 
            GameManager.selectData[offsetIdx + iid] = value ? 1 : 0; 
        }
    }

    public bool isActive
    {
        get { return GameManager.activeData[offsetIdx + iid]; }
        set
        {
            GameManager.activeData[offsetIdx + iid] = value;
            if (!value)
            {
                isSelected = value;
                //selectGroup = -1;
            }
        }
    }
   

    public int stateData
    {
        get { return GameManager.stateData[offsetIdx + iid]; }
        set
        {
            //var _stateData = GameManager.stateData;
            //if(_stateData != null)
            //{
            //    _stateData[offsetIdx + iid] = value;
            //}

            GameManager.stateData[offsetIdx + iid] = value;            
        }
    }

    public float4 terrainArea
    {
        //get
        //{
        //    return GameManager.terrainArea[offsetIdx + iid];
        //}

        get
        {
            float4 area = GameManager.terrainArea[offsetIdx + iid];

            if(area.x == 1.0f)
            {
                area = new float4(1.0f, 0.0f, 0.0f, 0.0f);
            }
            else if (area.y == 1.0f)
            {
                area = new float4(0.0f, 1.0f, 0.0f, 0.0f);
            }
            else if (area.z == 1.0f)
            {
                area = new float4(0.0f, 0.0f, 1.0f, 0.0f);
            }
            else if (area.w == 1.0f)
            {
                area = new float4(0.0f, 0.0f, 0.0f, 1.0f);
            }

            return area;
        }
    }

    public int minTargetIdx
    {
        get
        {
            float4 minDist = GameManager.minDist[offsetIdx + iid];
            int idx = -1;

            if (minDist.z > 0.0f)
            {
                idx = (int)minDist.x;
                return idx;
            }

            //_minTargetIdx = idx;
            return idx;
        }
    }

    public int _minTargetIdx;

    public Transform[] attackTr;

    public Transform positionTr
    {
        get; set;
    }

    

    public Transform targetTr
    {
        get
        {
            int minIdx = minTargetIdx;
            if (minIdx >= 0)
            {
                return GameManager.unitTrs[minIdx];
            }

            return null;
        }
    }
   

    public float3 targetPos
    {
        get
        {
            //if(Object.HasInputAuthority)
            {
                return GameManager.targetPos[offsetIdx + iid];
            }
            //else
            //{
            //    return targetPos_nt;
            //}            
        }
        set
        {
            int idx = offsetIdx + iid;
            GameManager.targetPos[idx] = value;
            GameManager.refTargetPos[idx] = value;
        }
    }

    


    public void SetAttackTr(Transform tr, int order)
    {
        if (order < attackTr.Length)
        {
            attackTr[order] = tr;          
        }
    }

   
   

    public Transform GetAttackTr()
    {
        int count = attackTr.Length;
        for (int i = 0; i < count; i++)
        {
            if (attackTr[i] != null)
            {
                if (attackTr[i].GetComponent<UnitActor>().isActive)
                {
                    return attackTr[i];
                }
                else
                {
                    attackTr[i] = null;
                }
            }
        }

        return null;
    }

    public Transform GetAttackTr(out int idx)
    {
        int count = attackTr.Length;
        idx = -1;

        for (int i = 0; i < count; i++)
        {
            if (attackTr[i] != null)
            {
                if (attackTr[i].GetComponent<UnitActor>().isActive)
                {
                    idx = i;
                    return attackTr[i];
                }
                else
                {
                    attackTr[i] = null;
                }
            }
        }


        return null;
    }

    public void ClearAttackTr()
    {
        int count = attackTr.Length;
        for (int i = 0; i < count; i++)
        {
            attackTr[i] = null;
        }
    }

    

    public int cullOffset
    {
        get; set;
    }

    public bool isCull
    {
        get
        {
            return CullManager.cullResult_pvf[cullOffset + iid] == 0.0f ? true : false;
        }
    }

    public bool _isCull_Audio;

    public bool isCull_Audio
    {
        get
        {
            return CullManager.cullResult_svf[cullOffset + iid] == 1.0f ? true : false;
        }

        //get
        //{
        //    return false;
        //}
    }



    public bool isCull_light_pvf
    {
        get
        {
            return DeferredCullManager.cullResult_pvf[cullOffset + iid] == 0.0f ? true : false;
        }
    }

    public bool isCull_light_svf
    {
        get
        {
            return DeferredCullManager.cullResult_svf[cullOffset + iid] == 1.0f ? true : false;
        }
    }


    public enum Type_Audio : int
    {
        Die = 0,
        Attack = 1,
        Arrow = 2        
    }

    public AudioClip[] aClips;
    public float[] aVolume;


    [Rpc(RpcSources.StateAuthority, RpcTargets.All,
    Channel = RpcChannel.Unreliable, HostMode = RpcHostMode.SourceIsHostPlayer,
    InvokeLocal = true, InvokeResim = false, TickAligned = true)]
    public void RPC_Play_EffectAudio(int type)
    {
        if(isCull_Audio)
        {
            return;
        }
       
        AudioManager.Play(ntPos, aClips[type], aVolume[type]);
    }


    public void InitAnim()
    {
        this.enabled = true;
        anim = GetComponent<UserAnimation>();
        player = anim.player;
        dicStates = anim.dicStates;


        dicStates["Idle_Running"] = new UserAnimCross("Idle_Running", dicStates["Idle"], dicStates["Running"], player);
        (dicStates["Idle_Running"] as UserAnimCross).InitTime(0.5f, 0.0f, 1.0f, 0.5f, 1.0f, 0.0f);
        dicStates["Idle_Running"].isRightNow = true;

        dicStates["Running_Idle"] = new UserAnimCross("Running_Idle", dicStates["Running"], dicStates["Idle"], player);
        (dicStates["Running_Idle"] as UserAnimCross).InitTime(0.05f, 0.0f, 0.075f, 0.5f, 1.0f, 0.0f);
        dicStates["Running_Idle"].isRightNow = true;

        dicStates["Running_Attacking"] = new UserAnimCross("Running_Attacking", dicStates["Running"], dicStates["Attacking"], player);
        (dicStates["Running_Attacking"] as UserAnimCross).InitTime(0.5f, 0.0f, 1.0f, 0.5f, 1.0f, 0.0f);
        dicStates["Running_Attacking"].isRightNow = true;

        dicStates["Attacking_Running"] = new UserAnimCross("Attacking_Running", dicStates["Attacking"], dicStates["Running"], player);
        (dicStates["Attacking_Running"] as UserAnimCross).InitTime(0.5f, 0.0f, 1.0f, 0.5f, 1.0f, 0.0f);
        dicStates["Attacking_Running"].isRightNow = true;

        dicStates["Attacking_Idle"] = new UserAnimCross("Attacking_Idle", dicStates["Attacking"], dicStates["Idle"], player);
        (dicStates["Attacking_Idle"] as UserAnimCross).InitTime(0.05f, 0.0f, 0.2f, 0.5f, 1.0f, 0.0f);
        dicStates["Attacking_Idle"].isRightNow = true;

        dicStates["Idle_Attacking"] = new UserAnimCross("Idle_Attacking", dicStates["Idle"], dicStates["Attacking"], player);
        (dicStates["Idle_Attacking"] as UserAnimCross).InitTime(0.5f, 0.0f, 1.0f, 0.5f, 1.0f, 0.0f);
        dicStates["Idle_Attacking"].isRightNow = true;


        player.SetDirection(AnimDirection.forward);
        //player.SetDirection(AnimDirection.backward);
        player.cState = dicStates["Idle"];
    }      
  
    IEnumerator EndTime(float _et)
    {
        float et = _et;
        float ct = 0.0f;        
reset:
        while (ct < et)
        {           
            yield return false;
            //ct = ct + Time.deltaTime;
            ct = ct + Runner.DeltaTime;

            //Debug.Log(ct.ToString());
        }
        ct = 0.0f;

        yield return true;
        goto reset;
    }  

    void TestAnim()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            anim.PlayCross("Idle");
        }
        else if (Input.GetKeyDown(KeyCode.O))
        {
            anim.PlayCross("Running");
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            anim.PlayCross("Attacking");
        }

        //if (Input.GetKeyDown(KeyCode.U))
        //{
        //    anim.PlayLoop("Idle");
        //}
        //else if (Input.GetKeyDown(KeyCode.I))
        //{
        //    anim.PlayLoop("Running");
        //}
        //else if (Input.GetKeyDown(KeyCode.O))
        //{
        //    anim.PlayLoop("Attacking");
        //}

        if (Input.GetKeyDown(KeyCode.K))
        {
            player.SetDirection(AnimDirection.forward);
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            player.SetDirection(AnimDirection.backward);
        }
    }

    protected virtual void UpdateBone()
    {
        if (hasStMesh)
        {
            for (int i = 0; i < stCount; i++)
            {
                float4x4 M = stM[i];

                stTr[i].position = M.c0.xyz;
                stTr[i].rotation = new quaternion(M.c1);
                stTr[i].localScale = M.c2.xyz;
            }
        }
    }  

   

    void Behave()
    {
        {            
            Test_ActionState();
            switch (state)
            {
                case ActionState.Idle:
                    ActState_Idle();
                    break;
                case ActionState.Run:
                    ActState_Run();
                    break;
                case ActionState.Attack:
                    ActState_Attack();
                    break;
                case ActionState.Die:
                    ActState_Die();
                    break;
                case ActionState.Sleep:
                    ActState_Sleep();
                    break;
                case ActionState.ReSpawn:
                    ActState_ReSpawn();
                    break;
                case ActionState.None:
                    ActState_None();
                    break;
            }
        }
    }

    ActionState preState;
    
    public void SinglePause(bool pause)
    {
        if(isActive)
        {
            if (pause)
            {
                //preState = state;
                //state = ActionState.None;
                
            }
            else
            {
                //state = preState;
            }
        }       
    }

    void Test_ActionState()
    {
        attackTr[1] = targetTr;

        bool active = isActive;
        float _rHp = rHp;

        ActionState preState = state;

        //state = (ActionState)stateData;
        if(GameManager.isNvStop)
        {

        }
        else if(state == ActionState.ReSpawn || state == ActionState.None)
        {
            
        }
        else if (active && _rHp <= 0.0f)
        {
            SetState_Die();
            GameManager.killedCount[pNum]++;
            RPC_Play_EffectAudio((int)Type_Audio.Die);
        }      
        else if (active)
        {

            //if (Input.GetKeyDown(KeyCode.Q) && isSelected)
            if (unit_move_stop && isSelected)
            {
                SetState_Idle();                
            }
            else
            {
                if (GetAttackTr() != null && positionTr == null)
                {
                    SetState_Attack();
                }
                else
                {
                    float3 tPos = targetPos;
                    //float3 cPos = transform.position;
                    float3 cPos = ntPos;

                    //if (math.distance(tPos, cPos) < 1.0f) 
                    if (math.distance(tPos, cPos) < 0.5f)
                    {
                        SetState_Idle();
                    }                  
                    else
                    {
                        //DebugInfo.Log("Run()");
                        SetState_Run();
                    }
                }
            }
        }
        else if (state == ActionState.Die)
        {
            if ((bool)(et_die.Current))
            {
                SetState_Sleep();
            }
        }

        //stateData = (int)state;

        //{
        //    //SetStateData((int)state);
        //    RPC_SetStateData((int)state);
        //    //RPC_SetStateData1((int)state);
        //
        //
        //    //RPC_SetStateData_Id((int)state, Object.Id);
        //    //stateData_nt = (int)state;
        //}

        {
            RPC_SetStateData((int)state);
        }

        //if(preState != state)
        //{
        //    RPC_SetStateData((int)state);
        //}

        {
            //RPC_isActive(active);
        }
    }
    
  


    protected void AnimPlayCross(string clip)
    {              
        switch (clip)
        {
            case "Idle":
                RPC_AnimPlayCross(0);
                break;
            case "Running":
                RPC_AnimPlayCross(1);
                break;
            case "Attacking":
                RPC_AnimPlayCross(2);
                break;
        }       
    }


    public void AnimPlayCross1(string B)
    {        
        if (player != null)
        {
            if (player.cState != null)
            {
                if (player.cState is UserAnimLoop)
                {
                    string A = player.cState.name;

                    if(A != B)
                    {
                        ApplyCross(A, B);
                    }
                    
                    //if (dicStates.ContainsKey(A + "_" + B))
                    //{
                    //    player.nState = dicStates[A + "_" + B];
                    //    player.nnState = dicStates[B];                        
                    //}
                }
            }
        }
    }


    public void ApplyCross(string A, string B)
    {
        int a = 0;
        int b = 0;

        switch (A)
        {
            case "Idle":
                a = 0;
                break;
            case "Running":
                a = 1;
                break;
            case "Attacking":
                a = 2;
                break;
        }

        switch (B)
        {
            case "Idle":
                b = 0;
                break;
            case "Running":
                b = 1;
                break;
            case "Attacking":
                b = 2;
                break;
        }

        RPC_ChangeCross(a, b);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ChangeCross(int a, int b)
    {
        string A = "Idle";
        string B = "Idel";

        switch (a)
        {
            case 0:
                A = "Idle";
                break;
            case 1:
                A = "Running";
                break;
            case 2:
                A = "Attacking";
                break;
        }

        switch (b)
        {
            case 0:
                B = "Idle";
                break;
            case 1:
                B = "Running";
                break;
            case 2:
                B = "Attacking";
                break;
        }

        if (dicStates.ContainsKey(A + "_" + B))
        {
            player.nState = dicStates[A + "_" + B];
            player.nnState = dicStates[B];
        }
    }



    protected virtual void ActState_Idle()
    {
        if(!nvAgent.isOnNavMesh)
        {
            return;
        }
        
        //RPC_nvStop(true);
        
        nvAgent.isStopped = true;
        //targetPos = transform.position;
        targetPos = ntPos;
        
        positionTr = null;
        ClearAttackTr();
        
        //RPC_Set_positionTr_Null(true);
        //RPC_ClearAttackTr();
        
        {
            aRadius = aRadiusDef;
            vRadius = vRadiusDef;
        }
        
        float speed = math.length(nvAgent.velocity);
        //if (speed < 0.01f)  
        if (speed < 0.1f)
        {
            //anim.PlayCross("Idle");
            AnimPlayCross("Idle");
        }    
        else
        {
            //anim.PlayCross("Running");
            AnimPlayCross("Running");
        }
    }   

    protected virtual void ActState_Run()
    {
        if (!nvAgent.isOnNavMesh)
        {
            //DebugInfo.Log("!nvAgent.isOnNavMesh");

            return;
        }

        AnimPlayCross("Running");

        if (GameManager.isNvStop)
        {
            nvAgent.isStopped = true;           
        }
        else
        {
            //DebugInfo.Log("ActState_Run()");

            //anim.PlayCross("Running");

            nvAgent.isStopped = false;
            nvAgent.SetDestination(targetPos);
            //RPC_SetDestination(targetPos);     
        }
    }    

    public float da_dt = 90.0f;
    public float bodyRadius;

    protected virtual void ActState_Attack0()
    {
        if (!nvAgent.isOnNavMesh)
        {
            return;
        }

        

        Transform _targetTr = GetAttackTr();       
        
        if (_targetTr != null)
        {                       
            UnitActor _targetActor = _targetTr.GetComponent<UnitActor>();

            float3 forward0 = math.rotate(ntRot, new float3(0.0f, 0.0f, 1.0f));
            float3 forward1 = (float3)_targetActor.ntPos - (float3)ntPos;
            float dist = math.length(forward1);

            targetPos = ntPos;
            positionTr = null;
            //RPC_Set_positionTr_Null(true);

            float r0 = bodyRadius;           
            float r1 = _targetTr.GetComponent<UnitActor>().bodyRadius;           
          
            _aRadius = 1.5f * (r0 + r1);           
            if (0.1f < dist && dist <= _aRadius)
            {
                nvAgent.isStopped = true;
                //RPC_nvStop(true);
                
                forward1 = math.normalize(new float3(forward1.x, 0.0f, forward1.z));
                float cosA = math.dot(forward0, forward1);
                if (cosA < 0.95f)
                {
                    //anim.PlayCross("Running");
                    AnimPlayCross("Running");

                    float sinA = math.dot(math.cross(forward0, forward1), new float3(0.0f, 1.0f, 0.0f));

                    //float da = da_dt * Time.deltaTime;
                    float da = da_dt * Runner.DeltaTime;


                    if (sinA > 0.0f)
                    {
                        da *= +1.0f;
                    }
                    else
                    {
                        da *= -1.0f;
                    }

                    //DebugInfo.Log("Rotate!!!");

                    //da = 10.0f;
                   
                    //{
                    //    transform.rotation = math.mul(transform.rotation,
                    //        quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(da)));
                    //}

                    {
                        transform.rotation = math.mul(ntRot,
                            quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(da)));
                    }

                    //{
                    //    Quaternion rot = math.mul(ntRot,
                    //        quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(da)));
                    //
                    //    ntTransform.WriteRotation(rot);
                    //}


                }
                else
                {
                    //anim.PlayCross("Attacking");
                    AnimPlayCross("Attacking");
                }
            }
            else if (_aRadius < dist)
            {
                //anim.PlayCross("Running");
                AnimPlayCross("Running");

                {
                    vRadius = dist * 1.5f;
                }

                {
                    nvAgent.isStopped = false;
                    nvAgent.SetDestination(_targetTr.position);
                    //RPC_SetDestination(_targetTr.position);
                }              
            }
        }
    }


    protected bool bAttackStarted = false;
    public float stime = 0.25f;

    protected virtual void ActState_Attack()
    {
        if (!nvAgent.isOnNavMesh)
        {
            return;
        }
      

        Transform _targetTr = GetAttackTr();

        if (_targetTr != null)
        {
            UnitActor _targetActor = _targetTr.GetComponent<UnitActor>();

            float3 forward0 = math.rotate(ntRot, new float3(0.0f, 0.0f, 1.0f));
            float3 forward1 = (float3)_targetActor.ntPos - (float3)ntPos;
            float dist = math.length(forward1);

            targetPos = ntPos;
            positionTr = null;
            //RPC_Set_positionTr_Null(true);

            float r0 = bodyRadius;
            float r1 = _targetTr.GetComponent<UnitActor>().bodyRadius;

            _aRadius = 1.5f * (r0 + r1);
            if (0.1f < dist && dist <= _aRadius)
            {
                nvAgent.isStopped = true;
                //RPC_nvStop(true);

                forward0 = math.normalize(new float3(forward0.x, 0.0f, forward0.z));
                forward1 = math.normalize(new float3(forward1.x, 0.0f, forward1.z));
                float cosA = math.dot(forward0, forward1);

                //if (cosA < 0.95f)
                if (cosA < 0.999f)
                {
                    //anim.PlayCross("Running");
                    AnimPlayCross("Running");

                    float sinA = math.dot(math.cross(forward0, forward1), new float3(0.0f, 1.0f, 0.0f));

                    //float da = da_dt * Time.deltaTime;
                    float da = da_dt * Runner.DeltaTime;
                    //float da = da_dt * Time.fixedDeltaTime;
                    
                    if (sinA > 0.0f)
                    {
                        da *= +1.0f;
                    }
                    else
                    {
                        da *= -1.0f;
                    }

                    //DebugInfo.Log("Rotate!!!");

                    //da = 10.0f;

                    //{
                    //    transform.rotation = math.mul(transform.rotation,
                    //        quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(da)));
                    //}

                    //{
                    //    transform.rotation = math.mul(ntRot,
                    //        quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(da)));                        
                    //}

                    {
                        Quaternion rot = math.mul(ntRot,
                            quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(da)));

                        //transform.rotation = rot;
                        //transform.localRotation = rot;
                        //ntTransform.WriteRotation(rot);                                                     
                        ntTransform.TeleportToRotation(rot);
                    }                    

                }
                else
                {
                    //anim.PlayCross("Attacking");
                    AnimPlayCross("Attacking");

                    //{
                    //    transform.rotation = quaternion.LookRotation(forward1, new float3(0.0f, 1.0f, 0.0f));
                    //}

                    {
                        Quaternion rot = quaternion.LookRotation(forward1, new float3(0.0f, 1.0f, 0.0f));

                        //transform.rotation = rot;
                        //transform.localRotation = rot;
                        //ntTransform.WriteRotation(rot);
                        ntTransform.TeleportToRotation(rot);
                    }


                    UserAnimPlayer player = anim.player;
                    UserAnimState animState = player.cState;
                    //float stime = 0.25f;
                    if (animState is UserAnimLoop)
                    {
                        UserAnimLoop animLoop = (animState as UserAnimLoop);
                        if (animLoop.name == "Attacking")
                        {
                            float ut = animLoop.ut;

                            if (math.abs(ut - stime) < 0.05f)
                            {
                                if (bAttackStarted == false)
                                {
                                    //ArrowManager.ShootArrow(this, shootTr, _targetTr);
                                    //GameManager.arrowMan.RPC_ShootArrow(Object.Id, _targetActor.Object.Id);
                                    //RPC_Attack_DamageHp(_targetActor.Object.Id, hitHp);

                                    _targetActor.RPC_DamageHp(hitHp);
                                    RPC_Play_EffectAudio((int)Type_Audio.Attack);

                                    bAttackStarted = true;

                                    //DebugInfo.Log("bAttackStarted");

                                    //AudioPlay(0);
                                    //AudioAttackPlay();
                                }
                            }
                            else
                            {
                                bAttackStarted = false;
                            }
                        }
                    }
                }
            }
            else if (_aRadius < dist)
            {
                //anim.PlayCross("Running");
                AnimPlayCross("Running");

                if (GameManager.isNvStop)
                {
                    nvAgent.isStopped = true;                    
                }
                else
                {
                    {
                        vRadius = dist * 1.5f;
                    }

                    {
                        nvAgent.isStopped = false;
                        nvAgent.SetDestination(_targetTr.position);
                        //RPC_SetDestination(_targetTr.position);
                    }
                }              
            }
        }
    }


    protected virtual void ActState_Die()
    {
        if (!nvAgent.isOnNavMesh)
        {
            return;
        }

        //float3 pos = transform.position;
        float3 pos = ntPos;

        //anim.PlayCross("Idle");
        AnimPlayCross("Idle");
        
        bodyCollider.enabled = false;
        hitCollider.enabled = false;
        nvAgent.enabled = true;

        nvAgent.isStopped = true;
        //RPC_nvStop(true);        

        targetPos = pos;
        positionTr = null;
        //RPC_Set_positionTr_Null(true);

        //isActive = false;
        RPC_Set_isActive(false);

        //DebugInfo.Log("ActState_Die()");

        et_die.MoveNext();        
    }

    protected virtual void ActState_Sleep()
    {
        //float3 pos = transform.position;
        float3 pos = ntPos;
        //float3 pos = unitMan.transform.position;

        //anim.PlayCross("Idle");
        AnimPlayCross("Idle");

        bodyCollider.enabled = false;
        hitCollider.enabled = false;        
        nvAgent.enabled = false;

        //ntTransform.enabled = false;        

        targetPos = pos;
        //ntTransform.WritePosition(pos);
        ntTransform.TeleportToPosition(new Vector3(0.0f, -10.0f, 0.0f));

        positionTr = null;       
        //RPC_Set_positionTr_Null(true);
    }

    protected virtual void ActState_ReSpawn()
    {        
        
        {
            float3 pos = unitMan.transform.position;

            //float3 sPos = transform.position;

            Ray ray = new Ray((Vector3)pos + new Vector3(0.0f, 50.0f, 0.0f), Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100.0f, LayerMask.GetMask("Terrain")))
            {
                pos.y = hit.point.y;
            }


            targetPos = pos;
            ntTransform.TeleportToPosition(pos);            
        }
       
        isActive = true;
        //Hp = maxHp;

        state = ActionState.None;
    }

    protected virtual void ActState_None()
    {
        ntTransform.TeleportToPosition(targetPos);

    }


    [Serializable]
    public enum ActionState : int
    {
        Idle = 0, Run = 1, Attack = 2, Die = 3, Sleep = 4, ReSpawn = 5, None = 6
    }

    public ActionState state;

    public void SetState_Idle()
    {
        state = ActionState.Idle;
    }

    public void SetState_Run()
    {
        state = ActionState.Run;
    }

    public void SetState_Attack()
    {
        state = ActionState.Attack;
    }

    public void SetState_Die()
    {
        state = ActionState.Die;
    }

    public void SetState_Sleep()
    {
        state = ActionState.Sleep;
    }

    private void OnTriggerEnter1(Collider other)
    {
        var hitGo = other.gameObject;

//#if UNITY_EDITOR
//        Debug.Log($"{gameObject.name} to {other.gameObject.name}");
//#endif

        if(Object.HasStateAuthority)
        {
            if (isActive)
            {
                HitActor hitActor = other.GetComponent<HitActor>();
                if (hitActor != null)
                {
                    UnitActor actor = hitActor.unitActor;
                    if (actor.isActive && actor.state == ActionState.Attack)
                    {
                        {
                            RPC_DamageHp(actor.hitHp);
                        }
                    }
                }
            }
        }        
    }   


    private void OnTriggerEnter0(Collider other)
    {
        var hitGo = other.gameObject;

//#if UNITY_EDITOR
//        Debug.Log($"{gameObject.name} to {other.gameObject.name}");
//#endif

        if (isActive)
        {
            HitActor hitActor = other.GetComponent<HitActor>();
            if (hitActor != null)
            {
                UnitActor actor = hitActor.unitActor;
                if (actor.isActive && actor.state == ActionState.Attack)
                {
                    {                       
                        DamageHp(actor.hitHp);                        
                    }
                }
            }
        }
    }

    public void DamageHp(float dHp)
    {
        float hp = Hp - dHp;
        if (hp < 0.0f)
        {
            hp = 0.0f;
            //hp = maxHp;
        }

        Hp = hp;
    }
  
}