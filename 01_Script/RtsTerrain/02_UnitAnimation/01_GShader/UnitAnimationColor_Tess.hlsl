#include "../../../Utility/01_GShader/LightUtil.hlsl"

struct VertexDynamic
{
    float3 posW;
    float3 nomW;
    float4 tanW;
};

struct VertexStatic
{
    float2 uv0;
    float2 uv1;
    float4 color;
};

cbuffer perView
{
    float4x4 S;
    float4x4 V;
    float4x4 CV;
};

cbuffer perLight
{
    float4 dirW_light;
};


cbuffer tessFactor
{
    float4 tFactor;
};

StructuredBuffer<VertexDynamic> vtxDynamic;
StructuredBuffer<VertexStatic> vtxStatic;
int vtxCount;

Texture2D tex_diffuse0;
SamplerState sampler_tex_diffuse0;

Texture2D tex_diffuse1;
SamplerState sampler_tex_diffuse1;

StructuredBuffer<int> active_Buffer;
int offsetIdx;

StructuredBuffer<int> state_Buffer;
float4 unitColor;

int cullOffset;
Texture3D<float> cullResult_pvf_Texture;

int unitIdx;
StructuredBuffer<float4> unit_tess_Buffer; //float4(isIn, idx, dist, count)

struct IA_Out
{
    uint vid : SV_VertexID;
    uint iid : SV_InstanceID;
};


struct VS_Out
{        
    float3 posW : PosW;
    float3 nomW : NomW;
    float3 tanW : TanW;
        
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    
    float4 color : COLOR0;
            
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
    float3 tanW : TanW;
        
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;    
    float4 color : COLOR0;       
     
    uint state : STATE;
    uint isActive : ISACTIVE;
    uint isCull : ISCULL;
    uint iid : SV_InstanceID;
};

struct DS_Out
{   
    float3 posW : PosW;
    float3 nomW : NomW;
    float3 tanW : TanW;
        
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    float4 color : COLOR0;
            
    uint state : STATE;
    uint isActive : ISACTIVE;
    uint isCull : ISCULL;
    uint iid : SV_InstanceID;
};

struct GS_Out
{   
    float4 posC : SV_Position;
    
    float3 posW : PosW;
    float3 nomW : NomW;
    float3 tanW : TanW;
        
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    float4 color : COLOR0;
            
    uint state : STATE;
    uint isActive : ISACTIVE;
    uint isCull : ISCULL;
    uint iid : SV_InstanceID;
};


struct RS_Out
{    
    float4 posS : SV_Position;
    
    float3 posW : PosW;
    float3 nomW : NomW;
    float3 tanW : TanW;
        
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    
    float4 color : COLOR0;    
     
    uint state : STATE;
    uint isActive : ISACTIVE;
    uint isCull : ISCULL;
    uint iid : SV_InstanceID;
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
    
    if (cullResult_pvf_Texture[int3(cullOffset + iid, 0, 0)] == 0.0f)
    {
        isCull = 1;
    }
            
    uint isActive = 0;
    if (active_Buffer[offsetIdx + iid] == 1)
    {
        isActive = 1;
    }
    
    int state = state_Buffer[offsetIdx + iid];
    
    if (state < 3 && isCull == 0)
    {
        VertexDynamic vtxd = vtxDynamic[iid * vtxCount + vid];
        VertexStatic vtxs = vtxStatic[vid];
           
        vOut.posW = vtxd.posW;
        //vOut.nomW = vtxd.nomW;
        vOut.nomW = normalize(vtxd.nomW);
        vOut.tanW = vtxd.tanW.xyz;
            
        vOut.uv0 = vtxs.uv0;
        vOut.uv1 = vtxs.uv1;
        
        vOut.color = vtxs.color;
    }
    else
    {      
        vOut.posW = float3(0.0f, 0.0f, 0.0f);
        vOut.nomW = float3(0.0f, 0.0f, 0.0f);
        vOut.tanW = float3(0.0f, 0.0f, 0.0f);
            
        vOut.uv0 = float2(0.0f, 0.0f);
        vOut.uv1 = float2(0.0f, 0.0f);
        
        vOut.color = float4(0.0f, 0.0f, 0.0f, 0.0f);
    }
    
