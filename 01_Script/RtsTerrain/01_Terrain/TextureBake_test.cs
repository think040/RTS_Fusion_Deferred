using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

using Unity.Mathematics;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif


[CreateAssetMenu(fileName = "TextureBake_test", menuName = "TextureBake_test")]
public class TextureBake_test :  ScriptableObject //MonoBehaviour// ScriptableObject
{
    public bool bBake = false;
    public TerrainData tData;
    public TerrainLayer[] tLayer;
    public Texture2D[] diffuseTex;
    public Texture2D[] normalMapTex;
    public Vector2[] layerSize;
    public Texture2D[] alphaMapTex;
    public string directoryPath = "02_Prefab/TextureBake_test";
    public string diffuseTexFileName = "TerrainDiffuseTex.png";
    public string normalMapTexFileName = "TerrainNormalMapTex.png";

    public Shader gshader;
    Material mte;
    MaterialPropertyBlock mpb;
    int pass;
    int pass_normal;
    Mesh mesh;

    public RenderTexture renTex;
    public RenderTexture renTexNom;
    RenderTargetIdentifier rti;   
    public Vector2Int dimTex = new Vector2Int(1024, 1024);
    
    byte[] imageSrc;
    byte[] normalSrc;

    ComputeBuffer layerSizeBuffer;
    float3 terrainSize;

    

