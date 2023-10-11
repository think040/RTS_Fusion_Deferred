#include "../../../Utility/01_GShader/LightUtil.hlsl"

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

//struct LightData
//{
//    float4 posW;
//    float4 dirW;
//    float4 posV;
//    float4 dirV;
//    
//    float4 color;
//    float4 data; //float4(range, intesity, 0.0f, 0.0f)
//};

float4x4 M;
float4 countInfo;

StructuredBuffer<Vertex> vtxBuffer;
StructuredBuffer<VertexStatic> vtxBuffer_St;

//StructuredBuffer<LightData> mLightData_Buffer;

Texture2D<float4> posTex;
Texture2D<float4> depthTex;
Texture2D<float4> nomTex;
Texture2D<float4> diffuseTex;

Texture2D<float4> lightTex;
Texture2D<float4> decalTex;

SamplerState sampler_posTex;
SamplerState sampler_depthTex;
SamplerState sampler_nomTex;
SamplerState sampler_diffuseTex;

SamplerState sampler_lightTex;
SamplerState sampler_decalTex;
 
struct DecalInfo
{
    int texId;
    int useLight;
};

StructuredBuffer<DecalInfo> decalInfo_Buffer;

float4 pixelSize;

struct IA_Out
{
    uint vid : SV_VertexID;
    uint iid : SV_InstanceID;
};

struct VS_Out
{
    float4 posC : SV_Position;
    float2 uv : UV0;
};

struct RS_Out
{
    float4 posS : SV_Position;
    float2 uv : UV0;
};

struct PS_Out
{
    float4 color : SV_Target0;
};


VS_Out VShader(IA_Out vin)
{
    VS_Out vOut;
    uint iid = vin.iid;
    uint vid = vin.vid;
    
    {
        Vertex vtx = vtxBuffer[iid * (uint) (countInfo[0]) + vid];
        VertexStatic vtx_st = vtxBuffer_St[vid];
                
        //vOut.posC = float4(vtx.posW.xyz, 1.0f);
        vOut.posC = mul(M, float4(vtx.posW.xyz, 1.0f));
        vOut.uv = vtx_st.uv.xy;
    }
    
    return vOut;
}


