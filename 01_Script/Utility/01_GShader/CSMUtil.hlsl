#ifndef CSMUTIL_HLSL
#define CSMUTIL_HLSL

#define csmCount 4
sampler2D csmTex0;
sampler2D csmTex1;
sampler2D csmTex2;
sampler2D csmTex3;

Texture2DArray csmTexArray;
SamplerState sampler_csmTexArray;

int csmWidth;
int csmHeight;

float dws[4];
float dhs[4];

cbuffer perObject
{
    float4x4 W;
    float4x4 W_IT;
    float4 color;
};

cbuffer perView
{
    float4x4 CV;
    float4x4 CV_view;
    float3 dirW_view;
    float3 posW_view;
};

cbuffer perLight
{
    //float intensityFactor;
    //float4 light_color;    
    //float3 posW_light;
    float specularPow;
    float3 dirW_light;
       
    float4x4 TCV_light[csmCount];
    float endZ[csmCount];
    int bArray;
};

struct CSMUtil
{
    static void getBias(float NdotL, float biasStart, float biasUnit, out float bias)
    {
	//const float biasStart = 0.000003f;		//0.005f;
	//const float biasUnit =  0.000001f;			//0.00001f;
	//const float maxNum = 1.0f;			//10.0f;
		
        const float maxNum = 1.0f;
        bias = biasStart + (1.0f - NdotL) * maxNum * biasUnit;
    }

    static float getNdotL(float3 normalV, float3 lightDirV)
    {
        return max(0.0f, dot(normalize(normalV), normalize(lightDirV)));
    }



    static float get_dk(float2 l, float2x2 m, float2 r)
    {
        float2 vecL;
        vecL.x = l.x;
        vecL.y = l.y;
        
        float2x2 matM;
        matM = m;

        float2 vecR;
        vecR.x = r.x;
        vecR.y = r.y;
               
        float2x2 ret = mul(vecL, mul(matM, vecR));
        return ret._m00;
    }

    static float2x2 get_xyxy_iijj(float4 dv_dx, float4 dv_dy)
    {
        float di_dx = dv_dx.x;
        float dj_dx = dv_dx.y;
        float di_dy = dv_dy.x;
        float dj_dy = dv_dy.y;

        float det = (dj_dy * di_dx - dj_dx * di_dy);
        float dx_di = +dj_dy;
        float dy_di = -dj_dx;
        float dx_dj = -di_dy;
        float dy_dj = +di_dx;

        float2x2 mat;
        mat._m00 = dx_di;
        mat._m01 = dy_di;
        mat._m10 = dx_dj;
        mat._m11 = dy_dj;

        mat = mat / det;
        return mat;
    }

    static float get_shadow_factor(sampler2D dst, int width, int height, float4 posT, float4 dv_dx, float4 dv_dy, float bias, int block, bool b_dk)
    {
        float di = 1.0f / (float) width;
        float dj = 1.0f / (float) height;

        float shadow_factor = 0.0f;

        float2x2 m1;
        if (b_dk)
        {
            m1 = get_xyxy_iijj(dv_dx, dv_dy);
        }
    
        float dk_dx = dv_dx.z;
        float dk_dy = dv_dy.z;
        float k = posT.z;
        float dk1 = 0.0f;
        int num = 0;
    
        for (int a = -block; a <= block; a++)
        {
            for (int b = -block; b <= block; b++)
            {
                if (b_dk)
                {
                    dk1 = get_dk(
						float2(di * (float) a, dj * (float) b),
						m1,
						float2(dk_dx, dk_dy));
                }
								
                float2 uv = float2(posT.xy + float2(di * (float) a, dj * (float) b));
			
			    {
                    shadow_factor += (
						(k + dk1) > ((float) tex2D(dst, uv) + bias)
						? 1.0f : 0.0f);
                    num++;
                }
            }
        }

        int count = (2 * block + 1);
        shadow_factor /= (count * count);

        return shadow_factor;
    }

