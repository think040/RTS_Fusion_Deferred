#ifndef LIGHTUTIL_HLSL
#define LIGHTUTIL_HLSL

#define csmCount 4

//for CSM
Texture2DArray csmTexture;
SamplerState sampler_csmTexture;
float csmSize;

//for CBM
TextureCubeArray cbmTexture;
SamplerState sampler_cbmTexture;
float cbmize;


cbuffer perCam
{
    //float4x4 CV;
    float4x4 CV_cam;
    float3 dirW_cam;
    float3 posW_cam;
};

cbuffer forCSM
{               
    float4x4 TCV_csm[csmCount];
    float endZ_csm[csmCount];    
};

cbuffer forCBM
{
    float4x4 Rt;
    float4x4 L_cbm;
};

struct LightData
{
    float intensityFactor;
    float3 posW;
               
    float specularPow;
    float3 dirW;
                
    float4 color;
    
    int type;

    //for Cube            
    float far_plane;
    float range;

    //for Spot        
    float spotAngle_half;
};

StructuredBuffer<LightData> light_Buffer;


struct CSM_depth_data
{
    float4x4 CV;
    float4x4 CV_depth;
};

StructuredBuffer<CSM_depth_data> csm_data_Buffer;

struct CBM_depth_data
{
    float4x4 CV;    
};

StructuredBuffer<CBM_depth_data> cbm_data_Buffer;

struct LightUtil
{            
    static float GetNdotL(float3 posW, float3 nomW)
    {
        LightData data = light_Buffer[0];
        
        float3 L;
        
        if(data.type == 1 ) //Directional
        {
            L = data.dirW;
        }
        else    //Point //Spot
        {
            L = posW - data.posW;
        }
                      
        return   max(0.0f, dot(normalize(nomW), normalize(-L)));        
    }
    
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

    static float GetSpecularFactor(float3 posW, float3 normalW)
    {
        float specularFactor = 2.0f;
        float3 reflectW;
        float3 dirView;
        
        LightData data = light_Buffer[0];
        float3 dirW_light = data.dirW;
        float3 specularPow = data.specularPow;
       
        reflectW = normalize(2 * dot(dirW_light, normalW) * normalW - dirW_light);
        dirView = normalize(posW_cam - posW);
        specularFactor = pow(max(0.0f, dot(reflectW, dirView)), specularPow);
                      
        return specularFactor;
    }
    
    static int GetType()
    {
        LightData data = light_Buffer[0];
        
        return data.type;
    }
    
    static float3 GetDirW()
    {
        LightData data = light_Buffer[0];
        
        return data.dirW;
    }
    
    static float3 GetPosW()
    {
        LightData data = light_Buffer[0];
        
        return data.posW;
    }
    

    //CSM
    static void get_bias_csm(float NdotL, float biasStart, float biasUnit, out float bias)
    {
	//const float biasStart = 0.000003f;		//0.005f;
	//const float biasUnit =  0.000001f;			//0.00001f;
	//const float maxNum = 1.0f;			//10.0f;
		
        const float maxNum = 1.0f;
        bias = biasStart + (1.0f - NdotL) * maxNum * biasUnit;
    }
    
    static float get_dk_csm(float2 l, float2x2 m, float2 r)
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

    static float2x2 get_xyxy_iijj_csm(float4 dv_dx, float4 dv_dy)
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
    
