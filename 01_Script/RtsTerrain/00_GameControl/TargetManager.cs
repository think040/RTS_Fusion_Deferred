using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Unity.Mathematics;

using UnityEngine;
using UnityEngine.Rendering;
using Random = Unity.Mathematics.Random;

using Fusion;
public class TargetManager : MonoBehaviour
{
    private void Awake()
    {

    }

    void Start()
    {

    }

    public ComputeShader cshader;
    public Terrain terrain;

    int count;
    UnitActor[] actors;

    TargetCompute targetCom;

    float3[] _targetPos;
    float4[] _minDist;
    float4x4[] debug_tPos;

    public NetworkRunner runner;

    public void Init()
    {
        count = GameManager.unitCount;

        if (count > 0)
        {
            actors = GameManager.unitActors;

            targetCom = new TargetCompute();
            targetCom.Init(count, cshader, actors, terrain);

            _targetPos = GameManager.targetPos_Buffer.data;
            _minDist = GameManager.minDist_Buffer.data;

            debug_tPos = TargetCompute.debug_Buffer.data;
        }

        {
            runner = GetComponent<NetworkRunner>();
        }
    }

    void Update()
    {                
        if (!GameManager.bUpdate)
        {
            return;
        }
    
        if (GameManager.isPause)
        {
            return;
        }

        //if(!GameManager.instance.runner.IsServer)
        //{
        //    return;
        //}
        if (runner.IsServer)
        {
            if (count > 0)
            {
                targetCom.Compute();
            }
        }        
    }

    //void FixedUpdate()
    //{
    //    if (!GameManager.bUpdate)
    //    {
    //        return;
    //    }
    //
    //    if (GameManager.isPause)
    //    {
    //        return;
    //    }
    //
    //    //if(!GameManager.instance.runner.IsServer)
    //    //{
    //    //    return;
    //    //}        
    //
    //    if(runner.IsServer)
    //    {
    //        if (count > 0)
    //        {
    //            targetCom.Compute();
    //        }
    //    }
    //   
    //}


    private void OnDestroy()
    {       
        if (targetCom != null)
        {
            targetCom.ReleaseResource(); targetCom = null;
        }
    }


    class TargetCompute
    {
        ComputeShader cshader;
        int ki_trW;
        int ki_tPos;
        int ki_tEnemy;

        int count;

        UnitActor[] actors;

        ROBuffer<float4x4> trM_Buffer;
        RWBuffer<float4x4> trW_Buffer;

        public static RWBuffer<float4> circle_Buffer
        {
            get; set;
        }

        public static RWBuffer<float4> terrainArea_Buffer
        {
            get; set;
        }

        COBuffer<float3> block_Buffer;
        ROBuffer<int> random_Buffer;
        public static RWBuffer<float3> refTargetPos_Buffer
        {
            get; set;
        }

        Texture2D alphaTex;
        Texture holeTex;
        float4 t1_t0;
        float4x4 T;

        public static RWBuffer<float3> targetPos_Buffer
        {
            get; set;
        }

        public static RWBuffer<float4x4> debug_Buffer
        {
            get; set;
        }

        public static ROBuffer<int> active_Buffer
        {
            get; set;
        }

        public static ROBuffer<float4> unitData_Buffer
        {
            get; set;
        }

        public static RWBuffer<float4> minDist_Buffer
        {
            get; set;
        }

        Terrain terrain;
        TerrainData tData;

        RenderTexture hMap;
        float3 dpCount;
        float3 gtCount;
        float3 tileCount;
        float3 terrainSize;
        float3 tileSize;

        const int trGpCount = 64;
        int trDpCount;

        public RenderTexture normalHeight_Tex;
        public static float4[] nhData
        {
            get; set;
        }

