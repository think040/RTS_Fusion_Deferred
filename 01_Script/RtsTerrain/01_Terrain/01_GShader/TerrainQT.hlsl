#include "../../../Utility/01_GShader/LightUtil.hlsl"

cbuffer perObject
{
    float4x4 W;
    float4x4 W_IT;
    
    float4 color;
    float maxTessFactor;
    
    int mode; //0 Pvf-tileBox //1 Ovf-tileBox
    int vfIdx; //mode == 0 -> vfIdx = 0    //mode == 1 -> vfIdx = 0, 1, 2, 3    
    //int pvfType; // 0 : CAM  1 : CBM
    int pvfMode; //0 => TShader CAM     //1 => TShader CBM
}
		
cbuffer perView
{
    float4x4 S;
    float4x4 V;
    float4x4 CV;
    float4x4 CV_csm[4];
    float4x4 CV_csm_depth[4];
    
    //float4 dirW_cam;
    float4 dirW_mainCam;
    float4 posW_mainCam;
}

cbuffer perLight
{
    float3 posW_light;
    float3 dirW_light;
};

Texture3D<float4> LodData_Tex;
Texture3D<float> TestCullPVF_Tex;
Texture3D<float> TestCullOVF_Tex;

Texture2D hMap;
SamplerState sampler_hMap;

float3 tileSize;
float3 terrainSize;
float3 tileCount;

int isCSM_Color;

//float2 layerSize[4];

Texture2D bakedTex;
SamplerState sampler_bakedTex;

Texture2D bakedNormalTex;
SamplerState sampler_bakedNormalTex;

Texture2D diffuseTex0;
Texture2D diffuseTex1;
Texture2D diffuseTex2;
Texture2D diffuseTex3;
SamplerState sampler_diffuseTex0;
SamplerState sampler_diffuseTex1;
SamplerState sampler_diffuseTex2;
SamplerState sampler_diffuseTex3;

Texture2D normalMapTex0;
Texture2D normalMapTex1;
Texture2D normalMapTex2;
Texture2D normalMapTex3;
SamplerState sampler_normalMapTex0;
SamplerState sampler_normalMapTex1;
SamplerState sampler_normalMapTex2;
SamplerState sampler_normalMapTex3;
//Texture2D<float4> normalMapTex0;
//Texture2D<float4> normalMapTex1;
//Texture2D<float4> normalMapTex2;
//Texture2D<float4> normalMapTex3;


Texture2D alphamap0;
SamplerState sampler_alphamap0;

Texture2D holeTex;
SamplerState sampler_holeTex;

Texture2D waterTex;
SamplerState sampler_waterTex;

StructuredBuffer<float2> layerSizeBuffer;

uint2 GetTileId(uint pid, uint cx);
float3 ToSNom(float3 input);
float reMapTessFactorByDist(float dist);
float reMapTessFactorByDev(float dev);
float reMapTessFactor(float f0, float f1);

struct IA_Out
{
    float3 posL : POSITION;
    uint iid : SV_InstanceID;
};
            
struct VS_Out
{
    float4 posL : SV_POSITION;
};
            
struct TS_Out
{
    float eFactor[4] : SV_TessFactor;
    float iFactor[2] : SV_InsideTessFactor;
};
            
struct HS_Out
{
    float4 posL : SV_POSITION;
};
            
struct DS_Out
{
    float4 posL : SV_POSITION;
    float3 normalL : NORMAL;
    float3 tangentL : TANGENT;
    float2 uv : UV;
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    float2 uv2 : TEXCOORD2;
    float2 uv3 : TEXCOORD3;
    uint2 tileId : TileID;
};
                
struct GS_Out
{
    float4 posC : SV_POSITION;
    float4 posC_depth : PosC_Depth;
    float3 normalW : NORMAL;
    float3 tangentW : TANGENT;
    float3 posW : TEXCOORD8;
    
    float2 uv : UV;
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    float2 uv2 : TEXCOORD2;
    float2 uv3 : TEXCOORD3;
        
    uint rid : SV_RenderTargetArrayIndex;
};
            
struct GS_Out_Wire
{
    float4 posC : SV_POSITION;
    float3 normalW : NORMAL;
    float3 posW : TEXCOORD8;
    
    float4 posCi : TEXCOORD4;
    float4 posC0 : TEXCOORD5;
    float4 posC1 : TEXCOORD6;
    float4 posC2 : TEXCOORD7;
        
    uint rid : SV_RenderTargetArrayIndex;
};

struct RS_Out
{
    float4 posS : SV_POSITION;
    float4 posC_depth : PosC_Depth;
    float3 normalW : NORMAL;
    float3 tangentW : TANGENT;
    float3 posW : TEXCOORD8;
    
    float2 uv : UV;
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    float2 uv2 : TEXCOORD2;
    float2 uv3 : TEXCOORD3;
        
    uint rid : SV_RenderTargetArrayIndex;
};

struct RS_Out_Wire
{
    float4 posS : SV_POSITION;
    float3 normalW : NORMAL;
    float3 posW : TEXCOORD8;
       
    float4 posCi : TEXCOORD4;
    float4 posC0 : TEXCOORD5;
    float4 posC1 : TEXCOORD6;
    float4 posC2 : TEXCOORD7;
        
    uint rid : SV_RenderTargetArrayIndex;
};

            
struct PS_Out
{
    float4 color : SV_Target;
};

struct PS_Out_GBuffer
{
    float4 color0 : SV_Target0;
    float4 color1 : SV_Target1;
    float4 color2 : SV_Target2;
    float4 color3 : SV_Target3;
};
            
struct PS_Out_Depth
{
    float depth : SV_Target;
};


VS_Out VShader(
		IA_Out vIn)
{
    VS_Out vOut;
	                
    vOut.posL = float4(vIn.posL, 1.0f);
	            
    return vOut;
}
	            


float pvfCount;
float ovfCount;

int fqtIdx;
int qtCount;


