#include "../../../Utility/01_GShader/LightUtil.hlsl"

struct VertexDynamic
{
    float3 posW;
    float3 normalW;
    float4 tangentW;
};

StructuredBuffer<VertexDynamic> vtxDynamic;

struct CSMdata
{
    float4x4 CV;
    float4x4 CV_depth;
};

StructuredBuffer<CSMdata> csmDataBuffer;

int vtxCount;

StructuredBuffer<int> active_Buffer;
StructuredBuffer<int> state_Buffer;
int offsetIdx;

int cullOffset;
Texture3D<float> cullResult_ovf_Texture;
Texture3D<float> cullResult_pvf_Texture;

int unitIdx;
StructuredBuffer<float4> unit_tess_Buffer; //float4(isIn, idx, dist, count)

//struct CSM_depth_data
//{
//    float4x4 CV;
//    float4x4 CV_depth;
//};
//
//StructuredBuffer<CSM_depth_data> csm_data_Buffer;
//
//struct CBM_depth_data
//{
//    float4x4 CV;
//};
//
//StructuredBuffer<CBM_depth_data> cbm_data_Buffer;

struct IA_Out
{
    uint vid : SV_VertexID;
    uint iid : SV_InstanceID;
};

struct VS_Out
{
    float4 posW : SV_POSITION;       
    
    uint state : STATE;
    uint isActive : ISACTIVE;
    uint isCull : ISCULL;
    uint iid : SV_InstanceID;
};

struct GS_Out
{
    float4 posC : SV_POSITION;
    float4 posC_depth : TEXCOORD6;
    float3 posW : PosW;
    
    uint state : STATE;
    uint isActive : ISACTIVE;
    uint isCull : ISCULL;
    uint iid : SV_InstanceID;
        
    uint rtIndex : SV_RenderTargetArrayIndex;
};

struct RS_Out
{
    float4 posS : SV_POSITION;
    float4 posC_depth : TEXCOORD6;
    float3 posW : PosW;
    
    uint state : STATE;
    uint isActive : ISACTIVE;
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
    
    uint isCull = 0;        
    
    float4 tessInfo = unit_tess_Buffer[unitIdx];
    if (tessInfo.x == 1.0f && (uint) (tessInfo.y) == iid)
    {
        isCull = 1;
    }
    
    uint isActive = 0;
    if (active_Buffer[offsetIdx + iid] == 1)
    {
        isActive = 1;
    }
    
    int state = state_Buffer[offsetIdx + iid];
    
    if(state < 4)
    {
        VertexDynamic vtxd = vtxDynamic[iid * vtxCount + vid];
        float3 posW = vtxd.posW;
    
        vOut.posW = float4(posW, 1.0f);                
    }       
    else
    {
        vOut.posW = float4(0.0f, 0.0f, 0.0f, 1.0f);
    }
    
    {
        vOut.state = state;
        vOut.iid = iid;
        vOut.isActive = isActive;
        vOut.isCull = isCull;
    }
            
    return vOut;
}

[maxvertexcount(12)]
void GShader_CSM(triangle VS_Out gIn[3], inout TriangleStream<GS_Out> gOut)
{
    uint iid = gIn[0].iid;
    uint isCull = gIn[0].isCull;
    uint isActive = gIn[0].isActive;
    uint state = gIn[0].state;
    
    if (isCull == 1)
    {
        return;
    }
    
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
            if (state < 4 && isCull == 0)
            {                
                output.posC = mul(CV, gIn[j].posW);
                output.posC_depth = mul(CV_depth, gIn[j].posW);
            }
            else
            {
                output.posC = float4(0.0f, 0.0f, 0.0f, 1.0f);
                output.posC_depth = float4(0.0f, 0.0f, 0.0f, 1.0f);
            }
                           
            {
                output.posW = gIn[j].posW;
                output.iid = iid;
                output.isActive = isActive;
                output.isCull = isCull;
                output.state = state;
            }
            
            gOut.Append(output);
        }
        gOut.RestartStrip();
    }
}