    void OnEnable()
    {
        Debug.Log("OnEnable()");
        RenderGOM.BeginFrameRender += Bake;

        {
            mesh = RenderUtil.CreateRectMesh_UI();
            mte = new Material(gshader);
            mpb = new MaterialPropertyBlock();
            pass = mte.FindPass("TerrainTexBake");
            pass_normal = mte.FindPass("NormalMap");
        }

        {
            terrainSize = tData.size;
            terrainSize.y *= 2.0f;
            mpb.SetVector("terrainSize", new float4(terrainSize, 0.0f));
        }

        {
            tLayer = tData.terrainLayers;
            if (tLayer != null)
            {
                layerSize = new Vector2[tLayer.Length];
                diffuseTex = new Texture2D[tLayer.Length];  
                normalMapTex = new Texture2D[tLayer.Length];
                for (int i = 0; i < tLayer.Length; i++)
                {
                    diffuseTex[i] = tLayer[i].diffuseTexture;
                    normalMapTex[i] = tLayer[i].normalMapTexture;
                    layerSize[i] = tLayer[i].tileSize;

                    mpb.SetTexture("diffuseTex" + i.ToString(), diffuseTex[i]);
                    layerSize[i] = tLayer[i].tileSize;

                    mpb.SetTexture("normalMapTex" + i.ToString(), normalMapTex[i]);
                }

                {
                    layerSizeBuffer = new ComputeBuffer(tLayer.Length, Marshal.SizeOf<Vector2>(), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                    layerSizeBuffer.SetData(layerSize);
                    mpb.SetBuffer("layerSizeBuffer", layerSizeBuffer);                    
                }
            }

            alphaMapTex = tData.alphamapTextures;
            if (alphaMapTex != null)
            {
                for (int i = 0; i < alphaMapTex.Length; i++)
                {
                    mpb.SetTexture("alphamap" + i.ToString(), alphaMapTex[i]);
                }
            }
        }
             
        {
            {
                RenderTextureDescriptor rtd = new RenderTextureDescriptor();
                rtd.msaaSamples = 1;
                rtd.depthBufferBits = 24;
                rtd.enableRandomWrite = false;
                rtd.colorFormat = RenderTextureFormat.ARGB32;
                rtd.dimension = TextureDimension.Tex2D;
                rtd.width = dimTex.x;
                rtd.height = dimTex.y;              
                rtd.volumeDepth = 1;

                renTex = new RenderTexture(rtd);
                rti = new RenderTargetIdentifier(renTex);

                rtd.colorFormat = RenderTextureFormat.ARGBFloat;
                renTexNom = new RenderTexture(rtd);
            }
        
            {
                imageSrc = new byte[dimTex.y * dimTex.x * 4];
                normalSrc = new byte[dimTex.y * dimTex.x * 4 * 4];
            }
        }

    }

    private void OnDestroy()
    {
        Debug.Log("OnDestroy()");

        if(layerSizeBuffer != null)
        {
            layerSizeBuffer.Dispose();
            layerSizeBuffer = null;
        }

        //if(diffuseTex != null)
        //{
        //    for(int i = 0; i < diffuseTex.Length; i++)
        //    {
        //        Object.DestroyImmediate(diffuseTex[i]);
        //    }
        //}
        //
        //if (normalMapTex != null)
        //{
        //    for (int i = 0; i < normalMapTex.Length; i++)
        //    {
        //        Object.DestroyImmediate(normalMapTex[i]);
        //    }
        //}
    }

    void OnDisable()
    {
        Debug.Log("OnDisable()");
        RenderGOM.BeginFrameRender -= Bake;       
    }
   

    void Bake(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cam)
    {
        if(!bBake)
        {            
            RenderGOM.EndFrameRender -= Bake;
            bBake = true;
            Debug.Log("Bake()");

            //CommandBuffer cmd = CommandBufferPool.Get();
            
            {
                //DiffuseTex
                cmd.SetRenderTarget(rti);
                cmd.ClearRenderTarget(true, true, Color.white);

                cmd.DrawMesh(mesh, Matrix4x4.identity, mte, 0, pass, mpb);

                cmd.RequestAsyncReadback(renTex,
                    (read) =>
                    {
                        var na = read.GetData<float>(0);
                        for (int i = 0; i < read.height; i++)
                        {
                            for (int j = 0; j < read.width; j++)
                            {
                                float pixel = na[i * read.width + j];
                                unsafe
                                {
                                    byte* ptr = (byte*)&pixel;
                                    for (int k = 0; k < 4; k++)
                                    {
                                        imageSrc[i * read.width * 4 + j * 4 + k] = ptr[k];
                                    }
                                }
                            }

                            int a = 0;
                        }

                        #if UNITY_EDITOR
                        {
                            byte[] image = ImageConversion.EncodeArrayToPNG(imageSrc, GraphicsFormat.R8G8B8A8_UNorm, (uint)read.width, (uint)read.height);                                                  
                            string path = Application.dataPath + "/" + directoryPath + "/" + diffuseTexFileName;

                            File.WriteAllBytes(path, image);
                        }
                        #endif
                    });
            }

            {
                //NormalMap
                cmd.SetRenderTarget(renTexNom);
                cmd.ClearRenderTarget(true, true, Color.white);


                cmd.DrawMesh(mesh, Matrix4x4.identity, mte, 0, pass_normal, mpb);

                cmd.RequestAsyncReadback(renTexNom,
                    (read) =>
                    {
                        var na = read.GetData<float4>(0);
                        for (int i = 0; i < read.height; i++)
                        {
                            for (int j = 0; j < read.width; j++)
                            {
                                float4 pixel = na[i * read.width + j];
                                unsafe
                                {
                                    byte* ptr = (byte*)&pixel;
                                    for (int k = 0; k < 16; k++)
                                    {
                                        normalSrc[i * read.width * 16 + j * 16 + k] = ptr[k];
                                    }
                                }
                            }
                        }

                        #if UNITY_EDITOR
                        {
                            byte[] image = ImageConversion.EncodeArrayToPNG(normalSrc, GraphicsFormat.R32G32B32A32_SFloat, (uint)read.width, (uint)read.height);                          
                            string path = Application.dataPath + "/" + directoryPath + "/" + normalMapTexFileName;

                            File.WriteAllBytes(path, image);
                        }
                        #endif
                    });
            }            


            //context.ExecuteCommandBuffer(cmd);
            //CommandBufferPool.Release(cmd);           
            
        }
    }


    //Test
    Texture2D finalTex;

    void StoreTexture()
    {
        //if(!bBake)
        {
            //bBake = true;
            //RenderGOM.AfterRender -= StoreTexture;
            Debug.Log("StoreTexture()");
            
            #if UNITY_EDITOR                       
            {
                string path = "E:/GameProgramming/Unity_2022/2022_1_12f1/SRP_Lab/Assets/02_Prefab/TextureBake_test/TerrainFinalTex.png";
                byte[] png = finalTex.EncodeToPNG();               
                File.WriteAllBytes(path, png);
            }
            #endif
        }
    }

    IEnumerator StoreTextuer1()
    {

        yield return new WaitForEndOfFrame();

        //if (!bBake)
        {
            bBake = true;            
            Debug.Log("StoreTexture1()");
            Debug.Log(Application.dataPath);

            #if UNITY_EDITOR                       
            {
                string path = "E:/GameProgramming/Unity_2022/2022_1_12f1/SRP_Lab/Assets/02_Prefab/TextureBake_test/TerrainFinalTex.png";
                path = Application.dataPath + "/02_Prefab/TextureBake_test/TerrainFinalTex.png";

                byte[] png = finalTex.EncodeToPNG();
                File.WriteAllBytes(path, png);
            }
            #endif
        }

    }
}