PS_Out PShader(RS_Out pin)
{
    PS_Out pout;
    float2 uv = pin.uv;
        
    float3 posW = posTex.Sample(sampler_posTex, uv).xyz;
    float3 nomW = nomTex.Sample(sampler_nomTex, uv).xyz;
    float zV = depthTex.Sample(sampler_depthTex, uv).x;
    float4 diffuse = diffuseTex.Sample(sampler_diffuseTex, uv);
    
    float3 dColor = diffuse.xyz;
    
    float4 diffuseInfo = diffuseTex.Load(int3((int2) (uv * pixelSize.xy), 0));
    bool isBG = diffuseInfo.w == 0.0f ? true : false;
    //isBG = false; //debug
                       
    float3 color;
    if (isBG)
    {
        color = dColor;
    }
    else
    {
        float3 lColor = lightTex.Sample(sampler_lightTex, uv).xyz;
        float4 decal = decalTex.Sample(sampler_decalTex, uv);
        
        float4 decalInfo = decalTex.Load(int3((int2) (uv * pixelSize.xy), 0));
        
        int decal_idx = decalInfo.w;
        
        bool useLight = true;
        
        if (decal_idx != -1)
        {
            //dColor = decal.xyz;
            dColor.xyz = decal.xyz * decal.w + dColor.xyz * (1.0f - decal.w);
            
            
            //DecalInfo decal_info = decalInfo_Buffer[decal_idx];
            //if (decal_info.useLight == 0)
            //{
            //    useLight = false;
            //    color = dColor;
            //}
        }
        
        //if (useLight)
        {
            nomW = normalize(nomW);
            //nomW = float3(0.0f, 1.0f, 0.0f);
            //posW = float3(0.0f, 0.0f, 0.0f);
        
            float NdotL;
            float sf = LightUtil::GetShadowFactor(posW, nomW, NdotL);
        
            ////Only DLight
            //{
            //    //dColor = NdotL * dColor;
            //
            //    float a;
            //    a = 0.25f;
            //    //a = 1.0f;
            //    //a = 0.5f;
            //
            //    color = a * dColor + (1.0f - a) * lColor;
            //}
            //
            ////Only MLight
            //{
            //    float3 c;
            //    c = dColor;
            //    //c = color;
            //    
            //    
            //    c = 0.25f * c + 0.75f * NdotL * (1.0f - sf) * c;
            //    //c = 0.25f * c + 1.0f * NdotL * (1.0f - sf) * c + 1.0f * lColor;
            //    
            //    color = c;
            //}
        
            //DLight + MLight
            {
                float3 c;
                c = dColor;
                //c = color;
                
                
                //c = 0.25f * c + 0.75f * NdotL * (1.0f - sf) * c;            
                
                int type = LightUtil::GetType();
            
                if (type == 1)  //Directional
                {
                    c = 0.15f * c + 1.0f * NdotL * (1.0f - sf) * c;
                }
                else
                {
                    c = 0.15f * c + 1.0f * NdotL * (1.0f - sf) * c + 5.0f * lColor;
                }
               
                
                color = c;
            }
        }
    }
    
    //Debug
    {
        //color = dColor;
        //color = lColor;
        //color = nomW;
        //color = normalize(mLightData_Buffer[0].dirW.xyz);
        //color = mLightData_Buffer[0].color.xyz;
    }
        
    pout.color = float4(color, 1.0f);
    //pout.color = float4(1.0f, 0.0f, 0.0f, 1.0f);
    
    return pout;
}

//Non-Apply-Decal
PS_Out PShader0(RS_Out pin)
{
    PS_Out pout;
    float2 uv = pin.uv;
        
    float3 posW = posTex.Sample(sampler_posTex, uv).xyz;
    float3 nomW = nomTex.Sample(sampler_nomTex, uv).xyz;
    float zV = depthTex.Sample(sampler_depthTex, uv).x;
    float4 diffuse = diffuseTex.Sample(sampler_diffuseTex, uv);
    
    float3 dColor = diffuse.xyz;
    bool isBG = diffuse.w == 0.0f ? true : false;
    //isBG = false; //debug
                       
    float3 color;
    if (isBG)
    {
        color = dColor;
    }
    else
    {
        float3 lColor = lightTex.Sample(sampler_lightTex, uv).xyz;
            
        nomW = normalize(nomW);
        //nomW = float3(0.0f, 1.0f, 0.0f);
        //posW = float3(0.0f, 0.0f, 0.0f);
        
        float NdotL;
        float sf = LightUtil::GetShadowFactor(posW, nomW, NdotL);
        
        ////Only DLight
        //{
        //    //dColor = NdotL * dColor;
        //
        //    float a;
        //    a = 0.25f;
        //    //a = 1.0f;
        //    //a = 0.5f;
        //
        //    color = a * dColor + (1.0f - a) * lColor;
        //}
        //
        ////Only MLight
        //{
        //    float3 c;
        //    c = dColor;
        //    //c = color;
        //    
        //    
        //    c = 0.25f * c + 0.75f * NdotL * (1.0f - sf) * c;
        //    //c = 0.25f * c + 1.0f * NdotL * (1.0f - sf) * c + 1.0f * lColor;
        //    
        //    color = c;
        //}
        
        //DLight + MLight
        {
            float3 c;
            c = dColor;
            //c = color;
            
            
            //c = 0.25f * c + 0.75f * NdotL * (1.0f - sf) * c;            
            
            int type = LightUtil::GetType();
            
            if (type == 1)
            {
                c = 0.15f * c + 1.0f * NdotL * (1.0f - sf) * c;
            }
            else
            {
                c = 0.15f * c + 1.0f * NdotL * (1.0f - sf) * c + 5.0f * lColor;
            }
           
            
            color = c;
        }
        
        
    }
    
    //Debug
    {
        //color = dColor;
        //color = lColor;
        //color = nomW;
        //color = normalize(mLightData_Buffer[0].dirW.xyz);
        //color = mLightData_Buffer[0].color.xyz;
    }
        
    pout.color = float4(color, 1.0f);
    //pout.color = float4(1.0f, 0.0f, 0.0f, 1.0f);
    
    return pout;
}

