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
    float4x4 TC;
    
    float4 posW;
    float4 dirW;
    float4 data;
};

struct LightData
{
    float4 posW;
    float4 dirW;
    float4 posV;
    float4 dirV;
    
    float4 color;
    float4 data; //float4(range, intesity, 0.0f, 0.0f)
};

int light_idx;

float4 countInfo;

StructuredBuffer<Vertex> vtxBuffer;
StructuredBuffer<VertexStatic> vtxBuffer_St;
StructuredBuffer<LightData> dLightData_Buffer;
StructuredBuffer<ViewData> camera;


Texture2D<float4> posTex;
Texture2D<float4> depthTex;
Texture2D<float4> nomTex;
//Texture2D<float4> lightTex;

SamplerState sampler_posTex;
SamplerState sampler_depthTex;
SamplerState sampler_nomTex;
//SamplerState sampler_lightTex;

int cullOffset;
Texture3D<float> cullResult_pvf_Texture;
StructuredBuffer<float> cullResult_svf_Buffer;
StructuredBuffer<int> active_Buffer;
StructuredBuffer<int> state_Buffer;

StructuredBuffer<float4> sphere_PosV_Buffer;
StructuredBuffer<float4> sphere_PosW_Buffer;

float4 pixelSize;

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
    
    uint isActive : ISACTIVE;
    uint isCull : ISCULL;
    uint iid : SV_InstanceID;
};

struct RS_Out
{
    float4 posS : SV_Position;
    float3 posW : PosW;
    float3 normalW : NomW;
    float3 tangentW : TanW;
    float4 uv : TEXCOORD;
    
    uint isActive : ISACTIVE;
    uint isCull : ISCULL;
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
    uint iid = light_idx;
    uint vid = vin.vid;
    
    //iid = 0;
    
    uint isCull = 1;
    
    //if (cullResult_pvf_Texture[int3(cullOffset + iid, 0, 0)] == 1.0f)
    if (cullResult_pvf_Texture[int3(iid, 0, 0)] == 1.0f)
    {
        isCull = 0;
    }
    
    if (cullResult_svf_Buffer[iid] == 0.0f)
    {
        isCull = 1;
    }
    
    uint isActive = 0;
    if (active_Buffer[iid] == 1)
    {
        isActive = 1;
    }
    
    int state = state_Buffer[iid];
    
    uint dvCount = (uint) countInfo[0];
    
    if (isActive == 1 && isCull == 0 && state < 3)
    {
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
        vOut.normalW = float3(0.0f, 0.0f, 0.0f);
        vOut.tangentW = float3(0.0f, 0.0f, 0.0f);
        vOut.uv = float4(0.0f, 0.0f, 0.0f, 0.0f);
    }
    
    {
        vOut.isActive = isActive;
        vOut.isCull = isCull;
        vOut.iid = iid;
    }
    
    return vOut;
}

