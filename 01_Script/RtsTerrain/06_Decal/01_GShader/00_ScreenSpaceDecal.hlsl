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
    
    float4x4 _C;
    float4x4 _CV;
    
    float4x4 T_C;
       
    float4x4 _C_I;
    float4x4 V_I;
    
    float4x4 D;
       
    float4 posW;
    float4 dirW;
    float4 data;
};



float4 countInfo;

StructuredBuffer<Vertex> vtxBuffer;
StructuredBuffer<VertexStatic> vtxBuffer_St;
StructuredBuffer<ViewData> camera;

StructuredBuffer<float4x4> W_I;

Texture2D<float4> depthTex;
SamplerState sampler_depthTex;

Texture2D<float4> decalTex;
SamplerState sampler_decalTex;

int decal_idx;
float4 pixelSize;


//struct DecalInfo
//{
//    int texId;
//    int useLight;
//};
//
//StructuredBuffer<DecalInfo> decalInfo_Buffer;

int vfIdx;

Texture3D<float> TestCullPVF_Tex;

int bAlphaControl;
float alpha;

struct IA_Out
{
    uint vid : SV_VertexID;
    uint iid : SV_InstanceID;
};

struct VS_Out
{
    float4 posC : SV_Position;
    float3 posW : PosW;
    float3 normalW : NomW;
    float3 tangentW : TanW;        
    float4 uv : TEXCOORD;
    
    int state : STATE;
    uint iid : SV_InstanceID;
};

struct RS_Out
{
    float4 posS : SV_Position;
    float3 posW : PosW;
    float3 normalW : NomW;
    float3 tangentW : TanW;
    float4 uv : TEXCOORD;
    
    int state : STATE;
    uint iid : SV_InstanceID;
};

struct PS_Out
{
    float4 target0 : SV_Target0;
};


VS_Out VShader(IA_Out vin)
{
    VS_Out vOut;
    //uint iid = vin.iid;
    uint iid = decal_idx;
    uint vid = vin.vid;
    
    //iid = 0;
    int state = 0;
    if (TestCullPVF_Tex[int3(iid, vfIdx, 0)] == 1)
    {
        state = 1;
    }
    //state = 1;
    
    if(state == 1)
    {
        uint dvCount = (uint) countInfo[0];
        
        Vertex vtx_sk = vtxBuffer[iid * dvCount + vid];
        VertexStatic vtx_st = vtxBuffer_St[vid];
    
        float3 posW = vtx_sk.posW.xyz;
        float3 nomW = vtx_sk.normalW.xyz;
        float3 tanW = vtx_sk.tangentW.xyz;
        float4 uv = vtx_st.uv;
        //float4 uv = 0.0f;
            
        float4 posC = mul(camera[0].CV, float4(posW, 1.0f));
        
        vOut.posC = posC;
        vOut.posW = posW;
        vOut.normalW = nomW;
        vOut.tangentW = tanW;
        vOut.uv = uv;        
    }
    else
    {
        vOut.posC = float4(0.0f, 0.0f, 0.0f, 1.0f);
        vOut.posW = float3(0.0f, 0.0f, 0.0f);
        vOut.normalW =  float3(0.0f, 0.0f, 0.0f);
        vOut.tangentW = float3(0.0f, 0.0f, 0.0f);
        vOut.uv = float4(0.0f, 0.0f, 0.0f, 0.0f);
    }
    
    {
        vOut.state = state;
        vOut.iid = iid;
    }
    
    return vOut;
}