    {
        vOut.iid = iid;
        vOut.isActive = isActive;
        vOut.isCull = isCull;
        vOut.state = state;
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
    outPos = (2.0f * ip[i].posW + ip[j].posW) / 3.0f + (-0.15f) * dot((ip[j].posW - ip[i].posW), ni) * ni;  // -0.15
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
    hsOut.tanW = hsIn.tanW;
    
    hsOut.uv0 = hsIn.uv0;
    hsOut.uv1 = hsIn.uv1;
    
    hsOut.color = hsIn.color;
    
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
        
            hsOut.posW      = hsIn.posW;
            hsOut.nomW      = hsIn.nomW;
            hsOut.tanW      = hsIn.tanW;  
        
            hsOut.uv0       = hsIn.uv0;
            hsOut.uv1       = hsIn.uv1;
            hsOut.color     = hsIn.color;            
            
            hsOut.state     = hsIn.state;
            hsOut.isCull    = hsIn.isCull;
            hsOut.isActive  = hsIn.isActive;
            hsOut.iid       = hsIn.iid;                         
            break;
        case 1:
            j = 1;
            hsIn = ip[j];
        
            hsOut.posW = hsIn.posW;
            hsOut.nomW = hsIn.nomW;
            hsOut.tanW = hsIn.tanW;
        
            hsOut.uv0 = hsIn.uv0;
            hsOut.uv1 = hsIn.uv1;
            hsOut.color = hsIn.color;
            
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
            hsOut.tanW = hsIn.tanW;
            
            hsOut.uv0 = hsIn.uv0;
            hsOut.uv1 = hsIn.uv1;
            hsOut.color = hsIn.color;
            
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
        
    float3 tan = p0.tanW * u + p1.tanW * v + p2.tanW * w;
    float2 uv0 = p0.uv0 * u + p1.uv0 * v + p2.uv0 * w;
    float2 uv1 = p0.uv1 * u + p1.uv1 * v + p2.uv1 * w;
    float4 color = p0.color * u + p1.color * v + p2.color * w;
    uint iid = p0.iid;
        
    dOut.posW = pos;
    dOut.nomW = nom;
    dOut.tanW = tan;
    
    dOut.uv0 = uv0;
    dOut.uv1 = uv1;
    dOut.color = color;
    dOut.iid = iid;
    
    dOut.state = p0.state;
    dOut.isCull = p0.isCull;
    dOut.isActive = p0.isActive;       

    return dOut;
}

[maxvertexcount(3)]
void GShader(triangle DS_Out gIn[3],
				inout TriangleStream<GS_Out> triangleStream)
{
    GS_Out gOut[3];
    for (int i = 0; i < 3; i++)
    {
        gOut[i].posC = mul(CV, float4(gIn[i].posW, 1.0f));
        
        gOut[i].posW = gIn[i].posW;
        gOut[i].nomW = gIn[i].nomW;
        gOut[i].tanW = gIn[i].tanW;
        
        gOut[i].uv0 = gIn[i].uv0;
        gOut[i].uv1 = gIn[i].uv1;
        gOut[i].color = gIn[i].color;
        
        gOut[i].state =    gIn[i].state;
        gOut[i].isCull =   gIn[i].isCull;
        gOut[i].isActive = gIn[i].isActive;
        gOut[i].iid = gIn[i].iid;
        triangleStream.Append(gOut[i]);
    }

    triangleStream.RestartStrip();
}

PS_Out_GBuffer PShader_GBuffer(RS_Out pIn)
{
    PS_Out_GBuffer pOut;
    float3 c = float3(0.0f, 1.0f, 0.0f);
    float a = 1.0f;
    
    uint iid = pIn.iid;
    uint isCull = pIn.isCull;
    uint isActive = pIn.isActive;
    
    int state = pIn.state;
    
    if (state < 3 && isCull == 0)
    {
        c = pIn.color.xyz;
              
        bool isMan = pIn.color.x == 0.0f ? true : false;
        
        if (isMan)
        {
            c = tex_diffuse0.Sample(sampler_tex_diffuse0, pIn.uv0).xyz;
        }
        else
        {
            c = tex_diffuse1.Sample(sampler_tex_diffuse1, pIn.uv0).xyz;
        }
        a = 1.0f;
    }
    else //state 3, 4 Dipe, Sleep
    {
        c = float3(0.0f, 0.0f, 0.0f);
        a = 0.0f;
        
        clip(-1);
    }
    
    float3 posW = pIn.posW;
    float zV = mul(V, float4(posW, 1.0f)).z;
    float3 nomW = normalize(pIn.nomW);
    float3 diffuse = c.xyz;
    
    if (a > 0.0f)
    {
        pOut.color0 = float4(posW, 0.0f);
        pOut.color1 = float4(zV, 0.0f, 0.0f, 0.0f);
        pOut.color2 = float4(nomW, 0.0f);
        pOut.color3 = float4(diffuse, a);
    }
    else
    {
        pOut.color0 = float4(0.0f, 0.0f, 0.0f, 0.0f);
        pOut.color1 = float4(-1.0f, 0.0f, 0.0f, 0.0f);
        pOut.color2 = float4(0.0f, 0.0f, 0.0f, 0.0f);
        pOut.color3 = float4(0.0f, 0.0f, 0.0f, 0.0f);
    }
                          
    return pOut;
}


struct GS_Out_Wire
{
    float4 posC : SV_POSITION;
    float3 posW : PosW;
    float3 nomW : NomW;
        