TS_Out TShader(
	                InputPatch<VS_Out, 4> ip,
	                uint pid : SV_PrimitiveID)
{
    TS_Out tOut;
    
    uint cx = (uint) tileCount.x;
    uint cz = (uint) tileCount.z;
    
    uint uj = pid % (uint) cx;
    uint vi = pid / (uint) cx;
    
    uint un = uj - 1;
    uint uc = uj;
    uint up = uj + 1;
    
    if (un < 0)
    {
        un = uc;
    }
    else if (cx <= up)
    {
        up = uc;
    }
    
    uint vn = vi - 1;
    uint vc = vi;
    uint vp = vi + 1;
    
    if (vn < 0)
    {
        vn = vc;
    }
    else if (cz <= vp)
    {
        vp = vc;
    }
    
    int i = 0;
    
    bool isCull = false;
    if (mode == 0)  //Pvf - TileBox
    {
        if(pvfMode == 0)  //CAM
        {
            if (TestCullPVF_Tex[int3(uc, fqtIdx * pvfCount + vfIdx, vc)] < 1.0f)            
            {
                isCull = true;
            }
        }
        else if(pvfMode == 1)  //CBM
        {
            //{
            //    isCull = true;
            //    for (i = 0; i < 6; i++)
            //    {
            //        if (TestCullPVF_Tex[int3(uc, fqtIdx * pvfCount + (i + 1), vc)] == 1.0f)
            //        {
            //            isCull = false;
            //            break;
            //        }
            //    } 
            //}
                        
            {
                if (TestCullOVF_Tex[int3(uc, fqtIdx * ovfCount + 4, vc)] < 1.0f)
                {
                    isCull = true;
                }
            }
        }                    
    }
    else //Ovf - TileBox
    {
        if (TestCullOVF_Tex[int3(uc, fqtIdx * ovfCount + vfIdx, vc)] < 1.0f)
        {
            isCull = true;
        }
    }
    
    //Debug
    //isCull = false;
    
    if (isCull)
    {
        tOut.eFactor[0] = 0.0f;
        tOut.eFactor[1] = 0.0f;
        tOut.eFactor[2] = 0.0f;
        tOut.eFactor[3] = 0.0f;
            
        tOut.iFactor[0] = 0.0f;
        tOut.iFactor[1] = 0.0f;
    }
    else
    {
        float fcc = reMapTessFactorByDev(LodData_Tex.Load(int4(uc, 0, vc, 0)).w);
        float fun = reMapTessFactorByDev(LodData_Tex.Load(int4(un, 0, vc, 0)).w);
        float fup = reMapTessFactorByDev(LodData_Tex.Load(int4(up, 0, vc, 0)).w);
        float fvn = reMapTessFactorByDev(LodData_Tex.Load(int4(uc, 0, vn, 0)).w);
        float fvp = reMapTessFactorByDev(LodData_Tex.Load(int4(uc, 0, vp, 0)).w);
        
        //
        float4 xn = float4(0.5f * (ip[0].posL.xyz + ip[1].posL.xyz), 1.0f);
        float4 xp = float4(0.5f * (ip[2].posL.xyz + ip[3].posL.xyz), 1.0f);
        float4 zn = float4(0.5f * (ip[3].posL.xyz + ip[0].posL.xyz), 1.0f);
        float4 zp = float4(0.5f * (ip[1].posL.xyz + ip[2].posL.xyz), 1.0f);
    
        xn = mul(W, xn);
        xp = mul(W, xp);
        zn = mul(W, zn);
        zp = mul(W, zp);
    
        float lxn = distance(xn.xyz, posW_mainCam.xyz);
        float lxp = distance(xp.xyz, posW_mainCam.xyz);
        float lzn = distance(zn.xyz, posW_mainCam.xyz);
        float lzp = distance(zp.xyz, posW_mainCam.xyz);
    
        float fxn = reMapTessFactor(0.5f * (fcc + fun), reMapTessFactorByDist(lxn));
        float fzp = reMapTessFactor(0.5f * (fcc + fvp), reMapTessFactorByDist(lzp));
        float fxp = reMapTessFactor(0.5f * (fcc + fup), reMapTessFactorByDist(lxp));
        float fzn = reMapTessFactor(0.5f * (fcc + fvn), reMapTessFactorByDist(lzn));
        
        ////Debug
        //fxn = 0.5f * (fcc + fun);
        //fzp = 0.5f * (fcc + fvp);
        //fxp = 0.5f * (fcc + fup);
        //fzn = 0.5f * (fcc + fvn);
    
        //    
        float fux = 0.5f * (fxn + fxp);
        float fvz = 0.5f * (fzn + fzp);
           
        tOut.eFactor[0] = fxn;
        tOut.eFactor[1] = fzp;
        tOut.eFactor[2] = fxp;
        tOut.eFactor[3] = fzn;
            
        tOut.iFactor[0] = fux;
        tOut.iFactor[1] = fvz;
            
        //tOut.eFactor[0] = clamp(fxn, 1.0f, 16.0f);
        //tOut.eFactor[1] = clamp(fzp, 1.0f, 16.0f);
        //tOut.eFactor[2] = clamp(fxp, 1.0f, 16.0f);
        //tOut.eFactor[3] = clamp(fzn, 1.0f, 16.0f);
        //    
        //tOut.iFactor[0] = clamp(fux, 1.0f, 16.0f);
        //tOut.iFactor[1] = clamp(fvz, 1.0f, 16.0f);
   
        ////   
        
        //{
        //    float3 patchNormal = LodData_Tex.Load(int4(uj, 0, vi, 0)).xyz;
        //    bool bBackPatch = false;
        //    //if (dot(patchNormal, dirW_mainCam.xyz) < 0.0f)
        //    if (dot(patchNormal, dirW_cam.xyz) < -0.25f)
        //    {
        //        bBackPatch = true;
        //    }
        //
        //    //Debug
        //    //bBackPatch = false;
        //
        //    if (bBackPatch)
        //    {
        //        tOut.eFactor[0] = 0.0f;
        //        tOut.eFactor[1] = 0.0f;
        //        tOut.eFactor[2] = 0.0f;
        //        tOut.eFactor[3] = 0.0f;
	    //        
        //        tOut.iFactor[0] = 0.0f;
        //        tOut.iFactor[1] = 0.0f;
        //    }
        //}
       
    
    }
    
    //{
    //    tOut.eFactor[0] = 1.0f;
    //    tOut.eFactor[1] = 1.0f;
    //    tOut.eFactor[2] = 1.0f;
    //    tOut.eFactor[3] = 1.0f;
    //        
    //    tOut.iFactor[0] = 1.0f;
    //    tOut.iFactor[1] = 1.0f;
    //}
	                
    return tOut;
}

[domain("quad")]
[partitioning("integer")]
	//[partitioning("fractional_even")]
	//[partitioning("fractional_odd")]	
	//[partitioning("pow2")]
[outputtopology("triangle_cw")]
[outputcontrolpoints(4)]
[patchconstantfunc("TShader")]
[maxtessfactor(64.0f)]
HS_Out HShader(
	                const InputPatch<VS_Out, 4> ip,
	                uint cid : SV_OutputControlPointID,
	                uint pid : SV_PrimitiveID)
{
    HS_Out hOut;
	            
    hOut.posL = ip[cid].posL;
	           
    return hOut;
}

