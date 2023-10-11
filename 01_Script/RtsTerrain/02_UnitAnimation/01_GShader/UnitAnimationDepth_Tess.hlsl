#include "../../../Utility/01_GShader/LightUtil.hlsl"

struct VertexDynamic
{
    float3 posW;
    float3 nomW;
    float4 tanW;
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


cbuffer tessFactor
{
    float4 tFactor;
};

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
    float3 posW : PosW;
    float3 nomW : NomW;
    
    uint state : STATE;
    uint isActive : ISACTIVE;
    uint isCull : ISCULL;
    uint iid : SV_InstanceID;
};

struct TS_Out
{
    float Edges[3] : SV_TessFactor;
    float Inside : SV_InsideTessFactor;
};

struct HS_Out
{
    float3 posW : PosW;
    float3 nomW : NomW;
    //float3 tanW : TanW;
    //    
    //float2 uv0 : TEXCOORD0;
    //float2 uv1 : TEXCOORD1;
    //float4 color : COLOR0;
     
    uint state : STATE;
    uint isActive : ISACTIVE;
    uint isCull : ISCULL;
    uint iid : SV_InstanceID;
};

struct DS_Out
{
    float3 posW : PosW;
    float3 nomW : NomW;
    //float3 tanW : TanW;
    //    
    //float2 uv0 : TEXCOORD0;
    //float2 uv1 : TEXCOORD1;
    //float4 color : COLOR0;
            
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
    
    //uint isCull = 0;
    uint isCull = 1;
    
    float4 tessInfo = unit_tess_Buffer[unitIdx];
    if (tessInfo.x == 1.0f)
    {
        isCull = 0;
        iid = (uint) tessInfo.y;
    }
    
    uint isActive = 0;
    if (active_Buffer[offsetIdx + iid] == 1)
    {
        isActive = 1;
    }
    
    int state = state_Buffer[offsetIdx + iid];
    
    if (state < 4)
    {
        VertexDynamic vtxd = vtxDynamic[iid * vtxCount + vid];        
    
        vOut.posW = vtxd.posW;
        vOut.nomW = vtxd.nomW;
    }
    else
    {
        vOut.posW = float3(0.0f, 0.0f, 0.0f);
    }
    
    {
        vOut.state = state;
        vOut.iid = iid;
        vOut.isActive = isActive;
        vOut.isCull = isCull;
    }
            
    return vOut;
}

TS_Out TShader(
				InputPatch<VS_Out, 3> ip,
				uint PatchID : SV_PrimitiveID)
{
    TS_Out tsOut;
	    
    tsOut.Edges[0] = tFactor.x;
    tsOut.Edges[1] = tFactor.x;
    tsOut.Edges[2] = tFactor.x;
    tsOut.Inside = tFactor.x;
    
    //float factor = 4;
    //tsOut.Edges[0] = factor;
    //tsOut.Edges[1] = factor;
    //tsOut.Edges[2] = factor;
    //tsOut.Inside   = factor;

    return tsOut;
}


float3 getEdgeCPoint(InputPatch<VS_Out, 3> ip, int i, int j)
{
    float3 ni = normalize(ip[i].nomW);
    
    float3 outPos = float3(0.0f, 0.0f, 0.0f);
    outPos = (2.0f * ip[i].posW + ip[j].posW) / 3.0f + (-0.15f) * dot((ip[j].posW - ip[i].posW), ni) * ni;
    return outPos;
}

float3 getFaceCPoint(InputPatch<VS_Out, 3> ip)
{
    float3 outPos = float3(0.0f, 0.0f, 0.0f);
    float3 E = (
					getEdgeCPoint(ip, 0, 1) + getEdgeCPoint(ip, 1, 0) +
					getEdgeCPoint(ip, 1, 2) + getEdgeCPoint(ip, 2, 1) +
					getEdgeCPoint(ip, 2, 0) + getEdgeCPoint(ip, 0, 2)
					) / 6.0f;
    float3 V = (
					ip[0].posW + ip[1].posW + ip[2].posW
					) / 3.0f;
    outPos = E + (E - V) / 2.0f;

    return outPos;
}

float3 getEdgeCNormal(InputPatch<VS_Out, 3> ip, int i, int j)
{
    float3 outNormal = float3(1.0f, 1.0f, 1.0f);
    
    float3 a = ip[i].nomW + ip[j].nomW;
    float3 b = ip[j].posW - ip[i].posW;

    outNormal = a - 2.0f * (dot(a, b) / dot(b, b) * b);
    //outNormal = a;
    outNormal = normalize(outNormal);

    return outNormal;
}


