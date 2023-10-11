using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

using Utility_JSB;

namespace UserAnimSpace
{    
    [System.Serializable]
    public class BoneNode
    {
        public string name;
        public BoneTransform transform;
        public int depth;
        public int insId;

        public BoneNode parent;
        protected List<BoneNode> children;
        public bool isRigid = false;

        public BoneNode()
        {
            parent = null;
            children = null;
            transform.posL = new float3(0.0f, 0.0f, 0.0f);
            transform.rotL = quaternion.identity;
            transform.scaL = new float3(1.0f, 1.0f, 1.0f);
        }

        public void AddChild(BoneNode child)
        {
            if (children == null)
            {
                children = new List<BoneNode>();
            }

            {
                child.parent = this;
                children.Add(child);
            }
        }

        public BoneNode GetChild(int index)
        {
            if (children == null)
            {
                return null;
            }
            else
            {
                if (index < childCount)
                {
                    return children[index];
                }
                else
                {
                    return null;
                }
            }
        }

        public bool RemoveChild(int index)
        {
            if (GetChild(index) != null)
            {
                children.RemoveAt(index);

                return true;
            }

            return false;
        }

        public bool RemoveChild(BoneNode bn)
        {
            if (children.Remove(bn))
            {
                return true;
            }

            return false;
        }

        public int childCount
        {
            get
            {
                if (children == null)
                {
                    return 0;
                }
                else
                {
                    return children.Count;
                }
            }
        }

        public BoneNode root
        {
            get
            {
                BoneNode bn = this;
                while (bn.parent != null)
                {
                    bn = bn.parent;
                }

                return bn;
            }
        }


        public dualquaternion rigid
        {
            get
            {
                return new dualquaternion(transform.rotL, transform.posL);
            }
        }

        public float4x4 WfromL
        {
            get
            {
                if (isRigid)
                {
                    return GetRigidWfromL(this).toMat();
                }
                else
                {
                    return GetWfromL(this);
                }
            }
        }

        public float4x4 LfromW
        {
            get
            {
                if (isRigid)
                {
                    return (~GetRigidWfromL(this)).toMat();
                }
                else
                {
                    return math.inverse(GetWfromL(this));
                }
            }
        }

        public float3 posW
        {
            get
            {
                return WfromL.c3.xyz;
            }
            set
            {
                transform.posL = value - (posW - transform.posL);
            }
        }

        public quaternion rotW      //using in case of non-uniform scale
        {
            get
            {
                return GetRotWfromL(this);
            }
            set
            {
                transform.rotL = math.mul(math.inverse(math.mul(rotW, math.inverse(transform.rotL))), value);
            }
        }

        public quaternion rotW1
        {
            get
            {
                float4x4 m0 = WfromL;
                float3x3 m1;
                m1.c0 = math.normalize(m0.c0.xyz);
                m1.c1 = math.normalize(m0.c1.xyz);
                m1.c2 = math.normalize(m0.c2.xyz);

                quaternion q0 = new quaternion(m1);

                return q0;
            }
            set
            {
                //rotL = math.mul(math.inverse(math.mul(rotW, math.inverse(rotL))), value);

                float4x4 m0 = WfromL;
                float3 s0;
                s0.x = math.length(m0.c0.xyz);
                s0.y = math.length(m0.c1.xyz);
                s0.z = math.length(m0.c2.xyz);
                float3 s1 = 1.0f / s0;
                //float3x3 m1;
                //m1.c0 = s1 * m0.c0.xyz;
                //m1.c1 = s1 * m0.c1.xyz;
                //m1.c2 = s1 * m0.c2.xyz;
                float3 p0 = m0.c3.xyz;

                float4x4 m2;
                float3x3 m3 = new float3x3(value);
                m3.c0 *= s0.x;
                m3.c1 *= s0.y;
                m3.c2 *= s0.z;
                m2.c0 = new float4(m3.c0, 0.0f);
                m2.c1 = new float4(m3.c1, 0.0f);
                m2.c2 = new float4(m3.c2, 0.0f);
                m2.c3 = new float4(p0, 1.0f);

                float4x4 m4 = GetPfromC(this);

                float4x4 m5 = math.mul(math.inverse(math.mul(m0, math.inverse(m4))), m2);
                float3x3 m6;
                m6.c0 = math.normalize(m5.c0.xyz);
                m6.c1 = math.normalize(m5.c1.xyz);
                m6.c2 = math.normalize(m5.c2.xyz);

                quaternion q0 = new quaternion(m6);

                transform.rotL = q0;
            }
        }


