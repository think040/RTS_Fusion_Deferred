cbuffer perView
{
    float4x4 CV;
    float3 dirW_view;
    float3 posW_view;    
};

float4 color;

struct IA_Out
{
    float3 posL : POSITION;
    float3 nomL : NORMAL;
    float3 tanL : TANGENT;

    uint vid : SV_VertexID;
    uint iid : SV_InstanceID;
};

struct VS_Out
{
    float4 posC : SV_Position;

    float3 posW : POS;
    float3 nomW : NOM;
    float3 tanW : TAN;
};

struct RS_Out
{
    float4 posS : SV_Position;

    float3 posW : POS;
    float3 nomW : NOM;
    float3 tanW : TAN;
};

struct PS_Out
{
    float4 color : SV_Target;
};


float4 baseVtx;

StructuredBuffer<float4x4> W0_Buffer;
StructuredBuffer<float4x4> W1_Buffer;
StructuredBuffer<float4x4> W2_Buffer;

VS_Out VShader(IA_Out vin)
{
    VS_Out vout;
    
    uint vid = vin.vid;
    uint iid = vin.iid;
    
    float4x4 _W;
    
    if (vid < (uint) baseVtx[1])
    {
        _W = W0_Buffer[iid];
    }
    else if (vid < (uint) baseVtx[2])
    {
        _W = W1_Buffer[iid];
    }
    else   
    {
        _W = W2_Buffer[iid];
    }
    
    float3 posW;
    float3 nomW;
    float3 tanW;
    posW = mul(_W, float4(vin.posL, 1.0f)).xyz;
    nomW = mul((float3x3)_W, vin.nomL);
    tanW = mul((float3x3)_W, vin.tanL);

    vout.posW = posW;
    vout.nomW = nomW;
    vout.tanW = tanW;
    vout.posC = mul(CV, float4(posW, 1.0f));
        
    return vout;
}


PS_Out PShader(RS_Out pin)
{
    PS_Out pout;
    
    float3 posW = pin.posW;
    float3 nomW = normalize(pin.nomW);
    float3 tanW = normalize(pin.tanW);

    float3 toView = normalize(posW_view - posW);

    float4 c = float4(1.0f, 1.0f, 1.0f, 1.0f);
    

    if (dot(nomW, toView) < 0.0f)
    {
        c = 0.25f * color;       
    }
    else
    {
        c = 0.75f * color;
    }
        
    pout.color = c;
    
    return pout;
}