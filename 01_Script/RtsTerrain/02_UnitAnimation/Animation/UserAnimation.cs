using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;

using UnityEngine;
using Utility_JSB;


namespace UserAnimSpace
{
    public class UserAnimation : MonoBehaviour
    {
        public BoneNode[] bns;           
        public Dictionary<string, BoneCurve[]> dicCurves;
        public Dictionary<string, UserAnimState> dicStates;
        public UserAnimPlayer player;

        public void Init(BoneNode[] bns, Dictionary<string, BoneCurve[]> dicCurves)
        {
            this.bns = bns;
            this.dicCurves = dicCurves;

            this.player = new UserAnimPlayer(this, true);
            this.dicStates = new Dictionary<string, UserAnimState>();

            foreach(var kv in dicCurves)
            {
                dicStates[kv.Key] = new UserAnimLoop(player, dicCurves[kv.Key]);
            }

            //dicStates["Idle"].ct = 1.0f;
        }

        public void Init1(BoneNode[] bns, Dictionary<string, BoneCurve[]> dicCurves, int iid)
        {
            this.bns = bns;
            this.dicCurves = dicCurves;

            this.player = new UserAnimPlayer(this, true);
            this.dicStates = new Dictionary<string, UserAnimState>();

            //SetPlayerData(playerData);
            this.player.iid = iid;

            int i = 0;
            foreach (var kv in dicCurves)           
            {               
                dicStates[kv.Key] = new UserAnimLoop(player, dicCurves[kv.Key], i);
                i++;
            }

            //dicStates["Idle"].ct = 1.0f;
        }

        public void PlayLoop(string A)
        {
            if (dicStates.ContainsKey(A))
            {
                UserAnimState state = dicStates[A];
                player.cState = state;
                state.isStarting = false;

                player.nState = null;
                player.nnState = null;
            }
        }


        public void PlayLoop1(string A)
        {
            if (dicStates.ContainsKey(A))
            {
                UserAnimState state = dicStates[A];
               
                if(player.cState != state)
                {
                    player.cState = state;
                    state.isStarting = false;

                    player.nState = null;
                    player.nnState = null;
                }               
            }
        }

        //public void PlayCross(string B)
        //{
        //    if (player.cState is UserAnimLoop)
        //    {
        //        string A = player.cState.name;
        //        if (dicStates.ContainsKey(A + "_" + B))
        //        {
        //            player.nState = dicStates[A + "_" + B];
        //            player.nnState = dicStates[B];
        //        }
        //    }
        //}

        public void PlayCross(string B)
        {
            if(player != null)
            {
                if(player.cState != null)
                {
                    if (player.cState is UserAnimLoop)
                    {
                        string A = player.cState.name;
                        if (dicStates.ContainsKey(A + "_" + B))
                        {
                            player.nState = dicStates[A + "_" + B];
                            player.nnState = dicStates[B];
                        }
                    }
                }
            }            
        }

        public void PlayCross_Fusion(string B)
        {
            if (player != null)
            {
                if (player.cState != null)
                {
                    if (player.cState is UserAnimLoop)
                    {
                        string A = player.cState.name;
            
                        if(dicStates != null && A != null)
                        {
                            if (dicStates.ContainsKey(A + "_" + B))
                            {
                                //if(player.nState != null && player.nnState != null)
            
                                if(dicStates.ContainsKey(B))
                                {
                                    player.nState = dicStates[A + "_" + B];
                                    player.nnState = dicStates[B];
                                }                               
                            }
                        }                      
                    }
                }
            }
        }


        //
        public void SetPlayerData(AnimPlayerData[] playerData)
        {
            if(player != null)
            {
                player.playerData = playerData;
            }          
        }

        public void BakeAnimation(string A, int j)
        {
            if (dicStates.ContainsKey(A))
            {
                UserAnimLoop state = dicStates[A] as UserAnimLoop;
                state.BakeAnimation(j);              
            }
        }
    }

    [System.Serializable]
    public struct AnimPlayerData
    {
        public uint type; //loop or cross

        public uint2 cid;   //clip index
        public uint2 fid;   //frmae index
        public float3 u;        
    };

    public class UserAnimPlayer
    {
        public BoneNode[] bns;
        public UserAnimation anim;
        public AnimPlayerData[] playerData;
        public int iid;

