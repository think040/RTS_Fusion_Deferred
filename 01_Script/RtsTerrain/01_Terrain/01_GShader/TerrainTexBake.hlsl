
Texture2D diffuseTex0;
SamplerState sampler_diffuseTex0;
Texture2D diffuseTex1;
SamplerState sampler_diffuseTex1;
Texture2D diffuseTex2;
SamplerState sampler_diffuseTex2;
Texture2D diffuseTex3;
SamplerState sampler_diffuseTex3;

Texture2D normalMapTex0;
SamplerState sampler_normalMapTex0;
Texture2D normalMapTex1;
SamplerState sampler_normalMapTex1;
Texture2D normalMapTex2;
SamplerState sampler_normalMapTex2;
Texture2D normalMapTex3;
SamplerState sampler_normalMapTex3;

Texture2D alphamap0;
SamplerState sampler_alphamap0;

StructuredBuffer<float2> layerSizeBuffer;

float3 terrainSize;

float3 ToSNom(float3 input);

struct IA_Out
{
    float3 pos : Position;
    float2 uv : TEXCOORD0;
};

struct VS_Out
{
    float4 posC : SV_Position;
    float2 uv : UV;
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    float2 uv2 : TEXCOORD2;
    float2 uv3 : TEXCOORD3;
};

struct RS_Out
{
    float4 posS : SV_Position;
    float2 uv : UV;
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    float2 uv2 : TEXCOORD2;
    float2 uv3 : TEXCOORD3;
};

struct PS_Out
{
    float4 color : SV_Target;
};

VS_Out VShader(IA_Out vin)
{
    VS_Out vout;
    
    vout.posC = float4(vin.pos, 1.0f);
    
    vout.uv = vin.uv;        
    vout.uv0 = terrainSize.xz * vin.uv / layerSizeBuffer[0].xy;
    vout.uv1 = terrainSize.xz * vin.uv / layerSizeBuffer[1].xy;
    vout.uv2 = terrainSize.xz * vin.uv / layerSizeBuffer[2].xy;
    vout.uv3 = terrainSize.xz * vin.uv / layerSizeBuffer[3].xy;
    
    return vout;
}

PS_Out PShader(RS_Out pin)
{
    PS_Out pout;
    float3 color = float3(0.0f, 0.0f, 1.0f);        
    
    {
        float3 diffuse[4];
        float alpha[4];
        float3 outDiffuse;                  
        
        float4 mask = alphamap0.Sample(sampler_alphamap0, pin.uv);
        alpha[0] = mask.r;
        alpha[1] = mask.g;
        alpha[2] = mask.b;
        alpha[3] = mask.a;
    
        diffuse[0] = diffuseTex0.Sample(sampler_diffuseTex0, pin.uv0);
        diffuse[1] = diffuseTex1.Sample(sampler_diffuseTex1, pin.uv1);
        diffuse[2] = diffuseTex2.Sample(sampler_diffuseTex2, pin.uv2);
        diffuse[3] = diffuseTex3.Sample(sampler_diffuseTex3, pin.uv3);
        
        outDiffuse = alpha[0] * diffuse[0] + alpha[1] * diffuse[1] + alpha[2] * diffuse[2] + alpha[3] * diffuse[3];
        color = outDiffuse;
    }
    
    {
        //color = diffuseTex0.Sample(sampler_diffuseTex0, pin.uv);
    }
    
    pout.color = float4(color, 1.0f);
    
    return pout;
}

PS_Out PShader_NormalMap(RS_Out pin)
{
    PS_Out pout;    
    float3 outNom;
    
    {                
        float3 nom[4];
        float alpha[4];        
        float4 mask = alphamap0.Sample(sampler_alphamap0, pin.uv);
                
        alpha[0] = mask.r;
        alpha[1] = mask.g;
        alpha[2] = mask.b;
        alpha[3] = mask.a;
                        
        nom[0] = normalMapTex0.Sample(sampler_normalMapTex0, pin.uv0).xyz;
        nom[1] = normalMapTex1.Sample(sampler_normalMapTex1, pin.uv1).xyz;
        nom[2] = normalMapTex2.Sample(sampler_normalMapTex2, pin.uv2).xyz;
        nom[3] = normalMapTex3.Sample(sampler_normalMapTex3, pin.uv3).xyz;
                      
        //nom[0] = ToSNom(nom[0]);
        //nom[1] = ToSNom(nom[1]);
        //nom[2] = ToSNom(nom[2]);
        //nom[3] = ToSNom(nom[3]);
                
        float as = alpha[0] + alpha[1] + alpha[2] + alpha[3];
        outNom = normalize((alpha[0] * nom[0] + alpha[1] * nom[1] + alpha[2] * nom[2] + alpha[3] * nom[3]) / as);                
    }   
    
    pout.color = float4(outNom, 1.0f);
    
    return pout;
}


float3 ToSNom(float3 input)
{
    float3 output;
    
    output = 2.0f * input - 1.0f;
    
    return output;
}