PS_Out PShader00(RS_Out pin)
{
    PS_Out pout;
    float2 uv = pin.uv;
        
    float3 posW = posTex.Sample(sampler_posTex, uv).xyz;
    float3 nomW = nomTex.Sample(sampler_nomTex, uv).xyz;
    float zV = depthTex.Sample(sampler_depthTex, uv).x;
    float4 diffuse = diffuseTex.Sample(sampler_diffuseTex, uv);
    
    float3 dColor = diffuse.xyz;
    
    float4 diffuseInfo = diffuseTex.Load(int3((int2) (uv * pixelSize.xy), 0));
    bool isBG = diffuseInfo.w == 0.0f ? true : false;
    //isBG = false; //debug
                       
    float3 color;
    if (isBG)
    {
        color = dColor;
    }
    else
    {
        float3 lColor = lightTex.Sample(sampler_lightTex, uv).xyz;
        float4 decal = decalTex.Sample(sampler_decalTex, uv);
        
        float4 decalInfo = decalTex.Load(int3((int2) (uv * pixelSize.xy), 0));
        
        int decal_idx = decalInfo.w;
        
        bool useLight = true;
        
        if (decal_idx != -1)
        {
            dColor = decal.xyz;
            
            DecalInfo decal_info = decalInfo_Buffer[decal_idx];
            if (decal_info.useLight == 0)
            {
                useLight = false;
                color = dColor;
            }
        }
        
        if (useLight)
        {
            nomW = normalize(nomW);
            //nomW = float3(0.0f, 1.0f, 0.0f);
            //posW = float3(0.0f, 0.0f, 0.0f);
        
            float NdotL;
            float sf = LightUtil::GetShadowFactor(posW, nomW, NdotL);
        
            ////Only DLight
            //{
            //    //dColor = NdotL * dColor;
            //
            //    float a;
            //    a = 0.25f;
            //    //a = 1.0f;
            //    //a = 0.5f;
            //
            //    color = a * dColor + (1.0f - a) * lColor;
            //}
            //
            ////Only MLight
            //{
            //    float3 c;
            //    c = dColor;
            //    //c = color;
            //    
            //    
            //    c = 0.25f * c + 0.75f * NdotL * (1.0f - sf) * c;
            //    //c = 0.25f * c + 1.0f * NdotL * (1.0f - sf) * c + 1.0f * lColor;
            //    
            //    color = c;
            //}
        
            //DLight + MLight
            {
                float3 c;
                c = dColor;
                //c = color;
                
                
                //c = 0.25f * c + 0.75f * NdotL * (1.0f - sf) * c;            
                
                int type = LightUtil::GetType();
            
                if (type == 1)  //Directional
                {
                    c = 0.15f * c + 1.0f * NdotL * (1.0f - sf) * c;
                }
                else
                {
                    c = 0.15f * c + 1.0f * NdotL * (1.0f - sf) * c + 5.0f * lColor;
                }
               
                
                color = c;
            }
        }
    }
    
    //Debug
    {
        //color = dColor;
        //color = lColor;
        //color = nomW;
        //color = normalize(mLightData_Buffer[0].dirW.xyz);
        //color = mLightData_Buffer[0].color.xyz;
    }
        
    pout.color = float4(color, 1.0f);
    //pout.color = float4(1.0f, 0.0f, 0.0f, 1.0f);
    
    return pout;
}