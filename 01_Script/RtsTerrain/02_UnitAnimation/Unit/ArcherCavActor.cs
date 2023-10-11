using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;

using UnityEngine;

using UserAnimSpace;

public class ArcherCavActor : UnitActor
{
    public override void Init(string[] stNames, float4x4[] stM, bool hasStMesh)
    {
        base.Init(stNames, stM, hasStMesh);
    }

    public override void Begin()
    {
        base.Begin();
    }
   
    public override void Update()
    {
        base.Update();
    }

    //bool bArrowStarted = false;
    //public Transform shootTr;


    protected override void ActState_Idle()
    {
        if (!nvAgent.isOnNavMesh)
        {
            return;
        }

        nvAgent.isStopped = true;        

        //targetPos = transform.position;
        targetPos = ntPos;

        positionTr = null;
        ClearAttackTr();
        

        float4 _terrainArea = terrainArea;

        if (_terrainArea.y == 1.0f || _terrainArea.z == 1.0f)
        {
            vRadius = vRadiusDef;
            aRadius = vRadius;
        }
        else
        {
            vRadius = vRadiusDef;
            aRadius = aRadiusDef;
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

    protected override void ActState_Attack()
    {
        if (!nvAgent.isOnNavMesh)
        {
            return;
        }

        if (GameManager.isNvStop)
        {
            nvAgent.isStopped = true;

            return;
        }

        int trId = -1;
        Transform _targetTr = GetAttackTr(out trId);
        float _aRadius = aRadius;
        float4 _terrainArea = terrainArea;

        if (_targetTr != null)
        {
            UnitActor _targetActor = _targetTr.GetComponent<UnitActor>();

            float3 forward0 = math.rotate(ntRot, new float3(0.0f, 0.0f, 1.0f));
            float3 forward1 = (float3)_targetActor.ntPos - (float3)ntPos;
            float dist = math.length(forward1);

            targetPos = ntPos;
            positionTr = null;            

            if (_terrainArea.y == 1.0f || _terrainArea.z == 1.0f)
            {
                vRadius = vRadiusDef;
                aRadius = vRadius;
            }
            else
            {
                vRadius = vRadiusDef;
                aRadius = aRadiusDef;
            }

            _aRadius = aRadius;

            if (0.1f < dist && dist <= _aRadius)
            {
                nvAgent.isStopped = true;

                forward0 = math.normalize(new float3(forward0.x, 0.0f, forward0.z));
                forward1 = math.normalize(new float3(forward1.x, 0.0f, forward1.z));
                float cosA = math.dot(forward0, forward1);

                if (cosA < 0.999f)
                //if (cosA < 0.95f)
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

                    //{
                    //    transform.rotation = math.mul(transform.rotation,
                    //    quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(da)));
                    //
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
                    float stime = 0.25f;
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
                                    GameManager.arrowMan.RPC_ShootArrow(Object.Id, _targetActor.Object.Id);
                                    RPC_Play_EffectAudio((int)Type_Audio.Attack);

                                    bAttackStarted = true;

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
                    if (trId == 0)
                    {
                        nvAgent.isStopped = false;
                        nvAgent.SetDestination(_targetTr.position);
                    }
                    else
                    {
                        float3 pos = (float3)transform.position + math.normalize(forward1) * 1.0f;

                        nvAgent.isStopped = false;
                        nvAgent.SetDestination(pos);
                    }
                }                
            }
        }
    }

    //protected override void ActState_Attack1()
    //{
    //    if (!nvAgent.isOnNavMesh)
    //    {
    //        return;
    //    }
    //
    //    int trId = -1;
    //    Transform _targetTr = GetAttackTr(out trId);
    //    float _aRadius = aRadius;
    //    float4 _terrainArea = terrainArea;
    //
    //    float3 _ntPos = ntTransform.ReadPosition();
    //    quaternion _ntRot = ntTransform.ReadRotation();
    //
    //    if (_targetTr != null)
    //    {
    //        UnitActor _targetActor = _targetTr.GetComponent<UnitActor>();
    //
    //        float3 forward0 = math.rotate(_ntRot, new float3(0.0f, 0.0f, 1.0f));
    //        float3 forward1 = (float3)_targetActor.ntPos - (float3)_ntPos;
    //        float dist = math.length(forward1);
    //
    //        targetPos = ntPos;
    //        positionTr = null;
    //
    //        if (_terrainArea.y == 1.0f || _terrainArea.z == 1.0f)
    //        {
    //            vRadius = vRadiusDef;
    //            aRadius = vRadius;
    //        }
    //        else
    //        {
    //            vRadius = vRadiusDef;
    //            aRadius = aRadiusDef;
    //        }
    //
    //        _aRadius = aRadius;
    //
    //        if (0.1f < dist && dist <= _aRadius)
    //        {
    //            nvAgent.isStopped = true;
    //
    //            forward0 = math.normalize(new float3(forward0.x, 0.0f, forward0.z));
    //            forward1 = math.normalize(new float3(forward1.x, 0.0f, forward1.z));
    //            float cosA = math.dot(forward0, forward1);
    //
    //            if (cosA < 0.999f)
    //            //if (cosA < 0.95f)
    //            {
    //                //anim.PlayCross("Running");
    //                AnimPlayCross("Running");
    //
    //                float sinA = math.dot(math.cross(forward0, forward1), new float3(0.0f, 1.0f, 0.0f));
    //
    //                //float da = da_dt * Time.deltaTime;
    //                float da = da_dt * Runner.DeltaTime;
    //                //float da = da_dt * Time.fixedDeltaTime;
    //
    //                if (sinA > 0.0f)
    //                {
    //                    da *= +1.0f;
    //                }
    //                else
    //                {
    //                    da *= -1.0f;
    //                }
    //
    //                //{
    //                //    transform.rotation = math.mul(transform.rotation,
    //                //    quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(da)));
    //                //
    //                //}
    //
    //                //{
    //                //    transform.rotation = math.mul(ntRot,
    //                //        quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(da)));                        
    //                //}
    //
    //                {
    //                    Quaternion rot = math.mul(_ntRot,
    //                        quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(da)));
    //
    //                    //transform.rotation = rot;
    //                    transform.localRotation = rot;
    //                    ntTransform.WriteRotation(rot);
    //                }
    //            }
    //            else
    //            {
    //                //anim.PlayCross("Attacking");
    //                AnimPlayCross("Attacking");
    //
    //                //{
    //                //    transform.rotation = quaternion.LookRotation(forward1, new float3(0.0f, 1.0f, 0.0f));
    //                //}
    //
    //                {
    //                    Quaternion rot = quaternion.LookRotation(forward1, new float3(0.0f, 1.0f, 0.0f));
    //
    //                    //transform.rotation = rot;
    //                    transform.localRotation = rot;
    //                    ntTransform.WriteRotation(rot);
    //                }
    //
    //                UserAnimPlayer player = anim.player;
    //                UserAnimState animState = player.cState;
    //                float stime = 0.25f;
    //                if (animState is UserAnimLoop)
    //                {
    //                    UserAnimLoop animLoop = (animState as UserAnimLoop);
    //                    if (animLoop.name == "Attacking")
    //                    {
    //                        float ut = animLoop.ut;
    //
    //                        if (math.abs(ut - stime) < 0.05f)
    //                        {
    //                            if (bAttackStarted == false)
    //                            {
    //                                //ArrowManager.ShootArrow(this, shootTr, _targetTr);
    //                                GameManager.arrowMan.RPC_ShootArrow(Object.Id, _targetActor.Object.Id);
    //
    //                                bAttackStarted = true;
    //
    //                                //AudioPlay(0);
    //                                //AudioAttackPlay();
    //                            }
    //                        }
    //                        else
    //                        {
    //                            bAttackStarted = false;
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //        else if (_aRadius < dist)
    //        {
    //            //anim.PlayCross("Running");
    //            AnimPlayCross("Running");
    //
    //
    //            if (trId == 0)
    //            {
    //                nvAgent.isStopped = false;
    //                nvAgent.SetDestination(_targetTr.position);
    //            }
    //            else
    //            {
    //                float3 pos = (float3)transform.position + math.normalize(forward1) * 1.0f;
    //
    //                nvAgent.isStopped = false;
    //                nvAgent.SetDestination(pos);
    //            }
    //
    //        }
    //    }
    //}

}