        Random random;
        public void Init(int count, ComputeShader cshader, UnitActor[] actors, Terrain terrain)
        {
            {
                this.count = count;
                this.cshader = cshader;
                this.actors = actors;
                this.terrain = terrain;

                tData = terrain.terrainData;
                ki_trW = cshader.FindKernel("CS_TrW");
                ki_tPos = cshader.FindKernel("CS_TargetPos");
                ki_tEnemy = cshader.FindKernel("CS_TargetEnemy");

                cshader.SetInt("count", count);
            }

            {
                active_Buffer = GameManager.active_Buffer;

                trM_Buffer = new ROBuffer<float4x4>(count);
                trW_Buffer = new RWBuffer<float4x4>(count);
                circle_Buffer = new RWBuffer<float4>(count);
                terrainArea_Buffer = GameManager.terrainArea_Buffer;

                block_Buffer = new COBuffer<float3>(8);
                random_Buffer = new ROBuffer<int>(count);
                refTargetPos_Buffer = GameManager.refTargetPos_Buffer;
                targetPos_Buffer = GameManager.targetPos_Buffer;
                debug_Buffer = new RWBuffer<float4x4>(count);

                unitData_Buffer = new ROBuffer<float4>(count);
                minDist_Buffer = GameManager.minDist_Buffer;
            }

            {
                cshader.SetBuffer(ki_trW, "trM_Buffer", trM_Buffer.value);
                cshader.SetBuffer(ki_trW, "circle_Buffer", circle_Buffer.value);
                cshader.SetBuffer(ki_trW, "trW_Buffer", trW_Buffer.value);
                cshader.SetBuffer(ki_trW, "terrainArea_Buffer", terrainArea_Buffer.value);
            }

            {
                cshader.SetBuffer(ki_tPos, "active_Buffer", active_Buffer.value);
                cshader.SetBuffer(ki_tPos, "state_Buffer", GameManager.state_Buffer.value);
                cshader.SetBuffer(ki_tPos, "trW_Buffer", trW_Buffer.value);
                cshader.SetBuffer(ki_tPos, "circle_Buffer", circle_Buffer.value);
                cshader.SetBuffer(ki_tPos, "block_Buffer", block_Buffer.value);
                cshader.SetBuffer(ki_tPos, "random_Buffer", random_Buffer.value);
                cshader.SetBuffer(ki_tPos, "refTargetPos_Buffer", refTargetPos_Buffer.value);
                cshader.SetBuffer(ki_tPos, "targetPos_Buffer", targetPos_Buffer.value);
                cshader.SetBuffer(ki_tPos, "debug_Buffer", debug_Buffer.value);
            }

            {
                cshader.SetBuffer(ki_tEnemy, "active_Buffer", active_Buffer.value);
                cshader.SetBuffer(ki_tEnemy, "unitData_Buffer", unitData_Buffer.value);
                cshader.SetBuffer(ki_tEnemy, "circle_Buffer", circle_Buffer.value);
                cshader.SetBuffer(ki_tEnemy, "minDist_Buffer", minDist_Buffer.value);
            }


            {
                for (int i = 0; i < count; i++)
                {
                    float radius = 0.5f;
                    circle_Buffer.data[i] = new float4(actors[i].transform.position, radius);
                }

                circle_Buffer.Write();
            }


            {
                random = new Random();
                random.InitState();

                for (int i = 0; i < count; i++)
                {
                    random_Buffer.data[i] = random.NextInt(0, 7);
                }

                random_Buffer.Write();
            }

            {
                var _data = block_Buffer.data;

                float r = 1.0f;

                _data[0] = new float3(-1.0f, 0.0f, -1.0f) * r;
                _data[1] = new float3(+0.0f, 0.0f, -1.0f) * r;
                _data[2] = new float3(+1.0f, 0.0f, -1.0f) * r;

                _data[3] = new float3(-1.0f, 0.0f, +0.0f) * r;

                _data[4] = new float3(+1.0f, 0.0f, +0.0f) * r;

                _data[5] = new float3(-1.0f, 0.0f, +1.0f) * r;
                _data[6] = new float3(+0.0f, 0.0f, +1.0f) * r;
                _data[7] = new float3(+1.0f, 0.0f, +1.0f) * r;

                block_Buffer.Write();
            }

            //Terrain
            {
                hMap = tData.heightmapTexture;

                dpCount = new float3((float)(hMap.width - 1), 1.0f, (float)(hMap.height - 1));
                gtCount = new float3(16.0f, 1.0f, 16.0f);
                tileCount = dpCount / gtCount;

                terrainSize = tData.size;
                terrainSize.y *= 2.0f;
                tileSize = terrainSize / tileCount;

                trDpCount = (count % trGpCount == 0) ? (count / trGpCount) : (count / trGpCount + 1);
            }

            {
                normalHeight_Tex = TerrainManager.normalHeight_Tex;
            }


            {
                cshader.SetVector("terrainSize", new Vector4(terrainSize.x, terrainSize.y, terrainSize.z, 0.0f));
                cshader.SetVector("dpCount", new Vector4(dpCount.x, dpCount.y, dpCount.z, 0.0f));
                cshader.SetTexture(ki_tPos, "normalHeight_Tex", normalHeight_Tex);
            }

            if (tData.alphamapTextures != null)
            {
                alphaTex = tData.alphamapTextures[0];
                cshader.SetTexture(ki_trW, "alphaTex", alphaTex);
                cshader.SetTexture(ki_tPos, "alphaTex", alphaTex);
            }


            if (tData.holesTexture != null)
            {
                holeTex = tData.holesTexture;
                cshader.SetTexture(ki_tPos, "holeTex", holeTex);
            }


            {
                float4 texSize = new float4((float)(alphaTex.width), (float)(alphaTex.height), 0.0f, 0.0f);
                t1_t0 = new float4(texSize.xy / terrainSize.xz, 0.0f, 0.0f);

                cshader.SetVector("t1_t0", t1_t0);
            }

            {
                for (int i = 0; i < count; i++)
                {
                    var data = unitData_Buffer.data;
                    data[i] = float4.zero;
                    data[i].x = (float)actors[i].pNum;
                    data[i].y = (float)actors[i].vRadius;
                }
                unitData_Buffer.Write();
            }
        }