[domain("quad")]
DS_Out DShader(
	                TS_Out tfactor,
	                float2 uv : SV_DomainLocation,
	                const OutputPatch<HS_Out, 4> op,
                    uint pid : SV_PrimitiveID)
{
    DS_Out dOut;
    
    float cx = tileCount.x;
    float cz = tileCount.z;
    
    //float tw = tileSize.x;
    //float th = tileSize.z;
        
    //float wu = terrainSize.x;
    //float hv = terrainSize.z;
    
    dOut.tileId = GetTileId(pid, (uint) cx);
    
    float u = uv.x;
    float v = uv.y;
       
    float h0 = terrainSize.y;
    float3 p0 = lerp(op[1].posL.xyz, op[0].posL.xyz, v);
    float3 p1 = lerp(op[2].posL.xyz, op[3].posL.xyz, v);
    float3 posL = lerp(p0, p1, u);
    v = 1.0f - v;
    
    uint uj = pid % (uint) cx;
    uint vi = pid / (uint) cx;
    
    //u = ((float) uj * (float) tw + (u * (float) tw)) / wu;
    //v = ((float) vi * (float) th + (v * (float) th)) / hv;
    
    float u1;
    float v1;
    u1 = ((float) uj + u) * (tileSize.x / terrainSize.x);
    v1 = ((float) vi + v) * (tileSize.z / terrainSize.z);
    
    //Test
    //u = ((float) uj + u);
    //v = ((float) vi + v);
        
  
    
    //float k = 1.0f;
    //float du = u / (k * wu);
    //float dv = v / (k * hv);
        
    float k = 1.0f; //0.375f;  
    k = 0.075f;
    float du = k / terrainSize.x;
    float dv = k / terrainSize.z;
        
    
    int i = 0;
    int j = 0;
    
    const int num = 1;
    const int count = 2 * num + 1;
    float h[count][count];
    
    //h[0][0] = h0 * hMap.SampleLevel(sampler_hMap, float2(u - du, v - dv), 0);
    //h[0][1] = h0 * hMap.SampleLevel(sampler_hMap, float2(u + 0 , v - dv), 0);
    //h[0][2] = h0 * hMap.SampleLevel(sampler_hMap, float2(u + du, v - dv), 0);
    //
    //h[1][0] = h0 * hMap.SampleLevel(sampler_hMap, float2(u - du, v + 0 ), 0);
    //h[1][1] = h0 * hMap.SampleLevel(sampler_hMap, float2(u + 0 , v + 0 ), 0);
    //h[1][2] = h0 * hMap.SampleLevel(sampler_hMap, float2(u + du, v + 0 ), 0);
    //
    //h[2][0] = h0 * hMap.SampleLevel(sampler_hMap, float2(u - du, v + dv), 0);
    //h[2][1] = h0 * hMap.SampleLevel(sampler_hMap, float2(u + 0 , v + dv), 0);
    //h[2][2] = h0 * hMap.SampleLevel(sampler_hMap, float2(u + du, v + dv), 0);
    
    
    [loop]
    for (i = -num; i <= num; i++)
    {
        for (j = -num; j <= num; j++)
        {
            h[i + num][j + num] = h0 * hMap.SampleLevel(sampler_hMap, float2(u1 + (float) j * du, v1 + (float) i * dv), 0);
        }
    }
    
    //float Gu = (h[0][0] - h[0][2]) + 2.0f * (h[1][0] - h[1][2]) + (h[2][0] - h[2][2]);
    //float Gv = (h[0][0] - h[2][0]) + 2.0f * (h[0][1] - h[2][1]) + (h[0][2] - h[2][2]);
    
    float Gu = 0.0f;
    float Gv = 0.0f;
    
    for (i = -num; i <= num; i++)
    {
        for (j = -num; j <= num; j++)
        {
            float ha = 0.5f * (count - (abs(i) + abs(j)));
            Gu += ha * (-sign(j)) * (h[+i + num][+j + num] - h[+i + num][-j + num]);
            Gv += ha * (-sign(i)) * (h[+i + num][+j + num] - h[-i + num][+j + num]);
        }
    }
       
              
    float Gw = sqrt(max(0.0f, 1.0f - Gu * Gu - Gv * Gv));
            
    //posL += float3(0.0f, h[num][num], 0.0f);
    
    posL += float3(0.0f, h[0][0], 0.0f);
    dOut.posL = float4(posL, 1.0f);
    
    float3 normal = normalize(float3(2.0f * Gu, Gw, 2.0f * Gv));
    //float3 normal = normalize(float3(1.0f * Gu, Gw, 1.0f * Gv));
    //float3 normal = normalize(float3(10.0f * Gu, Gw, 10.0f * Gv));
    //float3 normal = normalize(float3(2.0f * Gu, 10.0f * Gw, 2.0f * Gv));
    
    //float3 t = normalize(float3(2.0f, 2.0f * Gu, 0.0f));
    //float3 b = normalize(float3(2.0f, 0.0f, 2.0f * Gv));
    //normal = normalize(cross(b, t));
    
    float3 tangent = normalize(float3(1.0f, 0.0f, 0.0f) - normal * dot(normal, float3(1.0f, 0.0f, 0.0f)));
    //tangent = float3(1.0f, 0.0f, 0.0f);
        
    dOut.normalL = normal;
    dOut.tangentL = tangent;
                          
    dOut.uv = float2(u1, v1);
    
    dOut.uv0 = terrainSize.xz * float2(u1, v1) / layerSizeBuffer[0].xy;
    dOut.uv1 = terrainSize.xz * float2(u1, v1) / layerSizeBuffer[1].xy;
    dOut.uv2 = terrainSize.xz * float2(u1, v1) / layerSizeBuffer[2].xy;
    dOut.uv3 = terrainSize.xz * float2(u1, v1) / layerSizeBuffer[3].xy;
   
        
    //Test   
    //dOut.uv0 = float2(u, v);
    //dOut.uv1 = float2(u, v);
    //dOut.uv2 = float2(u, v);
    //dOut.uv3 = float2(u, v);
    
    //dOut.uv0 = 16.0f * float2(u1, v1);
    //dOut.uv0 = (terrainSize.xz / (4.0f * tileSize.xz)) * float2(u1, v1);
    
    return dOut;
}





