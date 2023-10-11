cbuffer perView
{
    float4x4 CV;
};

cbuffer perLight
{
    float3 dirW_light;
};

struct Vertex
{
    float3 posW;
    float3 normalW;    
};

StructuredBuffer<Vertex> vertex_Out_Buffer;
int dvCount;

StructuredBuffer<int> isSelect_Buffer;

StructuredBuffer<int> has_input_Buffer;

int cullOffset;
Texture3D<float> cullResult_pvf_Texture;

struct IA_Out
{
    uint vid : SV_VertexID;
    uint iid : SV_InstanceID;
};

struct VS_Out
{
    float4 posC : SV_POSITION;
    
    float3 posW : TEXCOORD1;
    float3 normalW : NORMAL;    
    
    uint isSelect : Select;
    uint isCull : ISCULL;
    uint iid : SV_InstanceID;
};

struct RS_Out
{
    float4 posS : SV_POSITION;
    
    float3 posW : TEXCOORD1;
    float3 normalW : NORMAL;    
    
    uint isSelect : Select;
    uint isCull : ISCULL;
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
    
    uint isCull = 0;    
    uint isSelect = 0;   
    
    if (cullResult_pvf_Texture[int3(cullOffset + iid, 0, 0)] == 0.0f)
    {
        isCull = 1;
    }
    
    //if (isSelect_Buffer[iid] == 1)
    if(isSelect_Buffer[iid] == 1 && has_input_Buffer[iid] == 1)
    {
        isSelect = 1;
    }
            
    if(isSelect == 1 && isCull == 0)
    {
        Vertex vtx = vertex_Out_Buffer[iid * dvCount + vid];
            
        vOut.posC = mul(CV, float4(vtx.posW, 1.0f));
    
        vOut.posW = vtx.posW;
        vOut.normalW = vtx.normalW;                    
    }
    else
    {    
        vOut.posC = float4(0.0f, 0.0f, 0.0f, 1.0f);
    
        vOut.posW = float3(0.0f, 0.0f, 0.0f);
        vOut.normalW = float3(0.0f, 0.0f, 0.0f);     
    }
    
    vOut.isSelect = isSelect;
    vOut.isCull = isCull;
    vOut.iid = iid;
        
    return vOut;
}


PS_Out PShader(RS_Out pIn)
{
    PS_Out pOut;
    float3 c;
    uint iid = pIn.iid;
    uint isCull = pIn.isCull;
    uint isSelect = pIn.isSelect;
    
    
    if (isSelect == 1 && isCull == 0)
    {
        float3 nom = normalize(pIn.normalW);
        float NdotL = max(0.5f, dot(nom, dirW_light));
        
        float3 color = float3(0.0f, 1.0f, 0.0f);
        color = NdotL * color;
        
        pOut.color = float4(color, 0.75f);
    }
    else
    {
        pOut.color = float4(0.0f, 0.0f, 0.0f, 0.0f);
    }
   
                      
    return pOut;
}