        public void Compute()
        {
            WriteToResource();
            DispatchCompute();
            ReadFromResource();
        }

        void WriteToResource0()
        {
            for (int i = 0; i < count; i++)
            {
                var tr = actors[i].transform;
                float4x4 Mat;
                Mat.c0 = new float4(tr.position, 0.0f);
                Mat.c1 = ((quaternion)tr.localRotation).value;
                Mat.c2 = new float4(tr.localScale, 0.0f);
                Mat.c3 = float4.zero;

                trM_Buffer.data[i] = Mat;
            }

            for (int i = 0; i < count; i++)
            {
                random_Buffer.data[i] = random.NextInt(0, 7);
            }

            trM_Buffer.Write();
            random_Buffer.Write();
            refTargetPos_Buffer.Write();
            targetPos_Buffer.Write();

            for (int i = 0; i < count; i++)
            {
                var data = unitData_Buffer.data;
                data[i].y = (float)actors[i].vRadius;
            }
            unitData_Buffer.Write();

            {
                cshader.SetMatrix("T", TerrainManager.matT);
            }
        }

        void WriteToResource()
        {
            for (int i = 0; i < count; i++)
            {
                var tr = actors[i].transform;
                var ntTrs = GameManager.unitNtTrs[i];

                float4x4 Mat;
                Mat.c0 = new float4(ntTrs.ReadPosition(), 0.0f);
                Mat.c1 = ((quaternion)ntTrs.ReadRotation()).value;
                Mat.c2 = new float4(tr.localScale, 0.0f);
                Mat.c3 = float4.zero;

                trM_Buffer.data[i] = Mat;
            }

            for (int i = 0; i < count; i++)
            {
                random_Buffer.data[i] = random.NextInt(0, 7);
            }

            trM_Buffer.Write();
            random_Buffer.Write();
            refTargetPos_Buffer.Write();
            targetPos_Buffer.Write();

            for (int i = 0; i < count; i++)
            {
                var data = unitData_Buffer.data;
                data[i].y = (float)actors[i].vRadius;
            }
            unitData_Buffer.Write();

            {
                cshader.SetMatrix("T", TerrainManager.matT);
            }
        }

        void DispatchCompute()
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            cmd.DispatchCompute(cshader, ki_trW, trDpCount, 1, 1);
            cmd.DispatchCompute(cshader, ki_tPos, count, 1, 1);
            cmd.DispatchCompute(cshader, ki_tEnemy, count, 1, 1);

            Graphics.ExecuteCommandBuffer(cmd);
            //Graphics.ExecuteCommandBufferAsync(cmd, ComputeQueueType.Urgent);            
            
            CommandBufferPool.Release(cmd);
        }

        public bool bDebug = false;

        void ReadFromResource()
        {
            if (bDebug)
            {
                debug_Buffer.Read();
            }

            {
                terrainArea_Buffer.Read();
                targetPos_Buffer.Read();
                minDist_Buffer.Read();
            }
        }

        public void ReleaseResource()
        {
            BufferBase<float4x4>.Release(trM_Buffer);
            BufferBase<float4x4>.Release(trW_Buffer);
            BufferBase<float4>.Release(circle_Buffer);
            BufferBase<float3>.Release(block_Buffer);
            BufferBase<int>.Release(random_Buffer);
            BufferBase<float4>.Release(unitData_Buffer);
            BufferBase<float4x4>.Release(debug_Buffer);
        }
    }
}