        public float3x3 RotL
        {
            get
            {
                return new float3x3(transform.rotL);
            }
        }

        public float3x3 RotW    //using in case of non-uniform scale
        {
            get
            {
                return new float3x3(rotW);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 GetPfromC(BoneNode child)
        {
            float4x4 P = float4x4.identity;
            float3 pos = child.transform.posL;
            float3x3 R = new float3x3(child.transform.rotL);
            float3 sca = child.transform.scaL;

            P.c0 = new float4(sca.x * R.c0, 0.0f);
            P.c1 = new float4(sca.y * R.c1, 0.0f);
            P.c2 = new float4(sca.z * R.c2, 0.0f);
            P.c3 = new float4(pos, 1.0f);

            return P;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 GetPfromC(float3 pos, quaternion rot, float3 sca)
        {
            float4x4 P = float4x4.identity;            
            float3x3 R = new float3x3(rot);           

            P.c0 = new float4(sca.x * R.c0, 0.0f);
            P.c1 = new float4(sca.y * R.c1, 0.0f);
            P.c2 = new float4(sca.z * R.c2, 0.0f);
            P.c3 = new float4(pos, 1.0f);

            return P;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 GetWfromL(BoneNode local)
        {
            float4x4 W = float4x4.identity;
            W = GetPfromC(local);
            BoneNode bn = local;
            while (bn.parent != null)
            {
                W = math.mul(GetPfromC(bn.parent), W);
                bn = bn.parent;
            }

            return W;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion GetRotWfromL(BoneNode local)
        {
            quaternion rotW = local.transform.rotL;
            BoneNode bn = local;
            while (bn.parent != null)
            {
                rotW = math.mul(bn.parent.transform.rotL, rotW);
                bn = bn.parent;
            }

            return rotW;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static dualquaternion GetRigidWfromL(BoneNode local)
        {
            dualquaternion rigid = local.rigid;
            BoneNode bn = local;
            while (bn.parent != null)
            {
                rigid = bn.parent.rigid * rigid;
                bn = bn.parent;
            }

            return rigid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 GetWfromLByDQ(BoneNode local)
        {
            float4x4 W = float4x4.identity;

            dualquaternion dq = local.transform.dqL;
            BoneNode bn = local;
            while (bn.parent != null)
            {
                dq = bn.parent.transform.dqL * dq;
                bn = bn.parent;
            }

            W = dq.toMat();
            return W;
        }


        //Animation               
        public static void ConstructBone(Transform tr, BoneNode bn)
        {
            bn.name = tr.name;
            bn.transform.posL = tr.localPosition;
            bn.transform.rotL = tr.localRotation;
            bn.transform.scaL = tr.localScale;
            //bn.transform.scaL = new float3(1.0f, 1.0f, 1.0f);
            //bn.transform.dqL = new dualquaternion(tr.localRotation, tr.localRotation);
            bn.transform.dqL = new dualquaternion(tr.localRotation, tr.localPosition);
            for (int i = 0; i < tr.childCount; i++)
            {
                bn.AddChild(new BoneNode());
                ConstructBone(tr.GetChild(i), bn.GetChild(i));
            }
        }

        public static void ConstructBones(Transform tr, BoneNode[] bns)
        {
            for(int i = 0; i < bns.Length; i++)
            {
                ConstructBone(tr, bns[i]);
            }
        }

        public Dictionary<string, BoneCurve> dicCurve;
        //public List<BoneCurve> listCurve;

        public static void BindBoneInfo(BoneNode root)
        {
            root.dicCurve = new Dictionary<string, BoneCurve>();
            //root.listCurve = new List<BoneCurve>();
            for (int i = 0; i < root.childCount; i++)
            {
                BindBoneInfo(root.GetChild(i));
            }
        }

        public static BoneNode[] ToArray(BoneNode bn)
        {
            List<BoneNode> bns = new List<BoneNode>();

            ToArray(bns, bn);

            return bns.ToArray();
        }

        public static BoneNode[] ToArray(BoneNode bn, out int count)
        {
            List<BoneNode> bns = new List<BoneNode>();

            ToArray(bns, bn);

            count = bns.Count;
            return bns.ToArray();
        }

        static void ToArray(List<BoneNode> bns, BoneNode bn)
        {
            bns.Add(bn);

            for (int i = 0; i < bn.childCount; i++)
            {
                ToArray(bns, bn.GetChild(i));
            }
        }

        public static Transform[] ToArray(Transform tr)
        {
            List<Transform> trs = new List<Transform>();

            ToArray(trs, tr);

            return trs.ToArray();
        }

        static void ToArray(List<Transform> trs, Transform tr)
        {
            trs.Add(tr);

            for (int i = 0; i < tr.childCount; i++)
            {
                ToArray(trs, tr.GetChild(i));
            }
        }

        public static BoneNode[][] ToArrayArray(BoneNode bn)
        {
            BoneNode[][] bns;

            List<List<BoneNode>> listBn = new List<List<BoneNode>>();
            ToList(bn, listBn, 0, -1);

            bns = ToArrayArray(listBn);
            return bns;
        }

        static BoneNode[][] ToArrayArray(List<List<BoneNode>> listBn)
        {
            BoneNode[][] bns;

            bns = new BoneNode[listBn.Count][];
            for(int i = 0; i < bns.Length; i++)
            {
                bns[i] = new BoneNode[listBn[i].Count];
                for(int j = 0; j < bns[i].Length; j++)
                {
                    bns[i][j] = listBn[i][j];
                }
            }
            return bns;
        }

        static void ToList(BoneNode bn, List<List<BoneNode>> list, int depth, int parent)
        {
            if (list.Count - 1 < depth)
            {
                list.Add(new List<BoneNode>());
            }
            bn.transform.parent = parent;
            bn.depth = depth;
            bn.transform.WfromL = float4x4.identity;
            bn.transform.dqWfromL = dualquaternion.identity;
            list[depth].Add(bn);
            for (int i = 0; i < bn.childCount; i++)
            {
                ToList(bn.GetChild(i), list, depth + 1, list[depth].Count - 1);
            }
        }

        public static BoneNode[][] ToArrayArrayIns(params BoneNode[] bns)
        {           
            int insCount = bns.Length;
            BoneNode[][][] bnsIn = new BoneNode[insCount][][];           
            for(int i = 0; i < insCount; i++)
            {
                bnsIn[i] = ToArrayArray(bns[i]);
            }

            int[] siblingCount = GetSiblingCount(bnsIn[0]);
            int depthCount = siblingCount.Length;

            BoneNode[][] bnsOut = new BoneNode[depthCount][];
            for(int i = 0; i < depthCount; i++)
            {
                bnsOut[i] = new BoneNode[insCount * siblingCount[i]];
            }
            
            for (int i = 0; i < insCount; i++)
            {
                for(int j = 0; j < depthCount; j++)
                {                    
                    for(int k = 0; k < siblingCount[j]; k++)
                    {
                        BoneNode bn = bnsIn[i][j][k];

                        if(j > 0)
                        {
                            bn.transform.parent += i * siblingCount[j - 1];
                            bn.transform.used = true;
                        }
                        
                        bnsOut[j][i * siblingCount[j] + k] = bn;
                    }
                }
            }

            return bnsOut;
        }


        public static BoneNode[] ToArrayForCompute(BoneNode bn, 
            out int[] siblingCount,
            out int depthCount,
            out int bnCount,
            out int[] idxMask,
            out int[] idxParent,
            out int[] idx,
            out int2[] idxMP)
        {           
            BoneNode[][] bns0 = ToArrayArray(bn);
            siblingCount = GetSiblingCount(bns0);
            depthCount = siblingCount.Length;
            bnCount = 0;
            for(int i = 0; i < depthCount; i++)
            {
                bnCount += siblingCount[i];
            }

            BoneNode[] bnsOut = new BoneNode[bnCount];
            idxMask = new int[bnCount];
            idxParent = new int[bnCount];
            idx = new int[depthCount + 1];
            int sIdx = 0;
            for(int i = 0; i < depthCount; i++)
            {
                idx[i] = sIdx;
                for(int j = 0; j < siblingCount[i]; j++)
                {
                    int k = sIdx + j;
                    bnsOut[k] = bns0[i][j];
                    idxMask[k] = i;
                    
                    if (i == 0)
                    {
                        idxParent[k] = -1;
                    }
                    //else if(i == 1)
                    //{
                    //    idxParent[k] = 0;
                    //}
                    else
                    {
                        idxParent[k] = idx[i - 1] + bns0[i][j].transform.parent; 
                    }                    
                }
                sIdx += siblingCount[i];
            }
            idx[depthCount] = sIdx;

            //Debug
            idxMP = new int2[bnCount];
            for(int i = 0; i < bnCount; i++)
            {
                idxMP[i].x = idxMask[i];
                idxMP[i].y = idxParent[i];
            }

            return bnsOut;
        }

        public static BoneNode[] ToArrayForCompute(BoneNode bn)
        {
            BoneNode[][] bns0 = ToArrayArray(bn);
            int[] siblingCount = GetSiblingCount(bns0);
            int depthCount = siblingCount.Length;
            int bnCount = 0;
            for (int i = 0; i < depthCount; i++)
            {
                bnCount += siblingCount[i];
            }

            BoneNode[] bnsOut = new BoneNode[bnCount];            
            int sIdx = 0;
            for (int i = 0; i < depthCount; i++)
            {               
                for (int j = 0; j < siblingCount[i]; j++)
                {
                    int k = sIdx + j;
                    bnsOut[k] = bns0[i][j];                   
                }
                sIdx += siblingCount[i];
            }           

            return bnsOut;
        }

        public static BoneNode[][] ToArrayArrayForCompute(BoneNode[] bns)
        {
            BoneNode[][] bnsOut;
            bnsOut = new BoneNode[bns.Length][];
           
            for (int i = 0; i < bnsOut.Length; i++)
            {
                bnsOut[i] = BoneNode.ToArrayForCompute(bns[i]);
            }

            return bnsOut;
        }

        public static BoneNode[][] ToArrayArray(params BoneNode[] bns)
        {
            BoneNode[][] bnsOut;
            bnsOut = new BoneNode[bns.Length][];
            
            for(int i = 0; i < bnsOut.Length; i++)
            {
                bnsOut[i] = BoneNode.ToArray(bns[i]);
            }
            
            return bnsOut;
        }

        public static BoneNode[][] ToArrayArray(BoneNode[] bns, out int count)
        {
            BoneNode[][] bnsOut;
            bnsOut = new BoneNode[bns.Length][];
            
            count = 0;
            for (int i = 0; i < bnsOut.Length; i++)
            {
                bnsOut[i] = BoneNode.ToArray(bns[i], out count);
            }

            return bnsOut;
        }

        public static NativeArray<BoneTransform>[] CreateNa(BoneNode[][] bns)
        {
            NativeArray<BoneTransform>[] nas;
            
            nas = new NativeArray<BoneTransform>[bns.Length];
            for(int i = 0; i < bns.Length; i++)
            {
                nas[i] = new NativeArray<BoneTransform>(bns[i].Length, Allocator.Persistent);
                for(int j = 0; j < bns[i].Length; j++)
                {
                    BoneTransform bt = new BoneTransform();
                    bt = bns[i][j].transform;
                    nas[i][j] = bt;
                }
            }

            return nas;
        }

        public static WfromLJob[] CreateJob(NativeArray<BoneTransform>[] nas)
        {
            WfromLJob[] jobs;
            jobs = new WfromLJob[nas.Length - 1];
            for(int i = 0; i < jobs.Length; i++)
            {
                jobs[i] = new WfromLJob();                                                
            }
          
            return jobs;
        }
        
        public static JobHandle[] CreateHandle(WfromLJob[] jobs)
        {
            JobHandle[] handles;
            handles = new JobHandle[jobs.Length];           
            
            return handles;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteToNA(BoneNode[][] bns, NativeArray<BoneTransform>[] nas)
        {
            for(int i = 0; i < bns.Length; i++)
            {
                for(int j = 0; j < bns[i].Length; j++)
                {
                    nas[i][j] = bns[i][j].transform;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteToJobs(WfromLJob[] jobs, bool bRigid)
        {
            for (int i = 0; i < jobs.Length; i++)
            {
                jobs[i].bRigid = bRigid;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ComputeRootW_NonRigid(float4x4[] rootM, BoneNode[][] bnsOut, bool bRigid)
        {
            BoneNode[] rootBns = bnsOut[0];
            if(!bRigid)
            {
                Parallel.For(0, rootM.Length,
                    (int i) =>
                    {
                        float4x4 M = rootM[i];
                        float3 pos = M.c0.xyz;
                        quaternion rot = M.c1;
                        float3 sca = M.c2.xyz;

                        float4x4 mat0 = BoneNode.GetPfromC(pos, rot, sca);
                        float4x4 mat1 = BoneNode.GetPfromC(rootBns[i]);

                        rootBns[i].transform.WfromL = math.mul(mat0, mat1);
                    });
            }       
            else
            {
                Parallel.For(0, rootM.Length,
                   (int i) =>
                   {                                                                   
                        dualquaternion dq = rootBns[i].transform.dqL;

                        rootBns[i].transform.dqWfromL = dq;
                        rootBns[i].transform.WfromL = dq.toMat();
                   });

                //for (int i = 0; i < rootM.Length; i++)
                //{               
                //    
                //    dualquaternion dq = rootBns[i].transform.dqL;
                //
                //    rootBns[i].transform.dqWfromL = dq;
                //    rootBns[i].transform.WfromL = dq.toMat();
                //}
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ComputeRootW_NonRigid(float4x4[] rootM, float4x4[] rootW, BoneNode[][] bnsOut, bool bRigid)
        {
            BoneNode[] rootBns = bnsOut[0];
            if (!bRigid)
            {
                Parallel.For(0, rootM.Length,
                    (int i) =>
                    {
                        float4x4 M = rootM[i];
                        float3 pos = M.c0.xyz;
                        quaternion rot = M.c1;
                        float3 sca = M.c2.xyz;

                        float4x4 mat0 = BoneNode.GetPfromC(pos, rot, sca);
                        float4x4 mat1 = BoneNode.GetPfromC(rootBns[i]);
                        
                        rootBns[i].transform.WfromL = math.mul(mat0, mat1);

                        rootW[i] = mat0;
                    });
            }
            else
            {
                Parallel.For(0, rootM.Length,
                   (int i) =>
                   {
                       dualquaternion dq = rootBns[i].transform.dqL;

                       rootBns[i].transform.dqWfromL = dq;
                       rootBns[i].transform.WfromL = dq.toMat();
                   });

                //for (int i = 0; i < rootM.Length; i++)
                //{               
                //    
                //    dualquaternion dq = rootBns[i].transform.dqL;
                //
                //    rootBns[i].transform.dqWfromL = dq;
                //    rootBns[i].transform.WfromL = dq.toMat();
                //}
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetJob(WfromLJob[] jobs, NativeArray<BoneTransform>[] nas)
        {
            for (int i = 0; i < jobs.Length; i++)
            {
                jobs[i].parent = nas[i];
                jobs[i].child = nas[i + 1];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetHandles(JobHandle[] handles, WfromLJob[] jobs)
        {
            for (int i = 0; i < jobs.Length; i++)
            {
                if (i == 0)
                {
                    handles[i] = jobs[i].Schedule(jobs[i].child.Length, 1);
                }
                else
                {
                    handles[i] = jobs[i].Schedule<WfromLJob>(jobs[i].child.Length, 1, handles[i - 1]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void JobComplete(JobHandle[] handles)
        {
            handles[handles.Length - 1].Complete();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadFromNA(BoneNode[][] bns, NativeArray<BoneTransform>[] nas)
        {
            for (int i = 0; i < bns.Length; i++)
            {
                for (int j = 0; j < bns[i].Length; j++)
                {
                    bns[i][j].transform = nas[i][j];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ComputeRootW_Rigid(float4x4[] rootM, BoneNode[][] bnsOut1, bool bRigid)
        {
            if (bRigid)
            {
                Parallel.For(0, rootM.Length,
                    (int i) =>
                    {
                        float4x4 M = rootM[i];
                        float3 pos = M.c0.xyz;
                        quaternion rot = M.c1;
                        float3 sca = M.c2.xyz;

                        float4x4 mat0 = BoneNode.GetPfromC(pos, rot, sca);
                        float4x4 mat1;                        
                        for (int j = 0; j < bnsOut1[i].Length; j++)
                        {                                     
                            mat1 = bnsOut1[i][j].transform.WfromL;                                                            
                            bnsOut1[i][j].transform.WfromL = math.mul(mat0, mat1);
                        }
                    });
            }          
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ComputeRootW_Rigid(float4x4[] rootM, float4x4[] rootW, BoneNode[][] bnsOut1, bool bRigid)
        {
            if (bRigid)
            {
                Parallel.For(0, rootM.Length,
                    (int i) =>
                    {
                        float4x4 M = rootM[i];
                        float3 pos = M.c0.xyz;
                        quaternion rot = M.c1;
                        float3 sca = M.c2.xyz;

                        float4x4 mat0 = BoneNode.GetPfromC(pos, rot, sca);
                        float4x4 mat1;
                        for (int j = 0; j < bnsOut1[i].Length; j++)
                        {
                            mat1 = bnsOut1[i][j].transform.WfromL;
                            bnsOut1[i][j].transform.WfromL = math.mul(mat0, mat1);
                        }

                        rootW[i] = mat0;
                    });
            }
        }

        public static void DeleteNa(NativeArray<BoneTransform>[] nas)
        {
            if(nas != null)
            {
                for (int i = 0; i < nas.Length; i++)
                {
                    if(nas[i].IsCreated) nas[i].Dispose();
                }
            }          
        }

        public static void GetFinalW(float4x4[] finalW, BoneNode[][] skbns, float4x4[] bindpose)
        {           
            Parallel.For(0, skbns.Length,
                (i) =>
                {
                    int count = skbns[i].Length;
                    for (int j = 0; j < count; j++)
                    {
                        finalW[i * count + j] = math.mul(skbns[i][j].transform.WfromL, bindpose[j]);
                    }
                });
        }

        static int[] GetSiblingCount(BoneNode[][] bns)
        {
            int[] count = new int[bns.Length];

            for(int i = 0; i < bns.Length; i++)
            {
                count[i] = bns[i].Length;
            }

            return count;
        }

        public static void BindBoneInfo(BoneNode[] bns)
        {
            for (int i = 0; i < bns.Length; i++)
            {
                bns[i].dicCurve = new Dictionary<string, BoneCurve>();
            }
        }

        public static void BindBoneClip(BoneNode[] bns, string clipName)
        {
            for (int i = 0; i < bns.Length; i++)
            {
                bns[i].dicCurve[clipName] = new BoneCurve(bns[i], clipName, false);
            }
        }

        public static int CountClip(BoneNode[] bns, string clipName)
        {
            int count = 0;
            for (int i = 0; i < bns.Length; i++)
            {
                if (bns[i].dicCurve[clipName].used)
                {
                    count++;
                }
            }

            return count;
        }

        public static BoneNode FindBone(BoneNode[] bns, string name)
        {
            BoneNode bn = null;

            for (int i = 0; i < bns.Length; i++)
            {
                if (bns[i].name == name)
                {
                    return bns[i];
                }
            }

            return bn;
        }

        public static BoneNode[] FindBones(BoneNode[] bns0, string[] boneNames)
        {
            BoneNode[] bns1 = new BoneNode[boneNames.Length];

            for (int i = 0; i < boneNames.Length; i++)
            {
                bns1[i] = BoneNode.FindBone(bns0, boneNames[i]);
            }

            return bns1;
        }

        public static BoneNode FindBone(BoneNode[] bns, string name, out int boneIdx)
        {
            BoneNode bn = null;
            boneIdx = -1;

            for (int i = 0; i < bns.Length; i++)
            {
                if (bns[i].name == name)
                {
                    boneIdx = i;
                    return bns[i];
                }
            }

            return bn;
        }

        public static BoneNode[] FindBones(BoneNode[] bns0, string[] boneNames, ref int[] boneIdx)
        {
            BoneNode[] bns1 = new BoneNode[boneNames.Length];
            
            //if(boneIdx != null)
            {
                boneIdx = new int[boneNames.Length];
            }

            for (int i = 0; i < boneNames.Length; i++)
            {
                bns1[i] = BoneNode.FindBone(bns0, boneNames[i], out boneIdx[i]);
            }

            return bns1;
        }

        public static void BindBoneCurve(BoneNode[] bns, string clipName, BoneCurve curve)
        {
            for (int i = 0; i < bns.Length; i++)
            {
                BoneNode bn = FindBone(bns, curve.boneName);
                if (bn != null)
                {
                    bn.dicCurve[clipName] = curve;
                    break;
                }
            }
        }

        public static void BindBoneCurve(BoneNode[] bns, string clipName, BoneCurve[] curves)
        {
            for (int i = 0; i < bns.Length; i++)
            {
                bns[i].dicCurve[clipName] = new BoneCurve(bns[i], clipName, false);
            }

            for (int i = 0; i < curves.Length; i++)
            {
                var curve = curves[i];
                BoneNode bn = FindBone(bns, curve.boneName);
                if (bn != null)
                {
                    bn.dicCurve[clipName] = curve;
                }
            }
        }

        public static BoneCurve[] ToBoneCurves(BoneNode[] bns, string clipName)
        {
            BoneCurve[] curves = new BoneCurve[bns.Length];

            for (int i = 0; i < curves.Length; i++)
            {
                curves[i] = bns[i].dicCurve[clipName];
            }

            return curves;
        }

        public static float4x4 MFromW(float4x4 W)
        {
            float4x4 M = float4x4.identity;

            float3 sca;
            sca.x = math.length(W.c0.xyz);
            sca.y = math.length(W.c1.xyz);
            sca.z = math.length(W.c2.xyz);

            float3x3 R;
            R.c0 = W.c0.xyz / sca.x;
            R.c1 = W.c1.xyz / sca.y;
            R.c2 = W.c2.xyz / sca.z;
            quaternion rot = new quaternion(R);

            float3 pos;
            pos.x = W.c3.x;
            pos.y = W.c3.y;
            pos.z = W.c3.z;

            M.c0.xyz = pos;
            M.c1.xyzw = rot.value;
            M.c2.xyz = sca;

            return M;
        }

        public static float4x4 MFromBn(BoneNode bn)
        {
            float4x4 M = float4x4.identity;
            BoneTransform bt = bn.transform;
            float4x4 W = bt.WfromL;

            W.c0.xyz *= bt.scaL.x;
            W.c1.xyz *= bt.scaL.y;
            W.c2.xyz *= bt.scaL.z;
            bn.transform.WfromL = W;

            float3 sca;
            sca.x = math.length(W.c0.xyz);
            sca.y = math.length(W.c1.xyz);
            sca.z = math.length(W.c2.xyz);

            float3x3 R;
            R.c0 = W.c0.xyz / sca.x;
            R.c1 = W.c1.xyz / sca.y;
            R.c2 = W.c2.xyz / sca.z;
            quaternion rot = new quaternion(R);

            float3 pos;
            pos.x = W.c3.x;
            pos.y = W.c3.y;
            pos.z = W.c3.z;

            M.c0.xyz = pos;
            M.c1.xyzw = rot.value;
            M.c2.xyz = sca;

            return M;
        }


        public static float4x4[] CheckOrthoNormal(float4x4[] input)
        {
            int count = input.Length;
            float4x4[] result = new float4x4[count];

            for(int i = 0; i < count; i++)
            {
                float3x3 R;
                R.c0 = input[i].c0.xyz;
                R.c1 = input[i].c1.xyz;
                R.c2 = input[i].c2.xyz;

                float3x3 _R;
                _R = math.transpose(R);

                float3x3 M = math.mul(_R, R);
                //float3x3 M = math.mul(R, _R);

                float4x4 N;
                N.c0 = new float4(M.c0, 0.0f);
                N.c1 = new float4(M.c1, 0.0f);
                N.c2 = new float4(M.c2, 0.0f);
                N.c3 = float4.zero;

                result[i] = N;               
            }
            
            return result;
        }
    }

    [System.Serializable]
    public struct BoneTransform
    {
        public float3 posL;
        public quaternion rotL;
        public float3 scaL;       
        public float4x4 WfromL;

        public dualquaternion dqWfromL;
        public dualquaternion dqL;

        public int parent;
        public bool used;        
    }

    [BurstCompile]
    public struct WfromLJob : IJobParallelFor
    {
        [ReadOnly]
        public bool bRigid;

        [ReadOnly]
        public NativeArray<BoneTransform> parent;
        public NativeArray<BoneTransform> child;       

        
        public void Execute(int i)
        {
            BoneTransform btChild = new BoneTransform();
            btChild = child[i];

            if (btChild.used)
            {
                int idxParent = btChild.parent;

                if (bRigid)
                {
                    btChild.dqWfromL = parent[idxParent].dqWfromL * btChild.dqL;
                    btChild.WfromL = btChild.dqWfromL.toMat();
                }
                else
                {                   
                    btChild.WfromL = math.mul(parent[idxParent].WfromL, ToL(btChild));                    
                }

                child[i] = btChild;
            }            
        }      

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4x4 ToL(BoneTransform bt)
        {
            float4x4 P = float4x4.identity;

            float3 t = bt.posL;
            float3x3 R = new float3x3(bt.rotL);
            float3 s = bt.scaL;

            P.c0 = new float4(s.x * R.c0, 0.0f);
            P.c1 = new float4(s.y * R.c1, 0.0f);
            P.c2 = new float4(s.z * R.c2, 0.0f);
            P.c3 = new float4(t, 1.0f);

            return P;
        }

    }
}