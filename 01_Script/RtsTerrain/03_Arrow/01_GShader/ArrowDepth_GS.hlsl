#include "../../../Utility/01_GShader/LightUtil.hlsl"

cbuffer perView
{
    float4x4 CV;
    float4x4 CV_depth;
};

struct ArrowConst
{
    bool active;
        
    float u;
    float3 sca;
    
    float3 pi;
    float3 p0;
    float3 p1;
};

struct VertexDynamic
{
    float3 posW;
    float3 normalW;
    float4 tangentW;
    int4 boneI;
};

struct CSMdata
{
    float4x4 CV;
    float4x4 CV_depth;
};

StructuredBuffer<CSMdata> csmDataBuffer;

StructuredBuffer<VertexDynamic> vtxDynamic;

StructuredBuffer<ArrowConst> arrowConst;

int vtxCount;

int cullOffset;
Texture3D<float> cullResult_ovf_Texture;
Texture3D<float> cullResult_pvf_Texture;

struct IA_Out
{
    uint vid : SV_VertexID;
    uint iid : SV_InstanceID;
};

struct VS_Out
{
    float4 posW : SV_POSITION;   
    
    uint iid : SV_InstanceID;
};

struct GS_Out
{
    float4 posC : SV_POSITION;
    float4 posC_depth : TEXCOORD6;
    float3 posW : PosW;
    
    uint isCull : ISCULL;
    uint iid : SV_InstanceID;    
    uint rtIndex : SV_RenderTargetArrayIndex;
};

struct RS_Out
{
    float4 posS : SV_POSITION;
    float4 posC_depth : TEXCOORD6;
    float3 posW : PosW;
    
    uint isCull : ISCULL;
    uint iid : SV_InstanceID;
    uint rtIndex : SV_RenderTargetArrayIndex;
};

struct PS_Out
{
    float depth : SV_Target;
};


VS_Out VShader(IA_Out vIn)
{
    VS_Out vOut;
           
    uint vid = vIn.vid;
    uint iid = vIn.iid;
    
    ArrowConst constData = arrowConst[iid];
    
    bool inOvf = false;
        
    if (constData.active)
    {
        VertexDynamic vtxd = vtxDynamic[iid * vtxCount + vid];
        float3 posW = vtxd.posW;
        vOut.posW = float4(posW, 1.0f);
    }
    else
    {
        vOut.posW = float4(0.0f, 0.0f, 0.0f, 1.0f);              
    }
                
    vOut.iid = iid;
    
    return vOut;
}

[maxvertexcount(12)]
void GShader_CSM(triangle VS_Out gIn[3], inout TriangleStream<GS_Out> gOut)
{
    uint iid;
    uint isCull = 0;
    iid = gIn[0].iid;
        
    for (int i = 0; i < 4; i++)
    {
        GS_Out output;
        output.rtIndex = i;
        float4x4 CV = csm_data_Buffer[i].CV;
        float4x4 CV_depth = csm_data_Buffer[i].CV_depth;
                       
        isCull = 0;
        if (cullResult_ovf_Texture[int3(cullOffset + iid, i, 0)] == 0.0f)
        {
            isCull = 1;
        }
        
        for (int j = 0; j < 3; j++)
        {
            if (isCull == 0)
            {
                output.posC = mul(CV, gIn[j].posW);
                output.posC_depth = mul(CV_depth, gIn[j].posW);
            }
            else
            {
                output.posC = float4(0.0f, 0.0f, 0.0f, 1.0f);
                output.posC_depth = float4(0.0f, 0.0f, 0.0f, 1.0f);
            }
            
            output.posW = gIn[j].posW;
            output.iid = gIn[j].iid;
            output.isCull = isCull;
            gOut.Append(output);
        }
        gOut.RestartStrip();
    }
}

[maxvertexcount(18)]
void GShader_CBM(triangle VS_Out gIn[3], inout TriangleStream<GS_Out> gOut)
{
    uint iid;
    uint isCull = 0;
    iid = gIn[0].iid;
        
    for (int i = 0; i < 6; i++)
    {
        GS_Out output;
        output.rtIndex = i;
        float4x4 CV = cbm_data_Buffer[i].CV;
        //float4x4 CV_depth = csm_data_Buffer[i].CV_depth;
                       
        isCull = 0;
        if (cullResult_pvf_Texture[int3(cullOffset + iid, (i + 1), 0)] == 0.0f)
        {
            isCull = 1;
        }
        
        for (int j = 0; j < 3; j++)
        {
            if (isCull == 0)
            {
                output.posC = mul(CV, gIn[j].posW);
                output.posC_depth = output.posC;
            }
            else
            {
                output.posC = float4(0.0f, 0.0f, 0.0f, 1.0f);
                output.posC_depth = float4(0.0f, 0.0f, 0.0f, 1.0f);
            }
                     
            output.posW = gIn[j].posW;
            output.iid = gIn[j].iid;
            output.isCull = isCull;
            gOut.Append(output);
        }
        gOut.RestartStrip();
    }
}


PS_Out PShader_CSM(RS_Out pIn)
{
    PS_Out pOut;
    
    uint iid = pIn.iid;
    uint isCull = pIn.isCull;
    
    ArrowConst constData = arrowConst[iid];      
        
    if (constData.active && isCull == 0)
    {
        float4 posC = pIn.posC_depth;
        pOut.depth = posC.z / posC.w;
    }
    else
    {
        pOut.depth = 1.0f;
    }
            
    return pOut;
}

PS_Out PShader_CBM(RS_Out pIn)
{
    PS_Out pOut;
    
    uint iid = pIn.iid;
    uint isCull = pIn.isCull;
    
    ArrowConst constData = arrowConst[iid];
        
    if (constData.active && isCull == 0)
    {
        LightData data = light_Buffer[0];
                
        float3 vec = LightUtil::GetVec_CBM(pIn.posW);
        pOut.depth = sqrt(dot(vec, vec)) / data.far_plane;
    }
    else
    {
        pOut.depth = 1.0f;
    }
            
    return pOut;
}


//Test
[maxvertexcount(12)]
void GShader0(triangle VS_Out gIn[3], inout TriangleStream<GS_Out> gOut)
{
    uint iid;
    uint isCull = 0;
    iid = gIn[0].iid;
        
    for (int i = 0; i < 4; i++)
    {
        GS_Out output;
        output.rtIndex = i;
        float4x4 CV = csmDataBuffer[i].CV;
        float4x4 CV_depth = csmDataBuffer[i].CV_depth;
                       
        isCull = 0;
        if (cullResult_ovf_Texture[int3(cullOffset + iid, i, 0)] == 0.0f)
        {
            isCull = 1;
        }
        
        for (int j = 0; j < 3; j++)
        {
            if (isCull == 0)
            {
                output.posC = mul(CV, gIn[j].posW);
                output.posC_depth = mul(CV_depth, gIn[j].posW);
            }
            else
            {
                output.posC = float4(0.0f, 0.0f, 0.0f, 1.0f);
                output.posC_depth = float4(0.0f, 0.0f, 0.0f, 1.0f);
            }
                     
            output.iid = gIn[j].iid;
            output.isCull = isCull;
            gOut.Append(output);
        }
        gOut.RestartStrip();
    }
}