    float4 posCi : TEXCOORD4;
    float4 posC0 : TEXCOORD5;
    float4 posC1 : TEXCOORD6;
    float4 posC2 : TEXCOORD7;
};

struct RS_Out_Wire
{
    float4 posS : SV_POSITION;
    float3 posW : PosW;
    float3 nomW : NomW;
        
    float4 posCi : TEXCOORD4;
    float4 posC0 : TEXCOORD5;
    float4 posC1 : TEXCOORD6;
    float4 posC2 : TEXCOORD7;
};


[maxvertexcount(3)]
void GShader_Wire(triangle DS_Out gin[3], inout TriangleStream<GS_Out_Wire> gout)
{
    GS_Out_Wire v[3];
    float4 ps[3];
    int i;
    int j;
    
    for (i = 0; i < 3; i++)
    {
        float4 posW = float4(gin[i].posW.xyz, 1.0f);
        ps[i] = mul(CV, posW);
        v[i].posW = posW.xyz;
        v[i].posC = ps[i];
        v[i].posCi = ps[i];
        v[i].nomW = gin[i].nomW;
    }
    
    for (i = 0; i < 3; i++)
    {
        v[i].posC0 = ps[0];
        v[i].posC1 = ps[1];
        v[i].posC2 = ps[2];
    }
            
    for (i = 0; i < 3; i++)
    {
        gout.Append(v[i]);
    }
    gout.RestartStrip();
}

PS_Out PShader_Wire(RS_Out_Wire pin)
{
    PS_Out pOut;
                   
    float3 c = float3(0.0f, 1.0f, 0.0f);      
    float NdotL = LightUtil::GetNdotL(pin.posW, pin.nomW);
    
    c = NdotL * c;
       
    {
        float2 vi = mul(S, pin.posCi / pin.posCi.w).xy;
        float2 v0 = mul(S, pin.posC0 / pin.posC0.w).xy;
        float2 v1 = mul(S, pin.posC1 / pin.posC1.w).xy;
        float2 v2 = mul(S, pin.posC2 / pin.posC2.w).xy;
        
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
    
        float range = 0.5f;
        if (d0 < range || d1 < range || d2 < range)
        {
            float d = min(d0, min(d1, d2));
            float alpha = exp(-pow(2.0f * (range * 0.5f - d), 2));
            pOut.color = float4(c, alpha);
        }
        else
        {
            //pOut.color = float4(0.75f, 0.75f, 0.75f, 1.0f);
            pOut.color = float4(0.0f, 0.0f, 0.0f, 0.0f);
        }
    }           
	            
    return pOut;
}