    static float get_shadow_factor_array(Texture2DArray dst, SamplerState samState, int index, int width, int height, float4 posT, float4 dv_dx, float4 dv_dy, float bias, int block, bool b_dk)
    {
        float di = 1.0f / (float) width;
        float dj = 1.0f / (float) height;
                
        di *= 0.1f;
        dj *= 0.1f;
        
        float shadow_factor = 0.0f;

        float2x2 m1;
        if (b_dk)
        {
            m1 = get_xyxy_iijj(dv_dx, dv_dy);
        }
		
        float dk_dx = dv_dx.z;
        float dk_dy = dv_dy.z;
        float k = posT.z;
        float dk1 = 0.0f;
        int num = 0;
    
        for (int a = -block; a <= block; a++)
        {
            for (int b = -block; b <= block; b++)
            {
                if (b_dk)
                {
                    dk1 = get_dk(
						float2(di * (float) a, dj * (float) b),
						m1,
						float2(dk_dx, dk_dy));
                }
				
                float2 uv = float2(posT.xy + float2(di * (float) a, dj * (float) b));
                float3 uvw = float3(uv, (float) index);
				
			    {
                    shadow_factor += (
						(k + dk1) > ((float) dst.Sample(samState, uvw) + bias)
						? 1.0f : 0.0f);
                    num++;
                }
            }
        }

        int count = (2 * block + 1);
        shadow_factor /= (count * count);

        return shadow_factor;
    }
    
    
    static float GetShadowFactor_CSM(float3 posW, float3 normalW, out float intensity)
    {
        float sf = 0.0f;
        int i = 0;
        float NdotL = getNdotL(normalW, dirW_light.xyz);
    
        //intensity = 1.0f;
        //intensity = NdotL;
        if (NdotL <= 0.0f)
        {
            sf = 1.0f;
            intensity = 0.0f;
        }
        else
        {
            intensity = max(0.0f, NdotL);
        
            float4 posC = mul(CV_view, float4(posW, 1.0f));
            float depth_view = posC.z / posC.w;
                                                                    
            float shadowFactor[csmCount];
            float4 posT[csmCount];
            float4 dv_dx[csmCount];
            float4 dv_dy[csmCount];
              
            int n = 0;
            for (i = 0; i < csmCount; i++)
            {
                if (depth_view < endZ[i])
                {
                    n = i;
                    break;
                }
            }
            
            [loop]
            for (i = n; i < csmCount; i++)
            {
                posT[i] = mul(TCV_light[i], float4(posW, 1.0f));
                posT[i] = posT[i] / posT[i].w;
                dv_dx[i] = ddx(posT[i]);
                dv_dy[i] = ddy(posT[i]);
            }
        
            float bias = 0.003f;
            getBias(
				NdotL,
				0.0001f, 0.00001f,
				bias);
            
            //getBias(
			//	NdotL,
			//	0.1f, 0.01f,
			//	bias);
            
            //getBias(
			//	normalW, dirW_light.xyz, NdotL,
			//	0.001f, 0.0001f,
			//	bias);
        
            int block = 1;
            bool b_dk = false;
			
            if (bArray == 1)
            {
                [loop]
                for (i = n; i < csmCount; i++)
                {
                    shadowFactor[i] = 0.0f;
                    shadowFactor[i] = get_shadow_factor_array(
					csmTexArray, sampler_csmTexArray, i,
					csmWidth, csmHeight,
					posT[i], dv_dx[i], dv_dy[i],
					bias, block, b_dk);
                }
            }
            else
            {
                [loop]
                for (i = n; i < csmCount; i++)
                {
                    shadowFactor[i] = 0.0f;
                    if (i == 0)
                    {
                        shadowFactor[i] = get_shadow_factor(
						csmTex0, (int) dws[i], (int) dhs[i],
						posT[i], dv_dx[i], dv_dy[i],
						bias, block, b_dk);
                    }
                    else if (i == 1)
                    {
                        shadowFactor[i] = get_shadow_factor(
						csmTex1, (int) dws[i], (int) dhs[i],
						posT[i], dv_dx[i], dv_dy[i],
						bias, block, b_dk);
                    }
                    else if (i == 2)
                    {
                        shadowFactor[i] = get_shadow_factor(
						csmTex2, (int) dws[i], (int) dhs[i],
						posT[i], dv_dx[i], dv_dy[i],
						bias, block, b_dk);
                    }
                    else if (i == 3)
                    {
                        shadowFactor[i] = get_shadow_factor(
						csmTex3, (int) dws[i], (int) dhs[i],
						posT[i], dv_dx[i], dv_dy[i],
						bias, block, b_dk);
                    }
                }
            }
    	    
            [loop]
            for (i = n; i < csmCount; i++)
            {
                sf += shadowFactor[i];
            }
            sf /= (float) (csmCount - n);
        }
               
        return sf;
    }
                
    static float GetSpecularFactor(float3 posW, float3 normalW)
    {
        float specularFactor = 2.0f;
        float3 reflectW;
        float3 dirView;
       
        reflectW = normalize(2 * dot(dirW_light, normalW) * normalW - dirW_light);
        dirView = normalize(posW_view - posW);
        specularFactor = pow(max(0.0f, dot(reflectW, dirView)), specularPow);
                      
        return specularFactor;
    }
};

struct NormalMap
{
    static float3x3 get_TBN_matrix(float3 n, float3 t)
    {
        float3x3 tbn;

        t = t - n * dot(n, t);
        t = normalize(t);
        float3 b = normalize(cross(n, t));

        tbn._m00 = t.x;
        tbn._m01 = b.x;
        tbn._m02 = n.x;
        
        tbn._m10 = t.y;
        tbn._m11 = b.y;
        tbn._m12 = n.y;
        
        tbn._m20 = t.z;
        tbn._m21 = b.z;
        tbn._m22 = n.z;

        return tbn;
    }
};
#endif