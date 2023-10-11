using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class CBM_Action : MonoBehaviour
{
    LightManager lightMan;
    new Light light;
    Camera mainCam;
    public bool bDepth = true;
    public bool bDebug = true;    

    int cbmCount = 6;

    [HideInInspector]
    public Rect cbmRect;

    public void Init()
    {
        {
            lightMan = GameManager.lightMan;
            light = GetComponent<Light>();
        }

        {
            InitData();
            InitRenderTex();
        }

    }

    public void Enable()
    {
        //RenderGOM.UpdateCBM += UpdateCBM;
        RenderGOM_DF.PreRenderCBM += PreRenderCBM;
        RenderGOM_DF.PostRenderCBM += PostRenderCBM;
    }

    public void Disable()
    {
        //RenderGOM.UpdateCBM -= UpdateCBM;
        RenderGOM_DF.PreRenderCBM -= PreRenderCBM;
        RenderGOM_DF.PostRenderCBM -= PostRenderCBM;
    }

    public void Begin()
    {

    }

    void InitData()
    {
        {
            rot_cbm = new quaternion[cbmCount];

            //{
            //    rot_cbm[0] = quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(0.0f));
            //    rot_cbm[1] = quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(180.0f));
            //    rot_cbm[2] = quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(90.0f));
            //    rot_cbm[3] = quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(270.0f));
            //    rot_cbm[4] = quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.radians(270.0f));
            //    rot_cbm[5] = quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.radians(90.0f));
            //}

            //{
            //    rot_cbm[0] = quaternion.AxisAngle(new float3(+0.0f, +1.0f, +0.0f), math.radians(90.0f));
            //    rot_cbm[1] = quaternion.AxisAngle(new float3(+0.0f, +1.0f, +0.0f), math.radians(270.0f));
            //    rot_cbm[2] = quaternion.AxisAngle(new float3(+1.0f, +0.0f, +0.0f), math.radians(-90.0f));
            //    rot_cbm[3] = quaternion.AxisAngle(new float3(+1.0f, +0.0f, +0.0f), math.radians(+90.0f));
            //    rot_cbm[4] = quaternion.AxisAngle(new float3(+0.0f, +1.0f, +0.0f), math.radians(0.0f));
            //    rot_cbm[5] = quaternion.AxisAngle(new float3(+0.0f, +1.0f, +0.0f), math.radians(180.0f));
            //}

            {
                rot_cbm[0] = quaternion.AxisAngle(new float3(+0.0f, +1.0f, +0.0f), math.radians(90.0f));
                rot_cbm[1] = quaternion.AxisAngle(new float3(+0.0f, +1.0f, +0.0f), math.radians(270.0f));
                rot_cbm[2] = quaternion.AxisAngle(new float3(+1.0f, +0.0f, +0.0f), math.radians(-90.0f));
                rot_cbm[3] = quaternion.AxisAngle(new float3(+1.0f, +0.0f, +0.0f), math.radians(+90.0f));
                rot_cbm[4] = quaternion.AxisAngle(new float3(+0.0f, +1.0f, +0.0f), math.radians(0.0f));
                rot_cbm[5] = quaternion.AxisAngle(new float3(+0.0f, +1.0f, +0.0f), math.radians(180.0f));
            }
        }

        {
            pos = new float3[1];
            rot = new quaternion[cbmCount];
            fi = new float4[1];
        }

        {
            rot_light = rot;

            W = new float4x4[cbmCount];
            V = new float4x4[cbmCount];

            CV = new Matrix4x4[cbmCount];
            //CV_depth = new Matrix4x4[cbmCount];
            //TCV_depth = new Matrix4x4[cbmCount];
        }

        {            
            cbm_data_Buffer = new ROBuffer<CBM_depth_data>(cbmCount);
        }
    }

    public RenderTexture renTex { get; set; }
    RenderTargetIdentifier rti;
    public Texture2D[] tex2D;
    public CubemapArray cubeTex { get; set; }

    public int cbmIdx { get; set; } = 0;

    public int cbmSize = 1024;

    void InitRenderTex()
    {
        {
            cbmRect = new Rect(0.0f, 0.0f, cbmSize, cbmSize);
        }

        {
            RenderTextureDescriptor rtd = new RenderTextureDescriptor();
            {
                rtd.msaaSamples = 1;
                rtd.depthBufferBits = 24;
                rtd.enableRandomWrite = false;

                if (bDepth)
                {
                    rtd.colorFormat = RenderTextureFormat.RFloat;
                }
                else
                {
                    rtd.colorFormat = RenderTextureFormat.ARGB32;
                }
                rtd.dimension = TextureDimension.Tex2DArray;
                rtd.width = cbmSize;
                rtd.height = cbmSize;
                rtd.volumeDepth = cbmCount;
            }
            renTex = new RenderTexture(rtd);
            rti = new RenderTargetIdentifier(renTex, 0, CubemapFace.Unknown, -1);
        }

        {
            tex2D = new Texture2D[cbmCount];
            for (int i = 0; i < cbmCount; i++)
            {
                if (bDepth)
                {
                    tex2D[i] = new Texture2D(cbmSize, cbmSize, TextureFormat.RFloat, false);
                }
                else
                {
                    tex2D[i] = new Texture2D(cbmSize, cbmSize, TextureFormat.ARGB32, false);
                }
            }
        }

        {
            if(bDepth)
            {
                cubeTex = new CubemapArray(cbmSize, 1, TextureFormat.RFloat, false);
            }
            else
            {
                cubeTex = new CubemapArray(cbmSize, 1, TextureFormat.ARGB32, false);
            }

            cubeTex.filterMode = FilterMode.Bilinear;
            //cubeTex.filterMode = FilterMode.Point;

            cubeTex.wrapMode = TextureWrapMode.Mirror;
            //

            //{
            //    Cubemap cmap = new Cubemap(cbmSize, TextureFormat.ARGB32, false);               
            //}            
        }
    }

    public void CopyToCubeMap(CommandBuffer cmd)
    {
        for(int i = 0; i < 6; i++)
        {
            cmd.CopyTexture(rti, i, cubeTex, i);           
        }
    }
   

    public void Bind_Data(MaterialPropertyBlock mpb)
    {
        mpb.SetBuffer("light_Buffer", LightManager.light_Buffer.value);       
        mpb.SetFloat("cbmSize", (float)cbmSize);
        mpb.SetBuffer("cbm_data_Buffer", cbm_data_Buffer.value);

        mpb.SetTexture("cbmTexture", cubeTex);
        //mpb.SetTexture("cbmTexture", renTex);
    }

    public void Update_Data(MaterialPropertyBlock mpb)
    {
        mpb.SetMatrix("Rt", Rt);
        mpb.SetMatrix("L_cbm", L_cbm);
    }
   

    public float4 fis { get; private set; }

    public float4 _fis = new float4(90.0f, 1.0f, 0.1f, 50.0f);    

    quaternion[] rot_cbm { get; set; }
    public quaternion[] rot_light { get; set; }

    public float3[] pos { get; set; }
    public quaternion[] rot { get; set; }
    public float4[] fi { get; private set; }

    public float4x4[] W { get; set; }
    public float4x4[] V { get; set; }
    float4x4 C_toTex;
    float4x4 C_depth;

    public Matrix4x4[] CV { get; set; }
    //public Matrix4x4[] CV_depth { get; set; }
    //public Matrix4x4[] TCV_depth { get; set; }

    float4x4 Rt;
    float4x4 L_cbm;

    public float3 posBox { get; set; }
    public quaternion rotBox { get; set; }
    public float4 fiBox { get; set; }

    struct CBM_depth_data
    {
        public float4x4 CV;
    };

    ROBuffer<CBM_depth_data> cbm_data_Buffer;

    [HideInInspector]
    public float4x4 ovfM;
    
    [HideInInspector]
    public float4x4 ovfW;

    [HideInInspector]
    public float4x4 ovfWn;   

    public void UpdateCBM(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {        
        if(LightManager.type == LightType.Point || LightManager.type == LightType.Spot)
        {

        }


        float3 pos0 = transform.position;
        quaternion rot0 = transform.rotation;

        pos[0] = pos0;
        fi[0] = fis = _fis;

        {
            float3x3 R = new float3x3(rot0);
            R = math.transpose(R);
            Rt.c0 = new float4(R.c0, 0.0f);
            Rt.c1 = new float4(R.c1, 0.0f);
            Rt.c2 = new float4(R.c2, 0.0f);
            Rt.c3 = float4.zero;
        }

        {
            L_cbm = RenderUtil.GetVfromW(pos0, rot0);
        }

        //RenderUtil.GetCfromV_Persp_Optimized(fis, out C_toTex, out C_depth);
        RenderUtil.GetCfromV_Persp_Optimized_Cubemap(fis, out C_toTex, out C_depth);

        Parallel.For(0, cbmCount,
            (int i) =>
            {
                rot_light[i] = math.mul(rot0, rot_cbm[i]);
                float3x3 R = new float3x3(rot_light[i]);

                unsafe
                {
                    fixed(float4x4* M = &W[i])
                    {
                        float4* vec = (float4*)M;                        

                        vec[0] = new float4(R.c0, 0.0f);
                        vec[1] = new float4(R.c1, 0.0f);
                        vec[2] = new float4(R.c2, 0.0f);
                        vec[3] = new float4(pos0, 1.0f);                        
                    }

                    {
                        //V[i] = RenderUtil.GetVfromW(pos0, rot0);
                        V[i] = RenderUtil.GetVfromW(pos0, rot_light[i]);
                        CV[i] = math.mul(C_toTex, V[i]);
                        //CV[i] = math.mul(C_depth, V[i]);

                        //CV_depth[i] = math.mul(C_depth, V[i]);
                        //TCV_depth[i] = math.mul(RenderUtil.GetTfromN(), CV_depth[i]);
                    }
                }              

            });

        for (int i = 0; i < cbmCount; i++)
        {
            cbm_data_Buffer.data[i].CV = CV[i];           
        }
        cbm_data_Buffer.Write();

        {
            UpdateOvfData_Debug();
        }

        {
            posBox = pos0;
            rotBox = rot0;
            float far = _fis.w;
            fiBox = new float4(far, 1.0f, -far, +far);
        }
        
    }

    public void UpdateCBM()
    {
        if (LightManager.type == LightType.Point || LightManager.type == LightType.Spot)
        {

        }


        float3 pos0 = transform.position;
        quaternion rot0 = transform.rotation;

        pos[0] = pos0;
        fi[0] = fis = _fis;

        {
            float3x3 R = new float3x3(rot0);
            R = math.transpose(R);
            Rt.c0 = new float4(R.c0, 0.0f);
            Rt.c1 = new float4(R.c1, 0.0f);
            Rt.c2 = new float4(R.c2, 0.0f);
            Rt.c3 = float4.zero;
        }

        {
            L_cbm = RenderUtil.GetVfromW(pos0, rot0);
        }

        //RenderUtil.GetCfromV_Persp_Optimized(fis, out C_toTex, out C_depth);
        RenderUtil.GetCfromV_Persp_Optimized_Cubemap(fis, out C_toTex, out C_depth);

        Parallel.For(0, cbmCount,
            (int i) =>
            {
                rot_light[i] = math.mul(rot0, rot_cbm[i]);
                float3x3 R = new float3x3(rot_light[i]);

                unsafe
                {
                    fixed (float4x4* M = &W[i])
                    {
                        float4* vec = (float4*)M;

                        vec[0] = new float4(R.c0, 0.0f);
                        vec[1] = new float4(R.c1, 0.0f);
                        vec[2] = new float4(R.c2, 0.0f);
                        vec[3] = new float4(pos0, 1.0f);
                    }

                    {
                        //V[i] = RenderUtil.GetVfromW(pos0, rot0);
                        V[i] = RenderUtil.GetVfromW(pos0, rot_light[i]);
                        CV[i] = math.mul(C_toTex, V[i]);
                        //CV[i] = math.mul(C_depth, V[i]);

                        //CV_depth[i] = math.mul(C_depth, V[i]);
                        //TCV_depth[i] = math.mul(RenderUtil.GetTfromN(), CV_depth[i]);
                    }
                }

            });

        for (int i = 0; i < cbmCount; i++)
        {
            cbm_data_Buffer.data[i].CV = CV[i];
        }
        cbm_data_Buffer.Write();

        {
            UpdateOvfData_Debug();
        }

        {
            posBox = pos0;
            rotBox = rot0;
            float far = _fis.w;
            fiBox = new float4(far, 1.0f, -far, +far);
        }

    }

    void UpdateOvfData_Debug()
    {
        float3 pos = transform.position;
        quaternion rot = transform.rotation;
        //float3 sca = _fis.w * new float3(1.0f, 1.0f, 1.0f);
        float3 sca = _fis.w * new float3(2.0f, 2.0f, 2.0f);

        {
            ovfM.c0 = new float4(pos, 0.0f);
            ovfM.c1 = rot.value;
            ovfM.c2 = new float4(sca, 0.0f);
        }

        {
            float3x3 R = new float3x3(rot);
            ovfW.c0 = new float4(sca.x * R.c0, 0.0f);
            ovfW.c1 = new float4(sca.y * R.c1, 0.0f);
            ovfW.c2 = new float4(sca.z * R.c2, 0.0f);
            ovfW.c3 = new float4(pos, 1.0f);

            float3 rs = 1.0f / sca;
            float3 r0 = rs.x * R.c0;
            float3 r1 = rs.y * R.c1;
            float3 r2 = rs.z * R.c2;
            ovfWn.c0 = new float4(r0, math.dot(r0, -pos));
            ovfWn.c1 = new float4(r1, math.dot(r1, -pos));
            ovfWn.c2 = new float4(r2, math.dot(r2, -pos));
            ovfWn.c3 = new float4(0.0f, 0.0f, 0.0f, 1.0f);
        }
    }


    float4x4 Get_Wcbm(Transform lightTr, quaternion rot1)
    {
        quaternion rot0 = lightTr.rotation;
        float3 pos0 = lightTr.position;

        float3x3 R = new float3x3(math.mul(rot0, rot1));

        float4x4 W = float4x4.identity;
        W.c0 = new float4(R.c0, 0.0f);
        W.c1 = new float4(R.c1, 0.0f);
        W.c2 = new float4(R.c2, 0.0f);
        W.c3 = new float4(pos0, 1.0f);

        return W;
    }

    Color clearColor = new Color(0.75f, 0.75f, 0.75f, 1.0f);

    void PreRenderCBM(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {        
        cmd.SetRenderTarget(rti, 0, CubemapFace.Unknown, -1);

        if (bDepth)
        {
            cmd.ClearRenderTarget(true, true, Color.white, 1.0f);
        }
        else
        {
            cmd.ClearRenderTarget(true, true, clearColor, 1.0f);
        }
    }

    void PostRenderCBM(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {
        {
            CopyToCubeMap(cmd);
        }
        

#if UNITY_EDITOR
        if (bDebug && !bDepth)        
        {            

            if (LightManager.type == LightType.Point || LightManager.type == LightType.Spot)
            {               

                for (int i = 0; i < cbmCount; i++)
                {
                    cmd.CopyTexture(rti, i, tex2D[i], 0);
                }

                for (int i = 0; i < cams.Length; i++)
                {
                    if (cams[i].gameObject.tag == "MainLight")
                    {
                        var cam = cams[i];

                        cmd.SetRenderTarget(cam.targetTexture);
                        cmd.ClearRenderTarget(true, true, RenderGOM.clearColor);

                        if (cbmIdx < 0)
                        {
                            return;
                        }
                       
                        cam.transform.position = transform.position;
                        //cam.transform.rotation = rot[cbmIdx];

                        cam.orthographic = false;

                        cam.fieldOfView = _fis.x;
                        cam.aspect = _fis.y;
                        cam.nearClipPlane = _fis.z;
                        cam.farClipPlane = _fis.w;
                                              
                        cmd.Blit(tex2D[cbmIdx], cam.targetTexture);
                    }
                }
            }            
        }
#endif

    }


    private void OnDestroy()
    {
        if (enabled)
        {            
            BufferBase<CBM_depth_data>.Release(cbm_data_Buffer);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.bUpdate)
        {
            return;
        }


    }
}
