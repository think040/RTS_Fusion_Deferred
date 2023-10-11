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

float4x4 M;
float4 countInfo;

StructuredBuffer<Vertex> vtxBuffer;
StructuredBuffer<VertexStatic> vtxBuffer_St;

Texture2D<float4> tex;

SamplerState sampler_tex;

struct IA_Out
{
    uint vid : SV_VertexID;
    uint iid : SV_InstanceID;
};

struct VS_Out
{
    float4 posC : SV_Position;
    float2 uv : TEXCOORD;
    uint iid : SV_InstanceID;
};

struct RS_Out
{
    float4 posS : SV_Position;
    float2 uv : TEXCOORD;
    uint iid : SV_InstanceID;
};

struct PS_Out
{
    float4 color : SV_Target;
};


VS_Out VShader(IA_Out vIn)
{
    VS_Out vOut;
    
    uint vid = vIn.vid;
    uint iid = vIn.iid;
    
    uint vtxCount = (uint) countInfo[0];
    //uint vtxCount = 4;
    
    {
        Vertex vtx = vtxBuffer[iid * vtxCount + vid];
        //Vertex vtx = vtxBuffer[iid * (uint) (4) + vid];
        VertexStatic vtx_st = vtxBuffer_St[vid];
                
        vOut.posC = mul(M, float4(vtx.posW.xyz, 1.0f));
        //vOut.posC = float4(vtx.posW.xyz, 1.0f);
        vOut.uv = vtx_st.uv.xy;
    }
           
    vOut.iid = iid;
    
    return vOut;
}


PS_Out PShader(RS_Out pIn)
{
    PS_Out pOut;
       
    float4 color;
           
    float2 uv = pIn.uv;
            
    {
        color = tex.Sample(sampler_tex, uv);
    }
    
    pOut.color = float4(color.xyz, 1.0f);
    //pOut.color = float4(1.0f, 0.0f, 0.0f, 1.0f);
    
    return pOut;
}