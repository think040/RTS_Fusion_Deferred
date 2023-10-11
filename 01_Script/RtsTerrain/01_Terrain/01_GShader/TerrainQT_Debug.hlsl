#define v4c0 _m00_m10_m20_m30
#define v4c1 _m01_m11_m21_m31
#define v4c2 _m02_m12_m22_m32
#define v4c3 _m03_m13_m23_m33

cbuffer perObject
{
    float4x4 W;
    float4x4 W_csm[4];
    float4x4 Ws_ovf[5];
    float4x4 Ws_pvf[7];
          
    int type; //0 Pvf //1 Ovf //2 tile_Box
    int mode; //0 Pvf-tileBox //1 Ovf-tileBox //2 non-check
    int vfIdx; //mode == 0 -> vfIdx = 0, 1, 2, 3, 4, 5, 6   //mode == 1 -> vfIdx = 0, 1, 2, 3
    int pvfType; // 0 : CAM  1 : CBM
    int ovfType; // 0 : CSM  1 : CBM
}
		
cbuffer perView
{
    float4x4 CV;
    //float4x4 CV_csm[4];
}

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

Texture2D<float4> Pvf_Vtx_Tex;
Texture2D<float4> Box_Vtx_Tex;

Texture3D<float> TestCullPVF_Tex;
Texture3D<float> TestCullOVF_Tex;
Texture3D<float4> TileW_Tex;

float3 tileCount;

struct IA_Out
{
    uint vid : SV_VertexID;
    uint iid : SV_InstanceID;
};

struct VS_Out
{
    float4 posL : SV_POSITION;
    uint iid : SV_InstanceID;
};

struct GS_Out
{
    float4 posC : SV_POSITION;
    int state : STATE;    
    
    uint rid : SV_RenderTargetArrayIndex;
};

struct RS_Out
{
    float4 posS : SV_POSITION;
    int state : STATE;
    
    uint rid : SV_RenderTargetArrayIndex;
};

struct PS_Out
{
    float4 color : SV_Target;
};

int qtIdx;
int qtSize;

float pvfCount;
float ovfCount;

int qtCount;


VS_Out VShader(IA_Out vIn)
{
    VS_Out vOut;
    
    float3 posL;
    
    if (type == 0)
    {
        if(pvfType == 0)
        {
            posL = Pvf_Vtx_Tex[int2(0, vIn.vid)];
        }
        else
        {
            posL = Pvf_Vtx_Tex[int2(1, vIn.vid)];
        }        
    }
    else
    {
        posL = Box_Vtx_Tex[int2(0, vIn.vid)];
    }
    
    vOut.posL = float4(posL, 1.0f);
    vOut.iid = vIn.iid;
    
    return vOut;
}

[maxvertexcount(8)]
void GShader_CSM(line VS_Out gIn[2], inout LineStream<GS_Out> gOut)
{
    int iid = gIn[0].iid;
    int i = 0;
    int j = 0;
    int k = 0;
    
    int cx = (int) tileCount.x;
    int cz = (int) tileCount.z;
            
    int state = 0;
        
    {
        int px = iid % cx;
        int pz = iid / cx;
        
        for (i = 0; i < 4; i++)
        {
            GS_Out v;
            if (TestCullOVF_Tex[int3(px, (qtIdx) * ovfCount + i, pz)] == 1)
            {
                state = 1;
            }
            
            if (state == 1)
            {
                float4x4 _W;
                _W.v4c0 = TileW_Tex[int3(px, qtIdx * 4 + 0, pz)].xyzw;
                _W.v4c1 = TileW_Tex[int3(px, qtIdx * 4 + 1, pz)].xyzw;
                _W.v4c2 = TileW_Tex[int3(px, qtIdx * 4 + 2, pz)].xyzw;
                _W.v4c3 = TileW_Tex[int3(px, qtIdx * 4 + 3, pz)].xyzw;
                
                for (j = 0; j < 2; j++)
                {
                    float4 posW = mul(_W, gIn[j].posL);
                    
                    float4x4 CV = csm_data_Buffer[i].CV;                                       
                    v.posC = mul(CV, posW);                    
                    v.state = state;
                    v.rid = i;
                
                    gOut.Append(v);
                }
                gOut.RestartStrip();
            }
        }
    }
}

[maxvertexcount(12)]
void GShader_CBM(line VS_Out gIn[2], inout LineStream<GS_Out> gOut)
{
    int iid = gIn[0].iid;
    int i = 0;
    int j = 0;
    int k = 0;
    
    int cx = (int) tileCount.x;
    int cz = (int) tileCount.z;
            
    int state = 0;
        
    {
        int px = iid % cx;
        int pz = iid / cx;
        
        for (i = 0; i < 6; i++)
        {
            GS_Out v;
            if (TestCullPVF_Tex[int3(px, (qtIdx) * pvfCount + (i + 1), pz)] == 1)
            {
                state = 1;
            }
            
            if (state == 1)
            {
                float4x4 _W;
                _W.v4c0 = TileW_Tex[int3(px, qtIdx * 4 + 0, pz)].xyzw;
                _W.v4c1 = TileW_Tex[int3(px, qtIdx * 4 + 1, pz)].xyzw;
                _W.v4c2 = TileW_Tex[int3(px, qtIdx * 4 + 2, pz)].xyzw;
                _W.v4c3 = TileW_Tex[int3(px, qtIdx * 4 + 3, pz)].xyzw;
                
                for (j = 0; j < 2; j++)
                {
                    float4 posW = mul(_W, gIn[j].posL);
                    
                    float4x4 CV = cbm_data_Buffer[i].CV;
                    v.posC = mul(CV, posW);
                    v.state = state;
                    v.rid = i;
                
                    gOut.Append(v);
                }
                gOut.RestartStrip();
            }
        }
    }
}

