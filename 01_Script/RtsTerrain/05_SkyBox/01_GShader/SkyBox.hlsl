#include "../../../Utility/01_GShader/LightUtil.hlsl"

struct VertexIn
{
    float4 posL;
    float4 normalL;
    float4 tangentL;
    float4 boneI;
    float4 boneW;
};

struct Vertex
{
    float4 posW;
    float4 normalW;
    float4 tangentW;
};

struct VertexStatic
{
    float4 uv;
};

struct ViewData
{
    float4x4 V;
    
    float4x4 C;
    float4x4 CV;
    
    float4x4 _C_I;
    float4x4 V_I;
        
    float4 posW;
    float4 dirW;
    float4 data;
};

float4x4 M;
float4 countInfo;

StructuredBuffer<Vertex> vtxBuffer;
StructuredBuffer<VertexStatic> vtxBuffer_St;
StructuredBuffer<ViewData> camera;

StructuredBuffer<VertexIn> vtxBuffer_Local;

TextureCube<float4> cubemap;
SamplerState sampler_cubemap;


struct IA_Out
{
    uint vid : SV_VertexID;
    uint iid : SV_InstanceID;
};

struct VS_Out
{
    float4 posC : SV_Position;
    float3 posL : PosL;
};

struct RS_Out
{
    float4 posS : SV_Position;
    float3 posL : PosL;
};

struct PS_Out
{
    float4 target0 : SV_Target0;
};

float3 GetPosCube(float3 pos);

VS_Out VShader(IA_Out vin)
{
    VS_Out vOut;
    uint iid = vin.iid;
    uint vid = vin.vid;
    
    //iid = 0;
    
    uint dvCount = (uint) countInfo[0];
    {
        Vertex vtx_sk = vtxBuffer[iid * dvCount + vid];
        VertexStatic vtx_st = vtxBuffer_St[vid];
        VertexIn vtx_local = vtxBuffer_Local[vid];
        
        float3 posL = vtx_local.posL.xyz;
        float3 posW = vtx_sk.posW.xyz;
            
        float4 posC = mul(M, float4(posW, 1.0f));
        
        vOut.posL = posL;
        vOut.posC = posC;
    }
    
    return vOut;
}


PS_Out PShader(RS_Out pin)
{
    PS_Out pout;
    float3 posL = pin.posL;
    
    ViewData cam = camera[0];
    float4 posN = float4(posL.xy, 0.0f, 1.0f);
    float4 posV = mul(cam._C_I, posN);
    posV = posV / posV.w;
    
    float3 dirW = mul((float3x3) cam.V_I, posV.xyz);
            
    float3 color = cubemap.Sample(sampler_cubemap, dirW).xyz;
    
    {
        int ltype = LightUtil::GetType();
    
        if (ltype == 0 || ltype == 2)
        {
            color *= 0.005f;
        }
        else
        {
            color *= LightUtil::GetNdotL(float3(0.0f, 0.0f, 0.0f), float3(0.0f, 1.0f, 0.0f));
        }
   
    }
                           
    pout.target0 = float4(color, 1.0f);
      
    return pout;
}