PS_Out PShader(RS_Out pin)
{
    PS_Out pout;
        
    uint iid = pin.iid;
    
    uint isActive = pin.isActive;
    uint isCull = pin.isCull;
        
    float4 color = float4(0.0f, 0.0f, 0.0f, 0.0f);
    
    if (isActive == 1 && isCull == 0)
    {
        float3 posW0 = pin.posW;
        float3 nomW0 = pin.normalW;
        float3 posV0 = mul(camera[0].V, float4(posW0, 1.0f)).xyz;
    
        float4 posT = mul(camera[0].TC, float4(posV0, 1.0f));
        posT /= posT.w;
        float2 uv = posT.xy;
        float pz = depthTex.Sample(sampler_depthTex, uv).x;
    
        float3 posW1 = posTex.Sample(sampler_posTex, uv).xyz;
        float3 nomW1 = nomTex.Sample(sampler_nomTex, uv).xyz;
        float3 posV1 = float3(0.0f, 0.0f, pz);
        
        //float4 color_l0 = lightTex.Sample(sam, uv);   
        
        float4 depthInfo = depthTex.Load(int3((int2) (uv * pixelSize.xy), 0));
        float pzV = depthInfo.x;
                
        //if (pz > 0.0f)
        if (pzV > 0.0f)
        {
            posV1 = (posV1.z / posV0.z) * posV0;

            LightData ldata = dLightData_Buffer[light_idx];
            //LightData ldata = dLightData_Buffer[iid];
            float3 posW_l;
            float3 posV_l;
            
            posW_l = sphere_PosW_Buffer[iid].xyz;
            posV_l = sphere_PosV_Buffer[iid].xyz;
    
            float4 color_l1 = ldata.color;
            //float range_l1 = ldata.data.x;
            float range_l1 = sphere_PosW_Buffer[iid].w;
            float intensity_l1 = ldata.data.y;

            float dist = distance(posV1, posV_l);
            //float dist = distance(posW1, posW_l);
                         
            float3 L = normalize(posW_l - posW1);
            nomW1 = normalize(nomW1);
            float NdotL = max(0.25f, dot(nomW1, L));
            //NdotL = 1.0f; //debug
       
            if (dist <= range_l1)
            {
                float rDist = dist / range_l1;
                color_l1.xyz = NdotL * intensity_l1 * color_l1.xyz * exp(-(rDist * rDist) * 2.0f);
                
                //color.xyz = color_l0.xyz + color_l1.xyz;
                color.xyz = color_l1.xyz;
                color.w = 1.0f;
            }
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

VS_Out VShader00(IA_Out vin)
{
    VS_Out vOut;
    //uint iid = vin.iid;
    uint iid = light_idx;
    uint vid = vin.vid;
    
    //iid = 0;
    
    uint isCull = 1;
    
    //if (cullResult_pvf_Texture[int3(cullOffset + iid, 0, 0)] == 1.0f)
    if (cullResult_pvf_Texture[int3(iid, 0, 0)] == 1.0f)
    {
        isCull = 0;
    }
    
    if (cullResult_svf_Buffer[iid] == 0.0f)
    {
        isCull = 1;
    }
    
    uint isActive = 0;
    if (active_Buffer[iid] == 1)
    {
        isActive = 1;
    }
    
    uint dvCount = (uint) countInfo[0];
    
    if (isActive == 1 && isCull == 0)
    {
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
        vOut.normalW = float3(0.0f, 0.0f, 0.0f);
        vOut.tangentW = float3(0.0f, 0.0f, 0.0f);
        vOut.uv = float4(0.0f, 0.0f, 0.0f, 0.0f);
    }
    
    {
        vOut.isActive = isActive;
        vOut.isCull = isCull;
        vOut.iid = iid;
    }
    
    return vOut;
}

VS_Out VShader0(IA_Out vin)
{
    VS_Out vOut;
    //uint iid = vin.iid;
    uint iid = light_idx;
    uint vid = vin.vid;
    
    //iid = 0;
    
    uint isCull = 0;
    
    //if (cullResult_pvf_Texture[int3(cullOffset + iid, 0, 0)] == 1.0f)
    if (cullResult_pvf_Texture[int3(cullOffset + iid, 0, 0)] == 0.0f)
    {
        isCull = 1;
    }
    
    uint isActive = 0;
    if (active_Buffer[iid] == 1)
    {
        isActive = 1;
    }
    
    uint dvCount = (uint) countInfo[0];
    
    if (isActive == 1 && isCull == 0)
    {
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
        vOut.normalW = float3(0.0f, 0.0f, 0.0f);
        vOut.tangentW = float3(0.0f, 0.0f, 0.0f);
        vOut.uv = float4(0.0f, 0.0f, 0.0f, 0.0f);
    }
    
    {
        vOut.isActive = isActive;
        vOut.isCull = isCull;
        vOut.iid = iid;
    }
    
    return vOut;
}

PS_Out PShader0(RS_Out pin)
{
    PS_Out pout;
        
    uint iid = pin.iid;
    
    uint isActive = pin.isActive;
    uint isCull = pin.isCull;
        
    float4 color = float4(0.0f, 0.0f, 0.0f, 0.0f);
    
    if (isActive == 1 && isCull == 0)
    {
        float3 posW0 = pin.posW;
        float3 nomW0 = pin.normalW;
        float3 posV0 = mul(camera[0].V, float4(posW0, 1.0f)).xyz;
    
        float4 posT = mul(camera[0].TC, float4(posV0, 1.0f));
        posT /= posT.w;
        float2 uv = posT.xy;
        float pz = depthTex.Sample(sampler_depthTex, uv).x;
    
        float3 posW1 = posTex.Sample(sampler_posTex, uv).xyz;
        float3 nomW1 = nomTex.Sample(sampler_nomTex, uv).xyz;
        float3 posV1 = float3(0.0f, 0.0f, pz);
        
        //float4 color_l0 = lightTex.Sample(sam, uv);        
        
        if (pz > 0.0f)
        {
            posV1 = (posV1.z / posV0.z) * posV0;

            LightData ldata = dLightData_Buffer[light_idx];
            //LightData ldata = dLightData_Buffer[iid];
            float3 posW_l = ldata.posW.xyz;
            float3 posV_l = ldata.posV.xyz;
            
            //posW_l = sphere_PosW_Buffer[iid];
            //posV_l = sphere_PosV_Buffer[iid];
    
            float4 color_l1 = ldata.color;
            float range_l1 = ldata.data.x;
            float intensity_l1 = ldata.data.y;

            float dist = distance(posV1, posV_l);
            //float dist = distance(posW1, posW_l);
                         
            float3 L = normalize(posW_l - posW1);
            nomW1 = normalize(nomW1);
            float NdotL = max(0.25f, dot(nomW1, L));
            //NdotL = 1.0f; //debug
       
            if (dist <= range_l1)
            {
                float rDist = dist / range_l1;
                color_l1.xyz = NdotL * intensity_l1 * color_l1.xyz * exp(-(rDist * rDist) * 2.0f);
                
                //color.xyz = color_l0.xyz + color_l1.xyz;
                color.xyz = color_l1.xyz;
                color.w = 1.0f;
            }
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
