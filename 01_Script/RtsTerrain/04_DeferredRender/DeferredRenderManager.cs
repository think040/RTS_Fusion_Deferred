using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.Mathematics;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class DeferredRenderManager : MonoBehaviour
{      
    public Mesh[] inputMeshes;    
   
    MeshInfo mesh_quad;
    MeshInfo mesh_dLight;    

    public Transform trMLight;

    public ComputeShader cshader;
    int ki_worldVertex;

    public Shader gshader;
    
    int pass_LBuffer;
    int pass_Final;
    int pass_Fxaa;
    int pass_Debug;

    public GameObject panel_fxaa_data;    

    Slider slider_ax;
    Slider slider_ay;
    Slider slider_az;

    InputField input_ax;
    InputField input_ay;
    InputField input_az;

    public float4 fxaa_data = new float4(1.0f, 0.166f, 0.0312f, 0.0f);

   


    Material mte;
    MaterialPropertyBlock mpb;
    

    public static Action<ScriptableRenderContext, CommandBuffer, Camera, RenderGOM_DF.PerCamera> OnRender_GBuffer 
    { get; set; } = (context, cmd, cam, perCam) => { };

    public static Action<ScriptableRenderContext, CommandBuffer, Camera, RenderGOM_DF.PerCamera> OnRender_DBuffer
    { get; set; } = (context, cmd, cam, perCam) => { };

    public static Action<ScriptableRenderContext, CommandBuffer, Camera, RenderGOM_DF.PerCamera> OnRender_SSD
    { get; set; } = (context, cmd, cam, perCam) => { };

    public static Action<ScriptableRenderContext, CommandBuffer, Camera, RenderGOM_DF.PerCamera> OnRender_SkyBox
    { get; set; } = (context, cmd, cam, perCam) => { };

    public static Action<ScriptableRenderContext, CommandBuffer, Camera, RenderGOM_DF.PerCamera> OnRender_Transparent 
    { get; set; } = (context, cmd, cam, perCam) => { };


    void Awake()
    {
        //{
        //    Application.targetFrameRate = 60;
        //    QualitySettings.vSyncCount = 1;
        //}
        //
        //
        //Init();
    }


    IEnumerator routine_debug;

   

    public void Init()
    {
        {
            ki_worldVertex = cshader.FindKernel("CS_WorldVertex");
        }

        {
            mte = new Material(gshader);
            mpb = new MaterialPropertyBlock();
            
            pass_LBuffer = mte.FindPass("LBuffer");
            pass_Final = mte.FindPass("Final");
            pass_Fxaa = mte.FindPass("Fxaa");
            pass_Debug = mte.FindPass("Debug");            
        }

        //{
        //    mpb.SetBuffer("decalInfo_Buffer", DecalManager.decalInfo_Buffer.value);
        //}

        {
            InitData();
            InitMesh();
            InitLight();
        }       

        {
            InitCamera();
        }

        {
            Init_UIcontrol(); //fxaa
        }

        {
            //StartCoroutine(ChangeDebugMode());
            routine_debug = ChangeDebugMode();
        }
    }

    Transform[] unitTrs;
    UnitActor[] unitActors;

    void InitData()
    {
        {
            count_dLight = GameManager.unitCount;
        }

        {
            unitTrs = GameManager.unitTrs;
            unitActors = GameManager.unitActors;
        }
    }


    unsafe void InitMesh()
    {
        {
            mesh_quad = new MeshInfo(RenderUtil.CreateNDCquadMesh(), 1);
        }

        if(count_dLight < 1) { return; }

        {           
            mesh_dLight = new MeshInfo(inputMeshes[0], count_dLight);       //Light            
        }
   
        return;
    }

    void InitCamera()
    {
        perViewBuffer = new COBuffer<ViewData>(1);
        mpb.SetBuffer("camera", perViewBuffer.value);
    }

    Transform trCam;

    void Init_UIcontrol()
    {
        {
            //trLight = LightManager.instance.transform;
            trCam = Camera.main.transform;
        }
        

        {
            // X
            // Only used on FXAA Quality.
            // This used to be the FXAA_QUALITY__SUBPIX define.
            // It is here now to allow easier tuning.
            // Choose the amount of sub-pixel aliasing removal.
            // This can effect sharpness.
            //   1.00 - upper limit (softer)
            //   0.75 - default amount of filtering
            //   0.50 - lower limit (sharper, less sub-pixel aliasing removal)
            //   0.25 - almost off
            //   0.00 - completely off
            //FxaaFloat fxaaQualitySubpix,

            //Y
            // Only used on FXAA Quality.
            // This used to be the FXAA_QUALITY__EDGE_THRESHOLD define.
            // It is here now to allow easier tuning.
            // The minimum amount of local contrast required to apply algorithm.
            //   0.333 - too little (faster)
            //   0.250 - low quality
            //   0.166 - default
            //   0.125 - high quality 
            //   0.063 - overkill (slower)
            //FxaaFloat fxaaQualityEdgeThreshold,


            //Z
            // Only used on FXAA Quality.
            // This used to be the FXAA_QUALITY__EDGE_THRESHOLD_MIN define.
            // It is here now to allow easier tuning.
            // Trims the algorithm from processing darks.
            //   0.0833 - upper limit (default, the start of visible unfiltered edges)
            //   0.0625 - high quality (faster)
            //   0.0312 - visible limit (slower)
            // Special notes when using FXAA_GREEN_AS_LUMA,
            //   Likely want to set this to zero.
            //   As colors that are mostly not-green
            //   will appear very dark in the green channel!
            //   Tune by looking at mostly non-green content,
            //   then start at zero and increase until aliasing is a problem.
            //FxaaFloat fxaaQualityEdgeThresholdMin,
        }


        {
            var slider = panel_fxaa_data.GetComponentsInChildren<Slider>();
            var input_field = panel_fxaa_data.GetComponentsInChildren<InputField>();

            slider_ax = slider[0];
            slider_ay = slider[1];
            slider_az = slider[2];

            input_ax = input_field[0];
            input_ay = input_field[1];
            input_az = input_field[2];
        }

        {
            slider_ax.minValue = 0.0f;  //0.0f
            slider_ax.maxValue = 1.0f;  //1.0f

            slider_ay.minValue = 0.063f;  //0.1f; // 0.063f;  //0.0f
            slider_ay.maxValue = 0.1f;  //0.15f; // 0.333f;  //1.0f
                                        
            slider_az.minValue = 0.0312f;  //0.02f; // 0.0312f;  //0.0f
            slider_az.maxValue = 0.05f;  //0.04f; // 0.0833f;  //0.1f

            slider_ax.onValueChanged.AddListener(
                (value) =>
                {
                    fxaa_data.x = value;
                    input_ax.text = value.ToString();
                });

            slider_ay.onValueChanged.AddListener(
                (value) =>
                {
                    fxaa_data.y = value;
                    input_ay.text = value.ToString();
                });

            slider_az.onValueChanged.AddListener(
                (value) =>
                {
                    fxaa_data.z = value;
                    input_az.text = value.ToString();
                });

            slider_ax.value = fxaa_data.x;
            slider_ay.value = fxaa_data.y;
            slider_az.value = fxaa_data.z;
        }

        {
            input_ax.onEndEdit.AddListener(
                (text) =>
                {
                    float value = 0;
                    if (float.TryParse(text, out value))
                    {
                        var slider = slider_ax;
                        value = math.clamp(value, slider.minValue, slider.maxValue);
                        slider.value = value;
                    }
                });

            input_ay.onEndEdit.AddListener(
                (text) =>
                {
                    float value = 0;
                    if (float.TryParse(text, out value))
                    {
                        var slider = slider_ay;
                        value = math.clamp(value, slider.minValue, slider.maxValue);
                        slider.value = value;
                    }
                });

            input_az.onEndEdit.AddListener(
                (text) =>
                {
                    float value = 0;
                    if (float.TryParse(text, out value))
                    {
                        var slider = slider_az;
                        value = math.clamp(value, slider.minValue, slider.maxValue);
                        slider.value = value;
                    }
                });
        }


    }


    void Update_fxaa()
    {
        float minY = 7.5f;
        float maxY = 40.0f;

        float y;


        {
            Ray ray = new Ray(trCam.position, math.rotate(trCam.rotation, new float3(0.0f, 0.0f, 1.0f)));
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit, 500.0f, LayerMask.GetMask("Terrain")))
            {
                y = math.distance(ray.origin, hit.point);
            }
            else
            {
                y = trCam.position.y;
            }

            y = math.clamp(y, minY, maxY);
        }

        {
            float min = slider_ax.minValue;
            float max = slider_ax.maxValue;
            float k = (min - max) / (maxY - minY);

            float value = k * (y - minY) + max;
            fxaa_data.x = value;
            slider_ax.value = value;
        }

        {
            float min = slider_ay.minValue;
            float max = slider_ay.maxValue;
            float k = (max - min) / (maxY - minY);

            float value = k * (y - minY) + min;
            fxaa_data.y = value;
            slider_ay.value = value;
        }

        {
            float min = slider_az.minValue;
            float max = slider_az.maxValue;
            float k = (max - min) / (maxY - minY);

            float value = k * (y - minY) + min;
            fxaa_data.z = value;
            slider_az.value = value;
        }
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

    };

    struct TrLight
    {
        public TrObject tr;

        public float4 color;
        public float intensity;
        public float range;
        public float3 center;
    };
    
    TrLight[] trs_dLight;

    void UpdateTexture()
    {
        mpb.SetTexture("posTex", RenTexInfo_DF.frame_gbuffer0.tex);
        mpb.SetTexture("depthTex", RenTexInfo_DF.frame_gbuffer1.tex);
        mpb.SetTexture("nomTex", RenTexInfo_DF.frame_gbuffer2.tex);
        mpb.SetTexture("diffuseTex", RenTexInfo_DF.frame_gbuffer3.tex);

        mpb.SetTexture("lightTex", RenTexInfo_DF.frame_lbuffer.tex);
        mpb.SetTexture("decalTex", RenTexInfo_DF.frame_dbuffer.tex);

        mpb.SetTexture("finalTex", RenTexInfo_DF.frame_final.tex);
    }


    struct LightData
    {
        public float4 posW;
        public float4 dirW;
        public float4 posV;
        public float4 dirV;
        public float4 color;
        public float4 data;
    };

    //COBuffer<LightData> mLightData_Buffer;
    ROBuffer<LightData> dLightData_Buffer;

    int count_dLight = 3;
    

    Unity.Mathematics.Random random;

    CSM_Action csm_action;
    CBM_Action cbm_action;

    public float3[] offset_dlight;
    public float[] range_dlight;

    void InitLight()
    {
        
        {
            {
                csm_action = LightManager.instance.csm_action;
                cbm_action = LightManager.instance.cbm_action;
            }

            {
                csm_action.Bind_Data(mpb);
                cbm_action.Bind_Data(mpb);
            }
        }

        if (count_dLight < 1) { return; }


        {
            {
                dLightData_Buffer = new ROBuffer<LightData>(count_dLight);
            }

            {
                mpb.SetBuffer("dLightData_Buffer", dLightData_Buffer.value);
            }
        }

        //{
        //    trs_dLight = new TrLight[count_dLight];
        //
        //    int c0 = 0;
        //    int c1; // = count_dLight / 2;
        //    c1 = CullManager.cullOffsets[4];
        //
        //    for (int i = 0; i < count_dLight; i++)
        //    {               
        //
        //        {
        //            float3 c;
        //            //c = new float3(0.25f, 0.25f, 0.25f);
        //
        //            if (i < c1)
        //            {
        //                c = new float3(0.0f, 0.0f, 0.25f);                        
        //            }
        //            else
        //            {
        //                c = new float3(0.25f, 0.0f, 0.0f);
        //            }
        //
        //            trs_dLight[i].color = new float4(c, 1.0f);
        //        }
        //
        //        {
        //            float value = 1.0f;  //0.5f
        //            trs_dLight[i].range = value;
        //        }
        //
        //        {
        //            float value = 1.0f;
        //            trs_dLight[i].intensity = value;
        //        }
        //
        //        {
        //            float3 center = new float3(0.0f, 1.5f, 0.0f);
        //            trs_dLight[i].center = center;
        //        }
        //       
        //    }
        //
        //    int a = 0;
        //}

        {
            trs_dLight = new TrLight[count_dLight];            


            int k = 0;
            int c0 = GameManager.unitMan.Length;
            for (int i = 0; i < c0; i++)
            {
                int c1 = GameManager.unitMan[i].count;
                for(int j = 0; j < c1; j++)
                {
                    {
                        float3 c;                       
                        if (i < 4)
                        {
                            c = new float3(0.0f, 0.0f, 0.25f);
                        }
                        else
                        {
                            c = new float3(0.25f, 0.0f, 0.0f);
                        }

                        trs_dLight[k].color = new float4(c, 1.0f);
                    }

                    {
                        float value = range_dlight[i];
                        trs_dLight[k].range = value;
                    }

                    {
                        float value = 1.0f;
                        trs_dLight[k].intensity = value;
                    }

                    {
                        float3 center = offset_dlight[i];
                        trs_dLight[k].center = center;
                    }

                    k++;
                }
            }
        }

        {
            mpb.SetBuffer("active_Buffer", GameManager.active_Buffer.value);
            mpb.SetBuffer("state_Buffer", GameManager.state_Buffer.value);

        }

        {
            GameManager.dfCullMan.Bind_GShader(mpb);
        }


        return;
    }

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


    public bool useFxaa = true;

    IEnumerator ChangeDebugMode()
    {
        int rNum0 = 0;
       
        KeyCode key0 = KeyCode.Delete;
        

        //Fxaa On/Off
        while (true)
        {
            {
                if (Input.GetKeyDown(key0))
                {
                    rNum0 = (++rNum0) % 2;
                }

                if (rNum0 == 0)
                {
                    useFxaa = true;
                }
                else
                {
                    useFxaa = false;
                }
            }           

            yield return null;

        }
    }

    public void Enable()
    {
        RenderGOM_DF.BeginFrameRender += Compute;
        RenderGOM_DF.OnRenderCam += Render;

        RenderGOM_DF.OnRenderCamViewport += Render_Debug;
    }

    public void Disable()
    {
        RenderGOM_DF.BeginFrameRender -= Compute;
        RenderGOM_DF.OnRenderCam -= Render;

        RenderGOM_DF.OnRenderCamViewport -= Render_Debug;
    }


    void OnDestroy()
    {
        //if(meshInfo != null) { meshInfo.ReleaseResource(); }    
        

        if (mesh_dLight != null)
        {
            mesh_dLight.ReleaseResource();
        }

        if (mesh_quad != null)
        {
            mesh_quad.ReleaseResource();
        }

        //BufferBase<LightData>.Release(mLightData_Buffer);
        BufferBase<LightData>.Release(dLightData_Buffer);

        BufferBase<ViewData>.Release(perViewBuffer);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //if (count_dLight < 1) { return; }

        if (!GameManager.bUpdate)
        {
            return;
        }

        {
            routine_debug.MoveNext();
        }
     
        {
            UpdateRootW();
        }

        //{
        //    Compute();
        //}
//#if UNITY_EDITOR
        {
            UpdateDebugMode();
        }
//#endif

        {
            Update_fxaa();
        }

    }


    bool bDebug = false;

    void UpdateDebugMode()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            bDebug = !bDebug;
        }

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
    }


    void UpdateCameraData(Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        var data = perViewBuffer.data;
        data[0].V = perCam.V;
        data[0].C = perCam.C;
        data[0].CV = perCam.CV;
        data[0].TC = math.mul(RenderUtil.GetTfromN(), RenderUtil.GetCfromV(cam, false));
        data[0].posW = new float4((float3)cam.transform.position, 0.0f);
        data[0].dirW = new float4(math.rotate(cam.transform.rotation, new float3(0.0f, 0.0f, 1.0f)), 0.0f);
        data[0].data = float4.zero;

        perViewBuffer.Write();

        {
            mpb.SetBuffer("camera", perViewBuffer.value);
        }

        {
            float4 pixelSize = new float4(cam.pixelWidth, cam.pixelHeight, 0.0f, 0.0f);
            mpb.SetVector("pixelSize", pixelSize);
        }
    }
    

    unsafe void UpdateLightData0(RenderGOM_DF.PerCamera perCam)
    {
        float t = Time.time;        

        //DeferredLight
        {
            {
                MeshInfo mesh = mesh_dLight;

                for(int i = 0; i < count_dLight; i++)
                {                    
                    trs_dLight[i].tr.pos = unitTrs[i].position;
                    trs_dLight[i].tr.rot = unitTrs[i].rotation;
                }

                //Debug
                {                   
                    int k = 0;
                    int c0 = GameManager.unitMan.Length;
                    for (int i = 0; i < c0; i++)
                    {
                        int c1 = GameManager.unitMan[i].count;
                        for (int j = 0; j < c1; j++)
                        {
                            {
                                float value = range_dlight[i];
                                trs_dLight[k].range = value;
                            }

                            {
                                float3 center = offset_dlight[i];
                                trs_dLight[k].center = center;
                            }
                            k++;
                        }
                    }
                }


                Parallel.For(0, count_dLight,
                    (int i) =>
                    {
                        float4x4 W = float4x4.identity;
                        float4x4 Wn = float4x4.identity;

                        fixed (LightData* ldata = &(dLightData_Buffer.data[i]))
                        {
                            fixed (TrLight* tr = &(trs_dLight[i]))
                            {
                                {
                                    tr->tr.pos += tr->center;
                                    //tr->tr.rot = unitTrs[i].rotation;
                                    tr->tr.sca = 2.0f * tr->range * new float3(1.0f, 1.0f, 1.0f);

                                    TrObject.GetW_Wn(&(tr->tr), &W, &Wn);
                                }

                                {
                                    var posW = tr->tr.pos;

                                    ldata->posW = new float4(posW, 1.0f);
                                    ldata->posV = math.mul(perCam.V, new float4(posW, 1.0f));
                                    ldata->color = tr->color;
                                    ldata->data = new float4(tr->range, tr->intensity, 0.0f, 0.0f);
                                }

                                mesh.wBone.data[i] = W;
                                mesh.wBoneIT.data[i] = Wn;
                            }
                        }
                    });

                mesh.wBone.Write();
                mesh.wBoneIT.Write();
            }

            {
                dLightData_Buffer.Write();
            }
        }


        return;
    }

    unsafe void UpdateLightData(RenderGOM_DF.PerCamera perCam)
    {
        float t = Time.time;

        if (count_dLight < 1) { return; }

        //DeferredLight
        {
            {
                MeshInfo mesh = mesh_dLight;

                for (int i = 0; i < count_dLight; i++)
                {
                    trs_dLight[i].tr.pos = unitTrs[i].position;
                    trs_dLight[i].tr.rot = unitTrs[i].rotation;
                }

                //Debug
                {
                    int k = 0;
                    int c0 = GameManager.unitMan.Length;
                    for (int i = 0; i < c0; i++)
                    {
                        int c1 = GameManager.unitMan[i].count;
                        for (int j = 0; j < c1; j++)
                        {
                            {
                                float value = range_dlight[i];
                                trs_dLight[k].range = value;
                            }

                            {
                                float3 center = offset_dlight[i];
                                trs_dLight[k].center = center;
                            }
                            k++;
                        }
                    }
                }


                Parallel.For(0, count_dLight,
                    (int i) =>
                    {                       

                        fixed (LightData* ldata = &(dLightData_Buffer.data[i]))
                        {
                            fixed (TrLight* tr = &(trs_dLight[i]))
                            {                                

                                {                                   
                                    ldata->color = tr->color;
                                    ldata->data = new float4(tr->range, tr->intensity, 0.0f, 0.0f);
                                }                               
                            }
                        }
                    });
             
            }

            {
                dLightData_Buffer.Write();
            }
        }


        return;
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

        if (LightManager.type == LightType.Directional)
        {
            return;
        }

        if (count_dLight < 1) { return; }

        {
            var mesh = mesh_dLight;

            cmd.SetComputeVectorParam(cshader, "countInfo", mesh.ci);
            cmd.SetComputeBufferParam(cshader, ki_worldVertex, "vIn", mesh.vtxIn_cs.value);
            //cmd.SetComputeBufferParam(cshader, ki_worldVertex, "bone", mesh.wBone.value);
            //cmd.SetComputeBufferParam(cshader, ki_worldVertex, "boneIT", mesh.wBoneIT.value);
            GameManager.dfCullMan.Bind_CShader(cmd, cshader, ki_worldVertex);
            cmd.SetComputeBufferParam(cshader, ki_worldVertex, "vOut", mesh.vtxIn.value);

            cmd.DispatchCompute(cshader, ki_worldVertex, mesh.insCount, mesh.vgCount, 1);
        }

        if (bDebug)
        {
            var mesh = mesh_dLight;

            mesh.vtxIn.Read();
        }        

    }
   

    private void Render(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        {
            UpdateCameraData(cam, perCam);
           
            {
                UpdateLightData(perCam);
            }

            UpdateTexture();
        }
              
        {
            csm_action.Update_Data(mpb, math.mul(RenderUtil.GetCfromV(cam, false), perCam.V));
            cbm_action.Update_Data(mpb);
        }

        {
            {
                Render_GBuffer(context, cmd, cam, perCam);
            }

            {                
                Render_LBuffer(cmd, perCam.V);
                Render_DBuffer(context, cmd, cam, perCam);
                Render_Final(cmd);
            }

            {
                Render_SSD(context, cmd, cam, perCam);
            }

            {
                Render_SkyBox(context, cmd, cam, perCam);
                Render_Tp(context, cmd, cam, perCam);
            }
        }

        {
            {
                cmd.SetRenderTarget(RenTexInfo_DF.frame_now.view, RenTexInfo_DF.depth_now.view);
                cmd.ClearRenderTarget(true, true, new Color(0.75f, 0.75f, 0.75f, 1.0f));
            }

            if (useFxaa)
            {
                Render_Fxaa(cmd);
            }
            else
            {
                Render_Noaa(cmd);
            }

            //Render_Debug(cmd);
        }

        //{
        //    cmd.Blit(RenTexInfo_DF.frame_now.view, cam.targetTexture);
        //}
    }



    void Render_GBuffer(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        {
            cmd.SetRenderTarget(RenTexInfo_DF.rti_gbuffer, RenTexInfo_DF.depth_now.view);
            cmd.ClearRenderTarget(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
        }

        {
            cmd.SetRenderTarget(RenTexInfo_DF.frame_gbuffer1.view, RenTexInfo_DF.depth_now.view);
            cmd.ClearRenderTarget(false, true, new Color(-1.0f, 0.0f, 0.0f, 1.0f));
        }

        {
            cmd.SetRenderTarget(RenTexInfo_DF.frame_gbuffer3.view, RenTexInfo_DF.depth_now.view);
            cmd.ClearRenderTarget(false, true, new Color(0.75f, 0.75f, 0.75f, 0.0f));
        }
      
        //{
        //    cmd.SetRenderTarget(RenTexInfo_DF.rti_gbuffer, RenTexInfo_DF.depth_now.view);                                   
        //}

        {
            cmd.SetRenderTarget(RenTexInfo_DF.rti_gbuffer, RenTexInfo_DF.rti_gbuffer_depth);
            
            //cmd.ClearRenderTarget(true, false, Color.white);
            cmd.ClearRenderTarget(true, false, Color.white, 1.0f, 0);  //Stencil Write ref 0
        }

        {
            OnRender_GBuffer(context, cmd, cam, perCam);
        }
    }

    void Render_LBuffer0(CommandBuffer cmd)
    {
        {
            cmd.SetRenderTarget(RenTexInfo_DF.frame_lbuffer.view, RenTexInfo_DF.depth_now.view);
            cmd.ClearRenderTarget(true, true, new Color(0.0f, 0.0f, 0.0f, 1.0f));
        }

        if(LightManager.type == LightType.Directional)
        {
            return;
        }

        {
            GameManager.cullMan.Read_PvfCullData();
        }        


        {
            var mesh = mesh_dLight;
            int insCount = 1;

            {
                mpb.SetVector("countInfo", mesh.ci);
                mpb.SetBuffer("vtxBuffer", mesh.vtxIn.value);
                mpb.SetBuffer("vtxBuffer_St", mesh.vtxIn_st.value);
            }

            for (int i = 0; i < count_dLight; i++)
            {
                if (unitActors[i].isCull) { continue; }

                mpb.SetInteger("light_idx", i);

                cmd.ClearRenderTarget(true, false, new Color(0.0f, 0.0f, 0.0f, 1.0f));
                cmd.DrawProcedural(mesh.idxBuffer, Matrix4x4.identity, mte, pass_LBuffer, MeshTopology.Triangles, mesh.idxCount, insCount, mpb);
            }
        }


    }

    void Render_LBuffer(CommandBuffer cmd, float4x4 camV)
    {
        {
            cmd.SetRenderTarget(RenTexInfo_DF.frame_lbuffer.view, RenTexInfo_DF.depth_now.view);
            cmd.ClearRenderTarget(true, true, new Color(0.0f, 0.0f, 0.0f, 1.0f));
        }

        if (LightManager.type == LightType.Directional)
        {
            return;
        }

        if(count_dLight < 1) { return; }

        {
            GameManager.dfCullMan.Compute_PosV(camV);
        }

        {
            var mesh = mesh_dLight;
            int insCount = 1;

            {
                mpb.SetVector("countInfo", mesh.ci);
                mpb.SetBuffer("vtxBuffer", mesh.vtxIn.value);
                mpb.SetBuffer("vtxBuffer_St", mesh.vtxIn_st.value);
            }
           
            for (int i = 0; i < count_dLight; i++)
            {
                var actor = unitActors[i];

                if (!actor.isCull_light_pvf && actor.isCull_light_svf)
                {
                    mpb.SetInteger("light_idx", i);

                    cmd.ClearRenderTarget(true, false, new Color(0.0f, 0.0f, 0.0f, 1.0f));
                    cmd.DrawProcedural(mesh.idxBuffer, Matrix4x4.identity, mte, pass_LBuffer, MeshTopology.Triangles, mesh.idxCount, insCount, mpb);
                }               
            }
        }


    }

    void Render_DBuffer(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        {
            cmd.SetRenderTarget(RenTexInfo_DF.frame_dbuffer.view, RenTexInfo_DF.depth_now.view);            
            cmd.ClearRenderTarget(true, true, new Color(0.0f, 0.0f, 0.0f, -1.0f));
        }

        //{            
        //    cmd.SetRenderTarget(RenTexInfo_DF.frame_dbuffer.view, RenTexInfo_DF.rti_gbuffer_depth);
        //    cmd.ClearRenderTarget(false, true, new Color(0.0f, 0.0f, 0.0f, -1.0f));
        //}

        {
            OnRender_DBuffer(context, cmd, cam, perCam);
        }
    }

    void Render_Final(CommandBuffer cmd)
    {
        {
            cmd.SetRenderTarget(RenTexInfo_DF.frame_final.view, RenTexInfo_DF.depth_now.view);
            cmd.ClearRenderTarget(true, true, new Color(0.75f, 0.75f, 0.75f, 1.0f));
        }

        {
            float4x4 M = RenderUtil.GetM_Unity();
            mpb.SetMatrix("M", M);
        }

        {
            mpb.SetVector("fxaa_data", fxaa_data);
        }

        {
            var mesh = mesh_quad;

            mpb.SetVector("countInfo", mesh.ci);
            mpb.SetBuffer("vtxBuffer", mesh.vtxIn.value);
            mpb.SetBuffer("vtxBuffer_St", mesh.vtxIn_st.value);

            {
                cmd.DrawProcedural(mesh.idxBuffer, Matrix4x4.identity, mte, pass_Final, MeshTopology.Triangles, mesh.idxCount, mesh.insCount, mpb);
            }
        }
    }

    void Render_SSD(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        {            
            cmd.SetRenderTarget(RenTexInfo_DF.frame_final.view, RenTexInfo_DF.depth_now.view);
            cmd.ClearRenderTarget(true, false, Color.white, 1.0f);
        }

        //{
        //    cmd.SetRenderTarget(RenTexInfo_DF.frame_final.view, RenTexInfo_DF.rti_gbuffer_depth);
        //}

        {
            OnRender_SSD(context, cmd, cam, perCam);
        }
    }

    void Render_SkyBox(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        {
            cmd.SetRenderTarget(RenTexInfo_DF.frame_final.view, RenTexInfo_DF.rti_gbuffer_depth);
        }

        {
            OnRender_SkyBox(context, cmd, cam, perCam);
        }
    }

    void Render_Tp(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        {
            cmd.SetRenderTarget(RenTexInfo_DF.frame_final.view, RenTexInfo_DF.rti_gbuffer_depth);
        }

        {
            OnRender_Transparent(context, cmd, cam, perCam);
        }
    }

    void Render_Fxaa(CommandBuffer cmd)
    {
        //{
        //    cmd.SetRenderTarget(RenTexInfo_DF.frame_fxaa.view, RenTexInfo_DF.depth_now.view);
        //    cmd.ClearRenderTarget(true, true, new Color(0.75f, 0.75f, 0.75f, 1.0f));
        //}

        {
            float4x4 M = RenderUtil.GetM_Unity();
            mpb.SetMatrix("M", M);
        }

        {
            var mesh = mesh_quad;

            mpb.SetVector("countInfo", mesh.ci);
            mpb.SetBuffer("vtxBuffer", mesh.vtxIn.value);
            mpb.SetBuffer("vtxBuffer_St", mesh.vtxIn_st.value);

            {
                cmd.DrawProcedural(mesh.idxBuffer, Matrix4x4.identity, mte, pass_Fxaa, MeshTopology.Triangles, mesh.idxCount, mesh.insCount, mpb);
            }
        }
    }

    void Render_Noaa(CommandBuffer cmd)
    {
        //{
        //    cmd.SetRenderTarget(RenTexInfo_DF.frame_fxaa.view, RenTexInfo_DF.depth_now.view);
        //    cmd.ClearRenderTarget(true, true, new Color(0.75f, 0.75f, 0.75f, 1.0f));
        //}

        {
            mpb.SetTexture("tex", RenTexInfo_DF.frame_final.tex);
        }

        {
            float4x4 M = RenderUtil.GetM_Unity();
            mpb.SetMatrix("M", M);
        }

        {
            var mesh = mesh_quad;

            mpb.SetVector("countInfo", mesh.ci);
            mpb.SetBuffer("vtxBuffer", mesh.vtxIn.value);
            mpb.SetBuffer("vtxBuffer_St", mesh.vtxIn_st.value);

            {
                cmd.DrawProcedural(mesh.idxBuffer, Matrix4x4.identity, mte, pass_Debug, MeshTopology.Triangles, mesh.idxCount, mesh.insCount, mpb);
            }
        }
    }



    void Render_Debug(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM_DF.PerCamera perCam)
    {
        //{
        //    cmd.SetRenderTarget(RenTexInfo_DF.frame_now.view, RenTexInfo_DF.depth_now.view);
        //    cmd.ClearRenderTarget(true, true, new Color(0.75f, 0.75f, 0.75f, 1.0f));
        //}

        if(!bDebug)
        {
            return;
        }

        {
            float4x4 M = RenderUtil.GetM_Unity();
            mpb.SetMatrix("M", M);
        }

        {
            var mesh = mesh_quad;

            mpb.SetVector("countInfo", mesh.ci);
            mpb.SetBuffer("vtxBuffer", mesh.vtxIn.value);
            mpb.SetBuffer("vtxBuffer_St", mesh.vtxIn_st.value);

            for (int i = 0; i < 7; i++)
            {
                cmd.SetViewport(RenTexInfo_DF.vRects[i]);
                mpb.SetTexture("tex", RenTexInfo_DF.frame[i].tex);

                cmd.DrawProcedural(mesh.idxBuffer, Matrix4x4.identity, mte, pass_Debug, MeshTopology.Triangles, mesh.idxCount, mesh.insCount, mpb);
            }

            //for (int i = 0; i < 6; i++)
            //{
            //    cmd.SetViewport(RenTexInfo_DF.vRects[i]);
            //    mpb.SetTexture("tex", RenTexInfo_DF.frame[i].tex);
            //
            //    cmd.DrawProcedural(mesh.idxBuffer, Matrix4x4.identity, mte, pass_Debug, MeshTopology.Triangles, mesh.idxCount, mesh.insCount, mpb);
            //}

            //{                
            //    mpb.SetTexture("tex", RenTexInfo_DF.frame[3].tex);
            //
            //    cmd.DrawProcedural(mesh.idxBuffer, Matrix4x4.identity, mte, pass_Debug, MeshTopology.Triangles, mesh.idxCount, mesh.insCount, mpb);
            //}
        }
    }

    //
   
   



    struct ViewData
    {
        public float4x4 V;
        public float4x4 C;
        public float4x4 CV;
        public float4x4 TC;

        public float4 posW;
        public float4 dirW;
        public float4 data;
    };

    COBuffer<ViewData> perViewBuffer;

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

        public MeshInfo(Mesh mesh, int insCount)
        {
            this.mesh = mesh;
            this.insCount = insCount;

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
    }   
}
