using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Serialization;
using Unity.Collections;
using Utility_JSB;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UserAnimSpace
{
    [CreateAssetMenu(fileName = "UserAnimClip", menuName = "UserAnimClip")]
    public class UserAnimClip : ScriptableObject
    {
        public new string name;
        public float ct;
        public AnimationClip clip;
        public Dictionary<string, BoneCurve> curveDic;
        [SerializeField]
        public BoneCurve[] curves;
        public string[] bonePath;

        void OnEnable()
        {
#if UNITY_EDITOR
            //name = clip.name;            
            ct = clip.length;
            clip.legacy = true;
            curveDic = new Dictionary<string, BoneCurve>();
            
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);                
                if (!curveDic.ContainsKey(binding.path))
                {
                    //curveDic[binding.path] = new BoneCurve(clip.name, binding.path, ct);                   
                    curveDic[binding.path] = new BoneCurve(this.name, binding.path, ct);
                }
                
                curveDic[binding.path].Add1(curve, binding.propertyName, ct);
            }

            
            curves = new BoneCurve[curveDic.Count];
            int i = 0;
            foreach (KeyValuePair<string, BoneCurve> kv in curveDic)
            {
                curves[i] = kv.Value;               
                i++;
            }

            int maxCount = 0;
            for (i = 0; i < curves.Length; i++)
            {
                if (maxCount < curves[i].posKeys_Pre.Length) maxCount = curves[i].posKeys_Pre.Length;
                if (maxCount < curves[i].rotKeys_Pre.Length) maxCount = curves[i].rotKeys_Pre.Length;
                if (maxCount < curves[i].scaKeys_Pre.Length) maxCount = curves[i].scaKeys_Pre.Length;
            }

            

            bonePath = new string[curves.Length];
            for (i = 0; i < curves.Length; i++)
            {
                //curves[i].Normalize(maxCount);
                curves[i].Normalize1();
                curves[i].NormalizeToDQ();

                bonePath[i] = curves[i].bonePath;
            }
#endif            
        } 
     
        

    }

    [System.Serializable]
    public class BoneCurve
    {

        public bool used = true;
        public bool bPos = false;
        public bool bRot = false;
        public bool bSca = false;
        

        public string clipName;
        public string bonePath;
        public string boneName;

        public int pc;
        public int rc;
        public int sc;

        public float3 lp;
        public quaternion lr;
        public float3 ls;

        public float[] posTime;
        public float[] rotTime;
        public float[] scaTime;

        public float ct;
        public float3[] posKeys_Pre;
        public quaternion[] rotKeys_Pre;
        public float3[] scaKeys_Pre;

        public int keyCount;
        public float3[] posKeys;
        public quaternion[] rotKeys;
        public float3[] scaKeys;

        public bool bDQ = false;
        public static int dqc;
        public dualquaternion ldq;
        public dualquaternion[] dqKeys;


        static BoneCurve()
        {
            BoneCurve.tc = 16;
            BoneCurve.dqc = 16;
        }

        public BoneCurve(string clipName, string bonePath, float ct)
        {
            this.clipName = clipName;
            this.bonePath = bonePath;
            string[] paths = SplitBindingPath(bonePath);
            this.boneName = paths[paths.Length - 1];
            this.ct = ct;
        }

        public BoneCurve(BoneNode bn, string clipName, bool used)
        {
            this.clipName = clipName;
            this.used = used;
            lp = bn.transform.posL;
            lr = bn.transform.rotL;
            ls = bn.transform.scaL;
            ldq = new dualquaternion(lr, lp);
        }

        string[] SplitBindingPath(string path)
        {
            string[] result;

            char[] cpath = path.ToCharArray();
            List<char> clist = new List<char>();
            List<string> slist = new List<string>();

            for (int i = 0; i < cpath.Length; i++)
            {
                if (cpath[i] != '/')
                {
                    clist.Add(cpath[i]);
                }

                if (cpath[i] == '/' || i == cpath.Length - 1)
                {
                    string st;
                    st = string.Concat(clist);
                    slist.Add(st);
                    clist.Clear();
                }
            }

            result = slist.ToArray();

            return result;
        }
        public void Add(AnimationCurve curve, string property)
        {
            if (property == "m_LocalPosition.x" || property == "m_LocalPosition.y" || property == "m_LocalPosition.z")
            {
                int num = curve.keys.Length;
                if (posKeys_Pre == null)
                {
                    posKeys_Pre = new float3[num];
                    posTime = new float[num];
                    bPos = true;
                    for (int i = 0; i < num; i++)
                    {
                        posTime[i] = curve.keys[i].time - curve.keys[0].time;
                    }
                }

                for (int i = 0; i < num; i++)
                {
                    if (property == "m_LocalPosition.x")
                    {
                        posKeys_Pre[i].x = curve.keys[i].value;
                    }
                    else if (property == "m_LocalPosition.y")
                    {
                        posKeys_Pre[i].y = curve.keys[i].value;
                    }
                    else if (property == "m_LocalPosition.z")
                    {
                        posKeys_Pre[i].z = curve.keys[i].value;
                    }
                }
            }
            else if (property == "m_LocalRotation.x" || property == "m_LocalRotation.y" || property == "m_LocalRotation.z" || property == "m_LocalRotation.w")
            {
                int num = curve.keys.Length;
                if (rotKeys_Pre == null)
                {
                    rotKeys_Pre = new quaternion[num];
                    rotTime = new float[num];
                    bRot = true;
                    for (int i = 0; i < num; i++)
                    {
                        rotTime[i] = curve.keys[i].time - curve.keys[0].time;
                    }
                }

                for (int i = 0; i < num; i++)
                {
                    if (property == "m_LocalRotation.x")
                    {
                        rotKeys_Pre[i].value.x = curve.keys[i].value;
                    }
                    else if (property == "m_LocalRotation.y")
                    {
                        rotKeys_Pre[i].value.y = curve.keys[i].value;
                    }
                    else if (property == "m_LocalRotation.z")
                    {
                        rotKeys_Pre[i].value.z = curve.keys[i].value;
                    }
                    else if (property == "m_LocalRotation.w")
                    {
                        rotKeys_Pre[i].value.w = curve.keys[i].value;
                    }
                }
            }
            else if (property == "m_LocalScale.x" || property == "m_LocalScale.y" || property == "m_LocalScale.z")
            {
                int num = curve.keys.Length;
                if (scaKeys_Pre == null)
                {
                    scaKeys_Pre = new float3[num];
                    scaTime = new float[num];
                    bSca = true;
                    for (int i = 0; i < num; i++)
                    {
                        scaTime[i] = curve.keys[i].time - curve.keys[0].time;
                    }
                }

                for (int i = 0; i < num; i++)
                {
                    if (property == "m_LocalScale.x")
                    {
                        scaKeys_Pre[i].x = curve.keys[i].value;
                    }
                    else if (property == "m_LocalScale.y")
                    {
                        scaKeys_Pre[i].y = curve.keys[i].value;
                    }
                    else if (property == "m_LocalScale.z")
                    {
                        scaKeys_Pre[i].z = curve.keys[i].value;
                    }
                }
            }


        }

        public void Add(AnimationCurve curve, string property, float ct)
        {
            if (property == "m_LocalPosition.x" || property == "m_LocalPosition.y" || property == "m_LocalPosition.z")
            {
                int num = curve.keys.Length;
                if (posKeys_Pre == null)
                {
                    posKeys_Pre = new float3[num + 1];
                    posTime = new float[num + 1];

                    bPos = true;
                    pc = posKeys_Pre.Length;
                    for (int i = 0; i < num + 1; i++)
                    {
                        if (i == num)
                        {
                            posTime[i] = ct;
                        }
                        else
                        {
                            posTime[i] = curve.keys[i].time - curve.keys[0].time;
                        }
                    }
                }

                for (int i = 0; i < num + 1; i++)
                {
                    if (property == "m_LocalPosition.x")
                    {
                        posKeys_Pre[i].x = curve.keys[i % num].value;
                    }
                    else if (property == "m_LocalPosition.y")
                    {
                        posKeys_Pre[i].y = curve.keys[i % num].value;
                    }
                    else if (property == "m_LocalPosition.z")
                    {
                        posKeys_Pre[i].z = curve.keys[i % num].value;
                    }
                }
            }
            else if (property == "m_LocalRotation.x" || property == "m_LocalRotation.y" || property == "m_LocalRotation.z" || property == "m_LocalRotation.w")
            {
                int num = curve.keys.Length;
                if (rotKeys_Pre == null)
                {
                    rotKeys_Pre = new quaternion[num + 1];
                    rotTime = new float[num + 1];

                    bRot = true;
                    rc = rotKeys_Pre.Length;
                    for (int i = 0; i < num + 1; i++)
                    {
                        if (i == num)
                        {
                            rotTime[i] = ct;
                        }
                        else
                        {
                            rotTime[i] = curve.keys[i].time - curve.keys[0].time;
                        }
                    }
                }

                for (int i = 0; i < num + 1; i++)
                {
                    if (property == "m_LocalRotation.x")
                    {
                        rotKeys_Pre[i].value.x = curve.keys[i % num].value;
                    }
                    else if (property == "m_LocalRotation.y")
                    {
                        rotKeys_Pre[i].value.y = curve.keys[i % num].value;
                    }
                    else if (property == "m_LocalRotation.z")
                    {
                        rotKeys_Pre[i].value.z = curve.keys[i % num].value;
                    }
                    else if (property == "m_LocalRotation.w")
                    {
                        rotKeys_Pre[i].value.w = curve.keys[i % num].value;
                    }
                }
            }
            else if (property == "m_LocalScale.x" || property == "m_LocalScale.y" || property == "m_LocalScale.z")
            {
                int num = curve.keys.Length;
                if (scaKeys_Pre == null)
                {
                    scaKeys_Pre = new float3[num + 1];
                    scaTime = new float[num + 1];

                    bSca = true;
                    sc = scaKeys_Pre.Length;
                    for (int i = 0; i < num + 1; i++)
                    {
                        if (i == num)
                        {
                            scaTime[i] = ct;
                        }
                        else
                        {
                            scaTime[i] = curve.keys[i].time - curve.keys[0].time;
                        }
                    }
                }

                for (int i = 0; i < num + 1; i++)
                {
                    if (property == "m_LocalScale.x")
                    {
                        scaKeys_Pre[i].x = curve.keys[i % num].value;
                    }
                    else if (property == "m_LocalScale.y")
                    {
                        scaKeys_Pre[i].y = curve.keys[i % num].value;
                    }
                    else if (property == "m_LocalScale.z")
                    {
                        scaKeys_Pre[i].z = curve.keys[i % num].value;
                    }
                }
            }
        }

        public void Add1(AnimationCurve curve, string property, float ct)
        {
            if (property == "m_LocalPosition.x" || property == "m_LocalPosition.y" || property == "m_LocalPosition.z")
            {
                int num = curve.keys.Length;
                if (posKeys_Pre == null)
                {
                    posKeys_Pre = new float3[num];
                    posTime = new float[num];

                    bPos = true;
                    pc = posKeys_Pre.Length;
                    for (int i = 0; i < num; i++)
                    {
                        if (i == num - 1)
                        {
                            posTime[i] = ct;
                        }
                        else
                        {
                            posTime[i] = curve.keys[i].time - curve.keys[0].time;
                        }
                    }
                }

                for (int i = 0; i < num; i++)
                {
                    if (property == "m_LocalPosition.x")
                    {
                        posKeys_Pre[i].x = curve.keys[i].value;
                    }
                    else if (property == "m_LocalPosition.y")
                    {
                        posKeys_Pre[i].y = curve.keys[i].value;
                    }
                    else if (property == "m_LocalPosition.z")
                    {
                        posKeys_Pre[i].z = curve.keys[i].value;
                    }
                }
            }
            else if (property == "m_LocalRotation.x" || property == "m_LocalRotation.y" || property == "m_LocalRotation.z" || property == "m_LocalRotation.w")
            {
                int num = curve.keys.Length;
                if (rotKeys_Pre == null)
                {
                    rotKeys_Pre = new quaternion[num];
                    rotTime = new float[num];

                    bRot = true;
                    rc = rotKeys_Pre.Length;
                    for (int i = 0; i < num; i++)
                    {
                        if (i == num - 1)
                        {
                            rotTime[i] = ct;
                        }
                        else
                        {
                            rotTime[i] = curve.keys[i].time - curve.keys[0].time;
                        }
                    }
                }

                for (int i = 0; i < num; i++)
                {
                    if (property == "m_LocalRotation.x")
                    {
                        rotKeys_Pre[i].value.x = curve.keys[i].value;
                    }
                    else if (property == "m_LocalRotation.y")
                    {
                        rotKeys_Pre[i].value.y = curve.keys[i].value;
                    }
                    else if (property == "m_LocalRotation.z")
                    {
                        rotKeys_Pre[i].value.z = curve.keys[i].value;
                    }
                    else if (property == "m_LocalRotation.w")
                    {
                        rotKeys_Pre[i].value.w = curve.keys[i].value;
                    }
                }
            }
            else if (property == "m_LocalScale.x" || property == "m_LocalScale.y" || property == "m_LocalScale.z")
            {
                int num = curve.keys.Length;
                if (scaKeys_Pre == null)
                {
                    scaKeys_Pre = new float3[num];
                    scaTime = new float[num];

                    bSca = true;
                    sc = scaKeys_Pre.Length;
                    for (int i = 0; i < num; i++)
                    {
                        if (i == num - 1)
                        {
                            scaTime[i] = ct;
                        }
                        else
                        {
                            scaTime[i] = curve.keys[i].time - curve.keys[0].time;
                        }
                    }
                }

                for (int i = 0; i < num; i++)
                {
                    if (property == "m_LocalScale.x")
                    {
                        scaKeys_Pre[i].x = curve.keys[i].value;
                    }
                    else if (property == "m_LocalScale.y")
                    {
                        scaKeys_Pre[i].y = curve.keys[i].value;
                    }
                    else if (property == "m_LocalScale.z")
                    {
                        scaKeys_Pre[i].z = curve.keys[i].value;
                    }
                }
            }
        }

        public void Normalize(int maxCount)
        {
            keyCount = maxCount;

            posKeys = new float3[keyCount];
            rotKeys = new quaternion[keyCount];
            scaKeys = new float3[keyCount];

            float ut = (posTime[posTime.Length - 1] - posTime[0]) / (float)(keyCount - 1);
            for (int i = 0; i < keyCount; i++)
            {
                posKeys[i] = SamplePos_Pre(posTime[0] + ut * (float)i);
                rotKeys[i] = SampleRot_Pre(posTime[0] + ut * (float)i);
                scaKeys[i] = SampleSca_Pre(posTime[0] + ut * (float)i);
            }

        }

        public void Normalize()
        {
            posKeys = new float3[pc];
            rotKeys = new quaternion[rc];
            scaKeys = new float3[sc];

            float pt = (posTime[posTime.Length - 1] - posTime[0]) / (float)(pc - 1);
            float rt = (rotTime[rotTime.Length - 1] - rotTime[0]) / (float)(rc - 1);
            float st = (scaTime[scaTime.Length - 1] - scaTime[0]) / (float)(sc - 1);
            for (int i = 0; i < pc; i++)
            {
                posKeys[i] = SamplePos_Pre1(posTime[0] + pt * (float)i);
            }

            for (int i = 0; i < rc; i++)
            {
                rotKeys[i] = SampleRot_Pre1(posTime[0] + rt * (float)i);
            }

            for (int i = 0; i < sc; i++)
            {
                scaKeys[i] = SampleSca_Pre1(posTime[0] + st * (float)i);
            }
        }

        public static int tc;
        public void Normalize1()
        {
            //tc = 16;

            posKeys = new float3[tc];
            rotKeys = new quaternion[tc];
            scaKeys = new float3[tc];

            float pt = (posTime[posTime.Length - 1] - posTime[0]) / (float)(tc - 1);
            float rt = (rotTime[rotTime.Length - 1] - rotTime[0]) / (float)(tc - 1);
            float st = (scaTime[scaTime.Length - 1] - scaTime[0]) / (float)(tc - 1);
            for (int i = 0; i < tc; i++)
            {
                posKeys[i] = SamplePos_Pre1(posTime[0] + pt * (float)i);
            }

            for (int i = 0; i < tc; i++)
            {
                rotKeys[i] = SampleRot_Pre1(posTime[0] + rt * (float)i);
            }

            for (int i = 0; i < tc; i++)
            {
                scaKeys[i] = SampleSca_Pre1(posTime[0] + st * (float)i);
            }
        }

        public void NormalizeToDQ()
        {                      
            if(bPos && bRot)
            {
                bDQ = true;
                //dqc = 16;
                dqKeys = new dualquaternion[dqc];

                quaternion r;
                float3 t;

                float ut = (rotTime[rotTime.Length - 1] - rotTime[0]) / (float)(dqc - 1);
                for (int i = 0; i < dqc; i++)
                {
                    r = SampleRot_Pre1(rotTime[0] + ut * (float)i);
                    t = SamplePos_Pre1(rotTime[0] + ut * (float)i);                    
                    dqKeys[i] = new dualquaternion(r, t);
                }
            }            
        }

        float epsilon = 0.01f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 SamplePos_Pre(float t)
        {
            if (posTime != null && posKeys_Pre != null)
            {
                float3 lp = float3.zero;
                int num = posKeys_Pre.Length - 1;
                for (int i = 0; i < num; i++)
                {
                    float t0 = posTime[i];
                    float t1 = posTime[i + 1];
                    if (math.abs(t1 - t0) > epsilon)
                    {
                        if (t0 <= t && t < t1)
                        {
                            float u = (t - t0) / (t1 - t0);
                            lp = math.lerp(posKeys_Pre[i], posKeys_Pre[i + 1], u);
                            return lp;
                        }
                    }
                    else
                    {
                        return posKeys_Pre[i + 1];
                    }
                }
            }

            return this.lp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion SampleRot_Pre(float t)
        {
            if (rotTime != null && rotKeys_Pre != null)
            {
                quaternion lr = quaternion.identity;
                int num = rotKeys_Pre.Length - 1;
                for (int i = 0; i < num; i++)
                {
                    float t0 = rotTime[i];
                    float t1 = rotTime[i + 1];
                    if (math.abs(t1 - t0) > epsilon)
                    {
                        if (t0 <= t && t < t1)
                        {
                            float u = (t - t0) / (t1 - t0);
                            lr = math.slerp(rotKeys_Pre[i], rotKeys_Pre[i + 1], u);
                            return lr;
                        }
                    }
                    else
                    {
                        return rotKeys_Pre[i + 1];
                    }
                }
            }

            return this.lr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 SampleSca_Pre(float t)
        {
            if (scaTime != null && scaKeys_Pre != null)
            {
                float3 ls = float3.zero;
                int num = scaKeys_Pre.Length - 1;
                for (int i = 0; i < num; i++)
                {
                    float t0 = scaTime[i];
                    float t1 = scaTime[i + 1];
                    if (math.abs(t1 - t0) > epsilon)
                    {
                        if (t0 <= t && t < t1)
                        {
                            float u = (t - t0) / (t1 - t0);
                            ls = math.lerp(scaKeys_Pre[i], scaKeys_Pre[i + 1], u);
                            return ls;
                        }
                    }
                    else
                    {
                        return scaKeys_Pre[i + 1];
                    }
                }
            }

            return this.ls;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 SamplePos_Pre1(float t)
        {
            if (posTime != null && posKeys_Pre != null)
            {
                float3 lp = float3.zero;
                int num = posKeys_Pre.Length - 1;
                for (int i = 0; i < num; i++)
                {
                    float t0 = posTime[i];
                    float t1 = posTime[i + 1];
                    //if (math.abs(t1 - t0) > epsilon)
                    {
                        if (t0 <= t && t < t1)
                        {
                            float u = (t - t0) / (t1 - t0);
                            lp = math.lerp(posKeys_Pre[i], posKeys_Pre[i + 1], u);
                            return lp;
                        }                                                
                    }                    
                }

                if (t == posTime[num])
                {
                    return posKeys_Pre[num];
                }
            }

            return this.lp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion SampleRot_Pre1(float t)
        {
            if (rotTime != null && rotKeys_Pre != null)
            {
                quaternion lr = quaternion.identity;
                int num = rotKeys_Pre.Length - 1;
                for (int i = 0; i < num; i++)
                {
                    float t0 = rotTime[i];
                    float t1 = rotTime[i + 1];
                    //if (math.abs(t1 - t0) > epsilon)
                    {
                        if (t0 <= t && t < t1)
                        {
                            float u = (t - t0) / (t1 - t0);
                            lr = math.slerp(rotKeys_Pre[i], rotKeys_Pre[i + 1], u);
                            return lr;
                        }
                       
                    }                    
                }

                if (t == rotTime[num])
                {
                    return rotKeys_Pre[num];
                }
            }

            return this.lr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 SampleSca_Pre1(float t)
        {
            if (scaTime != null && scaKeys_Pre != null)
            {
                float3 ls = float3.zero;
                int num = scaKeys_Pre.Length - 1;
                for (int i = 0; i < num; i++)
                {
                    float t0 = scaTime[i];
                    float t1 = scaTime[i + 1];
                    //if (math.abs(t1 - t0) > epsilon)
                    {
                        if (t0 <= t && t < t1)
                        {
                            float u = (t - t0) / (t1 - t0);
                            ls = math.lerp(scaKeys_Pre[i], scaKeys_Pre[i + 1], u);
                            return ls;
                        }                       
                    }                  
                }

                if (t == scaTime[num])
                {
                    return scaKeys_Pre[num];
                }
            }

            return this.ls;
        }


        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 SamplePos(float t)
        {
            float3 pos = float3.zero;
            if (pc > 1)
            {
                float st = ct / (float)(pc - 1);

                float s = t / st;
                int i = (int)math.floor(s);
                float u = math.frac(s);

                pos = math.lerp(posKeys_Pre[i], posKeys_Pre[i + 1], u);
            }
            else
            {
                pos = posKeys_Pre[0];
            }

            return pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion SampleRot(float t)
        {
            quaternion rot = quaternion.identity;
            if (rc > 1)
            {
                float st = ct / (float)(rc - 1);

                float s = t / st;
                int i = (int)math.floor(s);
                float u = math.frac(s);

                rot = math.slerp(rotKeys_Pre[i], rotKeys_Pre[i + 1], u);
            }
            else
            {
                rot = rotKeys_Pre[0];
            }

            return rot;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 SampleSca(float t)
        {
            float3 sca = float3.zero;
            if (sc > 1)
            {
                float st = ct / (float)(sc - 1);

                float s = t / st;
                int i = (int)math.floor(s);
                float u = math.frac(s);

                sca = math.lerp(scaKeys_Pre[i], scaKeys_Pre[i + 1], u);
            }
            else
            {
                sca = scaKeys_Pre[0];
            }

            return sca;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 SamplePos1(float t)
        {
            float3 pos = float3.zero;
            if (tc > 1)
            {
                float st = ct / (float)(tc - 1);

                float s = t / st;
                int i = (int)math.floor(s);
                float u = math.frac(s);
                //int i = (int)s;
                //float u = s - (float)i;

                pos = math.lerp(posKeys[i], posKeys[i + 1], u);
            }
            else
            {
                pos = posKeys[0];
            }

            return pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion SampleRot1(float t)
        {
            quaternion rot = quaternion.identity;
            if (tc > 1)
            {
                float st = ct / (float)(tc - 1);

                float s = t / st;
                int i = (int)math.floor(s);
                float u = math.frac(s);
                //int i = (int)s;
                //float u = s - (float)i;


                rot = math.slerp(rotKeys[i], rotKeys[i + 1], u);
            }
            else
            {
                rot = rotKeys[0];
            }

            return rot;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 SampleSca1(float t)
        {
            float3 sca = float3.zero;
            if (tc > 1)
            {
                float st = ct / (float)(tc - 1);

                float s = t / st;
                int i = (int)math.floor(s);
                float u = math.frac(s);
                //int i = (int)s;
                //float u = s - (float)i;

                sca = math.lerp(scaKeys[i], scaKeys[i + 1], u);
            }
            else
            {
                sca = scaKeys[0];
            }

            return sca;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sample(float t, BoneNode bn, bool nonScale = false)
        {
            float3 pos = float3.zero;
            quaternion rot = quaternion.identity;
            float3 sca = float3.zero;

            if (used)
            {
                if (bPos)
                {
                    pos = SamplePos1(t);
                }
                else
                {
                    pos = lp;
                }

                if (bRot)
                {
                    rot = SampleRot1(t);
                }
                else
                {
                    rot = lr;
                }

                if (bSca)
                {
                    sca = SampleSca1(t);
                }
                else
                {
                    sca = ls;
                }
            }
            else
            {
                pos = lp;
                rot = lr;
                sca = ls;
            }

            bn.transform.posL = pos;
            bn.transform.rotL = rot;
            bn.transform.scaL = sca;

            if(nonScale)
            {
                bn.transform.scaL = new float3(1.0f, 1.0f, 1.0f);
            }            
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 SamplePos_Safe(float t)
        {
            float3 pos = float3.zero;

            if(bPos)
            {
                pos = SamplePos1(t);
            }
            else
            {
                pos = lp;
            }           

            return pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion SampleRot_Safe(float t)
        {
            quaternion rot = quaternion.identity;

            if (bRot)
            {
                rot = SampleRot1(t);
            }
            else
            {
                rot = lr;
            }

            return rot;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 SampleSca_Safe(float t)
        {
            float3 sca = float3.zero;

            if (bSca)
            {
                sca = SampleSca1(t);
            }
            else
            {
                sca = ls;
            }

            return sca;
        }


        //dualQuaternion
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public dualquaternion SampleDualQuaternion(float t)
        {
            dualquaternion dq = dualquaternion.identity;
            if(dqc > 1)
            {
                float st = ct / (float)(dqc - 1);

                float s = t / st;
                int i = (int)math.floor(s);
                float u = math.frac(s);

                //float s = t / st;
                //float floor = math.floor(s);                
                //int i = (int)floor;
                //float u = math.frac(s);

                dq = dualquaternion.scLerp(dqKeys[i], dqKeys[i + 1], u);
            }
            else
            {
                dq = dqKeys[0];
            }

            return dq;
        }
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SampleDQ(float dt, BoneNode bn)
        {
            float3 t = lp;
            quaternion r = lr;
            dualquaternion dq = dualquaternion.identity;

            if (used)
            {
                if(bDQ)
                {
                     dq = SampleDualQuaternion(dt);
                }
                else
                {                   
                    if(bPos)
                    {
                        t = SamplePos1(dt);                        
                    }                 
                
                    if(bRot)
                    {
                        r = SampleRot1(dt);
                    }
                   
                    dq = new dualquaternion(r, t);
                }
            }
            else
            {
                dq = new dualquaternion(r, t);
            }

            bn.transform.dqL = dq;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public dualquaternion SampleDQ(float dt)
        {
            float3 t = lp;
            quaternion r = lr;
            dualquaternion dq = dualquaternion.identity;

            if (used)
            {
                if (bDQ)
                {
                    dq = SampleDualQuaternion(dt);
                }
                else
                {
                    if (bPos)
                    {
                        t = SamplePos1(dt);
                    }

                    if (bRot)
                    {
                        r = SampleRot1(dt);
                    }

                    dq = new dualquaternion(r, t);
                }
            }
            else
            {
                dq = new dualquaternion(r, t);
            }

            return dq;
        }


        //Animation Baking
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetFrameInfo(float t, out int i, out float u, bool bDQ = true)
        {
            i = 0;
            u = 0.0f;
            float st;

            if (bDQ)
            {
                st = ct / (float)(dqc - 1);
            }
            else
            {
                st = ct / (float)(tc - 1);
            }

            float s = t / st;
            i = (int)math.floor(s);
            u = math.frac(s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 BakePos(int i)
        {
            float3 pos = float3.zero;
            if (tc > 1)
            {
                pos = posKeys[i];
            }
            else
            {
                pos = posKeys[0];
            }

            return pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion BakeRot(int i)
        {
            quaternion rot = quaternion.identity;
            if (tc > 1)
            {
                rot = rotKeys[i];
            }
            else
            {
                rot = rotKeys[0];
            }

            return rot;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 BakeSca(int i)
        {
            float3 sca = float3.zero;
            if (tc > 1)
            {               
                sca = scaKeys[i];
            }
            else
            {
                sca = scaKeys[0];
            }

            return sca;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public dualquaternion BakeDualQuaternion(int i)
        {
            dualquaternion dq = dualquaternion.identity;
            if (dqc > 1)
            {
                dq = dqKeys[i];
            }
            else
            {
                dq = dqKeys[0];
            }

            return dq;
        }

        public void BakeDQ(int j, BoneNode bn)
        {
            float3 t = lp;
            quaternion r = lr;
            dualquaternion dq = dualquaternion.identity;

            if (used)
            {
                if (bDQ)
                {
                    dq = BakeDualQuaternion(j);
                }
                else
                {
                    if (bPos)
                    {
                        t = BakePos(j);
                    }

                    if (bRot)
                    {
                        r = BakeRot(j);
                    }

                    dq = new dualquaternion(r, t);
                }
            }
            else
            {
                dq = new dualquaternion(r, t);
            }

            bn.transform.dqL = dq;
        }
    }        
}