[maxvertexcount(12)]
void GShader_CSM(triangle DS_Out gin[3], inout TriangleStream<GS_Out> gOut)
{
    for (int i = 0; i < 4; i++)
    {
        int2 tId = (int2) gin[0].tileId;
        uint rid = i;
        if (TestCullOVF_Tex[int3(tId.x, fqtIdx * ovfCount + i, tId.y)] < 1.0f)
        {
            GS_Out vertice;
            vertice.posC = float4(0.0f, 0.0f, 0.0f, 0.0f);
            vertice.posC_depth = float4(0.0f, 0.0f, 0.0f, 0.0f);
            vertice.normalW = float3(0.0f, 0.0f, 0.0f);
            vertice.tangentW = float3(0.0f, 0.0f, 0.0f);
            vertice.posW = float3(0.0f, 0.0f, 0.0f);
            vertice.uv = float2(0.0f, 0.0f);
            vertice.uv0 = float2(0.0f, 0.0f);
            vertice.uv1 = float2(0.0f, 0.0f);
            vertice.uv2 = float2(0.0f, 0.0f);
            vertice.uv3 = float2(0.0f, 0.0f);
                               
            vertice.rid = rid;
        }
        else
        {
            for (int j = 0; j < 3; j++)
            {
                GS_Out vertice;
                
                float4x4 CV = csm_data_Buffer[rid].CV;
                float4x4 CV_depth = csm_data_Buffer[rid].CV_depth;
                
                float4 posW = mul(W, gin[j].posL);
                vertice.posC = mul(CV, posW);
                vertice.posC_depth = mul(CV_depth, posW);
                vertice.normalW = mul((float3x3) W_IT, gin[j].normalL);
                vertice.tangentW = mul((float3x3) W_IT, gin[j].tangentL);
                vertice.posW = posW;
                vertice.uv = gin[j].uv;
                vertice.uv0 = gin[j].uv0;
                vertice.uv1 = gin[j].uv1;
                vertice.uv2 = gin[j].uv2;
                vertice.uv3 = gin[j].uv3;
                                   
                vertice.rid = rid;
                gOut.Append(vertice);
            }
            gOut.RestartStrip();
        }
    }
            
}

[maxvertexcount(12)]
void GShader_CSM_Wire(triangle DS_Out gin[3], inout TriangleStream<GS_Out_Wire> gOut)
{
    int i = 0;
    int j = 0;
    int k = 0;
    
    for (i = 0; i < 4; i++)
    {
        GS_Out_Wire v[3];
        float4 ps[3];
        
        int2 tId = (int2) gin[0].tileId;
        if (TestCullOVF_Tex[int3(tId.x, fqtIdx * ovfCount + i, tId.y)] < 1.0f)
        {
                 
        }
        else
        {
            for (j = 0; j < 3; j++)
            {
                float4x4 CV = csm_data_Buffer[i].CV;                
                
                float4 posW = mul(W, gin[j].posL);
                //ps[j] = mul(CV_csm[i], posW);
                ps[j] = mul(CV, posW);
                v[j].posC = ps[j];
                v[j].posCi = ps[j];
                v[j].normalW = mul((float3x3) W_IT, gin[j].normalL);
                v[j].posW = posW;
                               
                v[j].rid = i;
            }
          
            for (j = 0; j < 3; j++)
            {
                v[j].posC0 = ps[0];
                v[j].posC1 = ps[1];
                v[j].posC2 = ps[2];
            }
                             
            for (j = 0; j < 3; j++)
            {
                gOut.Append(v[j]);
            }
            gOut.RestartStrip();
        }
    }
}

[maxvertexcount(18)]
void GShader_CBM(triangle DS_Out gin[3], inout TriangleStream<GS_Out> gOut)
{
    for (int i = 0; i < 6; i++)
    {
        int2 tId = (int2) gin[0].tileId;
        uint rid = i;
        if (TestCullPVF_Tex[int3(tId.x, fqtIdx * pvfCount + (i + 1), tId.y)] < 1.0f)
        {
            GS_Out vertice;
            vertice.posC = float4(0.0f, 0.0f, 0.0f, 0.0f);
            vertice.posC_depth = float4(0.0f, 0.0f, 0.0f, 0.0f);
            vertice.normalW = float3(0.0f, 0.0f, 0.0f);
            vertice.tangentW = float3(0.0f, 0.0f, 0.0f);
            vertice.posW = float3(0.0f, 0.0f, 0.0f);
            vertice.uv = float2(0.0f, 0.0f);
            vertice.uv0 = float2(0.0f, 0.0f);
            vertice.uv1 = float2(0.0f, 0.0f);
            vertice.uv2 = float2(0.0f, 0.0f);
            vertice.uv3 = float2(0.0f, 0.0f);
                               
            vertice.rid = rid;
        }
        else
        {
            for (int j = 0; j < 3; j++)
            {
                GS_Out vertice;
                
                float4x4 CV = cbm_data_Buffer[rid].CV;
               
                float4 posW = mul(W, gin[j].posL);
                vertice.posC = mul(CV, posW);
                vertice.posC_depth = vertice.posC;
                vertice.normalW = mul((float3x3) W_IT, gin[j].normalL);
                vertice.tangentW = mul((float3x3) W_IT, gin[j].tangentL);
                vertice.posW = posW;
                vertice.uv = gin[j].uv;
                vertice.uv0 = gin[j].uv0;
                vertice.uv1 = gin[j].uv1;
                vertice.uv2 = gin[j].uv2;
                vertice.uv3 = gin[j].uv3;
                                   
                vertice.rid = rid;
                gOut.Append(vertice);
            }
            gOut.RestartStrip();
        }
    }
            
}


[maxvertexcount(18)]
void GShader_CBM_Wire(triangle DS_Out gin[3], inout TriangleStream<GS_Out_Wire> gOut)
{
    int i = 0;
    int j = 0;
    int k = 0;
    
    for (i = 0; i < 6; i++)
    {
        GS_Out_Wire v[3];
        float4 ps[3];
        
        int2 tId = (int2) gin[0].tileId;
        if (TestCullPVF_Tex[int3(tId.x, fqtIdx * pvfCount + (i + 1), tId.y)] < 1.0f)
        {
                 
        }
        else
        {
            for (j = 0; j < 3; j++)
            {
                float4x4 CV = cbm_data_Buffer[i].CV;
                
                float4 posW = mul(W, gin[j].posL);                
                ps[j] = mul(CV, posW);
                v[j].posC = ps[j];
                v[j].posCi = ps[j];
                v[j].normalW = mul((float3x3) W_IT, gin[j].normalL);
                v[j].posW = posW;
                               
                v[j].rid = i;
            }
          
            for (j = 0; j < 3; j++)
            {
                v[j].posC0 = ps[0];
                v[j].posC1 = ps[1];
                v[j].posC2 = ps[2];
            }
                             
            for (j = 0; j < 3; j++)
            {
                gOut.Append(v[j]);
            }
            gOut.RestartStrip();
        }
    }
}