PS_Out PShader_Vspace(RS_Out pin)
{
    PS_Out pout;
    uint iid = pin.iid;
    int state = pin.state;
    
    if (state == 0)
    {
        clip(-1);
    }
    
    ViewData cam = camera[0];
    
    float3 posW = pin.posW;
    float3 posV = mul(cam.V, float4(posW, 1.0f)).xyz;
    float4 posT = mul(cam.T_C, float4(posV, 1.0f));
    
    posT /= posT.w;
    float2 uv = posT.xy;
   
    //float4 depthInfo = depthTex.Sample(sampler_depthTex, uv);
    //float pzV = depthInfo.x;        
    //int layer = (int) depthInfo.y;
    
    float4 depthInfo = depthTex.Load(int3((int2) (uv * pixelSize.xy), 0));
    float pzV = depthInfo.x;
    int layer = (int) depthInfo.y;
    
        
    if (layer == 0)
    {
        clip(-1);
    }
                    
    if (pzV < 0.0f)
    {
        clip(-1);
    }
    
    float4 color = float4(0.0f, 0.0f, 0.0f, 0.0f);
           
    {
        float3 posV0 = posV;
        float3 posV1 = (pzV / posV0.z) * posV0;
        
        float3 posW = mul(cam.V_I, float4(posV1, 1.0f)).xyz;
        float3 posL = mul(W_I[iid], float4(posW, 1.0f)).xyz;
        
        if (abs(posL.x) > 0.5f || abs(posL.y) > 0.5f || abs(posL.z) > 0.5f)
        {
            clip(-1);
        }
                 
        {
            float3 posD = mul(cam.D, float4(posL, 1.0f)).xyz;
            float2 uvD = posD.xz;
        
            //color = float4(decalTex.Sample(sampler_decalTex, uvD).xyz, 0.5f);
            color = decalTex.Sample(sampler_decalTex, uvD);
        }
    }
    
    //color.w = 1.0f;
    //color.w = 0.75f;
    
    if(bAlphaControl == 1)
    {
        color.w = alpha;
    }
    
    
    pout.target0 = color;
        
    //Debug
    {
        //pout.target0 = float4(pz, 0.0f, 0.0f, 1.0f);
        //pout.target0 = float4(posV1, 1.0f);
        //pout.target0 = float4(posV_l, 1.0f);
        
        //pout.target0 = float4(1.0f, 0.0f, 0.0f, 1.0f);
    }
    
    return pout;
}

PS_Out PShader_Nspace(RS_Out pin)
{
    PS_Out pout;
    uint iid = pin.iid;
    
    ViewData cam = camera[0];
    
    float3 posW = pin.posW;
    float3 posV = mul(cam.V, float4(posW, 1.0f)).xyz;
    float4 posT = mul(cam.T_C, float4(posV, 1.0f));
    
    posT /= posT.w;
    float2 uv = posT.xy;
    
    float4 depthInfo = depthTex.Sample(sampler_depthTex, uv);
    float pzN = depthInfo.x;
    int layer = (int) depthInfo.y;
    
    if (layer > 0)
    {
        clip(-1);
    }
    
    if (pzN < 0.0f)
    {
        clip(-1);
    }
    
    float4 posV_ = mul(cam._C_I, float4(0.0f, 0.0f, pzN, 1.0f));
    posV_ /= posV_.w;
    
    float pzV = posV_.z;
                            
    float4 color = float4(0.0f, 0.0f, 0.0f, 0.0f);
       
    {
        float3 posV0 = posV;
        float3 posV1 = (pzV / posV0.z) * posV0;
        
        float3 posW = mul(cam.V_I, float4(posV1, 1.0f)).xyz;
        float3 posL = mul(W_I[iid], float4(posW, 1.0f)).xyz;
        
        if (abs(posL.x) > 0.5f || abs(posL.y) > 0.5f || abs(posL.z) > 0.5f)
        {
            clip(-1);
        }
                
        {
            float3 posD = mul(cam.D, float4(posL, 1.0f)).xyz;
            float2 uvD = posD.xz;
        
            //color = float4(decalTex.Sample(sampler_decalTex, uvD).xyz, 0.5f);
            color = decalTex.Sample(sampler_decalTex, uvD);
        }
    }
              
    pout.target0 = color;
        
    //Debug
    {
        //pout.target0 = float4(pz, 0.0f, 0.0f, 1.0f);
        //pout.target0 = float4(posV1, 1.0f);
        //pout.target0 = float4(posV_l, 1.0f);
        
        //pout.target0 = float4(1.0f, 0.0f, 0.0f, 1.0f);
    }
    
    return pout;
}