    static float get_shadow_factor_array_csm(Texture2DArray dst, SamplerState samState, int index, int width, int height, float4 posT, float4 dv_dx, float4 dv_dy, float bias, int block, bool b_dk)
    {
        float di = 1.0f / (float) width;
        float dj = 1.0f / (float) height;
                
        di *= 0.1f;
        dj *= 0.1f;
        
        float shadow_factor = 0.0f;

        float2x2 m1;
        if (b_dk)
        {
            m1 = get_xyxy_iijj_csm(dv_dx, dv_dy);
        }
		
        float dk_dx;
        float dk_dy;
        if(b_dk)
        {
            dk_dx = dv_dx.z;
            dk_dy = dv_dy.z;
        }
                
        float k = posT.z;
        float dk1 = 0.0f;
        //int num = 0;
    
        for (int a = -block; a <= block; a++)
        {
            for (int b = -block; b <= block; b++)
            {
                if (b_dk)
                {
                    dk1 = get_dk_csm(
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
                    //num++;
                }
            }
        }

        int count = (2 * block + 1);
        shadow_factor /= (count * count);

        return shadow_factor;
    }
        
    static float GetShadowFactor_CSM(float3 posW, float3 nomW, out float NdotL)
    {
        float sf = 0.0f;
        int i = 0;
        
        LightData data = light_Buffer[0];        
        float3 dirW_light = data.dirW;
               
        NdotL = GetNdotL(posW, nomW);
        //NdotL = 0.1f;
        
        //intensity = 1.0f;
        //intensity = NdotL;
        if (NdotL <= 0.0f)
        {
            sf = 1.0f;
            NdotL = 0.0f;
        }
        else
        {
            NdotL = max(0.0f, NdotL);
        
            float4 posC = mul(CV_cam, float4(posW, 1.0f));
            float depth_view = posC.z / posC.w;
                                                                    
            float shadowFactor[csmCount];
            float4 posT[csmCount];
            float4 dv_dx[csmCount];
            float4 dv_dy[csmCount];
              
            int n = 0;
            for (i = 0; i < csmCount; i++)
            {
                if (depth_view < endZ_csm[i])
                {
                    n = i;
                    break;
                }
            }
            
            bool b_dk = false;
            
            [loop]
            for (i = n; i < csmCount; i++)
            {
                posT[i] = mul(TCV_csm[i], float4(posW, 1.0f));
                posT[i] = posT[i] / posT[i].w;
                
                if(b_dk)
                {
                    dv_dx[i] = ddx(posT[i]);
                    dv_dy[i] = ddy(posT[i]);
                }               
            }
        
            float bias = 0.003f;
            get_bias_csm(
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
            int size = (int) csmSize;
            
            {
                [loop]
                for (i = n; i < csmCount; i++)
                {
                    shadowFactor[i] = 0.0f;
                    shadowFactor[i] = get_shadow_factor_array_csm(
					csmTexture, sampler_csmTexture, i,
					size, size,
					posT[i], dv_dx[i], dv_dy[i],
					bias, block, b_dk);
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
   

    
    //CBM
    static float3 get_vec_cbm(float3 posW, float3 posW_light, float3x3 _Rt)
    {
        float3 vec = posW - posW_light;
        vec = mul(_Rt, vec);               
        
        return vec;
    }
    
    static float3 get_vec_cbm(float3 posW, float4x4 L)
    {
        float3 vec = mul(L, float4(posW, 1.0f)).xyz;
        
        return vec;
    }
        
    static float3 translate_vec_cbm(float3 pos)
    {
        float _pp = dot(pos, float3(+0.0f, +1.0f, +1.0f));
        float _nn = -_pp;
        float _pn = dot(pos, float3(+0.0f, +1.0f, -1.0f));
        float _np = -_pn;

        float p_p = dot(pos, float3(+1.0f, +0.0f, +1.0f));
        float n_n = -p_p;
        float p_n = dot(pos, float3(+1.0f, +0.0f, -1.0f));
        float n_p = -p_n;

        float pp_ = dot(pos, float3(+1.0f, +1.0f, +0.0f));
        float nn_ = -pp_;
        float pn_ = dot(pos, float3(+1.0f, -1.0f, +0.0f));
        float np_ = -pn_;


        if (pp_ >= 0.0f && pn_ >= 0.0f && //+x face
					p_p >= 0.0f && p_n >= 0.0f)
        {
            pos = pos * float3(+1.0f, -1.0, +1.0f);
        }
        else if (pp_ < 0.0f && pn_ < 0.0f && //-x face
					p_p < 0.0f && p_n < 0.0f)
        {
            pos = pos * float3(+1.0f, -1.0, +1.0f);
        }
        else if (pp_ >= 0.0f && np_ >= 0.0f && //+y face
					_pp >= 0.0f && _pn >= 0.0f)
        {
            pos = pos * float3(+1.0f, +1.0, -1.0f);
        }
        else if (pp_ < 0.0f && np_ < 0.0f && //-y face
					_pp < 0.0f && _pn < 0.0f)
        {
            pos = pos * float3(+1.0f, +1.0, -1.0f);
        }
        else if (p_p >= 0.0f && n_p >= 0.0f && //+z face
					_pp >= 0.0f && _np >= 0.0f)
        {
            pos = pos * float3(+1.0f, -1.0, +1.0f);
        }
        else if (p_p < 0.0f && n_p < 0.0f && //-z face
					_pp < 0.0f && _np < 0.0f)
        {
            pos = pos * float3(+1.0f, -1.0, +1.0f);
        }

        return pos;
    }           
            
    static float get_shadow_factor_array_cbm(float3 posW, float3 posW_light,
        TextureCubeArray texCubeArray, SamplerState samplerState, int index, float far_plane, float3x3 _Rt)
    {
        float hNum = 1.0f; //2.0f
        float num = 2 * hNum + 1;
        float hBlockSize = 0.02f; //0.1f // 0.02f
        float offset = hBlockSize / hNum;
        float shadowFactor = 0.0f;
        //float3 center = (posW - posW_light);
        float3 center = get_vec_cbm(posW, posW_light, _Rt);
        //float currentDis = length(center);
                
        for (float x = -hBlockSize; x <= +hBlockSize; x += offset)
        {
            for (float y = -hBlockSize; y <= +hBlockSize; y += offset)
            {
                for (float z = -hBlockSize; z <= +hBlockSize; z += offset)
                {
                    float3 vec = center + float3(x, y, z);
                    //vec = center;
                    
                    //float3 vec1 = normalize(center) + float3(x, y, z);
                    //vec = 5.0f * normalize(vec);
                    //vec = get_vec_cbm(posW, posW_light, _Rt);
                    //float closetDis = (float) texCubeArray.Sample(samplerState, float4(translate_vec_cbm(vec), (float) index));
                    float closetDis = (float) texCubeArray.Sample(samplerState, float4(vec, (float) index));
                    //float closetDis = (float) texCubeArray.Sample(samplerState, float4(translate_vec_cbm(normalize(vec)), (float) index));
                    //float closetDis = (float) texCubeArray.Sample(samplerState, float4(normalize(translate_vec_cbm(vec)), (float) index));
                                        
                    //if (abs(1.0f - closetDis) < 0.1f)
                    //{
                    //    num--;
                    //    continue;
                    //}
                    
                    closetDis *= far_plane;
                    float currentDis = length(vec);
                    //float currentDis = length(center);
                    
                    //shadowFactor += (currentDis > closetDis + 0.0001f) ? 1.0f : 0.0f;
                    shadowFactor += (currentDis > closetDis + 0.1f) ? 1.0f : 0.0f;
                }
            }
        }

        return shadowFactor / ((num) * (num) * (num));
    }
    
    static float3 GetVec_CBM(float3 posW)
    {        
        float3 vec;
        //{
        //    LightData data = light_Buffer[0];
        //    float3 posW_light = data.posW;
        //    float3x3 _Rt = (float3x3) Rt;
        //
        //    //float3 vec = posW - posW_light;
        //    //vec = mul(_Rt, vec);
        //
        //    vec = get_vec_cbm(posW, posW_light, _Rt);
        //}
        
        {
            vec = get_vec_cbm(posW, L_cbm);
        }
        
        
        return vec;
    }
    
    static float GetShadowFactor_CBM(float3 posW, float3 nomW, out float NdotL)
    {
        float sf = 0.0f;
        int i = 0;
        
        LightData data = light_Buffer[0];
        float3 dirW_light = data.dirW;
        float3 posW_light = data.posW;
        float far_plane =   data.far_plane;
        //float3x3 Rt = Rt
                
        NdotL = GetNdotL(posW, nomW);
            
        if (NdotL <= 0.0f)
        {
            sf = 1.0f;
            NdotL = 0.0f;
        }
        else
        {
            sf = get_shadow_factor_array_cbm(posW, posW_light, cbmTexture, sampler_cbmTexture, 0, far_plane, (float3x3) Rt);
        }
        
        
        return sf;
    }        
    
    
    //Light
    static float GetShadowFactor_Directional(float3 posW, float3 nomW, out float NdotL)
    {
        float sf = 0.0f;                
        
        {
            sf = GetShadowFactor_CSM(posW, nomW, NdotL);
        }                
        
        return sf;
    }
    
    static float GetShadowFactor_Point(float3 posW, float3 nomW, out float NdotL)
    {
        float sf = 0.0f;                
        
        LightData data = light_Buffer[0];        
        float3 posW_light = data.posW;
        float3 intensity = data.intensityFactor;
        float3 range = data.range;                
        
        float3 vec = posW - posW_light;
        {
            sf = GetShadowFactor_CBM(posW, nomW, NdotL);
        }
        
        NdotL = intensity * NdotL * pow(exp(-(1.0f / range) * 0.005f * dot(vec, vec)), 3);
        
        return sf;
    }
    
    static float GetShadowFactor_Spot(float3 posW, float3 nomW, out float NdotL)
    {
        float sf = 0.0f;
        
        LightData data = light_Buffer[0];        
        float3 posW_light = data.posW;
        float3 intensity = data.intensityFactor;
        float3 range = data.range;
        
        float3 dirW_light = data.dirW;
        float spotAngle = data.spotAngle_half;
        
        float3 vec = posW - posW_light;
        {
            sf = GetShadowFactor_CBM(posW, nomW, NdotL);
        }
        
        NdotL = intensity * NdotL * pow(exp(-(1.0f / range) * 0.005f * dot(vec, vec)), 3);
                
        float spotIntensity = 0.0f;
        {                                    
            float x0 = cos(spotAngle);
            float x1 = 1.0f;
            float y0 = 0.0f;
            float y1 = 1.0f;
            
            float k = (y1 - y0) / (x1 - x0);
            
            float x = max(x0, dot(normalize(vec), normalize(dirW_light)));
            float y = k * (x - x0) + y0;
                                    
            spotIntensity = max(0.0f, y);
        }
        
        NdotL *= spotIntensity;
                
        return sf;
    }
            
        
    static float GetShadowFactor(float3 posW, float3 nomW, out float NdotL)
    {
        float sf = 0.0f;
        NdotL = 1.0f;
        
        LightData data = light_Buffer[0];
        int type = data.type;
        if(type == 0)
        {
            sf = GetShadowFactor_Spot(posW, nomW, NdotL);
        }
        else if(type == 1)
        {
            sf = GetShadowFactor_Directional(posW, nomW, NdotL);
        }
        else if(type == 2)
        {
            sf = GetShadowFactor_Point(posW, nomW, NdotL);
        }
        
        return sf;
    }
    
    
    
    //Test
    static float get_shadow_factor_array_cbm0(float3 posW, float3 posW_light,
        TextureCubeArray texCubeArray, SamplerState samplerState, int index, float far_plane, float3x3 _Rt)
    {
        float hNum = 1.0f; //2.0f
        float num = 2 * hNum + 1;
        float hBlockSize = 0.02f; //0.1f // 0.02f
        float offset = hBlockSize / hNum;
        float shadowFactor = 0.0f;
        //float3 center = (posW - posW_light);
        float3 center = get_vec_cbm(posW, posW_light, _Rt);
        
        for (float x = -hBlockSize; x <= +hBlockSize; x += offset)
        {
            for (float y = -hBlockSize; y <= +hBlockSize; y += offset)
            {
                for (float z = -hBlockSize; z <= +hBlockSize; z += offset)
                {
                    float3 vec = center + float3(x, y, z);
                    //vec = normalize(vec);
                    //vec = get_vec_cbm(posW, posW_light, _Rt);
                    float closetDis = (float) texCubeArray.Sample(samplerState, float4(translate_vec_cbm(vec), (float) index));
                    //float closetDis = (float) texCubeArray.Sample(samplerState, float4(normalize(translate_vec_cbm(vec)), (float) index));
                    
                    closetDis *= far_plane;
                    float currentDis = length(vec);
                    //shadowFactor += (currentDis > closetDis + 0.0001f) ? 1.0f : 0.0f;
                    shadowFactor += (currentDis > closetDis + 0.1f) ? 1.0f : 0.0f;
                }
            }
        }

        return shadowFactor / ((num) * (num) * (num));
    }
    
    static float GetShadowFactor_CBM_1(float3 posW, float3 nomW, float NdotL)
    {
        float sf = 0.0f;
        int i = 0;
        
        LightData data = light_Buffer[0];
        float3 dirW_light = data.dirW;
        float3 posW_light = data.posW;
        float far_plane = data.far_plane;
        //float3x3 Rt = Rt
                
        //NdotL = GetNdotL(posW, nomW);
            
        if (NdotL <= 0.0f)
        {
            sf = 1.0f;
            NdotL = 0.0f;
        }
        else
        {
            sf = get_shadow_factor_array_cbm(posW, posW_light, cbmTexture, sampler_cbmTexture, 0, far_plane, (float3x3) Rt);
        }
        
        
        return sf;
    }
    
    static float GetShadowFactor_Point_1(float3 posW, float3 nomW, float NdotL)
    {
        float sf = 0.0f;
        
        LightData data = light_Buffer[0];
        float3 posW_light = data.posW;
        float3 intensity = data.intensityFactor;
        float3 range = data.range;
        
        float3 vec = posW - posW_light;
        NdotL = intensity * NdotL * pow(exp(-(1.0f / range) * 0.005f * dot(vec, vec)), 3);
        
        if (NdotL > 0.25f)
        {
            sf = GetShadowFactor_CBM_1(posW, nomW, NdotL);
        }
        else
        {
            sf = 0.0f;
        }                
        
        return sf;
    }
};
#endif