[maxvertexcount(3)]
void GShader_CAM(triangle DS_Out gin[3], inout TriangleStream<GS_Out> gOut)
{
    for (int i = 0; i < 3; i++)
    {
        GS_Out vertice;
        uint rid = 0;
        
        float4 posW = mul(W, gin[i].posL);
        vertice.posC = mul(CV, posW);
        vertice.posC_depth = vertice.posC;
        vertice.normalW = mul((float3x3) W_IT, gin[i].normalL);
        vertice.tangentW = mul((float3x3) W_IT, gin[i].tangentL);       
        vertice.posW = posW;
        vertice.uv = gin[i].uv;
        vertice.uv0 = gin[i].uv0;
        vertice.uv1 = gin[i].uv1;
        vertice.uv2 = gin[i].uv2;
        vertice.uv3 = gin[i].uv3;
                                
        vertice.rid = rid;
        gOut.Append(vertice);
    }
    gOut.RestartStrip();
}

[maxvertexcount(3)]
void GShader_CAM_Wire(triangle DS_Out gin[3], inout TriangleStream<GS_Out_Wire> gOut)
{
    GS_Out_Wire v[3];
    float4 ps[3];
    int i;
    int j;
        
    for (i = 0; i < 3; i++)
    {
        uint rid = 0;
        
        float4 posW = mul(W, gin[i].posL);
        ps[i] = mul(CV, posW);
        v[i].posC = ps[i];
        v[i].posCi = ps[i];
        v[i].normalW = mul((float3x3) W_IT, gin[i].normalL);
        v[i].posW = posW;
               
        v[i].rid = rid;
    }
    
    for (i = 0; i < 3; i++)
    {
        v[i].posC0 = ps[0];
        v[i].posC1 = ps[1];
        v[i].posC2 = ps[2];
    }
            
    for (i = 0; i < 3; i++)
    {
        gOut.Append(v[i]);
    }
    gOut.RestartStrip();
}

	

PS_Out PShader(RS_Out pIn)
{
    PS_Out pOut;
    
    float3 c;
    c = color.xyz;
    
    c = bakedTex.Sample(sampler_bakedTex, pIn.uv0);
    
    float3 N = normalize(pIn.normalW);
    float3 L = normalize(dirW_light);
    
    //L = float3(0.0f, 1.0f, 0.0f);
   
    //L = normalize(float3(1.0f, 1.0f, -1.0f));
    float NdotL = max(0.25f, dot(N, L));
    
    c = 0.5f * c + 0.5f * NdotL * c;
    
    pOut.color = float4(c, 1.0f);
            
    return pOut;
}

PS_Out PShader_Light_Debug(RS_Out pIn)
{
    PS_Out pOut;
    
    //float NdotL = CSMUtil::getNdotL(normalize(pIn.normalW), dirW_light.xyz);
    
    LightData data = light_Buffer[0];
    float3 _dirW_light = data.dirW;
    
    float NdotL = LightUtil::GetNdotL(pIn.normalW, _dirW_light);
    float3 color = float3(0.0f, 1.0f, 0.0f);
    
    {
        pOut.color = float4(NdotL * color, 1.0f);
    }
                      
    return pOut;
}

PS_Out_Depth PShader_CSM(RS_Out pIn)
{
    PS_Out_Depth pOut;
    
    float alpha = holeTex.Sample(sampler_holeTex, pIn.uv);
    
    if (alpha <= 0.0f)
    {
        pOut.depth = 1.0f;
    }
    else
    {
        float4 posC = pIn.posC_depth;
        pOut.depth = posC.z / posC.w;
    }
                      
    return pOut;
}

PS_Out_Depth PShader_CBM(RS_Out pIn)
{
    PS_Out_Depth pOut;
    
    float alpha = holeTex.Sample(sampler_holeTex, pIn.uv);
    
    if (alpha <= 0.0f)
    {
        pOut.depth = 1.0f;
        //pOut.depth = 1.5f;
    }
    else
    {
        LightData data = light_Buffer[0];
        
        //float3 vec = pIn.posW - posW_light;
        float3 vec = LightUtil::GetVec_CBM(pIn.posW);
        pOut.depth = sqrt(dot(vec, vec)) / data.far_plane;
    }
                      
    return pOut;
}