        public float speed
        {
            get; set;
        }

        public AnimDirection animDir
        {
            get;
            protected set;
        }

        public event Action<AnimDirection> ESetDirection;

        public void SetDirection(AnimDirection dir)
        {
            animDir = dir;
            ESetDirection(dir);
        }

        public bool bRigid
        {
            get; set;
        }

        public class Node
        {
            public Node pNode;
            public UserAnimState state;
            public Node nNode;
        }

        public UserAnimPlayer(UserAnimation anim, bool bRigid)
        {
            this.anim = anim;
            this.speed = 1.0f;
            this.bns = anim.bns;
            this.bRigid = bRigid;

            nodes = new Node[3];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = new Node();               
            }

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].nNode = nodes[(i + 1) % nodes.Length];
                nodes[(i + 1) % nodes.Length].pNode = nodes[i];
            }

            cNode = nodes[0];
        }
        private Node[] nodes;
        public Node cNode
        {
            get; set;
        }

        public Node pNode
        {
            get
            {
                return cNode.pNode;
            }
        }

        public Node ppNode
        {
            get
            {
                return cNode.pNode.pNode;
            }
        }

        public Node nNode
        {
            get
            {
                return cNode.nNode;
            }
        }

        public Node nnNode
        {
            get
            {
                return cNode.nNode.nNode;
            }
        }

        public UserAnimState cState
        {
            get
            {
                return cNode.state;
            }
            set
            {
                cNode.state = value;
            }
        }

        public UserAnimState pState
        {
            get
            {
                return pNode.state;
            }
            set
            {
                pNode.state = value;
            }
        }

        public UserAnimState ppState
        {
            get
            {
                return ppNode.state;
            }
            set
            {
                ppNode.state = value;
            }
        }

        public UserAnimState nState
        {
            get
            {
                return nNode.state;
            }
            set
            {
                nNode.state = value;
            }
        }

        public UserAnimState nnState
        {
            get
            {
                return nnNode.state;
            }
            set
            {
                nnNode.state = value;
            }
        }

        public void MoveNext()
        {
            cNode = cNode.nNode;
        }

        public void MovePre()
        {
            cNode = cNode.pNode;
        }

    }

    public enum AnimDirection
    {
        forward, backward
    }

    public abstract class UserAnimState
    {
        public UserAnimState(UserAnimPlayer player)
        {
            this.player = player;
            this.player.ESetDirection += SetDirection;
            this.inView = true;
        }

        public UserAnimPlayer player
        {
            get; set;
        }
       

        public string name
        {
            get; set;
        }

        public bool isStarting
        {
            get; set;
        }

        public float sp
        {
            get
            {
                return player.speed;
            }
        }

        public float ct
        {
            get;
            set;
        }

        protected float[,] initTime
        {
            get; set;
        }

        public bool isRightNow
        {
            get; set;
        }

        public abstract void SetDirection(AnimDirection dir);

        public abstract float rt0
        {
            get; set;
        }

        public abstract float rt1
        {
            get; set;
        }

        public abstract float rt
        {
            get;
        }
       
        public abstract bool inView
        {
            get; set;
        }

        public abstract void Sample(float dt);    
        
        public abstract void Sample_Baked(float dt);

        public abstract void Sample_Total(float dt);

    }

    public class UserAnimLoop : UserAnimState
    {
        public UserAnimLoop(UserAnimPlayer player, BoneCurve[] bcs) : base(player)
        {            
            this.ct = bcs[0].ct;
            this.name = bcs[0].clipName;
            
            this.isStarting = false;
            this.isPlaying = false;
            
            this.rt0 = 0.0f;
            this.rt1 = 1.0f;
          
            this.bcs = bcs;
            this.bns = player.bns;
            this.count = player.bns.Length;
            this.bRigid = player.bRigid;
          
            initTime = new float[2, 2];
            this.InitTime(0.0f, 1.0f, 1.0f, 0.0f);
            this.SetDirection(AnimDirection.forward);
        }

        public UserAnimLoop(UserAnimPlayer player, BoneCurve[] bcs, int clipId) : base(player)
        {
            this.ct = bcs[0].ct;
            this.name = bcs[0].clipName;

            this.isStarting = false;
            this.isPlaying = false;

            this.rt0 = 0.0f;
            this.rt1 = 1.0f;

            this.bcs = bcs;
            this.bns = player.bns;
            this.count = player.bns.Length;
            this.bRigid = player.bRigid;

            initTime = new float[2, 2];
            this.InitTime(0.0f, 1.0f, 1.0f, 0.0f);
            this.SetDirection(AnimDirection.forward);

            this.clipId = clipId;
        }

        public int clipId
        {
            get; set;
        }

        public bool bRigid
        {
            get; protected set;
        }

        public int count
        {
            get; protected set;
        }

        public BoneCurve[] bcs
        {
            get; set;
        }

        public BoneNode[] bns
        {
            get; set;
        }

        public void InitTime(float ft0, float ft1, float bt0, float bt1)
        {
            initTime[0, 0] = ft0;
            initTime[0, 1] = ft1;
            initTime[1, 0] = bt0;
            initTime[1, 1] = bt1;
        }

        public float lt
        {
            get;
            private set;
        }

        public float ut
        {
            get;
            private set;
        }

        public bool isPlaying
        {
            get; set;
        }

        public override void SetDirection(AnimDirection dir)
        {
            if (dir == AnimDirection.forward)
            {
                rt0 = initTime[0, 0];
                rt1 = initTime[0, 1];
            }
            else
            {
                rt0 = initTime[1, 0];
                rt1 = initTime[1, 1];
            }
        }
       
        public override float rt0
        {
            get; set;
        }

        public override float rt1
        {
            get; set;
        }

        public override float rt
        {
            get
            {
                return ut / ct;
            }
        }
        
        public override bool inView
        {
            get; set;
        }

        public override void Sample(float dt)
        {
            if (isStarting == false)
            {
                lt = (rt1 - rt0) * ct;
                ut = math.frac(rt0) * ct;
                isStarting = true;
            }
            else
            {
                if (lt >= 0.0f)
                {
                    ut = ut + sp * dt;

                    if (ut >= ct)
                    {
                        ut = ut - ct;
                    }
                }
                else //if(lt < 0.0f) 
                {
                    ut = ut - sp * dt;

                    if (ut < 0.0f)
                    {
                        ut = ut + ct;
                    }
                }
            }

            bRigid = player.bRigid;

            //if(inView)
            {
                for (int i = 0; i < count; i++)
                {
                    if (bRigid)
                    {
                        bcs[i].SampleDQ(ut, bns[i]);
                    }
                    else
                    {
                        //bcs[i].Sample(ut, bns[i], true);
                        bcs[i].Sample(ut, bns[i], false);
                    }
                }
            }
              

            if (player.nState != null)
            {
                if (math.abs(math.frac(player.nState.rt0) - rt) <= 0.01f)
                {
                    player.nState.isStarting = false;

                    player.MoveNext();
                    player.pState.isStarting = false;
                    player.pState = null;
                }
                else if (player.nState.isRightNow == true)
                {
                    player.nState.rt0 = rt;
                    player.nState.isStarting = false;

                    player.MoveNext();
                    player.pState.isStarting = false;
                    player.pState = null;
                }
            }
        }

        public float ComputeUtPart(
           float dt, float rt0, float rt1)
        {
            if (isStarting == false)
            {
                ut = math.frac(rt0) * ct;
                lt = ut + (rt1 - rt0) * ct;
                isStarting = true;
                isPlaying = true;
            }
            else
            {
                if (ut <= lt)
                {
                    ut = ut + sp * dt;

                    if (lt <= ut)
                    {
                        isPlaying = false;
                    }
                    else
                    {
                        if (ct <= ut)
                        {
                            ut = ut - ct;
                            lt = lt - ct;
                        }
                    }

                }
                else //if(lt < ut)
                {
                    ut = ut - sp * dt;

                    if (ut < lt)
                    {
                        isPlaying = false;
                    }
                    else
                    {
                        if (ut < 0.0f)
                        {
                            ut = ut + ct;
                            lt = lt + ct;
                            //player.loopCount++;
                        }
                    }
                }
            }

            return ut;
        }


        //BakedAnimation
        public void BakeAnimation(int j)
        {
            for (int i = 0; i < count; i++)
            {
                if (bRigid)
                {
                    bcs[i].BakeDQ(j, bns[i]);
                }
                else
                {
                   
                }
            }
        }

        public override void Sample_Baked(float dt)
        {
            if (isStarting == false)
            {
                lt = (rt1 - rt0) * ct;
                ut = math.frac(rt0) * ct;
                isStarting = true;
            }
            else
            {
                if (lt >= 0.0f)
                {
                    ut = ut + sp * dt;

                    if (ut >= ct)
                    {
                        ut = ut - ct;
                    }
                }
                else //if(lt < 0.0f) 
                {
                    ut = ut - sp * dt;

                    if (ut < 0.0f)
                    {
                        ut = ut + ct;
                    }
                }
            }

            bRigid = player.bRigid;
           
            {               
                int fid;
                float u;
                bcs[0].GetFrameInfo(ut, out fid, out u);

                int iid = player.iid;
                player.playerData[iid].type = 0;
                player.playerData[iid].cid.x = (uint)clipId;
                player.playerData[iid].fid.x = (uint)fid;
                player.playerData[iid].u.x = u;
            }


            if (player.nState != null)
            {
                if (math.abs(math.frac(player.nState.rt0) - rt) <= 0.01f)
                {
                    player.nState.isStarting = false;

                    player.MoveNext();
                    player.pState.isStarting = false;
                    player.pState = null;
                }
                else if (player.nState.isRightNow == true)
                {
                    player.nState.rt0 = rt;
                    player.nState.isStarting = false;

                    player.MoveNext();
                    player.pState.isStarting = false;
                    player.pState = null;
                }
            }
        }

        public override void Sample_Total(float dt)
        {
            if (isStarting == false)
            {
                lt = (rt1 - rt0) * ct;
                ut = math.frac(rt0) * ct;
                isStarting = true;
            }
            else
            {
                if (lt >= 0.0f)
                {
                    ut = ut + sp * dt;

                    if (ut >= ct)
                    {
                        ut = ut - ct;
                    }
                }
                else //if(lt < 0.0f) 
                {
                    ut = ut - sp * dt;

                    if (ut < 0.0f)
                    {
                        ut = ut + ct;
                    }
                }
            }

            bRigid = player.bRigid;

            {
                int fid;
                float u;
                bcs[0].GetFrameInfo(ut, out fid, out u);

                int iid = player.iid;
                player.playerData[iid].type = 0;
                player.playerData[iid].cid.x = (uint)clipId;
                player.playerData[iid].fid.x = (uint)fid;
                player.playerData[iid].u.x = u;
            }


            if (player.nState != null)
            {
                if (math.abs(math.frac(player.nState.rt0) - rt) <= 0.01f)
                {
                    player.nState.isStarting = false;

                    player.MoveNext();
                    player.pState.isStarting = false;
                    player.pState = null;
                }
                else if (player.nState.isRightNow == true)
                {
                    player.nState.rt0 = rt;
                    player.nState.isStarting = false;

                    player.MoveNext();
                    player.pState.isStarting = false;
                    player.pState = null;
                }
            }
        }

    }

    public class UserAnimCross : UserAnimState
    {
        public UserAnimLoop stateA
        {
            get; set;
        }

        public UserAnimLoop stateB
        {
            get; set;
        }

        public BoneCurve[] curvesA
        {
            get;
            private set;
        }

        public BoneCurve[] curvesB
        {
            get;
            private set;
        }

        public UserAnimCross(string name, UserAnimState stateA, UserAnimState stateB, UserAnimPlayer player) : base(player)
        {
            this.name = name;
            this.bns = player.bns;
            this.count = player.bns.Length;
            this.bRigid = player.bRigid;

            this.stateA = (UserAnimLoop)stateA;
            this.stateB = (UserAnimLoop)stateB;         
            this.curvesA = this.stateA.bcs;
            this.curvesB = this.stateB.bcs;

            this.isStarting = false;           

            initTime = new float[2, 3];
            this.InitTime(0.5f, 0.0f, 1.0f, 0.5f, 1.0f, 0.0f);
            //this.SetDirection(AnimDirection.forward);
        }

        public bool bRigid
        {
            get; protected set;
        }

        public int count
        {
            get; protected set;
        }

        public BoneNode[] bns
        {
            get; set;
        }

        public override void SetDirection(AnimDirection dir)
        {
            //base.animDir = dir;
            if (dir == AnimDirection.forward)
            {
                rta0 = initTime[0, 0];
                rtb0 = initTime[0, 1];
                rtb1 = initTime[0, 2];
            }
            else
            {
                rta0 = initTime[1, 0];
                rtb0 = initTime[1, 1];
                rtb1 = initTime[1, 2];
            }
        }

        public void InitTime(float fta0, float ftb0, float ftb1, float bta0, float btb0, float btb1)
        {
            initTime[0, 0] = fta0;
            initTime[0, 1] = ftb0;
            initTime[0, 2] = ftb1;
            initTime[1, 0] = bta0;
            initTime[1, 1] = btb0;
            initTime[1, 2] = btb1;
        }

        public float rta0
        {
            get; set;
        }

        public float rta1
        {
            get;
            protected set;
        }

        public float rtb0
        {
            get; set;
        }

        public float rtb1
        {
            get; set;
        }

        public override float rt0
        {
            get
            {
                return rta0;
            }
            set
            {
                rta0 = value;
            }
        }

        public override float rt1
        {
            get
            {
                return rtb1;
            }
            set
            {
                rtb1 = value;
            }
        }

        public override float rt
        {
            get
            {
                return stateB.rt;
            }
        }

        public float ut
        {
            get;
            private set;
        }

        public override bool inView
        {
            get; set;
        }

        public override void Sample(float dt)
        {
            if (isStarting == false)
            {
                stateA.isStarting = false;
                stateB.isStarting = false;
                rta1 = stateB.ct / stateA.ct * (rtb1 - rtb0) + rta0;
                ct = math.abs(stateB.ct * (rtb1 - rtb0));
                isStarting = true;
                ut = 0.0f;
            }
            ut = ut + sp * dt;
            float u = ut / ct;

            float uta = stateA.ComputeUtPart(dt, rta0, rta1);
            float utb = stateB.ComputeUtPart(dt, rtb0, rtb1);

            //if(inView)
            {
                if (u <= 1.0f && stateA.isPlaying == true && stateB.isPlaying == true)
                {
                    for (int i = 0; i < bns.Length; i++)
                    {
                        BoneTransform bTr = bns[i].transform;
                        bRigid = player.bRigid;
                        if (bRigid)
                        {
                            dualquaternion ldq = bTr.dqL;

                            ldq = dualquaternion.scLerp(curvesA[i].SampleDQ(uta), curvesB[i].SampleDQ(utb), u);

                            bns[i].transform.dqL = ldq;
                        }
                        else
                        {
                            float3 lp = bTr.posL;
                            quaternion lr = bTr.rotL;
                            float3 ls = bTr.scaL;

                            {
                                lp = math.lerp(curvesA[i].SamplePos_Safe(uta), curvesB[i].SamplePos_Safe(utb), u);
                                lr = math.slerp(curvesA[i].SampleRot_Safe(uta), curvesB[i].SampleRot_Safe(utb), u);
                                ls = math.lerp(curvesA[i].SampleSca_Safe(uta), curvesB[i].SampleSca_Safe(utb), u);
                            }

                            //if (curvesA[i].used == true && curvesB[i].used == true)
                            //{
                            //    lp = math.lerp(curvesA[i].SamplePos(uta), curvesB[i].SamplePos(utb), u);
                            //    lr = math.slerp(curvesA[i].SampleRot(uta), curvesB[i].SampleRot(utb), u);
                            //    ls = math.lerp(curvesA[i].SampleSca(uta), curvesB[i].SampleSca(utb), u);
                            //}
                            //else if (curvesA[i].used == true && curvesB[i].used != true)
                            //{
                            //    lp = math.lerp(curvesA[i].SamplePos(uta), curvesB[i].lp, u);
                            //    lr = math.slerp(curvesA[i].SampleRot(uta), curvesB[i].lr, u);
                            //    ls = math.lerp(curvesA[i].SampleSca(uta), curvesB[i].ls, u);
                            //}
                            //else if (curvesA[i].used != true && curvesB[i].used == true)
                            //{
                            //    lp = math.lerp(curvesA[i].lp, curvesB[i].SamplePos(utb), u);
                            //    lr = math.slerp(curvesA[i].lr, curvesB[i].SampleRot(utb), u);
                            //    ls = math.lerp(curvesA[i].ls, curvesB[i].SampleSca(utb), u);
                            //}                       

                            bns[i].transform.posL = lp;
                            bns[i].transform.rotL = lr;
                            bns[i].transform.scaL = ls;
                        }
                    }
                }
            }

            
            if (player.nState != null)
            {
                if (u >= 1.0f - 0.01f)
                {
                    player.nState.rt0 = rt;
                    player.nState.rt1 = rt + math.sign(rtb1 - rtb0); ;
                    player.nState.isStarting = false;

                    this.player.MoveNext();
                    this.player.pState.isStarting = false;
                    this.player.pState = null;
                }
            }
        }


        //BakedAnimation
        public override void Sample_Baked(float dt)
        {
            if (isStarting == false)
            {
                stateA.isStarting = false;
                stateB.isStarting = false;
                rta1 = stateB.ct / stateA.ct * (rtb1 - rtb0) + rta0;
                ct = math.abs(stateB.ct * (rtb1 - rtb0));
                isStarting = true;
                ut = 0.0f;
            }
            ut = ut + sp * dt;
            float u = ut / ct;

            float uta = stateA.ComputeUtPart(dt, rta0, rta1);
            float utb = stateB.ComputeUtPart(dt, rtb0, rtb1);
         
            {
                if (u <= 1.0f && stateA.isPlaying == true && stateB.isPlaying == true)
                {                   
                    int fid;
                    float ur;
                    int iid = player.iid;
                    {
                        curvesA[0].GetFrameInfo(uta, out fid, out ur);

                        player.playerData[iid].cid.x = (uint)stateA.clipId;
                        player.playerData[iid].fid.x = (uint)fid;
                        player.playerData[iid].u.x = ur;
                    }

                    {
                        curvesB[0].GetFrameInfo(utb, out fid, out ur);

                        player.playerData[iid].cid.y = (uint)stateB.clipId;
                        player.playerData[iid].fid.y = (uint)fid;
                        player.playerData[iid].u.y = ur;
                    }

                    {
                        player.playerData[iid].type = 1;
                        player.playerData[iid].u.z = u;
                    }
                }
            }


            if (player.nState != null)
            {
                if (u >= 1.0f - 0.01f)
                {
                    player.nState.rt0 = rt;
                    player.nState.rt1 = rt + math.sign(rtb1 - rtb0); ;
                    player.nState.isStarting = false;

                    this.player.MoveNext();
                    this.player.pState.isStarting = false;
                    this.player.pState = null;
                }
            }
        }


        public override void Sample_Total(float dt)
        {
            if (isStarting == false)
            {
                stateA.isStarting = false;
                stateB.isStarting = false;
                rta1 = stateB.ct / stateA.ct * (rtb1 - rtb0) + rta0;
                ct = math.abs(stateB.ct * (rtb1 - rtb0));
                isStarting = true;
                ut = 0.0f;
            }
            ut = ut + sp * dt;
            float u = ut / ct;

            float uta = stateA.ComputeUtPart(dt, rta0, rta1);
            float utb = stateB.ComputeUtPart(dt, rtb0, rtb1);

            {
                if (u <= 1.0f && stateA.isPlaying == true && stateB.isPlaying == true)
                {
                    int fid;
                    float ur;
                    int iid = player.iid;
                    {
                        curvesA[0].GetFrameInfo(uta, out fid, out ur);

                        player.playerData[iid].cid.x = (uint)stateA.clipId;
                        player.playerData[iid].fid.x = (uint)fid;
                        player.playerData[iid].u.x = ur;
                    }

                    {
                        curvesB[0].GetFrameInfo(utb, out fid, out ur);

                        player.playerData[iid].cid.y = (uint)stateB.clipId;
                        player.playerData[iid].fid.y = (uint)fid;
                        player.playerData[iid].u.y = ur;
                    }

                    {
                        player.playerData[iid].type = 1;
                        player.playerData[iid].u.z = u;
                    }
                }
            }


            if (player.nState != null)
            {
                if (u >= 1.0f - 0.01f)
                {
                    player.nState.rt0 = rt;
                    player.nState.rt1 = rt + math.sign(rtb1 - rtb0); ;
                    player.nState.isStarting = false;

                    this.player.MoveNext();
                    this.player.pState.isStarting = false;
                    this.player.pState = null;
                }
            }
        }
    }
}