[domain("tri")]
[partitioning("integer")]
//[partitioning("fractional_odd")]
//[partitioning("fractional_even")]
//[partitioning("pow2")]
[outputtopology("triangle_cw")]
//[outputtopology("triangle_ccw")]
[outputcontrolpoints(13)]
[patchconstantfunc("TShader")]
[maxtessfactor(64.0f)]
			HS_Out HShader(
				const InputPatch<VS_Out, 3> ip,
				uint i : SV_OutputControlPointID,
				uint PatchID : SV_PrimitiveID)
{
    HS_Out hsOut;
    
    hsOut.posW = float3(0.0f, 0.0f, 0.0f);
    hsOut.nomW = float3(1.0f, 1.0f, 1.0f);
    
    VS_Out hsIn = ip[0];
       
    hsOut.posW = hsIn.posW;
    hsOut.nomW = hsIn.nomW;    
       
    
    hsOut.state = hsIn.state;
    hsOut.isCull = hsIn.isCull;
    hsOut.isActive = hsIn.isActive;
    hsOut.iid = hsIn.iid;
       
    int j;
    switch (i)
    {
		//vertex control position normal
        case 0:
            j = 0;
            hsIn = ip[j];
        
            hsOut.posW = hsIn.posW;
            hsOut.nomW = hsIn.nomW;           
            
            hsOut.state = hsIn.state;
            hsOut.isCull = hsIn.isCull;
            hsOut.isActive = hsIn.isActive;
            hsOut.iid = hsIn.iid;
            break;
        case 1:
            j = 1;
            hsIn = ip[j];
        
            hsOut.posW = hsIn.posW;
            hsOut.nomW = hsIn.nomW;           
            
            hsOut.state = hsIn.state;
            hsOut.isCull = hsIn.isCull;
            hsOut.isActive = hsIn.isActive;
            hsOut.iid = hsIn.iid;
            break;
        case 2:
            j = 2;
            hsIn = ip[j];
        
            hsOut.posW = hsIn.posW;
            hsOut.nomW = hsIn.nomW;
           
            hsOut.state = hsIn.state;
            hsOut.isCull = hsIn.isCull;
            hsOut.isActive = hsIn.isActive;
            hsOut.iid = hsIn.iid;
            break;
					//Edge control position 0 , 1
        case 3:
            hsOut.posW = getEdgeCPoint(ip, 0, 1);
            break;
        case 4:
            hsOut.posW = getEdgeCPoint(ip, 1, 0);
            break;
					//Edge control position 1 , 2
        case 5:
            hsOut.posW = getEdgeCPoint(ip, 1, 2);
            break;
        case 6:
            hsOut.posW = getEdgeCPoint(ip, 2, 1);
            break;
					//Edge control position 2 , 0
        case 7:
            hsOut.posW = getEdgeCPoint(ip, 2, 0);
            break;
        case 8:
            hsOut.posW = getEdgeCPoint(ip, 0, 2);
            break;
					//Face control position 0 , 1
        case 9:
            hsOut.posW = getFaceCPoint(ip);
            break;
					//Edge control normal 0 , 1 , 2
        case 10:
            hsOut.nomW = getEdgeCNormal(ip, 0, 1);
            break;
        case 11:
            hsOut.nomW = getEdgeCNormal(ip, 1, 2);
            break;
        case 12:
            hsOut.nomW = getEdgeCNormal(ip, 2, 0);
            break;

    }

    return hsOut;
}


[domain("tri")]
DS_Out DShader(
				const OutputPatch<HS_Out, 13> op,
				float3 bc : SV_DomainLocation,
				TS_Out tsOut,
                uint pid : SV_PrimitiveID)
{
    DS_Out dOut;

    float u = bc.x;
    float v = bc.y;
    float w = bc.z;

				//Control Point
    float3 p300 = op[0].posW;
    float3 p030 = op[1].posW;
    float3 p003 = op[2].posW;

    float3 p210 = op[3].posW;
    float3 p120 = op[4].posW;

    float3 p021 = op[5].posW;
    float3 p012 = op[6].posW;

    float3 p102 = op[7].posW;
    float3 p201 = op[8].posW;

    float3 p111 = op[9].posW;

				//Control Normal
    float3 n200 = op[0].nomW;
    float3 n020 = op[1].nomW;
    float3 n002 = op[2].nomW;

    float3 n110 = op[10].nomW;
    float3 n011 = op[11].nomW;
    float3 n101 = op[12].nomW;

    float3 pos =
					p300 * pow(u, 3) + p030 * pow(v, 3) + p003 * pow(w, 3) +
					3.0f * p210 * pow(u, 2) * v + 3.0f * p120 * u * pow(v, 2) +
					3.0f * p021 * pow(v, 2) * w + 3.0f * p012 * v * pow(w, 2) +
					3.0f * p102 * pow(w, 2) * u + 3.0f * p201 * w * pow(u, 2) +
					6.0f * p111 * u * v * w;

    float3 nom =
					n200 * pow(u, 2) + n020 * pow(v, 2) + n002 * pow(w, 2) +
					2.0f * n110 * u * v +
					2.0f * n011 * v * w +
					2.0f * n101 * w * u;
    
    //float3 nom =
	//				n200 * u + n020 * v + n002 * w;
    
    HS_Out p0 = op[0];
    HS_Out p1 = op[1];
    HS_Out p2 = op[2];
        
    
    uint iid = p0.iid;
        
    dOut.posW = pos;
    dOut.nomW = nom;
   
    dOut.iid = iid;
    
    dOut.state = p0.state;
    dOut.isCull = p0.isCull;
    dOut.isActive = p0.isActive;

    return dOut;
}



[maxvertexcount(12)]
void GShader_CSM(triangle DS_Out gIn[3], inout TriangleStream<GS_Out> gOut)
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
                float4 _posW = float4(gIn[j].posW, 1.0f);
                output.posC = mul(CV, _posW);
                output.posC_depth = mul(CV_depth, _posW);
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
void GShader_CBM(triangle DS_Out gIn[3], inout TriangleStream<GS_Out> gOut)
{
    uint iid = gIn[0].iid;
    uint isCull = gIn[0].isCull;
    uint isActive = gIn[0].isActive;
    uint state = gIn[0].state;
    
    if (isCull == 1)
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
                output.posC = mul(CV, float4(gIn[j].posW, 1.0f));
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