PS_Out PShader_CAM(RS_Out pIn)
{
    PS_Out pOut;
    
    float3 c;
    c = color.xyz;
    
    float3 normal;
    
    float holeAlpha = holeTex.Sample(sampler_holeTex, pIn.uv);
    
    if (holeAlpha <= 0.0f)
    {
        c = waterTex.Sample(sampler_waterTex, pIn.uv0).xyz;
        //c = float3(0.0f, 0.0f, 0.75f);
        normal = normalize(pIn.normalW);
    }
    else
    {
        float alpha[4];
        {
            float4 mask = alphamap0.Sample(sampler_alphamap0, pIn.uv);
            alpha[0] = mask.r;
            alpha[1] = mask.g;
            alpha[2] = mask.b;
            alpha[3] = mask.a;
        }
   
    //#define LAYERED_NORMAL_MAP
    //#define BAKED_NORMAL_MAP
    //#define NONE_NORMAL_MAP
#if defined(LAYERED_NORMAL_MAP)    
        {                
            float3 nom[4];        
            float3 outNom;        
                            
            nom[0] = normalMapTex0.Sample(sampler_normalMapTex0, pIn.uv0).xyz;
            nom[1] = normalMapTex1.Sample(sampler_normalMapTex1, pIn.uv1).xyz;
            nom[2] = normalMapTex2.Sample(sampler_normalMapTex2, pIn.uv2).xyz;
            nom[3] = normalMapTex3.Sample(sampler_normalMapTex3, pIn.uv3).xyz;
                          
            nom[0] = ToSNom(nom[0]);
            nom[1] = ToSNom(nom[1]);
            nom[2] = ToSNom(nom[2]);
            nom[3] = ToSNom(nom[3]);
                    
            float as = alpha[0] + alpha[1] + alpha[2] + alpha[3];
            outNom = normalize((alpha[0] * nom[0] + alpha[1] * nom[1] + alpha[2] * nom[2] + alpha[3] * nom[3]) / as);        
                        
            float3x3 TBN = LightUtil::get_TBN_matrix(normalize(pIn.normalW), normalize(pIn.tangentW));
            normal = mul(TBN, outNom);    
            
            //normal = normalize(2.0f * normalize(pIn.normalW) + mul(TBN, outNom));
            //normal = float3(0.0f, 1.0f, 0.0f);
            //normal = nom[0];
        }
#elif defined(BAKED_NORMAL_MAP)       
        {
            float3 nom;
            nom = bakedNormalTex.Sample(sampler_bakedNormalTex, pIn.uv);
            nom = ToSNom(nom);
                    
            float3x3 TBN = LightUtil::get_TBN_matrix(normalize(pIn.normalW), normalize(pIn.tangentW));
            normal = mul(TBN, nom);
        }    
#elif defined(NONE_NORMAL_MAP)
        {
            normal = normalize(pIn.normalW);
        }    
#endif
                        
                    
    //#define LAYERED_DIFFUSE_TEX 
    //#define BAKED_DIFFUSE_TEX        
#if defined(LAYERED_DIFFUSE_TEX)  
        {
            float3 diffuse[4];       
            float3 outDiffuse;                     
        
            diffuse[0] = diffuseTex0.Sample(sampler_diffuseTex0, pIn.uv0);
            diffuse[1] = diffuseTex1.Sample(sampler_diffuseTex1, pIn.uv1);
            diffuse[2] = diffuseTex2.Sample(sampler_diffuseTex2, pIn.uv2);
            diffuse[3] = diffuseTex3.Sample(sampler_diffuseTex3, pIn.uv3);
            
            outDiffuse = alpha[0] * diffuse[0] + alpha[1] * diffuse[1] + alpha[2] * diffuse[2] + alpha[3] * diffuse[3];
            c = outDiffuse;            
        }    
#elif defined(BAKED_DIFFUSE_TEX)
        {
            c = bakedTex.Sample(sampler_bakedTex, pIn.uv);
        }
#endif                                                 
    }
    
    
    {        
        float3 pos = pIn.posW;
        
        float NdotL;        
        float sf;
        
        LightData data = light_Buffer[0];
        int type = data.type;    
        
        {
            sf = LightUtil::GetShadowFactor(pos, normal, NdotL);
        }
                
        //{
        //    NdotL = LightUtil::GetNdotL(pos, normal);
        //    sf = LightUtil::GetShadowFactor_Point_1(pos, normal, NdotL);
        //}
        
        //sf = 0;
        //NdotL = 0.5f;
        
        //sf = LightUtil::GetShadowFactor_Directional(pos, normal, NdotL);
        //sf = LightUtil::GetShadowFactor_Point(pos, normal, NdotL);
        //sf = LightUtil::GetShadowFactor_Spot(pos, normal, NdotL);
        
        sf = isCSM_Color == 1 ? 0 : sf;
        
        float k = 1.75f;
        float m = 0.4f;
        float l = 0.4f;
    
        //float specularFactor;
        //specularFactor = CSMUtil::GetSpecularFactor(pos, normal);
        
        //c = k * NdotL * (1.0f - m * sf) * ((1.0f - l) * c + l * specularFactor * float3(1.0f, 1.0f, 1.0f));  
        
        
        c = 0.25f * c + 0.75f * NdotL * (1.0f - sf) * c;
        //c = 0.5f * c + 0.5f * NdotL *  c;
        //c = 0.5f * c + 0.5f * (1.0f - sf) * c;
        
        //c = 0.5f * c + 0.5f * NdotL * c;
        
        pOut.color = float4(c.xyz, 1.0f);
    }
    
                      
    return pOut;
}

PS_Out_GBuffer PShader_CAM_GBuffer(RS_Out pIn)
{
    PS_Out_GBuffer pOut;   
  
    float3 c;
    c = color.xyz;
    
    float3 normal;
    
    float holeAlpha = holeTex.Sample(sampler_holeTex, pIn.uv);
    
    if (holeAlpha <= 0.0f)
    {
        c = waterTex.Sample(sampler_waterTex, pIn.uv0).xyz;
        //c = float3(0.0f, 0.0f, 0.75f);
        normal = normalize(pIn.normalW);
    }
    else
    {
        float alpha[4];
        {
            float4 mask = alphamap0.Sample(sampler_alphamap0, pIn.uv);
            alpha[0] = mask.r;
            alpha[1] = mask.g;
            alpha[2] = mask.b;
            alpha[3] = mask.a;
        }
   
    //#define LAYERED_NORMAL_MAP
    //#define BAKED_NORMAL_MAP
    //#define NONE_NORMAL_MAP
#if defined(LAYERED_NORMAL_MAP)    
        {                
            float3 nom[4];        
            float3 outNom;        
                            
            nom[0] = normalMapTex0.Sample(sampler_normalMapTex0, pIn.uv0).xyz;
            nom[1] = normalMapTex1.Sample(sampler_normalMapTex1, pIn.uv1).xyz;
            nom[2] = normalMapTex2.Sample(sampler_normalMapTex2, pIn.uv2).xyz;
            nom[3] = normalMapTex3.Sample(sampler_normalMapTex3, pIn.uv3).xyz;
                          
            nom[0] = ToSNom(nom[0]);
            nom[1] = ToSNom(nom[1]);
            nom[2] = ToSNom(nom[2]);
            nom[3] = ToSNom(nom[3]);
                    
            float as = alpha[0] + alpha[1] + alpha[2] + alpha[3];
            outNom = normalize((alpha[0] * nom[0] + alpha[1] * nom[1] + alpha[2] * nom[2] + alpha[3] * nom[3]) / as);        
                        
            float3x3 TBN = LightUtil::get_TBN_matrix(normalize(pIn.normalW), normalize(pIn.tangentW));
            normal = mul(TBN, outNom);    
            
            //normal = normalize(2.0f * normalize(pIn.normalW) + mul(TBN, outNom));
            //normal = float3(0.0f, 1.0f, 0.0f);
            //normal = nom[0];
        }
#elif defined(BAKED_NORMAL_MAP)       
        {
            float3 nom;
            nom = bakedNormalTex.Sample(sampler_bakedNormalTex, pIn.uv);
            nom = ToSNom(nom);
                    
            float3x3 TBN = LightUtil::get_TBN_matrix(normalize(pIn.normalW), normalize(pIn.tangentW));
            normal = mul(TBN, nom);
        }    
#elif defined(NONE_NORMAL_MAP)
        {
            normal = normalize(pIn.normalW);
        }    
#endif
                        
                    
    //#define LAYERED_DIFFUSE_TEX 
    //#define BAKED_DIFFUSE_TEX        
#if defined(LAYERED_DIFFUSE_TEX)  
        {
            float3 diffuse[4];       
            float3 outDiffuse;                     
        
            diffuse[0] = diffuseTex0.Sample(sampler_diffuseTex0, pIn.uv0);
            diffuse[1] = diffuseTex1.Sample(sampler_diffuseTex1, pIn.uv1);
            diffuse[2] = diffuseTex2.Sample(sampler_diffuseTex2, pIn.uv2);
            diffuse[3] = diffuseTex3.Sample(sampler_diffuseTex3, pIn.uv3);
            
            outDiffuse = alpha[0] * diffuse[0] + alpha[1] * diffuse[1] + alpha[2] * diffuse[2] + alpha[3] * diffuse[3];
            c = outDiffuse;            
        }    
#elif defined(BAKED_DIFFUSE_TEX)
        {
            c = bakedTex.Sample(sampler_bakedTex, pIn.uv);
        }
#endif                                                 
    }
    
    float3 posW = pIn.posW;
    float zV = mul(V, float4(posW, 1.0f)).z;
    float3 nomW = normal;
    float3 diffuse = c.xyz;
    
    pOut.color0 = float4(posW, 0.0f);
    //pOut.color1 = float4(zV, 0.0f, 0.0f, 0.0f);
    pOut.color1 = float4(zV, 1.0f, 0.0f, 0.0f);  // y : 1 => for decal
    pOut.color2 = float4(nomW, 0.0f);
    pOut.color3 = float4(diffuse, 1.0f);
            
    return pOut;
};