[maxvertexcount(18)]
void GShader_CBM(triangle VS_Out gIn[3], inout TriangleStream<GS_Out> gOut)
{
    uint iid = gIn[0].iid;
    uint isCull = gIn[0].isCull;
    uint isActive = gIn[0].isActive;
    uint state = gIn[0].state;
    
    if(isCull == 1)
    {
        return;
    }
    
    
    for (int i = 0; i < 6; i++)
    {
        GS_Out output;
        output.rtIndex = i;
        float4x4 CV = cbm_data_Buffer[i].CV;
        
        
        isCull = 0;
        if (cullResult_pvf_Texture[int3(cullOffset + iid, (i + 1), 0)] == 0.0f)
        {
            isCull = 1;
        }
        
        for (int j = 0; j < 3; j++)
        {
            if (state < 4 && isCull == 0)
            {
                output.posC = mul(CV, gIn[j].posW);
                output.posC_depth = output.posC;
            }
            else
            {
                output.posC = float4(0.0f, 0.0f, 0.0f, 1.0f);
                output.posC_depth = float4(0.0f, 0.0f, 0.0f, 1.0f);
            }
                           
            {
                output.posW = gIn[j].posW;
                output.iid = iid;
                output.isActive = isActive;
                output.isCull = isCull;
                output.state = state;
            }
            
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
    uint isActive = pIn.isActive;
    
    int state = state_Buffer[offsetIdx + iid];
    
    if (state < 4 && isCull == 0)
    {
        float4 posC = pIn.posC_depth;
        pOut.depth = posC.z / posC.w;
    }
    else //state  4  Sleep      
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
    uint isActive = pIn.isActive;
    
    int state = state_Buffer[offsetIdx + iid];
    
    if (state < 4 && isCull == 0)
    {
        LightData data = light_Buffer[0];
                
        float3 vec = LightUtil::GetVec_CBM(pIn.posW);
        pOut.depth = sqrt(dot(vec, vec)) / data.far_plane;
    }
    else //state  4  Sleep      
    {
        pOut.depth = 1.0f;
    }
    
    return pOut;
}



//Test
[maxvertexcount(12)]
void GShader00(triangle VS_Out gIn[3], inout TriangleStream<GS_Out> gOut)
{
    uint iid = gIn[0].iid;
    uint isCull = gIn[0].isCull;
    uint isActive = gIn[0].isActive;
    uint state = gIn[0].state;
    
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
            if (state < 4 && isCull == 0)
            {
                output.posC = mul(CV, gIn[j].posW);
                output.posC_depth = mul(CV_depth, gIn[j].posW);
            }
            else
            {
                output.posC = float4(0.0f, 0.0f, 0.0f, 1.0f);
                output.posC_depth = float4(0.0f, 0.0f, 0.0f, 1.0f);
            }
                           
            {
                output.iid = iid;
                output.isActive = isActive;
                output.isCull = isCull;
                output.state = state;
            }
            
            gOut.Append(output);
        }
        gOut.RestartStrip();
    }
}

PS_Out PShader0(RS_Out pIn)
{
    PS_Out pOut;
    
    uint iid = pIn.iid;
    uint isCull = pIn.isCull;
    uint isActive = pIn.isActive;
    
    if (isActive == 1)
    {
        float4 posC = pIn.posC_depth;
        pOut.depth = posC.z / posC.w;
    }
    else
    {
        int state = state_Buffer[offsetIdx + iid];
        if (state == 3)
        {
            float4 posC = pIn.posC_depth;
            pOut.depth = posC.z / posC.w;
        }
        else
        {
            pOut.depth = 1.0f;
        }
       
    }
    
    return pOut;
}

PS_Out PShader1(RS_Out pIn)
{
    PS_Out pOut;
    
    uint iid = pIn.iid;
    uint isCull = pIn.isCull;
    uint isActive = pIn.isActive;
    
    int state = state_Buffer[offsetIdx + iid];
    
    if (state < 4)
    {
        float4 posC = pIn.posC_depth;
        pOut.depth = posC.z / posC.w;
    }
    else //state 3, 4 Die, Sleep      
    {
        pOut.depth = 1.0f;
    }
    
    return pOut;
}