[maxvertexcount(2)]
void GShader_CAM(line VS_Out gIn[2], inout LineStream<GS_Out> gOut)
{
    int iid = gIn[0].iid;
    int i = 0;
    int j = 0;
    int k = 0;
    int m = 0;
    
    //int cx = (int) tileCount.x;
    //int cz = (int) tileCount.z;
    
    int cx = (int) qtSize;
    int cz = (int) qtSize;
    
    int state = 2;
    if (type == 0)       //Pvf
    {
        GS_Out v;
        if (mode == 0)
        {
            state = 0;
            for (m = 0; m < qtCount; m++)
            {
                cz = cx = (int) pow(2.0f, m);
                
                for (j = 0; j < cx; j++)
                {
                    for (k = 0; k < cz; k++)
                    {
                        if (TestCullPVF_Tex[int3(j, m * pvfCount + vfIdx, k)] == 1)
                        {
                            state = 1;
                            j = cx;
                            k = cz;
                            m = qtCount;
                        }
                    }
                }
            }
        }
        
        for (j = 0; j < 2; j++)
        {
            float4 posW;
            if (pvfType == 0)
            {
                posW = mul(Ws_pvf[0], gIn[j].posL);
            }
            else
            {
                posW = mul(Ws_pvf[1 + iid], gIn[j].posL);
            }
            
            
            v.posC = mul(CV, posW);
            v.state = state;
            v.rid = 0;
            gOut.Append(v);
        }
        gOut.RestartStrip();
    }
    else if (type == 1)  //Ovf
    {
        GS_Out v;
        if (mode == 1)
        {
            state = 0;
            for (m = 0; m < qtCount; m++)
            {
                cz = cx = (int) pow(2.0f, m);
                
                for (j = 0; j < cx; j++)
                {
                    for (k = 0; k < cz; k++)
                    {
                        //if (TestCullOVF_Tex[int3(j, (float) (qtCount - 1) * ovfCount + vfIdx, k)] == 1)
                        //if (TestCullOVF_Tex[int3(j, (qtIdx) * ovfCount + vfIdx, k)] == 1)
                        if (TestCullOVF_Tex[int3(j, m * ovfCount + vfIdx, k)] == 1)
                        {
                            state = 1;
                            j = cx;
                            k = cz;
                            m = qtCount;
                        }
                    }
                }
            }
        }
        
        for (j = 0; j < 2; j++)
        {
            //float4 posW = mul(W_csm[vfIdx], gIn[j].posL);
                        
            float4 posW = mul(Ws_ovf[vfIdx], gIn[j].posL);
            
            //float4 posW;
            //if (ovfType == 0)
            //{
            //    posW = mul(Ws_ovf[vfIdx], gIn[j].posL);
            //}
            //else
            //{
            //    posW = mul(Ws_pvf[4 + iid], gIn[j].posL);
            //}
            
            v.posC = mul(CV, posW);
            v.state = state;
            v.rid = 0;
                
            gOut.Append(v);
        }
        gOut.RestartStrip();
    }
    else //if(type == 2)        //TileBox
    {
        int px = iid % cx;
        int pz = iid / cx;
        
        GS_Out v;
        state = 0;
        
        float4x4 _W;
        _W.v4c0 = TileW_Tex[int3(px, qtIdx * 4 + 0, pz)].xyzw;
        _W.v4c1 = TileW_Tex[int3(px, qtIdx * 4 + 1, pz)].xyzw;
        _W.v4c2 = TileW_Tex[int3(px, qtIdx * 4 + 2, pz)].xyzw;
        _W.v4c3 = TileW_Tex[int3(px, qtIdx * 4 + 3, pz)].xyzw;
                
        if (mode == 0)   //TileBox - Pvf
        {
            if (TestCullPVF_Tex[int3(px, (qtIdx) * pvfCount + vfIdx, pz)] == 1)
            {
                state = 1;
            }
        }
        else //(mode == 1) //TileBox - Ovf
        {
            if (TestCullOVF_Tex[int3(px, (qtIdx) * ovfCount + vfIdx, pz)] == 1)
            {
                state = 1;
            }
        }
                       
        if (state == 1)
        {
            for (j = 0; j < 2; j++)
            {
                float4 posW = mul(_W, gIn[j].posL);
                
                //Debug
                //posW = gIn[j].posL;
                    
                v.posC = mul(CV, posW);
                v.state = state;
                v.rid = 0;
                
                gOut.Append(v);
            }
            gOut.RestartStrip();
        }
    }
        
   
}

PS_Out PShader(RS_Out pIn)
{
    PS_Out pOut;
    float3 color = float3(0.0f, 1.0f, 0.0f);
    
    int state = pIn.state;
    
    if (state == 0)
    {
        color = float3(0.0f, 0.0f, 1.0f);
    }
    else if (state == 1)
    {
        color = float3(1.0f, 0.0f, 0.0f);
    }
    
    pOut.color = float4(color, 1.0f);
    
    return pOut;
}


//Test