PS_Out PShader_Wire(RS_Out_Wire pIn)
{
    PS_Out pOut;

    float2 vi = mul(S, pIn.posCi / pIn.posCi.w).xy;
    float2 v0 = mul(S, pIn.posC0 / pIn.posC0.w).xy;
    float2 v1 = mul(S, pIn.posC1 / pIn.posC1.w).xy;
    float2 v2 = mul(S, pIn.posC2 / pIn.posC2.w).xy;
        
    float2 e0 = v2 - v1;
    float2 e1 = v0 - v2;
    float2 e2 = v1 - v0;
    
    float2 n0 = normalize(float2(e0.y, -e0.x));
    float2 n1 = normalize(float2(e1.y, -e1.x));
    float2 n2 = normalize(float2(e2.y, -e2.x));
    
    float2 p0 = v1;
    float2 p1 = v2;
    float2 p2 = v0;
    
    float d0 = abs(dot(vi - p0, n0));
    float d1 = abs(dot(vi - p1, n1));
    float d2 = abs(dot(vi - p2, n2));
    
    pOut.color = float4(0.0f, 0.0f, 0.0f, 1.0f);
    
    float range = 1.0f;
    if (d0 < range || d1 < range || d2 < range)
    {
        float d = min(d0, min(d1, d2));
        float alpha = exp(-pow(2.0f * (range * 0.5f - d), 2));
        pOut.color = float4(color.r, color.g, color.b, alpha);
    }
    else
    {
        //pOut.color = float4(0.75f, 0.75f, 0.75f, 1.0f);
        pOut.color = float4(0.0f, 0.0f, 0.0f, 0.0f);
    }
        
	            
    return pOut;
}


//Line
struct GS_Out_Line
{
    float4 posC : SV_POSITION;
    uint type : TYPE;
};

struct RS_Out_Line
{
    float4 posS : SV_POSITION;
    uint type : TYPE;
};

[maxvertexcount(4)]
void GShader_CAM_Line(point DS_Out gin[1], inout LineStream<GS_Out_Line> gOut)
{
    int i = 0;
    float3 posW;
    float4 posC;
    {
        posW = mul(W, gin[i].posL).xyz;
        posC = mul(CV, float4(posW, 1.0f));
    }
    
    GS_Out_Line vertice;
    //normal
    for (i = 0; i < 1; i++)
    {
        vertice.posC = posC;
        vertice.type = 0;
        gOut.Append(vertice);
     
        float3 pos1 = mul((float3x3) W_IT, gin[i].normalL);
        pos1 = posW + pos1;
        vertice.posC = mul(CV, float4(pos1, 1.0f));
        vertice.type = 0;
        gOut.Append(vertice);
    }
    gOut.RestartStrip();
    
    //tangent
    for (i = 0; i < 1; i++)
    {
        vertice.posC = posC;
        vertice.type = 1;
        gOut.Append(vertice);
     
        float3 pos1 = mul((float3x3) W_IT, gin[i].tangentL);
        pos1 = posW + pos1;
        vertice.posC = mul(CV, float4(pos1, 1.0f));
        vertice.type = 1;
        gOut.Append(vertice);
    }
    gOut.RestartStrip();
}

PS_Out PShader_Line(RS_Out_Line pIn)
{
    PS_Out pOut;
    
    float3 c;
    if (pIn.type == 0)
    {
        c = float3(0.0f, 1.0f, 0.0f);
    }
    else
    {
        c = float3(1.0f, 0.0f, 0.0f);
    }
    
    pOut.color = float4(c, 1.0f);
            
    return pOut;
}



//
uint2 GetTileId(uint pid, uint cx)
{
    uint2 tileId = uint2(pid % cx, pid / cx);
    return tileId;
}

float Clamp(float value, float min, float max)
{
    if (value < min)
    {
        return min;
    }
    else if (max < value)
    {
        return max;
    }
        
    return value;
}

float3 ToSNom(float3 input)
{
    float3 output;
    
    output = 2.0f * input - 1.0f;
    
    return output;
}

float reMapTessFactorByDist(float dist)
{
    float factor;
    
    float minDist = 1.0f;
    float maxDist = 250.0f;
    float minFactor = 1.0f;
    float maxFactor = maxTessFactor;
    
    dist = Clamp(dist, minDist, maxDist);
    
    factor = ((minFactor - maxFactor) / (maxDist - minDist)) * (dist - minDist) + maxFactor;
    
    factor = Clamp(factor, minFactor, maxFactor);
    
    return factor;
}

float reMapTessFactorByDev(float dev)
{
    float factor;
    
    //float minDev = 1.0f;
    //float maxDev = 16.0f;
            
    //float minDev = 1.0f;
    //float maxDev = 100000.0f;
    
    //float minDev = 0.0000001;
    //float maxDev = 0.01f;  
    
    float minDev = 0.0f;
    float maxDev = 0.1f;
    
    float minFactor = 1.0f;
    float maxFactor = maxTessFactor;
    
    dev = Clamp(dev, minDev, maxDev);
    
    factor = ((maxFactor - minFactor) / (maxDev - minDev)) * (dev - minDev) + minFactor;
    
    factor = Clamp(factor, minFactor, maxFactor);
       
    
    return factor;
}