//Test
PS_Out PShader_Vspace0(RS_Out pin)
{
    PS_Out pout;
    uint iid = pin.iid;
    
    ViewData cam = camera[0];
    
    float3 posW = pin.posW;
    float3 posV = mul(cam.V, float4(posW, 1.0f)).xyz;
    float4 posT = mul(cam.T_C, float4(posV, 1.0f));
    
    posT /= posT.w;
    float2 uv = posT.xy;
   
    float4 depthInfo = depthTex.Sample(sampler_depthTex, uv);
    float pzV = depthInfo.x;
    int layer = (int) depthInfo.y;
    
    if (layer == 0)
    {
        clip(-1);
    }
                    
    if (pzV < 0.0f)
    {
        clip(-1);
    }
    
    float4 color = float4(0.0f, 0.0f, 0.0f, 0.0f);
       
    {
        float3 posV0 = posV;
        float3 posV1 = (pzV / posV0.z) * posV0;
        
        float3 posW = mul(cam.V_I, float4(posV1, 1.0f)).xyz;
        float3 posL = mul(W_I[iid], float4(posW, 1.0f)).xyz;
        
        if (abs(posL.x) > 0.5f || abs(posL.y) > 0.5f || abs(posL.z) > 0.5f)
        {
            clip(-1);
        }
                
        {
            float3 posD = mul(cam.D, float4(posL, 1.0f)).xyz;
            float2 uvD = posD.xz;
        
            //color = float4(decalTex.Sample(sampler_decalTex, uvD).xyz, 0.5f);
            color = decalTex.Sample(sampler_decalTex, uvD);
        }
    }
    
    //color.w = 1.0f;
    color.w = iid;
    
    pout.target0 = color;
        
    //Debug
    {
        //pout.target0 = float4(pz, 0.0f, 0.0f, 1.0f);
        //pout.target0 = float4(posV1, 1.0f);
        //pout.target0 = float4(posV_l, 1.0f);
        
        //pout.target0 = float4(1.0f, 0.0f, 0.0f, 1.0f);
    }
    
    return pout;
}

PS_Out PShader_Vspace1(RS_Out pin)
{
    PS_Out pout;
    uint iid = pin.iid;
    
    ViewData cam = camera[0];
                  
    float4 color = float4(1.0f, 0.0f, 0.0f, 1.0f);
    
    pout.target0 = color;
            
    
    return pout;
}

PS_Out PShader_Vspace00(RS_Out pin)
{
    PS_Out pout;
    uint iid = pin.iid;
    int state = pin.state;
    
    if (state == 0)
    {
        clip(-1);
    }
    
    ViewData cam = camera[0];
    
    float3 posW = pin.posW;
    float3 posV = mul(cam.V, float4(posW, 1.0f)).xyz;
    float4 posT = mul(cam.T_C, float4(posV, 1.0f));
    
    posT /= posT.w;
    float2 uv = posT.xy;
   
    //float4 depthInfo = depthTex.Sample(sampler_depthTex, uv);
    //float pzV = depthInfo.x;        
    //int layer = (int) depthInfo.y;
    
    float4 depthInfo = depthTex.Load(int3((int2) (uv * pixelSize.xy), 0));
    float pzV = depthInfo.x;
    int layer = (int) depthInfo.y;
    
        
    if (layer == 0)
    {
        clip(-1);
    }
                    
    if (pzV < 0.0f)
    {
        clip(-1);
    }
    
    float4 color = float4(0.0f, 0.0f, 0.0f, 0.0f);
           
    {
        float3 posV0 = posV;
        float3 posV1 = (pzV / posV0.z) * posV0;
        
        float3 posW = mul(cam.V_I, float4(posV1, 1.0f)).xyz;
        float3 posL = mul(W_I[iid], float4(posW, 1.0f)).xyz;
        
        if (abs(posL.x) > 0.5f || abs(posL.y) > 0.5f || abs(posL.z) > 0.5f)
        {
            clip(-1);
        }
                 
        {
            float3 posD = mul(cam.D, float4(posL, 1.0f)).xyz;
            float2 uvD = posD.xz;
        
            //color = float4(decalTex.Sample(sampler_decalTex, uvD).xyz, 0.5f);
            color = decalTex.Sample(sampler_decalTex, uvD);
        }
    }
    
    //color.w = 1.0f;
    color.w = iid;
    
    pout.target0 = color;
        
    //Debug
    {
        //pout.target0 = float4(pz, 0.0f, 0.0f, 1.0f);
        //pout.target0 = float4(posV1, 1.0f);
        //pout.target0 = float4(posV_l, 1.0f);
        
        //pout.target0 = float4(1.0f, 0.0f, 0.0f, 1.0f);
    }
    
    return pout;
}