float reMapTessFactor(float fDev, float fDist)
{
    float factor;
            
    float minFactor = 1.0f;
    float maxFactor = maxTessFactor;
    
    //dev = clamp(dev, minDev, maxDev);
        
    
    factor = ((fDev - minFactor) / (maxFactor - minFactor)) * (fDist - minFactor) + minFactor;
    
    factor = Clamp(factor, minFactor, maxFactor);
    
    
    
    return factor;
}



//Test
TS_Out TShader0(
	                InputPatch<VS_Out, 4> ip,
	                uint pid : SV_PrimitiveID)
{
    TS_Out tOut;
    
    uint cx = (uint) tileCount.x;
    uint cz = (uint) tileCount.z;
    
    uint uj = pid % (uint) cx;
    uint vi = pid / (uint) cx;
    
    uint un = uj - 1;
    uint uc = uj;
    uint up = uj + 1;
    
    if (un < 0)
    {
        un = uc;
    }
    else if (cx <= up)
    {
        up = uc;
    }
    
    uint vn = vi - 1;
    uint vc = vi;
    uint vp = vi + 1;
    
    if (vn < 0)
    {
        vn = vc;
    }
    else if (cz <= vp)
    {
        vp = vc;
    }
    
    bool isCull = false;
    if (mode == 0)  //Pvf - TileBox
    {
        if (TestCullPVF_Tex[int3(uc, fqtIdx * pvfCount + vfIdx, vc)] < 1.0f)
        //if (TestCullPVF_Tex[int3(uc, fqtIdx * pvfCount + 6, vc)] < 1.0f)
        {
            isCull = true;
        }
    }
    else //Ovf - TileBox
    {
        if (TestCullOVF_Tex[int3(uc, fqtIdx * ovfCount + vfIdx, vc)] < 1.0f)
        {
            isCull = true;
        }
    }
    
    //Debug
    //isCull = false;
    
    if (isCull)
    {
        tOut.eFactor[0] = 0.0f;
        tOut.eFactor[1] = 0.0f;
        tOut.eFactor[2] = 0.0f;
        tOut.eFactor[3] = 0.0f;
            
        tOut.iFactor[0] = 0.0f;
        tOut.iFactor[1] = 0.0f;
    }
    else
    {
        float fcc = reMapTessFactorByDev(LodData_Tex.Load(int4(uc, 0, vc, 0)).w);
        float fun = reMapTessFactorByDev(LodData_Tex.Load(int4(un, 0, vc, 0)).w);
        float fup = reMapTessFactorByDev(LodData_Tex.Load(int4(up, 0, vc, 0)).w);
        float fvn = reMapTessFactorByDev(LodData_Tex.Load(int4(uc, 0, vn, 0)).w);
        float fvp = reMapTessFactorByDev(LodData_Tex.Load(int4(uc, 0, vp, 0)).w);
        
        //
        float4 xn = float4(0.5f * (ip[0].posL.xyz + ip[1].posL.xyz), 1.0f);
        float4 xp = float4(0.5f * (ip[2].posL.xyz + ip[3].posL.xyz), 1.0f);
        float4 zn = float4(0.5f * (ip[3].posL.xyz + ip[0].posL.xyz), 1.0f);
        float4 zp = float4(0.5f * (ip[1].posL.xyz + ip[2].posL.xyz), 1.0f);
    
        xn = mul(W, xn);
        xp = mul(W, xp);
        zn = mul(W, zn);
        zp = mul(W, zp);
    
        float lxn = distance(xn.xyz, posW_mainCam.xyz);
        float lxp = distance(xp.xyz, posW_mainCam.xyz);
        float lzn = distance(zn.xyz, posW_mainCam.xyz);
        float lzp = distance(zp.xyz, posW_mainCam.xyz);
    
        float fxn = reMapTessFactor(0.5f * (fcc + fun), reMapTessFactorByDist(lxn));
        float fzp = reMapTessFactor(0.5f * (fcc + fvp), reMapTessFactorByDist(lzp));
        float fxp = reMapTessFactor(0.5f * (fcc + fup), reMapTessFactorByDist(lxp));
        float fzn = reMapTessFactor(0.5f * (fcc + fvn), reMapTessFactorByDist(lzn));
        
        ////Debug
        //fxn = 0.5f * (fcc + fun);
        //fzp = 0.5f * (fcc + fvp);
        //fxp = 0.5f * (fcc + fup);
        //fzn = 0.5f * (fcc + fvn);
    
        //    
        float fux = 0.5f * (fxn + fxp);
        float fvz = 0.5f * (fzn + fzp);
           
        tOut.eFactor[0] = fxn;
        tOut.eFactor[1] = fzp;
        tOut.eFactor[2] = fxp;
        tOut.eFactor[3] = fzn;
            
        tOut.iFactor[0] = fux;
        tOut.iFactor[1] = fvz;
            
        //tOut.eFactor[0] = clamp(fxn, 1.0f, 16.0f);
        //tOut.eFactor[1] = clamp(fzp, 1.0f, 16.0f);
        //tOut.eFactor[2] = clamp(fxp, 1.0f, 16.0f);
        //tOut.eFactor[3] = clamp(fzn, 1.0f, 16.0f);
        //    
        //tOut.iFactor[0] = clamp(fux, 1.0f, 16.0f);
        //tOut.iFactor[1] = clamp(fvz, 1.0f, 16.0f);
   
        ////   
        
        //{
        //    float3 patchNormal = LodData_Tex.Load(int4(uj, 0, vi, 0)).xyz;
        //    bool bBackPatch = false;
        //    //if (dot(patchNormal, dirW_mainCam.xyz) < 0.0f)
        //    if (dot(patchNormal, dirW_cam.xyz) < -0.25f)
        //    {
        //        bBackPatch = true;
        //    }
        //
        //    //Debug
        //    //bBackPatch = false;
        //
        //    if (bBackPatch)
        //    {
        //        tOut.eFactor[0] = 0.0f;
        //        tOut.eFactor[1] = 0.0f;
        //        tOut.eFactor[2] = 0.0f;
        //        tOut.eFactor[3] = 0.0f;
	    //        
        //        tOut.iFactor[0] = 0.0f;
        //        tOut.iFactor[1] = 0.0f;
        //    }
        //}
       
    
    }
    
    //{
    //    tOut.eFactor[0] = 1.0f;
    //    tOut.eFactor[1] = 1.0f;
    //    tOut.eFactor[2] = 1.0f;
    //    tOut.eFactor[3] = 1.0f;
    //        
    //    tOut.iFactor[0] = 1.0f;
    //    tOut.iFactor[1] = 1.0f;
    //}
	                
    return tOut;
}