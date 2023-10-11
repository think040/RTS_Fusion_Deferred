using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Unity.Mathematics;


using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;


public static class RenderUtil
{
    public static readonly GraphicsDeviceType gdt;

    enum Type
    {
        d3d = 0,
        gl = 1,
        vk = 2,
    }
    static Type type;

    static RenderUtil()
    {
        RenderUtil.gdt = SystemInfo.graphicsDeviceType;

        if (gdt == GraphicsDeviceType.Direct3D11 || gdt == GraphicsDeviceType.Direct3D12)
        {
            type = Type.d3d;
        }
        else if(gdt == GraphicsDeviceType.OpenGLCore || gdt == GraphicsDeviceType.OpenGLES3)
        {
            type = Type.gl;
        }
        else if(gdt == GraphicsDeviceType.Vulkan)
        {
            type = Type.vk;
        }
    }

    public static Mesh CreateTriangleMesh(float size = 1.0f, float normalAngle = 0.0f)
    {
        Mesh mesh;
        mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals =  new List<Vector3>();
        int[] indices = new int[3];

        float3 backVec = new float3(0.0f, 0.0f, -1.0f);

        //Set vertices
        {           
            vertices.Add(new float3(0.0f, size * math.sin(math.radians(60.0f)) * (2.0f / 3.0f), 0.0f));
            vertices.Add(math.rotate(quaternion.AxisAngle(backVec, math.radians(+120.0f)), vertices[0]));
            vertices.Add(math.rotate(quaternion.AxisAngle(backVec, math.radians(-120.0f)), vertices[0]));
        }

        //Set normals         
        {          
            normals.Add(math.rotate(quaternion.AxisAngle(math.cross(backVec, vertices[0]), normalAngle), backVec));
            normals.Add(math.rotate(quaternion.AxisAngle(math.cross(backVec, vertices[1]), normalAngle), backVec));
            normals.Add(math.rotate(quaternion.AxisAngle(math.cross(backVec, vertices[2]), normalAngle), backVec));
        }

        //Set indices
        {
            indices[0] = 0;
            indices[1] = 1;
            indices[2] = 2;
        }
       
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        
        return mesh;
    }
    public static Mesh CreateCubeMeshWire()
    {
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();

        {
            vertices.Add(new Vector3(-0.5f, -0.5f, +0.5f));
            vertices.Add(new Vector3(-0.5f, +0.5f, +0.5f));
            vertices.Add(new Vector3(+0.5f, +0.5f, +0.5f));
            vertices.Add(new Vector3(+0.5f, -0.5f, +0.5f));
            vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f));
            vertices.Add(new Vector3(-0.5f, +0.5f, -0.5f));
            vertices.Add(new Vector3(+0.5f, +0.5f, -0.5f));
            vertices.Add(new Vector3(+0.5f, -0.5f, -0.5f));
        }

        {
            indices.Add(0);
            indices.Add(1);
            indices.Add(1);
            indices.Add(2);
            indices.Add(2);
            indices.Add(3);
            indices.Add(3);
            indices.Add(0);

            indices.Add(4);
            indices.Add(5);
            indices.Add(5);
            indices.Add(6);
            indices.Add(6);
            indices.Add(7);
            indices.Add(7);
            indices.Add(4);

            indices.Add(0);
            indices.Add(4);
            indices.Add(1);
            indices.Add(5);
            indices.Add(2);
            indices.Add(6);
            indices.Add(3);
            indices.Add(7);
        }

        {
            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
        }

        return mesh;
    }
    public static Mesh CreateTorusMesh(float radiusOut = 1.0f, float radiusIn = 2.0f, int sliceCone = 24, int sliceCircle = 24)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector4> tangents = new List<Vector4>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> indices = new List<int>();

        float r0 = radiusOut;
        float r1 = radiusIn;
        int s0 = sliceCone;
        int s1 = sliceCircle;
        float dtheta = (float)math.radians(360.0f / s0);
        float dphi = (float)math.radians(360.0f / s1);
        float theta = 0.0f;
        float phi = 0.0f;

        for (int i = 0; i < s0; i = i + 1)
        {
            theta = i * dtheta;
            for (int j = 0; j < s1; j = j + 1)
            {
                phi = j * dphi;

                float3 r = new float3(
                     (+r0 * math.sin(theta) + r1) * math.sin(phi),
                     (+r0 * math.cos(theta)),
                     (+r0 * math.sin(theta) + r1) * math.cos(phi)
                 );

                float3 vTheta = new float3(
                        (+r0 * math.cos(theta)) * math.sin(phi),
                        (-r0 * math.sin(theta)),
                        (+r0 * math.cos(theta)) * math.cos(phi)
                        );

                float3 vPhi = new float3(
                        (+r0 * math.sin(theta) + r1) * (+math.cos(phi)),
                        (0.0f),
                        (+r0 * math.sin(theta) + r1) * (-math.sin(phi))
                        );

                float3 vertex = r;
                float3 normal = math.normalize(math.cross(vTheta, vPhi));
                float4 tangent = new float4(math.normalize(vPhi), 0.0f);
                float2 uv = new float2((float)i / (float)s0, (float)j / (float)s1);
                vertices.Add(vertex);
                normals.Add(normal);
                tangents.Add(tangent);
                uvs.Add(uv);
            }
        }

        for (int i = 0; i < s0; i = i + 1)
        {
            for (int j = 0; j < s1; j = j + 1)
            {
                int i0 = i * s1;
                int i1 = ((i + 1) % s0) * s1;
                int j0 = j;
                int j1 = (j + 1) % s1;

                int v00 = i0 + j0;
                int v10 = i1 + j0;
                int v01 = i0 + j1;
                int v11 = i1 + j1;

                indices.Add(v00);
                indices.Add(v11);
                indices.Add(v01);

                indices.Add(v11);
                indices.Add(v00);
                indices.Add(v10);

                //indices.Add(v00);
                //indices.Add(v11);
                //indices.Add(v10);
                //
                //indices.Add(v11);
                //indices.Add(v00);
                //indices.Add(v01);
            }
        }

        //mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTangents(tangents);
        mesh.SetUVs(0, uvs);

        //mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);        
        //mesh.RecalculateBounds();
        //mesh.RecalculateNormals();
        //mesh.RecalculateTangents();        
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);

        return mesh;
    }
    public static Mesh CreateTerrainMeshGrid(float3 size, int3 count)
    {
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector4> tangents = new List<Vector4>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> indices = new List<int>();

        int cx = count.x;
        int cz = count.z;

        float dx = size.x / (float)count.x;
        float dz = size.z / (float)count.z;

        for (int i = 0; i < cz + 1; i++)
        {
            for (int j = 0; j < cx + 1; j++)
            {
                vertices.Add(new Vector3(dx * j, 0.0f, dz * i));
            }
        }

        for (int i = 0; i < cz; i++)
        {
            for (int j = 0; j < cx; j++)
            {
                int i0 = (i + 0) * (cx + 1);
                int i1 = (i + 1) * (cx + 1);
                int j0 = (j + 0);
                int j1 = (j + 1);

                indices.Add(i0 + j0);
                indices.Add(i1 + j0);
                indices.Add(i1 + j1);
                indices.Add(i0 + j1);
            }
        }

        mesh.SetVertices(vertices);
        //mesh.SetNormals(normals);
        //mesh.SetTangents(tangents);
        //mesh.SetUVs(0, uvs);        
        mesh.SetIndices(indices, MeshTopology.Quads, 0);

        return mesh;
    }
    public static Mesh CreateRectMesh_UI()
    {
        Mesh mesh = new Mesh();

        var vertices = new List<Vector3>();
        vertices.Add(new Vector3(-1.0f, -1.0f, 0.0f));
        vertices.Add(new Vector3(-1.0f, +1.0f, 0.0f));
        vertices.Add(new Vector3(+1.0f, +1.0f, 0.0f));
        vertices.Add(new Vector3(+1.0f, -1.0f, 0.0f));
        mesh.SetVertices(vertices);

        var uvs = new List<Vector2>();
        //uvs.Add(new Vector2(+0.0f, +0.0f));
        //uvs.Add(new Vector2(+0.0f, +1.0f));
        //uvs.Add(new Vector2(+1.0f, +1.0f));
        //uvs.Add(new Vector2(+1.0f, +0.0f));

        uvs.Add(new Vector2(+0.0f, +1.0f));
        uvs.Add(new Vector2(+0.0f, +0.0f));
        uvs.Add(new Vector2(+1.0f, +0.0f));
        uvs.Add(new Vector2(+1.0f, +1.0f));
        mesh.SetUVs(0, uvs);

        var indices = new List<int>();
        indices.Add(0);
        indices.Add(2);
        indices.Add(3);
        indices.Add(2);
        indices.Add(0);
        indices.Add(1);
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);

        return mesh;
    }
    public static Mesh CreateSphereMesh_ForArrow(float radius, int sliceCone, int sliceCircle, out List<float> bonePos)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector4> tangents = new List<Vector4>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector4> boneI = new List<Vector4>();
        List<int> indices = new List<int>();
        bonePos = new List<float>();

        float r0 = radius;
        int s0 = 2 * sliceCone;
        int s1 = sliceCircle;
        float dtheta = (float)math.radians(180.0f / s0);
        float dphi = (float)math.radians(360.0f / s1);
        float theta = 0.0f;
        float phi = 0.0f;

        quaternion rot = quaternion.LookRotation(new float3(1.0f, 0.0f, 0.0f), new float3(0.0f, 0.0f, 1.0f));

        for (int i = 0; i <= s0; i = i + 1)
        {
            theta = i * dtheta;
            float3 vertex = float3.zero;
            for (int j = 0; j < s1; j = j + 1)
            {
                phi = j * dphi;

                float3 r = new float3(
                    (+r0 * math.sin(theta)) * math.sin(phi),
                    (+r0 * math.cos(theta)),
                    (+r0 * math.sin(theta)) * math.cos(phi)
                );

                float3 vTheta = new float3(
                        (+r0 * math.cos(theta)) * math.sin(phi),
                        (-r0 * math.sin(theta)),
                        (+r0 * math.cos(theta)) * math.cos(phi)
                        );

                float3 vPhi = new float3(
                        (+r0 * math.sin(theta)) * (+math.cos(phi)),
                        (0.0f),
                        (+r0 * math.sin(theta)) * (-math.sin(phi))
                        );

                vertex = r;
                float3 normal = float3.zero;
                float3 tangent = float3.zero;
                float2 uv = new float2((float)i / (float)s0, (float)j / (float)s1);

                if (i == 0)
                {
                    normal = new float3(0.0f, +1.0f, 0.0f);
                    vPhi = new float3(+math.cos(phi), 0.0f, -math.sin(phi));
                    tangent = vPhi;
                }
                else if (i == s0)
                {
                    normal = new float3(0.0f, -1.0f, 0.0f);
                    vPhi = new float3(+math.cos(phi), 0.0f, -math.sin(phi));
                    tangent = vPhi;
                }
                else
                {
                    normal = math.normalize(math.cross(vTheta, vPhi));
                    tangent = math.normalize(vPhi);
                }

                vertex = math.rotate(rot, vertex);
                normal = math.rotate(rot, normal);
                tangent = math.rotate(rot, tangent);

                vertices.Add(vertex);
                normals.Add(normal);
                tangents.Add(new float4(tangent, 0.0f));
                uvs.Add(uv);
                boneI.Add(new Vector4((float)i, 0.0f, 0.0f, 0.0f));
            }
            bonePos.Add(vertex.z);
        }

        for (int i = 0; i < s0; i = i + 1)
        {
            for (int j = 0; j < s1; j = j + 1)
            {
                int i0 = i * s1;
                int i1 = (i + 1) * s1;
                int j0 = j;
                int j1 = (j + 1) % s1;

                int v00 = i0 + j0;
                int v10 = i1 + j0;
                int v01 = i0 + j1;
                int v11 = i1 + j1;

                indices.Add(v00);
                indices.Add(v11);
                indices.Add(v01);

                indices.Add(v11);
                indices.Add(v00);
                indices.Add(v10);

                //indices.Add(v00);
                //indices.Add(v11);
                //indices.Add(v10);
                //
                //indices.Add(v11);
                //indices.Add(v00);
                //indices.Add(v01);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTangents(tangents);
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(4, boneI);
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);

        return mesh;

    }

    public static Mesh CreateNDCquadMesh()
    {
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> indices = new List<int>();

        {
            vertices.Add(new Vector3(-1.0f, -1.0f, 0.0f));
            vertices.Add(new Vector3(-1.0f, +1.0f, 0.0f));
            vertices.Add(new Vector3(+1.0f, +1.0f, 0.0f));
            vertices.Add(new Vector3(+1.0f, -1.0f, 0.0f));

            uvs.Add(new Vector2(+0.0f, +0.0f));
            uvs.Add(new Vector2(+0.0f, +1.0f));
            uvs.Add(new Vector2(+1.0f, +1.0f));
            uvs.Add(new Vector2(+1.0f, +0.0f));

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
        }

        {
            indices.Add(0);
            indices.Add(1);
            indices.Add(2);

            indices.Add(2);
            indices.Add(3);
            indices.Add(0);

            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        }



        return mesh;
    }

    //Collider Mesh
    public static Mesh CreateBoxMeshWirePartsDetail_Normal(float3 size, out int[] baseVtx)
    {
        Mesh mesh = new Mesh();

        List<Vector3> _vtx = new List<Vector3>();
        List<Vector3> _nom = new List<Vector3>();
        List<Vector3> _tan = new List<Vector3>();
        List<int> _idx = new List<int>();

        {
            _vtx.Add(new Vector3(-1.0f, -1.0f, +1.0f));
            _vtx.Add(new Vector3(-1.0f, +1.0f, +1.0f));
            _vtx.Add(new Vector3(+1.0f, +1.0f, +1.0f));
            _vtx.Add(new Vector3(+1.0f, -1.0f, +1.0f));
        }

        {
            _nom.Add(new Vector3(+0.0f, +0.0f, +1.0f));
            _nom.Add(new Vector3(+0.0f, +0.0f, +1.0f));
            _nom.Add(new Vector3(+0.0f, +0.0f, +1.0f));
            _nom.Add(new Vector3(+0.0f, +0.0f, +1.0f));
        }

        {
            _tan.Add(new Vector3(+0.0f, +1.0f, +0.0f));
            _tan.Add(new Vector3(+1.0f, +0.0f, +0.0f));
            _tan.Add(new Vector3(+0.0f, -1.0f, +0.0f));
            _tan.Add(new Vector3(-1.0f, +0.0f, +0.0f));
        }

        {
            _idx.Add(0);
            _idx.Add(1);
            _idx.Add(1);
            _idx.Add(2);
            _idx.Add(2);
            _idx.Add(3);
            _idx.Add(3);
            _idx.Add(0);
        }

        {
            float3x3 M = float3x3.identity;
            M.c0 *= size.x;
            M.c1 *= size.y;
            M.c2 *= size.z;

            for (int i = 0; i < _vtx.Count; i++)
            {
                _vtx[i] = math.mul(M, _vtx[i]);
            }
        }

        quaternion[] rot = new quaternion[6];
        rot[0] = quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(+0.0f));
        rot[1] = quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(+90.0f));
        rot[2] = quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(+180.0f));
        rot[3] = quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(+270.0f));
        rot[4] = quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.radians(+90.0f));
        rot[5] = quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.radians(-90.0f));


        List<Vector3> vtx = new List<Vector3>();
        List<Vector3> nom = new List<Vector3>();
        List<Vector4> tan = new List<Vector4>();
        List<int> idx = new List<int>();

        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < _vtx.Count; j++)
            {
                vtx.Add(math.rotate(rot[i], _vtx[j]));
                nom.Add(math.rotate(rot[i], _nom[j]));
                tan.Add(new float4(math.rotate(rot[i], _tan[j]), 0.0f));
            }

            for (int j = 0; j < _idx.Count; j++)
            {
                int id = _vtx.Count * i + _idx[j];
                idx.Add(id);
            }
        }

        {
            mesh.SetVertices(vtx);
            mesh.SetNormals(nom);
            mesh.SetTangents(tan);
            mesh.SetIndices(idx, MeshTopology.Lines, 0);
        }

        {
            baseVtx = new int[4];
            baseVtx[0] = 0;
            baseVtx[1] = vtx.Count;
            baseVtx[2] = vtx.Count;
            baseVtx[3] = vtx.Count;
        }

        return mesh;
    }
    
    public static Mesh CreateCapsuleMeshWirePartsDetail_Normal(float radius, float height, int sliceCone2, int sliceCircle4, int sliceCylinder2, out int[] baseVtx)
    {
        Mesh mesh = new Mesh();

        float r0 = radius;
        float hh = 0.5f * height - radius;
        if (hh < 0.0f)
        {
            hh = 0.0f;
        }
        int s0 = 2 * sliceCone2;
        int s1 = 4 * sliceCircle4;
        int s2 = 2 * sliceCylinder2;
        int s3;

        float dtheta = (float)math.radians(90.0f / s0);
        float dphi0 = (float)math.radians(360.0f / 4.0f);
        float dphi1 = (float)math.radians(360.0f / s1);
        float dh = hh / sliceCylinder2;

        float theta;
        float phi;
        float h;

        //HemiSphere
        List<Vector3> vtx0 = new List<Vector3>();
        List<Vector3> nom0 = new List<Vector3>();
        List<Vector4> tan0 = new List<Vector4>();
        List<int> idx0 = new List<int>();
        {
            s0 = 2 * sliceCone2;
            s1 = 4 * sliceCircle4;

            for (int i = 0; i <= s0; i++)
            {
                theta = i * dtheta;
                for (int j = 0; j < s1; j++)
                {
                    phi = j * dphi1;
                    float3 r = new float3(
                      (+r0 * math.sin(theta)) * math.sin(phi),
                      (+r0 * math.cos(theta)),
                      (+r0 * math.sin(theta)) * math.cos(phi));

                    float3 vTheta = new float3(
                        (+r0 * math.cos(theta)) * math.sin(phi),
                        (-r0 * math.sin(theta)),
                        (+r0 * math.cos(theta)) * math.cos(phi)
                        );

                    float3 vPhi = new float3(
                            (+r0 * math.sin(theta)) * (+math.cos(phi)),
                            (0.0f),
                            (+r0 * math.sin(theta)) * (-math.sin(phi))
                            );

                    float3 vertex = r;
                    float3 normal = float3.zero;
                    float3 tangent = float3.zero;

                    if (i == 0)
                    {
                        normal = new float3(0.0f, +1.0f, 0.0f);
                        vPhi = new float3(+math.cos(phi), 0.0f, -math.sin(phi));
                        tangent = vPhi;
                    }
                    else
                    {
                        normal = math.normalize(math.cross(vTheta, vPhi));
                        tangent = math.normalize(vPhi);
                    }

                    vtx0.Add(vertex);
                    nom0.Add(normal);
                    tan0.Add(new float4(tangent, 0.0f));
                }
            }

            for (int i = 0; i <= s0; i++)
            {
                for (int j = 0; j < s1; j++)
                {
                    int i0 = i * s1;
                    int i1 = (i + 1) * s1;
                    int j0 = j;
                    int j1 = (j + 1) % s1;

                    int v00 = i0 + j0;
                    int v10 = i1 + j0;
                    int v01 = i0 + j1;

                    if (i < s0)
                    {
                        idx0.Add(v00);
                        idx0.Add(v10);
                        idx0.Add(v00);
                        idx0.Add(v01);
                    }
                    else
                    {
                        idx0.Add(v00);
                        idx0.Add(v01);
                    }
                }
            }
        }

        //Cylinder
        List<Vector3> vtx1 = new List<Vector3>();
        List<Vector3> nom1 = new List<Vector3>();
        List<Vector4> tan1 = new List<Vector4>();
        List<int> idx1 = new List<int>();
        {
            s0 = 2 * sliceCylinder2;
            s1 = 4 * sliceCircle4;

            for (int i = 0; i <= s0; i++)
            {
                h = hh - i * dh;
                for (int j = 0; j < s1; j++)
                {
                    phi = j * dphi1;
                    float3 r = new float3(
                      (+r0) * math.sin(phi),
                        h,
                      (+r0) * math.cos(phi));

                    float3 vTheta = new float3(0.0f, -1.0f, 0.0f);

                    float3 vPhi = new float3(
                            (+r0) * (+math.cos(phi)),
                            (0.0f),
                            (+r0) * (-math.sin(phi))
                            );

                    float3 vertex = r;
                    float3 normal = float3.zero;
                    float3 tangent = float3.zero;

                    {
                        normal = math.normalize(math.cross(vTheta, vPhi));
                        tangent = math.normalize(vPhi);
                    }

                    vtx1.Add(vertex);
                    nom1.Add(normal);
                    tan1.Add(new float4(tangent, 0.0f));
                }
            }

            for (int i = 0; i <= s0; i++)
            {
                for (int j = 0; j < s1; j++)
                {
                    int i0 = i * s1;
                    //int i1 = (i + 1) * s1;
                    int j0 = j;
                    int j1 = (j + 1) % s1;

                    int v00 = i0 + j0;
                    int v01 = i0 + j1;

                    idx1.Add(v00);
                    idx1.Add(v01);
                }
            }

            //s0 = 1;
            //s1 = 4 * sliceCircle4;
            //s2 = 4;
            //s3 = sliceCircle4;
            for (int i = 0; i < s0; i++)
            {
                for (int j = 0; j < s1; j++)
                {
                    int i0 = i * s1;
                    int i1 = (i + 1) * s1;
                    int j0 = j;
                    //int j1 = (j + 1) % s1;

                    int v00 = i0 + j0;
                    int v10 = i1 + j0;

                    idx1.Add(v00);
                    idx1.Add(v10);
                }
            }
        }

        baseVtx = new int[4];
        {
            baseVtx[0] = 0;
            baseVtx[1] = vtx0.Count;
            baseVtx[2] = baseVtx[1] + vtx1.Count;
            baseVtx[3] = baseVtx[2] + vtx0.Count;
        }

        //Merge
        List<Vector3> vtx = new List<Vector3>();
        List<Vector3> nom = new List<Vector3>();
        List<Vector4> tan = new List<Vector4>();
        List<int> idx = new List<int>();
        {
            for (int i = 0; i < vtx0.Count; i++)
            {
                float3 v = vtx0[i];
                v.y *= +1.0f;

                float3 n = nom0[i];
                n.y *= +1.0f;

                vtx.Add(v);
                nom.Add(n);
                tan.Add(tan0[i]);
            }

            for (int i = 0; i < vtx1.Count; i++)
            {
                vtx.Add(vtx1[i]);
                nom.Add(nom1[i]);
                tan.Add(tan1[i]);
            }

            for (int i = 0; i < vtx0.Count; i++)
            {
                float3 v = vtx0[i];
                v.y *= -1.0f;

                float3 n = nom0[i];
                n.y *= -1.0f;

                vtx.Add(v);
                nom.Add(n);
                tan.Add(tan0[i]);
            }


            for (int i = 0; i < idx0.Count; i++)
            {
                idx.Add(baseVtx[0] + idx0[i]);
            }

            for (int i = 0; i < idx1.Count; i++)
            {
                idx.Add(baseVtx[1] + idx1[i]);
            }

            for (int i = 0; i < idx0.Count; i++)
            {
                idx.Add(baseVtx[2] + idx0[i]);
            }
        }

        {
            mesh.SetVertices(vtx);
            mesh.SetNormals(nom);
            mesh.SetTangents(tan);
            mesh.SetIndices(idx, MeshTopology.Lines, 0);
        }

        return mesh;
    }
    
    public static Mesh CreateSphereMeshWirePartsDetail_Normal(float radius, int sliceCone2, int sliceCircle4, out int[] baseVtx)
    {
        Mesh mesh = new Mesh();

        float r0 = radius;
        int s0 = 2 * sliceCone2;
        int s1 = 4 * sliceCircle4;

        float dtheta = (float)math.radians(180.0f / s0);
        float dphi = (float)math.radians(360.0f / s1);
        float theta;
        float phi;

        List<Vector3> vtx = new List<Vector3>();
        List<Vector3> nom = new List<Vector3>();
        List<Vector4> tan = new List<Vector4>();
        List<int> idx = new List<int>();
        {
            s0 = 2 * sliceCone2;
            s1 = 4 * sliceCircle4;

            for (int i = 0; i <= s0; i++)
            {
                theta = i * dtheta;
                for (int j = 0; j < s1; j++)
                {
                    phi = j * dphi;
                    float3 r = new float3(
                      (+r0 * math.sin(theta)) * math.sin(phi),
                      (+r0 * math.cos(theta)),
                      (+r0 * math.sin(theta)) * math.cos(phi));

                    float3 vTheta = new float3(
                        (+r0 * math.cos(theta)) * math.sin(phi),
                        (-r0 * math.sin(theta)),
                        (+r0 * math.cos(theta)) * math.cos(phi)
                        );

                    float3 vPhi = new float3(
                            (+r0 * math.sin(theta)) * (+math.cos(phi)),
                            (0.0f),
                            (+r0 * math.sin(theta)) * (-math.sin(phi))
                            );

                    float3 vertex = r;
                    float3 normal = float3.zero;
                    float3 tangent = float3.zero;

                    if (i == 0)
                    {
                        normal = new float3(0.0f, +1.0f, 0.0f);
                        vPhi = new float3(+math.cos(phi), 0.0f, -math.sin(phi));
                        tangent = vPhi;
                    }
                    else if (i == s0)
                    {
                        normal = new float3(0.0f, -1.0f, 0.0f);
                        vPhi = new float3(+math.cos(phi), 0.0f, -math.sin(phi));
                        tangent = vPhi;
                    }
                    else
                    {
                        normal = math.normalize(math.cross(vTheta, vPhi));
                        tangent = math.normalize(vPhi);
                    }

                    vtx.Add(vertex);
                    nom.Add(normal);
                    tan.Add(new float4(tangent, 0.0f));
                }
            }

            for (int i = 0; i < s0; i++)
            {
                for (int j = 0; j < s1; j++)
                {
                    int i0 = i * s1;
                    int i1 = (i + 1) * s1;
                    int j0 = j;
                    int j1 = (j + 1) % s1;

                    int v00 = i0 + j0;
                    int v10 = i1 + j0;
                    int v01 = i0 + j1;

                    idx.Add(v00);
                    idx.Add(v10);
                    idx.Add(v00);
                    idx.Add(v01);
                }
            }
        }


        baseVtx = new int[4];
        {
            baseVtx[0] = 0;
            baseVtx[1] = vtx.Count;
            baseVtx[2] = vtx.Count;
            baseVtx[3] = vtx.Count;
        }

        {
            mesh.SetVertices(vtx);
            mesh.SetNormals(nom);
            mesh.SetTangents(tan);
            mesh.SetIndices(idx, MeshTopology.Lines, 0);
        }

        return mesh;
    }
   
    public static Mesh CreateCylinderMeshWirePartsDetail_Normal(float radius, float height, int sliceRadius, int sliceCircle4, int sliceCylinder2, out int[] baseVtx)
    {
        Mesh mesh = new Mesh();

        float r0 = radius;
        float hh = 0.5f * height;
        if (hh < 0.0f)
        {
            hh = 0.0f;
        }
        int s0 = sliceRadius;
        int s1 = 4 * sliceCircle4;
        int s2 = 2 * sliceCylinder2;
        int s3;

        float dr = r0 / s0;
        //float dtheta = (float)math.radians(90.0f / s0);
        float dphi0 = (float)math.radians(360.0f / 4.0f);
        float dphi1 = (float)math.radians(360.0f / s1);
        float dh = hh / sliceCylinder2;

        float r;
        //float theta;
        float phi;
        float h;

        //Circle
        List<Vector3> vtx0 = new List<Vector3>();
        List<Vector3> nom0 = new List<Vector3>();
        List<Vector4> tan0 = new List<Vector4>();
        List<int> idx0 = new List<int>();
        {
            s0 = sliceRadius;
            s1 = 4 * sliceCircle4;

            for (int i = 0; i <= s0; i++)
            {
                r = i * dr;
                for (int j = 0; j < s1; j++)
                {
                    phi = j * dphi1;
                    float3 pos = new float3(
                      (+r) * math.sin(phi),
                      (+hh),
                      (+r) * math.cos(phi));

                    float3 vTheta = new float3(
                        math.sin(phi),
                        0.0f,
                        math.cos(phi)
                        );

                    float3 vPhi = new float3(
                            (+r) * (+math.cos(phi)),
                            (0.0f),
                            (+r) * (-math.sin(phi))
                            );

                    float3 vertex = pos;
                    float3 normal = float3.zero;
                    float3 tangent = float3.zero;

                    if (i == 0)
                    {
                        normal = new float3(0.0f, +1.0f, 0.0f);
                        vPhi = new float3(+math.cos(phi), 0.0f, -math.sin(phi));
                        tangent = vPhi;
                    }
                    else
                    {
                        normal = math.normalize(math.cross(vTheta, vPhi));
                        tangent = math.normalize(vPhi);
                    }

                    vtx0.Add(vertex);
                    nom0.Add(normal);
                    tan0.Add(new float4(tangent, 0.0f));
                }
            }

            for (int i = 0; i <= s0; i++)
            {
                for (int j = 0; j < s1; j++)
                {
                    int i0 = i * s1;
                    int i1 = (i + 1) * s1;
                    int j0 = j;
                    int j1 = (j + 1) % s1;

                    int v00 = i0 + j0;
                    int v10 = i1 + j0;
                    int v01 = i0 + j1;

                    if (i < s0)
                    {
                        idx0.Add(v00);
                        idx0.Add(v10);
                        idx0.Add(v00);
                        idx0.Add(v01);
                    }
                    else
                    {
                        idx0.Add(v00);
                        idx0.Add(v01);
                    }
                }
            }
        }

        //Cylinder
        List<Vector3> vtx1 = new List<Vector3>();
        List<Vector3> nom1 = new List<Vector3>();
        List<Vector4> tan1 = new List<Vector4>();
        List<int> idx1 = new List<int>();
        {
            s0 = 2 * sliceCylinder2;
            s1 = 4 * sliceCircle4;

            for (int i = 0; i <= s0; i++)
            {
                h = hh - i * dh;
                for (int j = 0; j < s1; j++)
                {
                    phi = j * dphi1;
                    float3 pos = new float3(
                      (+r0) * math.sin(phi),
                        h,
                      (+r0) * math.cos(phi));

                    float3 vTheta = new float3(0.0f, -1.0f, 0.0f);

                    float3 vPhi = new float3(
                            (+r0) * (+math.cos(phi)),
                            (0.0f),
                            (+r0) * (-math.sin(phi))
                            );

                    float3 vertex = pos;
                    float3 normal = float3.zero;
                    float3 tangent = float3.zero;

                    {
                        normal = math.normalize(math.cross(vTheta, vPhi));
                        tangent = math.normalize(vPhi);
                    }

                    vtx1.Add(vertex);
                    nom1.Add(normal);
                    tan1.Add(new float4(tangent, 0.0f));
                }
            }

            for (int i = 0; i <= s0; i++)
            {
                for (int j = 0; j < s1; j++)
                {
                    int i0 = i * s1;
                    //int i1 = (i + 1) * s1;
                    int j0 = j;
                    int j1 = (j + 1) % s1;

                    int v00 = i0 + j0;
                    int v01 = i0 + j1;

                    idx1.Add(v00);
                    idx1.Add(v01);
                }
            }

            //s0 = 1;
            //s1 = 4 * sliceCircle4;
            //s2 = 4;
            //s3 = sliceCircle4;
            for (int i = 0; i < s0; i++)
            {
                for (int j = 0; j < s1; j++)
                {
                    int i0 = i * s1;
                    int i1 = (i + 1) * s1;
                    int j0 = j;
                    //int j1 = (j + 1) % s1;

                    int v00 = i0 + j0;
                    int v10 = i1 + j0;

                    idx1.Add(v00);
                    idx1.Add(v10);
                }
            }
        }

        baseVtx = new int[4];
        {
            baseVtx[0] = 0;
            baseVtx[1] = vtx0.Count;
            baseVtx[2] = baseVtx[1] + vtx1.Count;
            baseVtx[3] = baseVtx[2] + vtx0.Count;
        }

        //Merge
        List<Vector3> vtx = new List<Vector3>();
        List<Vector3> nom = new List<Vector3>();
        List<Vector4> tan = new List<Vector4>();
        List<int> idx = new List<int>();
        {
            for (int i = 0; i < vtx0.Count; i++)
            {
                float3 v = vtx0[i];
                v.y *= +1.0f;

                float3 n = nom0[i];
                n.y *= +1.0f;

                vtx.Add(v);
                nom.Add(n);
                tan.Add(tan0[i]);
            }

            for (int i = 0; i < vtx1.Count; i++)
            {
                vtx.Add(vtx1[i]);
                nom.Add(nom1[i]);
                tan.Add(tan1[i]);
            }

            for (int i = 0; i < vtx0.Count; i++)
            {
                float3 v = vtx0[i];
                v.y *= -1.0f;

                float3 n = nom0[i];
                n.y *= -1.0f;

                vtx.Add(v);
                nom.Add(n);
                tan.Add(tan0[i]);
            }


            for (int i = 0; i < idx0.Count; i++)
            {
                idx.Add(baseVtx[0] + idx0[i]);
            }

            for (int i = 0; i < idx1.Count; i++)
            {
                idx.Add(baseVtx[1] + idx1[i]);
            }

            for (int i = 0; i < idx0.Count; i++)
            {
                idx.Add(baseVtx[2] + idx0[i]);
            }
        }

        {
            for (int i = 0; i < vtx.Count; i++)
            {
                float3 pos = vtx[i];
                pos.y += hh;
                vtx[i] = pos;
            }
        }

        {
            mesh.SetVertices(vtx);
            mesh.SetNormals(nom);
            mesh.SetTangents(tan);
            mesh.SetIndices(idx, MeshTopology.Lines, 0);
        }

        {
            baseVtx[0] = 0;
            baseVtx[1] = vtx.Count;
            baseVtx[2] = vtx.Count;
            baseVtx[3] = vtx.Count;
        }

        return mesh;
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 GetPfromC(Transform tr)
    {
        float4x4 P = float4x4.identity;
        float3 pos = tr.localPosition;
        float3x3 R = new float3x3(tr.localRotation);
        float3 sca = tr.localScale;

        P.c0 = new float4(sca.x * R.c0, 0.0f);
        P.c1 = new float4(sca.y * R.c1, 0.0f);
        P.c2 = new float4(sca.z * R.c2, 0.0f);
        P.c3 = new float4(pos, 1.0f);

        return P;
    }

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 GetWfromL(Transform localTr)
    {
        float4x4 W = float4x4.identity;
        W = GetPfromC(localTr);
        Transform tr = localTr;
        while (tr.parent != null)
        {
            W = math.mul(GetPfromC(tr.parent), W);
            tr = tr.parent;
        }

        return W;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 GetVfromW(Camera cam)
    {
        float4x4 V = float4x4.identity;

        quaternion rot = cam.transform.rotation;
        float3x3 rotM = new float3x3(rot);
        float3 pos = cam.transform.position;

        float3 t = -new float3(math.dot(rotM.c0, pos), math.dot(rotM.c1, pos), math.dot(rotM.c2, pos));
        V.c0 = new float4(rotM.c0, t.x);
        V.c1 = new float4(rotM.c1, t.y);
        V.c2 = new float4(rotM.c2, t.z);
        V.c3 = new float4(0.0f, 0.0f, 0.0f, 1.0f);
        V = math.transpose(V);       

        return V;        
    }
   
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 GetCfromV(Camera cam, bool toTex = false)
    {
        float4x4 C = float4x4.identity;

        float fov = cam.fieldOfView;
        float aspect = cam.aspect;
        float near = cam.nearClipPlane;
        float far = cam.farClipPlane;

        float hv = cam.orthographicSize;       

        float4x4 M = float4x4.identity;
        float3 t = float3.zero;
        float3 s = float3.zero;

#if DirectX        
        {
            if (!cam.orthographic)
            {
                float cotFov = 1.0f / math.tan(math.radians(fov / 2.0f));
                C.c0.x = (1.0f / aspect) * cotFov;
                C.c1.y = cotFov;

                C.c2.z = +far / (far - near);
                C.c3.z = -(far * near / (far - near));
                C.c2.w = 1.0f;
                C.c3.w = 0.0f;
            }
            else
            {               
                s = new float3(1.0f, 1.0f, 1.0f) / new float3(aspect * hv, hv, far - near);
                t = new float3(0.0f, 0.0f, -near);

                C.c0.x = s.x;               
                C.c1.y = s.y;
                C.c2.z = s.z;
                C.c3 = new float4(s * t, 1.0f);
            }

            if (toTex)
            {
                t = new float3(+0.0f, +0.0f, +1.0f);
                s = new float3(+1.0f, -1.0f, -1.0f);
            }
        }

#elif OpenGL        
        {
            if (!cam.orthographic)
            {
                float cotFov = 1.0f / math.tan(math.radians(fov / 2.0f));
                C.c0.x = (1.0f / aspect) * cotFov;
                C.c1.y = cotFov;
        
                C.c2.z = (far + near) / (far - near);
                C.c3.z = -(2 * far * near) / (far - near);
                C.c2.w = 1.0f;
                C.c3.w = 0.0f;                       
            }
            else
            {
                s = new float3(1.0f, 1.0f, 2.0f) / new float3(aspect * hv, hv, far - near);
                t = new float3(0.0f, 0.0f, (far + near) * (-0.5f));                     
        
                C.c0.x = s.x;
                C.c1.y = s.y;
                C.c2.z = s.z;
                C.c3 = new float4(s * t, 1.0f);
            }
        
            if (toTex)
            {
                t = new float3(+0.0f, +0.0f, +0.0f);
                s = new float3(+1.0f, +1.0f, +1.0f);
            }
        }

#elif Vulkan       
        {
            if (!cam.orthographic)
            {
                float cotFov = 1.0f / math.tan(math.radians(fov / 2.0f));
                C.c0.x = (1.0f / aspect) * cotFov;
                C.c1.y = cotFov;
                       
                C.c2.z = +far / (far - near);
                C.c3.z = -(far * near / (far - near));
                C.c2.w = 1.0f;
                C.c3.w = 0.0f;
            }
            else
            {                
                s = new float3(1.0f, 1.0f, 1.0f) / new float3(aspect * hv, hv, far - near);
                t = new float3(0.0f, 0.0f, -near);
        
                C.c0.x = s.x;
                C.c1.y = s.y;
                C.c2.z = s.z;
                C.c3 = new float4(s * t, 1.0f);
            }
        
            if (toTex)
            {
                t = new float3(+0.0f, +0.0f, +1.0f);
                s = new float3(+1.0f, -1.0f, -1.0f);
            }
        }
#endif

        if (toTex)
        {
            M.c3 = new float4(t, 1.0f);
            M.c0.x = s.x;
            M.c1.y = s.y;
            M.c2.z = s.z;

            C = math.mul(M, C);
        }

        return C;        
    }
   
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 GetSfromN(Camera cam, bool toTex = false)
    {
        float4x4 S = float4x4.identity;
        Rect pRect = cam.pixelRect;
        float x = pRect.x;
        float y = pRect.y;
        float w = pRect.width;
        float h = pRect.height;

        float3 t = float3.zero;
        float3 s = float3.zero;

        float4x4 M = float4x4.identity;
#if DirectX
        {
            t = new float3(new float2(x, y) + new float2(w, h) * 0.5f, 0.0f);
            s = new float3(new float2(w, h) * 0.5f, 1.0f);

            S.c3 = new float4(t, 1.0f);
            S.c0.x = s.x;
            S.c1.y = s.y;
            S.c2.z = s.z;

            if (toTex)
            {
                s = new float3(+1.0f, -1.0f, -1.0f);
                t = new float3(+0.0f, +0.0f, -1.0f);
            }
        }

#elif OpenGL       
        {
            t = new float3(new float2(x, y) + new float2(w, h) * 0.5f, 0.5f);
            s = new float3(new float2(w, h) * 0.5f, 0.5f);           

            S.c3 = new float4(t, 1.0f);
            S.c0.x = s.x;
            S.c1.y = s.y;
            S.c2.z = s.z;

            if (toTex)
            {
                s = new float3(+1.0f, +1.0f, +1.0f);
                t = new float3(+0.0f, +0.0f, +0.0f);
            }
        }

#elif Vulkan       
        {
            t = new float3(new float2(x, y) + new float2(w, h) * 0.5f, 0.0f);
            s = new float3(new float2(w, h) * 0.5f, 1.0f);

            S.c3 = new float4(t, 1.0f);
            S.c0.x = s.x;
            S.c1.y = s.y;
            S.c2.z = s.z;

            if (toTex)
            {
                s = new float3(+1.0f, -1.0f, -1.0f);
                t = new float3(+0.0f, +0.0f, -1.0f);
            }
        }
#endif

        if (toTex)
        {
            M.c0.x = s.x;
            M.c1.y = s.y;
            M.c2.z = s.z;
            M.c3 = new float4(s * t, 1.0f);

            S = math.mul(S, M);
        }

        return S;
    }


    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 GetNfromS(Camera cam, bool toTex = false)
    {
        float4x4 N = float4x4.identity;
        Rect pRect = cam.pixelRect;
        float x = pRect.x;
        float y = pRect.y;
        float w = pRect.width;
        float h = pRect.height;

        float3 t = float3.zero;
        float3 s = float3.zero;

        float4x4 M = float4x4.identity;

#if DirectX
        {
            s = new float3(2.0f / new float2(w, h), 1.0f);
            t = new float3(-(new float2(x, y) + new float2(w, h) * 0.5f), 0.0f);

            N.c0.x = s.x;
            N.c1.y = s.y;
            N.c2.z = s.z;
            N.c3 = new float4(s * t, 1.0f);

            if (toTex)
            {
                t = new float3(+0.0f, +0.0f, +1.0f);
                s = new float3(+1.0f, -1.0f, -1.0f);
            }
        }

#elif OpenGL
        {
            s = new float3(2.0f / new float2(w, h), 2.0f);
            t = new float3(-(new float2(x, y) + new float2(w, h) * 0.5f), -0.5f);           

            N.c0.x = s.x;
            N.c1.y = s.y;
            N.c2.z = s.z;
            N.c3 = new float4(s * t, 1.0f);

            if (toTex)
            {
                t = new float3(+0.0f, +0.0f, +0.0f);
                s = new float3(+1.0f, +1.0f, +1.0f);
            }            
        }

#elif Vulkan
        {
            s = new float3(2.0f / new float2(w, h), 1.0f);
            t = new float3(-(new float2(x, y) + new float2(w, h) * 0.5f), 0.0f);

            N.c0.x = s.x;
            N.c1.y = s.y;
            N.c2.z = s.z;
            N.c3 = new float4(s * t, 1.0f);

            if (toTex)
            {
                t = new float3(+0.0f, +0.0f, +1.0f);
                s = new float3(+1.0f, -1.0f, -1.0f);
            }            
        }
#endif
        
        if (toTex)
        {
            M.c3 = new float4(t, 1.0f);
            M.c0.x = s.x;
            M.c1.y = s.y;
            M.c2.z = s.z;

            N = math.mul(M, N);
        }

        return N;
    }    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 GetVfromC(Camera cam, bool toTex = false)
    {
        float4x4 V = float4x4.identity;

        float fov = cam.fieldOfView;
        float aspect = cam.aspect;
        float near = cam.nearClipPlane;
        float far = cam.farClipPlane;

        float hv = cam.orthographicSize;       

        float4x4 M = float4x4.identity;
        float3 s = float3.zero;
        float3 t = float3.zero;

#if DirectX
        {
            if (!cam.orthographic)
            {
                float tanFov = math.tan(math.radians(fov / 2.0f));
                V.c0.x = aspect * tanFov;
                V.c1.y = tanFov;

                V.c2.z = 0.0f;
                V.c3.z = 1.0f;
                V.c2.w = -(far - near) / (far * near);
                V.c3.w = (far) / (far * near);
            }
            else
            {
                t = new float3(0.0f, 0.0f, near);
                s = new float3(hv, hv, (far - near)) * new float3(aspect, 1.0f, 1.0f);

                V.c3 = new float4(t, 1.0f);
                V.c0.x = s.x;
                V.c1.y = s.y;
                V.c2.z = s.z;
            }

            if (toTex)
            {
                s = new float3(+1.0f, -1.0f, -1.0f);
                t = new float3(+0.0f, +0.0f, -1.0f);
            }
        }

#elif OpenGL
        {
            if (!cam.orthographic)
            {
                float tanFov = math.tan(math.radians(fov / 2.0f));
                V.c0.x = aspect * tanFov;
                V.c1.y = tanFov;

                V.c2.z = 0.0f;
                V.c3.z = 1.0f;
                V.c2.w = -(far - near) / (2.0f * far * near);
                V.c3.w = (far + near) / (2.0f * far * near);
            }
            else
            {
                t = new float3(0.0f, 0.0f, (far + near) * 0.5f);
                s = new float3(hv, hv, (far - near)) * new float3(aspect, 1.0f, 0.5f);              

                V.c3 = new float4(t, 1.0f);
                V.c0.x = s.x;
                V.c1.y = s.y;
                V.c2.z = s.z;
            }

            if (toTex)
            {
                s = new float3(+1.0f, +1.0f, +1.0f);
                t = new float3(+0.0f, +0.0f, +0.0f);
            }
        }

#elif Vulkan
        {
            if (!cam.orthographic)
            {
                float tanFov = math.tan(math.radians(fov / 2.0f));
                V.c0.x = aspect * tanFov;
                V.c1.y = tanFov;

                V.c2.z = 0.0f;
                V.c3.z = 1.0f;
                V.c2.w = -(far - near) / (far * near);
                V.c3.w = (far) / (far * near);
            }
            else
            {              
                t = new float3(0.0f, 0.0f, near);
                s = new float3(hv, hv, (far - near)) * new float3(aspect, 1.0f, 1.0f);

                V.c3 = new float4(t, 1.0f);
                V.c0.x = s.x;
                V.c1.y = s.y;
                V.c2.z = s.z;
            }

            if (toTex)
            {
                s = new float3(+1.0f, -1.0f, -1.0f);
                t = new float3(+0.0f, +0.0f, -1.0f);
            }            
        }
#endif

        if (toTex)
        {
            M.c0.x = s.x;
            M.c1.y = s.y;
            M.c2.z = s.z;
            M.c3 = new float4(s * t, 1.0f);

            V = math.mul(V, M);
        }

        return V;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 GetWfromV(Camera cam)
    {
        float4x4 W = float4x4.identity;
        Transform tr = cam.transform;
        float3 t = tr.position;
        float3x3 R = new float3x3(tr.rotation);

        W.c0 = new float4(R.c0, 0.0f);
        W.c1 = new float4(R.c1, 0.0f);
        W.c2 = new float4(R.c2, 0.0f);
        W.c3 = new float4(t, 1.0f);


        return W;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 GetLfromW(Transform localTr)
    {
        float3 pos = localTr.position;
        quaternion rot = localTr.rotation;
        float3 sca = localTr.localScale;

        float4x4 L = float4x4.identity;
        float3x3 R = new float3x3(rot);
        float3 rs = 1.0f / sca;

        R.c0 = rs.x * R.c0;
        R.c1 = rs.y * R.c1;
        R.c2 = rs.z * R.c2;

        float3 t = -new float3(math.dot(R.c0, pos), math.dot(R.c1, pos), math.dot(R.c2, pos));
        L.c0 = new float4(R.c0, t.x);
        L.c1 = new float4(R.c1, t.y);
        L.c2 = new float4(R.c2, t.z);
        L.c3 = new float4(0.0f, 0.0f, 0.0f, 1.0f);

        L = math.transpose(L);

        return L;
    }




    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 GetPosition_SfromW(float3 posW, Camera cam, bool zNormal = true)
    {
        float3 posS = float3.zero;

        float4x4 M = math.mul(RenderUtil.GetCfromV(cam), RenderUtil.GetVfromW(cam));
        float4 vec = math.mul(M, new float4(posW, 1.0f));
        vec = (1.0f / vec.w) * vec;
        posS = math.mul(RenderUtil.GetSfromN(cam), vec).xyz;

        if (!zNormal)
        {
            quaternion r = cam.transform.rotation;
            float3 t = cam.transform.position;
            float3 n = math.rotate(r, new float3(0.0f, 0.0f, 1.0f));           
            float d = math.dot((posW - t), n);
            posS.z = d;
        }

        return posS;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ray GetRay_WfromS(float3 posS, Camera cam)
    {
        Ray ray = new Ray();

        float4 posV = math.mul(
                   math.mul(RenderUtil.GetVfromC(cam), RenderUtil.GetNfromS(cam)), new float4(posS.xy, 0.0f, 1.0f));
        posV = (1.0f / posV.w) * posV;
        float4x4 W = RenderUtil.GetWfromV(cam);
        float3 pos1 = math.mul(W, posV).xyz;
        float3 pos0 = math.mul(W, new float4(0.0f, 0.0f, 0.0f, 1.0f)).xyz;

        ray.origin = pos1;
        if (!cam.orthographic)
        {
            ray.direction = math.normalize(pos1 - pos0);
        }
        else
        {
            quaternion r = cam.transform.rotation;
            ray.direction = math.mul(math.mul(r, new quaternion(0.0f, 0.0f, 1.0f, 0.0f)), math.conjugate(r)).value.xyz;
        }

        return ray;
    }


    //Terrain
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 GetVfromW(float3 pos, quaternion rot)
    {
        float4x4 V = float4x4.identity;

        float3x3 rotM = new float3x3(rot);

        float3 t = -new float3(math.dot(rotM.c0, pos), math.dot(rotM.c1, pos), math.dot(rotM.c2, pos));
        V.c0 = new float4(rotM.c0, t.x);
        V.c1 = new float4(rotM.c1, t.y);
        V.c2 = new float4(rotM.c2, t.z);
        V.c3 = new float4(0.0f, 0.0f, 0.0f, 1.0f);
        V = math.transpose(V);

        return V;
    }   

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 GetCfromV_Ortho(
        float size, float aspect, float near, float far,
        bool toTex = true
        )
    {
        float4x4 C = float4x4.identity;       

        float hv = size;

        float4x4 M = float4x4.identity;
        float3 t = float3.zero;
        float3 s = float3.zero;

#if DirectX        
        {            
            {
                s = new float3(1.0f, 1.0f, 1.0f) / new float3(aspect * hv, hv, far - near);
                t = new float3(0.0f, 0.0f, -near);

                C.c0.x = s.x;
                C.c1.y = s.y;
                C.c2.z = s.z;
                C.c3 = new float4(s * t, 1.0f);
            }

            if (toTex)
            {
                t = new float3(+0.0f, +0.0f, +1.0f);
                s = new float3(+1.0f, -1.0f, -1.0f);
            }
        }

#elif OpenGL        
        {            
            {
                s = new float3(1.0f, 1.0f, 2.0f) / new float3(aspect * hv, hv, far - near);
                t = new float3(0.0f, 0.0f, (far + near) * (-0.5f));                     
        
                C.c0.x = s.x;
                C.c1.y = s.y;
                C.c2.z = s.z;
                C.c3 = new float4(s * t, 1.0f);
            }
        
            if (toTex)
            {
                t = new float3(+0.0f, +0.0f, +0.0f);
                s = new float3(+1.0f, +1.0f, +1.0f);
            }
        }

#elif Vulkan       
        {            
            {                
                s = new float3(1.0f, 1.0f, 1.0f) / new float3(aspect * hv, hv, far - near);
                t = new float3(0.0f, 0.0f, -near);
        
                C.c0.x = s.x;
                C.c1.y = s.y;
                C.c2.z = s.z;
                C.c3 = new float4(s * t, 1.0f);
            }
        
            if (toTex)
            {
                t = new float3(+0.0f, +0.0f, +1.0f);
                s = new float3(+1.0f, -1.0f, -1.0f);
            }
        }
#endif

        if (toTex)
        {
            M.c3 = new float4(t, 1.0f);
            M.c0.x = s.x;
            M.c1.y = s.y;
            M.c2.z = s.z;

            C = math.mul(M, C);
        }

        return C;
        //return cam.projectionMatrix;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetCfromV_Ortho_Optimized(
        float size, float aspect, float near, float far,
        out float4x4 C_toTex, out float4x4 C_depth
        )
    {
        C_toTex = float4x4.identity;
        C_depth = float4x4.identity;

        float hv = size;

        float4x4 M = float4x4.identity;
        float3 t0 = float3.zero;
        float3 s0 = float3.zero;
        float3 t1 = float3.zero;
        float3 s1 = float3.zero;

#if DirectX
        {
            {
                s0 = new float3(1.0f, 1.0f, 1.0f) / new float3(aspect * hv, hv, far - near);
                t0 = new float3(0.0f, 0.0f, -near);
            }

            {
                t1 = new float3(+0.0f, +0.0f, +1.0f);
                s1 = new float3(+1.0f, -1.0f, -1.0f);
            }
        }
#elif OpenGL
        {
            {
                s0 = new float3(1.0f, 1.0f, 2.0f) / new float3(aspect * hv, hv, far - near);
                t0 = new float3(0.0f, 0.0f, (far + near) * (-0.5f));              
            }

            {
                t1 = new float3(+0.0f, +0.0f, +0.0f);
                s1 = new float3(+1.0f, +1.0f, +1.0f);
            }
        }
#elif Vulkan
        {
            {
                s0 = new float3(1.0f, 1.0f, 1.0f) / new float3(aspect * hv, hv, far - near);
                t0 = new float3(0.0f, 0.0f, -near);
            }

            {
                t1 = new float3(+0.0f, +0.0f, +1.0f);
                s1 = new float3(+1.0f, -1.0f, -1.0f);
            }
        }
#endif        

        {
            C_depth.c0.x = s0.x;               //C.c0.x = (1.0f / aspect) * s.x;
            C_depth.c1.y = s0.y;
            C_depth.c2.z = s0.z;
            C_depth.c3 = new float4(s0 * t0, 1.0f);

            M.c3 = new float4(t1, 1.0f);
            M.c0.x = s1.x;
            M.c1.y = s1.y;
            M.c2.z = s1.z;

            C_toTex = math.mul(M, C_depth);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetCfromV_Persp_Optimized(
        float fov, float aspect, float near, float far, out float4x4 C_toTex, out float4x4 C_depth)
    {
        float4x4 C = float4x4.identity;

        float4x4 M = float4x4.identity;
        float3 t = float3.zero;
        float3 s = float3.zero;

#if DirectX        
        {

            {
                float cotFov = 1.0f / math.tan(math.radians(fov / 2.0f));
                C.c0.x = (1.0f / aspect) * cotFov;
                C.c1.y = cotFov;

                C.c2.z = +far / (far - near);
                C.c3.z = -(far * near / (far - near));
                C.c2.w = 1.0f;
                C.c3.w = 0.0f;
            }

            {
                t = new float3(+0.0f, +0.0f, +1.0f);
                s = new float3(+1.0f, -1.0f, -1.0f);
                //s = new float3(+1.0f, +1.0f, -1.0f);
            }
        }

#elif OpenGL
        {            
            {
                float cotFov = 1.0f / math.tan(math.radians(fov / 2.0f));
                C.c0.x = (1.0f / aspect) * cotFov;
                C.c1.y = cotFov;
        
                C.c2.z = (far + near) / (far - near);
                C.c3.z = -(2 * far * near) / (far - near);
                C.c2.w = 1.0f;
                C.c3.w = 0.0f;                       
            }
                               
            {
                t = new float3(+0.0f, +0.0f, +0.0f);
                s = new float3(+1.0f, +1.0f, +1.0f);
            }
        }

#elif Vulkan
        {
            
            {
                float cotFov = 1.0f / math.tan(math.radians(fov / 2.0f));
                C.c0.x = (1.0f / aspect) * cotFov;
                C.c1.y = cotFov;
                       
                C.c2.z = +far / (far - near);
                C.c3.z = -(far * near / (far - near));
                C.c2.w = 1.0f;
                C.c3.w = 0.0f;
            }
                              
            {
                t = new float3(+0.0f, +0.0f, +1.0f);
                s = new float3(+1.0f, -1.0f, -1.0f);
            }
        }
#endif

        {
            M.c3 = new float4(t, 1.0f);
            M.c0.x = s.x;
            M.c1.y = s.y;
            M.c2.z = s.z;

            C_depth = C;
            C_toTex = math.mul(M, C);
        }

    }

    public static void GetCfromV_Persp_Optimized(
       float4 fi, out float4x4 C_toTex, out float4x4 C_depth)
    {
        GetCfromV_Persp_Optimized(fi.x, fi.y, fi.z, fi.w, out C_toTex, out C_depth);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetCfromV_Persp_Optimized_Cubemap(
        float fov, float aspect, float near, float far, out float4x4 C_toTex, out float4x4 C_depth)
    {
        float4x4 C = float4x4.identity;

        float4x4 M = float4x4.identity;
        float3 t = float3.zero;
        float3 s = float3.zero;

#if DirectX        
        {

            {
                float cotFov = 1.0f / math.tan(math.radians(fov / 2.0f));
                C.c0.x = (1.0f / aspect) * cotFov;
                C.c1.y = cotFov;

                C.c2.z = +far / (far - near);
                C.c3.z = -(far * near / (far - near));
                C.c2.w = 1.0f;
                C.c3.w = 0.0f;
            }

            {
                t = new float3(+0.0f, +0.0f, +1.0f);
                //t = new float3(+0.0f, +0.0f, +0.0f);

                //s = new float3(+1.0f, -1.0f, -1.0f);
                s = new float3(+1.0f, +1.0f, -1.0f);
                //s = new float3(+1.0f, +1.0f, +1.0f);
            }
        }

#elif OpenGL
        {            
            {
                float cotFov = 1.0f / math.tan(math.radians(fov / 2.0f));
                C.c0.x = (1.0f / aspect) * cotFov;
                C.c1.y = cotFov;
        
                C.c2.z = (far + near) / (far - near);
                C.c3.z = -(2 * far * near) / (far - near);
                C.c2.w = 1.0f;
                C.c3.w = 0.0f;                       
            }
                               
            {
                t = new float3(+0.0f, +0.0f, +0.0f);
                s = new float3(+1.0f, +1.0f, +1.0f);
            }
        }

#elif Vulkan
        {
            
            {
                float cotFov = 1.0f / math.tan(math.radians(fov / 2.0f));
                C.c0.x = (1.0f / aspect) * cotFov;
                C.c1.y = cotFov;
                       
                C.c2.z = +far / (far - near);
                C.c3.z = -(far * near / (far - near));
                C.c2.w = 1.0f;
                C.c3.w = 0.0f;
            }
                              
            {
                t = new float3(+0.0f, +0.0f, +1.0f);
                s = new float3(+1.0f, -1.0f, -1.0f);
            }
        }
#endif

        {
            M.c3 = new float4(t, 1.0f);
            M.c0.x = s.x;
            M.c1.y = s.y;
            M.c2.z = s.z;

            C_depth = C;
            C_toTex = math.mul(M, C);
        }

    }

    public static void GetCfromV_Persp_Optimized_Cubemap(
       float4 fi, out float4x4 C_toTex, out float4x4 C_depth)
    {
        GetCfromV_Persp_Optimized_Cubemap(fi.x, fi.y, fi.z, fi.w, out C_toTex, out C_depth);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 GetTfromN()
    {
        float4x4 T = float4x4.identity;

#if DirectX
        {
            T.c0 = new float4(+0.5f, +0.0f, +0.0f, +0.0f);
            T.c1 = new float4(+0.0f, +0.5f, +0.0f, +0.0f);
            T.c2 = new float4(+0.0f, +0.0f, +1.0f, +0.0f);
            T.c3 = new float4(+0.5f, +0.5f, +0.0f, +1.0f);
        }
#elif OpenGL
        {
            T.c0 = new float4(+0.5f, +0.0f, +0.0f, +0.0f);
            T.c1 = new float4(+0.0f, +0.5f, +0.0f, +0.0f);
            T.c2 = new float4(+0.0f, +0.0f, +1.0f, +0.0f);
            T.c3 = new float4(+0.5f, +0.5f, +0.0f, +1.0f);
        }      
#elif Vulkan
        {
            T.c0 = new float4(+0.5f, +0.0f, +0.0f, +0.0f);
            T.c1 = new float4(+0.0f, +0.5f, +0.0f, +0.0f);
            T.c2 = new float4(+0.0f, +0.0f, +1.0f, +0.0f);
            T.c3 = new float4(+0.5f, +0.5f, +0.0f, +1.0f);
        }
#endif
        
        return T;
    }   

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 GetSfromN(Rect rect, bool toTex = true)
    {
        float4x4 S = float4x4.identity;
        //Rect pRect = cam.pixelRect;
        float x = rect.x;
        float y = rect.y;
        float w = rect.width;
        float h = rect.height;

        float3 t = float3.zero;
        float3 s = float3.zero;       

        float4x4 M = float4x4.identity;
#if DirectX
        {
            t = new float3(new float2(x, y) + new float2(w, h) * 0.5f, 0.0f);
            s = new float3(new float2(w, h) * 0.5f, 1.0f);

            S.c3 = new float4(t, 1.0f);
            S.c0.x = s.x;
            S.c1.y = s.y;
            S.c2.z = s.z;

            if (toTex)
            {
                s = new float3(+1.0f, -1.0f, -1.0f);
                t = new float3(+0.0f, +0.0f, -1.0f);
            }
        }

#elif OpenGL       
        {
            t = new float3(new float2(x, y) + new float2(w, h) * 0.5f, 0.5f);
            s = new float3(new float2(w, h) * 0.5f, 0.5f);           

            S.c3 = new float4(t, 1.0f);
            S.c0.x = s.x;
            S.c1.y = s.y;
            S.c2.z = s.z;

            if (toTex)
            {
                s = new float3(+1.0f, +1.0f, +1.0f);
                t = new float3(+0.0f, +0.0f, +0.0f);
            }
        }

#elif Vulkan       
        {
            t = new float3(new float2(x, y) + new float2(w, h) * 0.5f, 0.0f);
            s = new float3(new float2(w, h) * 0.5f, 1.0f);

            S.c3 = new float4(t, 1.0f);
            S.c0.x = s.x;
            S.c1.y = s.y;
            S.c2.z = s.z;

            if (toTex)
            {
                s = new float3(+1.0f, -1.0f, -1.0f);
                t = new float3(+0.0f, +0.0f, -1.0f);
            }
        }
#endif

        if (toTex)
        {
            M.c0.x = s.x;
            M.c1.y = s.y;
            M.c2.z = s.z;
            M.c3 = new float4(s * t, 1.0f);

            S = math.mul(S, M);
        }

        return S;
    }
   

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetMat_SfromW(
       Transform trCam,
       Camera cam,
       out float4x4 S, out float4x4 CV)
    {
        float3 posS = float3.zero;

        float3 pos = trCam.position;
        quaternion rot = trCam.rotation;
        float fov = cam.fieldOfView;
        float aspect = cam.aspect;
        float near = cam.nearClipPlane;
        float far = cam.farClipPlane;
        float hv = cam.orthographicSize;

        Rect pRect = cam.pixelRect;
        float x = pRect.x;
        float y = pRect.y;
        float w = pRect.width;
        float h = pRect.height;
        bool isOrtho = cam.orthographic;

        float4x4 V = float4x4.identity;
        {
            float3x3 R = new float3x3(rot);

            float3 t = -new float3(math.dot(R.c0, pos), math.dot(R.c1, pos), math.dot(R.c2, pos));
            R = math.transpose(R);
            V.c0 = new float4(R.c0, 0.0f);
            V.c1 = new float4(R.c1, 0.0f);
            V.c2 = new float4(R.c2, 0.0f);
            V.c3 = new float4(t, 1.0f);
        }

        float4x4 C = float4x4.identity;
        {
            if (!isOrtho)
            {
                float cotFov = 1.0f / math.tan(math.radians(fov / 2.0f));
                C.c0.x = (1.0f / aspect) * cotFov;
                C.c1.y = cotFov;

#if DirectX
                {
                    C.c2.z = +far / (far - near);
                    C.c3.z = -(far * near / (far - near));
                    C.c2.w = 1.0f;
                    C.c3.w = 0.0f;
                }
#elif OpenGL
                {                   
                    C.c2.z = (far + near) / (far - near);
                    C.c3.z = -(2 * far * near) / (far - near);
                    C.c2.w = 1.0f;
                    C.c3.w = 0.0f;
                }
#elif Vulkan
                {
                    C.c2.z = +far / (far - near);
                    C.c3.z = -(far * near / (far - near));
                    C.c2.w = 1.0f;
                    C.c3.w = 0.0f;
                }
#endif               
            }
            else
            {
                float3 s = float3.zero;
                float3 t = float3.zero;
#if DirectX
                {
                    s = new float3(1.0f, 1.0f, 1.0f) / new float3(aspect * hv, hv, far - near);
                    t = new float3(0.0f, 0.0f, -near);
                }
#elif OpenGL
                {
                    s = new float3(1.0f, 1.0f, 2.0f) / new float3(aspect * hv, hv, far - near);
                    t = new float3(0.0f, 0.0f, (far + near) * (-0.5f));
                }
#elif Vulkan
                {
                    s = new float3(1.0f, 1.0f, 1.0f) / new float3(aspect * hv, hv, far - near);
                    t = new float3(0.0f, 0.0f, -near);
                }
#endif              
                C.c0.x = s.x;
                C.c1.y = s.y;
                C.c2.z = s.z;
                C.c3 = new float4(s * t, 1.0f);
            }
        }

        S = float4x4.identity;
        {
            float3 s = float3.zero;
            float3 t = float3.zero;

#if DirectX
            {
                t = new float3(new float2(x, y) + new float2(w, h) * 0.5f, 0.0f);
                s = new float3(new float2(w, h) * 0.5f, 1.0f);
            }
#elif OpenGL
            {
                t = new float3(new float2(x, y) + new float2(w, h) * 0.5f, 0.5f);
                s = new float3(new float2(w, h) * 0.5f, 0.5f);
            }
#elif Vulkan
            {
                t = new float3(new float2(x, y) + new float2(w, h) * 0.5f, 0.0f);
                s = new float3(new float2(w, h) * 0.5f, 1.0f);
            }
#endif           
            S.c3 = new float4(t, 1.0f);
            S.c0.x = s.x;
            S.c1.y = s.y;
            S.c2.z = s.z;
        }

        CV = math.mul(C, V);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 GetM_Unity()
    {        
        float4x4 M = float4x4.identity;
        float3 t = float3.zero;
        float3 s = float3.zero;

#if DirectX        
        {
            t = new float3(+0.0f, +0.0f, +1.0f);
            s = new float3(+1.0f, -1.0f, -1.0f);
        }

#elif OpenGL        
        {
            t = new float3(+0.0f, +0.0f, +0.0f);
            s = new float3(+1.0f, +1.0f, +1.0f);
        }

#elif Vulkan       
        {
            t = new float3(+0.0f, +0.0f, +1.0f);
            s = new float3(+1.0f, -1.0f, -1.0f);
        }
#endif
        
        {
            M.c3 = new float4(t, 1.0f);
            M.c0.x = s.x;
            M.c1.y = s.y;
            M.c2.z = s.z;            
        }

        return M;
    }


    //Test
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 GetCfromV0(Camera cam, bool correct = true, bool toTex = false)
    {
        float4x4 C = float4x4.identity;

        float fov = cam.fieldOfView;
        float aspect = cam.aspect;
        float near = cam.nearClipPlane;
        float far = cam.farClipPlane;

        float hv = cam.orthographicSize;
        //float right = hv;
        //float left = -right;
        //float top = right;
        //float bottom = -top;

        float4x4 M = float4x4.identity;
        float3 t = float3.zero;
        float3 s = float3.zero;
        if (gdt == GraphicsDeviceType.Direct3D11 || gdt == GraphicsDeviceType.Direct3D12)
        {
            if (!cam.orthographic)
            {
                float cotFov = 1.0f / math.tan(math.radians(fov / 2.0f));
                C.c0.x = (1.0f / aspect) * cotFov;
                C.c1.y = cotFov;

                C.c2.z = +far / (far - near);
                C.c3.z = -(far * near / (far - near));
                C.c2.w = 1.0f;
                C.c3.w = 0.0f;
            }
            else
            {
                //s = new float3(2.0f / (right - left), 2.0f / (top - bottom), 2.0f / (far - near));
                //t = new float3(-(right + left) * 0.5f, -(top + bottom) * 0.5f, -(far + near) * 0.5f);
                //s = 2.0f / (new float3(right, top, far) - new float3(left, bottom, near));
                //t = (new float3(right, top, far) + new float3(left, bottom, near)) * (-0.5f);

                //s = new float3(1.0f, 1.0f, 2.0f) / new float3(aspect * hv, hv, far - near);
                //t = new float3(0.0f, 0.0f, (far + near) * (-0.5f));

                s = new float3(1.0f, 1.0f, 1.0f) / new float3(aspect * hv, hv, far - near);
                t = new float3(0.0f, 0.0f, -near);

                C.c0.x = s.x;               //C.c0.x = (1.0f / aspect) * s.x;
                C.c1.y = s.y;
                C.c2.z = s.z;
                C.c3 = new float4(s * t, 1.0f);
            }

            if (toTex)
            {
                {
                    t = new float3(+0.0f, +0.0f, +1.0f);
                    s = new float3(+1.0f, -1.0f, -1.0f);
                }

                //{
                //    t = new float3(+0.0f, +0.0f, +0.0f);
                //    s = new float3(+1.0f, +1.0f, +1.0f);
                //}
            }
            else
            {
                if (cam.cameraType == CameraType.Game)
                {
                    t = new float3(+0.0f, +0.0f, +1.0f);
                    s = new float3(+1.0f, +1.0f, -1.0f);
                    //s = new float3(+1.0f, -1.0f, -1.0f);
                }
                else if (cam.cameraType == CameraType.SceneView)
                {
                    t = new float3(+0.0f, +0.0f, +1.0f);
                    s = new float3(+1.0f, -1.0f, -1.0f);
                }
            }
        }
        else if (gdt == GraphicsDeviceType.OpenGLCore || gdt == GraphicsDeviceType.OpenGLES3)
        {
            if (!cam.orthographic)
            {
                float cotFov = 1.0f / math.tan(math.radians(fov / 2.0f));
                C.c0.x = (1.0f / aspect) * cotFov;
                C.c1.y = cotFov;

                C.c2.z = (far + near) / (far - near);
                C.c3.z = -(2 * far * near) / (far - near);
                C.c2.w = 1.0f;
                C.c3.w = 0.0f;

                //C.c2.z = +far / (far - near);
                //C.c3.z = -(far * near / (far - near));
                //C.c2.w = 1.0f;
                //C.c3.w = 0.0f;
            }
            else
            {
                s = new float3(1.0f, 1.0f, 2.0f) / new float3(aspect * hv, hv, far - near);
                t = new float3(0.0f, 0.0f, (far + near) * (-0.5f));

                //s = new float3(1.0f, 1.0f, 1.0f) / new float3(aspect * hv, hv, far - near);
                //t = new float3(0.0f, 0.0f, -near);

                C.c0.x = s.x;
                C.c1.y = s.y;
                C.c2.z = s.z;
                C.c3 = new float4(s * t, 1.0f);
            }

            if (toTex)
            {
                {
                    t = new float3(+0.0f, +0.0f, +0.0f);
                    s = new float3(+1.0f, +1.0f, +1.0f);
                }
            }
            else
            {
                if (cam.cameraType == CameraType.Game)
                {
                    t = new float3(+0.0f, +0.0f, +0.0f);
                    s = new float3(+1.0f, +1.0f, +1.0f);
                }
                else if (cam.cameraType == CameraType.SceneView)
                {
                    t = new float3(+0.0f, +0.0f, +0.0f);
                    s = new float3(+1.0f, +1.0f, +1.0f);
                }
            }

        }
        else if (gdt == GraphicsDeviceType.Vulkan)
        {
            if (!cam.orthographic)
            {
                float cotFov = 1.0f / math.tan(math.radians(fov / 2.0f));
                C.c0.x = (1.0f / aspect) * cotFov;
                C.c1.y = cotFov;

                //C.c2.z = (far + near) / (far - near);
                //C.c3.z = -(2 * far * near) / (far - near);
                //C.c2.w = 1.0f;
                //C.c3.w = 0.0f;

                C.c2.z = +far / (far - near);
                C.c3.z = -(far * near / (far - near));
                C.c2.w = 1.0f;
                C.c3.w = 0.0f;
            }
            else
            {
                //s = new float3(1.0f, 1.0f, 2.0f) / new float3(aspect * hv, hv, far - near);
                //t = new float3(0.0f, 0.0f, (far + near) * (-0.5f));

                s = new float3(1.0f, 1.0f, 1.0f) / new float3(aspect * hv, hv, far - near);
                t = new float3(0.0f, 0.0f, -near);

                C.c0.x = s.x;
                C.c1.y = s.y;
                C.c2.z = s.z;
                C.c3 = new float4(s * t, 1.0f);
            }

            if (toTex)
            {
                {
                    t = new float3(+0.0f, +0.0f, +1.0f);
                    s = new float3(+1.0f, -1.0f, -1.0f);
                }
            }
            else
            {
                if (cam.cameraType == CameraType.Game)
                {
                    t = new float3(+0.0f, +0.0f, +1.0f);
                    s = new float3(+1.0f, +1.0f, -1.0f);
                }
                else if (cam.cameraType == CameraType.SceneView)
                {
                    t = new float3(+0.0f, +0.0f, +1.0f);
                    s = new float3(+1.0f, -1.0f, -1.0f);
                }
            }
        }

        if (correct || toTex)
        {
            M.c3 = new float4(t, 1.0f);
            M.c0.x = s.x;
            M.c1.y = s.y;
            M.c2.z = s.z;

            C = math.mul(M, C);
        }

        return C;
        //return cam.projectionMatrix;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 GetSfromN0(Camera cam, bool correct = true, bool toTex = false)
    {
        float4x4 S = float4x4.identity;
        Rect pRect = cam.pixelRect;
        float x = pRect.x;
        float y = pRect.y;
        float w = pRect.width;
        float h = pRect.height;

        float3 t = float3.zero;
        float3 s = float3.zero;

        float4x4 M = float4x4.identity;
        if (gdt == GraphicsDeviceType.Direct3D11 || gdt == GraphicsDeviceType.Direct3D12)
        {
            if (!cam.orthographic)
            {
                //t = new float3(x + w / 2.0f, y + h / 2.0f, 0.0f);
                //s = new float3(w / 2.0f, h / 2.0f, 1.0f);
                t = new float3(new float2(x, y) + new float2(w, h) * 0.5f, 0.0f);
                s = new float3(new float2(w, h) * 0.5f, 1.0f);
            }
            else
            {
                //t = new float3(x + w / 2.0f, y + h / 2.0f, 0.5f);
                //s = new float3(w / 2.0f, h / 2.0f, 0.5f);
                //t = new float3(new float2(x, y) + new float2(w, h) * 0.5f, 0.5f);
                //s = new float3(new float2(w, h) * 0.5f, 0.5f);
                t = new float3(new float2(x, y) + new float2(w, h) * 0.5f, 0.0f);
                s = new float3(new float2(w, h) * 0.5f, 1.0f);
            }

            S.c3 = new float4(t, 1.0f);
            S.c0.x = s.x;
            S.c1.y = s.y;
            S.c2.z = s.z;

            if (toTex)
            {
                {
                    s = new float3(+1.0f, -1.0f, -1.0f);
                    t = new float3(+0.0f, +0.0f, -1.0f);
                }
            }
            else
            {
                if (cam.cameraType == CameraType.Game)
                {
                    s = new float3(+1.0f, +1.0f, -1.0f);
                    t = new float3(+0.0f, +0.0f, -1.0f);
                }
                else if (cam.cameraType == CameraType.SceneView)
                {
                    s = new float3(+1.0f, -1.0f, -1.0f);
                    t = new float3(+0.0f, +0.0f, -1.0f);
                }
            }

        }
        else if (gdt == GraphicsDeviceType.OpenGLCore || gdt == GraphicsDeviceType.OpenGLES3)
        {
            t = new float3(new float2(x, y) + new float2(w, h) * 0.5f, 0.5f);
            s = new float3(new float2(w, h) * 0.5f, 0.5f);
            //t = new float3(new float2(x, y) + new float2(w, h) * 0.5f, 0.0f);
            //s = new float3(new float2(w, h) * 0.5f, 1.0f);

            S.c3 = new float4(t, 1.0f);
            S.c0.x = s.x;
            S.c1.y = s.y;
            S.c2.z = s.z;

            if (toTex)
            {
                {
                    s = new float3(+1.0f, +1.0f, +1.0f);
                    t = new float3(+0.0f, +0.0f, +0.0f);
                }
            }
            else
            {
                if (cam.cameraType == CameraType.Game)
                {
                    s = new float3(+1.0f, +1.0f, +1.0f);
                    t = new float3(+0.0f, +0.0f, +0.0f);
                }
                else if (cam.cameraType == CameraType.SceneView)
                {
                    s = new float3(+1.0f, +1.0f, +1.0f);
                    t = new float3(+0.0f, +0.0f, +0.0f);
                }
            }
        }
        else if (gdt == GraphicsDeviceType.Vulkan)
        {
            //t = new float3(new float2(x, y) + new float2(w, h) * 0.5f, 0.5f);
            //s = new float3(new float2(w, h) * 0.5f, 0.5f);
            t = new float3(new float2(x, y) + new float2(w, h) * 0.5f, 0.0f);
            s = new float3(new float2(w, h) * 0.5f, 1.0f);

            S.c3 = new float4(t, 1.0f);
            S.c0.x = s.x;
            S.c1.y = s.y;
            S.c2.z = s.z;

            if (toTex)
            {
                {
                    s = new float3(+1.0f, -1.0f, -1.0f);
                    t = new float3(+0.0f, +0.0f, -1.0f);
                }
            }
            else
            {
                if (cam.cameraType == CameraType.Game)
                {
                    s = new float3(+1.0f, +1.0f, -1.0f);
                    t = new float3(+0.0f, +0.0f, -1.0f);
                }
                else if (cam.cameraType == CameraType.SceneView)
                {
                    s = new float3(+1.0f, -1.0f, -1.0f);
                    t = new float3(+0.0f, +0.0f, -1.0f);
                }
            }
        }

        if (correct || toTex)
        {
            M.c0.x = s.x;
            M.c1.y = s.y;
            M.c2.z = s.z;
            M.c3 = new float4(s * t, 1.0f);

            S = math.mul(S, M);
        }

        return S;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 GetNfromS0(Camera cam, bool correct = true, bool toTex = false)
    {
        float4x4 N = float4x4.identity;
        Rect pRect = cam.pixelRect;
        float x = pRect.x;
        float y = pRect.y;
        float w = pRect.width;
        float h = pRect.height;

        float3 t = float3.zero;
        float3 s = float3.zero;


        float4x4 M = float4x4.identity;
        if (gdt == GraphicsDeviceType.Direct3D11 || gdt == GraphicsDeviceType.Direct3D12)
        {
            if (!cam.orthographic)
            {
                //s = new float3(2.0f / w, 2.0f / h, 1.0f);
                //t = new float3(-(x + w / 2.0f), -(y + h / 2.0f), 0.0f);
                s = new float3(2.0f / new float2(w, h), 1.0f);
                t = new float3(-(new float2(x, y) + new float2(w, h) * 0.5f), 0.0f);
            }
            else
            {
                //s = new float3(2.0f / w, 2.0f / h, 2.0f);
                //t = new float3(-(x + w / 2.0f), -(y + h / 2.0f), -0.5f);
                //s = new float3(2.0f / new float2(w, h), 2.0f);
                //t = new float3(-(new float2(x, y) + new float2(w, h) * 0.5f), -0.5f);
                s = new float3(2.0f / new float2(w, h), 1.0f);
                t = new float3(-(new float2(x, y) + new float2(w, h) * 0.5f), 0.0f);
            }

            N.c0.x = s.x;
            N.c1.y = s.y;
            N.c2.z = s.z;
            N.c3 = new float4(s * t, 1.0f);

            if (toTex)
            {
                {
                    t = new float3(+0.0f, +0.0f, +1.0f);
                    s = new float3(+1.0f, -1.0f, -1.0f);
                }
            }
            else
            {
                if (cam.cameraType == CameraType.Game)
                {
                    t = new float3(+0.0f, +0.0f, +1.0f);
                    s = new float3(+1.0f, +1.0f, -1.0f);
                }
                else if (cam.cameraType == CameraType.SceneView)
                {
                    t = new float3(+0.0f, +0.0f, +1.0f);
                    s = new float3(+1.0f, -1.0f, -1.0f);
                }
            }
        }
        else if (gdt == GraphicsDeviceType.OpenGLCore || gdt == GraphicsDeviceType.OpenGLES3)
        {
            s = new float3(2.0f / new float2(w, h), 2.0f);
            t = new float3(-(new float2(x, y) + new float2(w, h) * 0.5f), -0.5f);

            //s = new float3(2.0f / new float2(w, h), 1.0f);
            //t = new float3(-(new float2(x, y) + new float2(w, h) * 0.5f), 0.0f);

            N.c0.x = s.x;
            N.c1.y = s.y;
            N.c2.z = s.z;
            N.c3 = new float4(s * t, 1.0f);

            if (toTex)
            {
                {
                    t = new float3(+0.0f, +0.0f, +0.0f);
                    s = new float3(+1.0f, +1.0f, +1.0f);
                }
            }
            else
            {
                if (cam.cameraType == CameraType.Game)
                {
                    t = new float3(+0.0f, +0.0f, +0.0f);
                    s = new float3(+1.0f, +1.0f, +1.0f);
                }
                else if (cam.cameraType == CameraType.SceneView)
                {
                    t = new float3(+0.0f, +0.0f, +0.0f);
                    s = new float3(+1.0f, +1.0f, +1.0f);
                }
            }
        }
        else if (gdt == GraphicsDeviceType.Vulkan)
        {
            //s = new float3(2.0f / new float2(w, h), 2.0f);
            //t = new float3(-(new float2(x, y) + new float2(w, h) * 0.5f), -0.5f);

            s = new float3(2.0f / new float2(w, h), 1.0f);
            t = new float3(-(new float2(x, y) + new float2(w, h) * 0.5f), 0.0f);

            N.c0.x = s.x;
            N.c1.y = s.y;
            N.c2.z = s.z;
            N.c3 = new float4(s * t, 1.0f);

            if (toTex)
            {
                {
                    t = new float3(+0.0f, +0.0f, +1.0f);
                    s = new float3(+1.0f, -1.0f, -1.0f);
                }
            }
            else
            {
                if (cam.cameraType == CameraType.Game)
                {
                    t = new float3(+0.0f, +0.0f, +1.0f);
                    s = new float3(+1.0f, +1.0f, -1.0f);
                }
                else if (cam.cameraType == CameraType.SceneView)
                {
                    t = new float3(+0.0f, +0.0f, +1.0f);
                    s = new float3(+1.0f, -1.0f, -1.0f);
                }
            }
        }

        if (correct || toTex)
        {
            M.c3 = new float4(t, 1.0f);
            M.c0.x = s.x;
            M.c1.y = s.y;
            M.c2.z = s.z;

            N = math.mul(M, N);
        }

        return N;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float4x4 GetVfromC0(Camera cam, bool correct = true, bool toTex = false)
    {
        float4x4 V = float4x4.identity;

        float fov = cam.fieldOfView;
        float aspect = cam.aspect;
        float near = cam.nearClipPlane;
        float far = cam.farClipPlane;

        float hv = cam.orthographicSize;
        //float right = n;
        //float left = -right;
        //float top = right;
        //float bottom = -top;

        float4x4 M = float4x4.identity;
        float3 s = float3.zero;
        float3 t = float3.zero;

        if (gdt == GraphicsDeviceType.Direct3D11 || gdt == GraphicsDeviceType.Direct3D12)
        {
            if (!cam.orthographic)
            {
                float tanFov = math.tan(math.radians(fov / 2.0f));
                V.c0.x = aspect * tanFov;
                V.c1.y = tanFov;

                V.c2.z = 0.0f;
                V.c3.z = 1.0f;
                V.c2.w = -(far - near) / (far * near);
                V.c3.w = (far) / (far * near);
            }
            else
            {
                //t = new float3((right + left) * 0.5f, (top + bottom) * 0.5f, (far + near) * 0.5f);
                //s = new float3((right - left) / 2.0f, (top - bottom) / 2.0f, (far - near) / 2.0f);
                //t = (new float3(right, top, far) + new float3(left, bottom, near)) * 0.5f;
                //s = (new float3(right, top, far) - new float3(left, bottom, near)) * 0.5f;

                //t = new float3(0.0f, 0.0f, (far + near) * 0.5f);
                //s = new float3(hv, hv, (far - near)) * new float3(aspect, 1.0f, 0.5f);

                t = new float3(0.0f, 0.0f, near);
                s = new float3(hv, hv, (far - near)) * new float3(aspect, 1.0f, 1.0f);

                V.c3 = new float4(t, 1.0f);
                V.c0.x = s.x;           //V.c0.x = aspect * s.x;
                V.c1.y = s.y;
                V.c2.z = s.z;
            }

            if (toTex)
            {
                {
                    s = new float3(+1.0f, -1.0f, -1.0f);
                    t = new float3(+0.0f, +0.0f, -1.0f);
                }
            }
            else
            {
                if (cam.cameraType == CameraType.Game)
                {
                    s = new float3(+1.0f, +1.0f, -1.0f);
                    t = new float3(+0.0f, +0.0f, -1.0f);
                }
                else if (cam.cameraType == CameraType.SceneView)
                {
                    s = new float3(+1.0f, -1.0f, -1.0f);
                    t = new float3(+0.0f, +0.0f, -1.0f);
                }
            }


        }
        else if (gdt == GraphicsDeviceType.OpenGLCore || gdt == GraphicsDeviceType.OpenGLES3)
        {
            if (!cam.orthographic)
            {
                float tanFov = math.tan(math.radians(fov / 2.0f));
                V.c0.x = aspect * tanFov;
                V.c1.y = tanFov;

                V.c2.z = 0.0f;
                V.c3.z = 1.0f;
                V.c2.w = -(far - near) / (2.0f * far * near);
                V.c3.w = (far + near) / (2.0f * far * near);

                //V.c2.z = 0.0f;
                //V.c3.z = 1.0f;
                //V.c2.w = -(far - near) / (far * near);
                //V.c3.w = (far) / (far * near);
            }
            else
            {
                t = new float3(0.0f, 0.0f, (far + near) * 0.5f);
                s = new float3(hv, hv, (far - near)) * new float3(aspect, 1.0f, 0.5f);

                //t = new float3(0.0f, 0.0f, near);
                //s = new float3(hv, hv, (far - near)) * new float3(aspect, 1.0f, 1.0f);

                V.c3 = new float4(t, 1.0f);
                V.c0.x = s.x;
                V.c1.y = s.y;
                V.c2.z = s.z;
            }

            if (toTex)
            {
                {
                    s = new float3(+1.0f, +1.0f, +1.0f);
                    t = new float3(+0.0f, +0.0f, +0.0f);
                }
            }
            else
            {
                if (cam.cameraType == CameraType.Game)
                {
                    s = new float3(+1.0f, +1.0f, +1.0f);
                    t = new float3(+0.0f, +0.0f, +0.0f);
                }
                else if (cam.cameraType == CameraType.SceneView)
                {
                    s = new float3(+1.0f, +1.0f, +1.0f);
                    t = new float3(+0.0f, +0.0f, +0.0f);
                }
            }
        }
        else if (gdt == GraphicsDeviceType.Vulkan)
        {
            if (!cam.orthographic)
            {
                float tanFov = math.tan(math.radians(fov / 2.0f));
                V.c0.x = aspect * tanFov;
                V.c1.y = tanFov;

                //V.c2.z = 0.0f;
                //V.c3.z = 1.0f;
                //V.c2.w = -(far - near) / (2.0f * far * near);
                //V.c3.w = (far + near) / (2.0f * far * near);

                V.c2.z = 0.0f;
                V.c3.z = 1.0f;
                V.c2.w = -(far - near) / (far * near);
                V.c3.w = (far) / (far * near);
            }
            else
            {
                //t = new float3(0.0f, 0.0f, (far + near) * 0.5f);
                //s = new float3(hv, hv, (far - near)) * new float3(aspect, 1.0f, 0.5f);

                t = new float3(0.0f, 0.0f, near);
                s = new float3(hv, hv, (far - near)) * new float3(aspect, 1.0f, 1.0f);

                V.c3 = new float4(t, 1.0f);
                V.c0.x = s.x;
                V.c1.y = s.y;
                V.c2.z = s.z;
            }

            if (toTex)
            {
                {
                    s = new float3(+1.0f, -1.0f, -1.0f);
                    t = new float3(+0.0f, +0.0f, -1.0f);
                }
            }
            else
            {
                if (cam.cameraType == CameraType.Game)
                {
                    s = new float3(+1.0f, +1.0f, -1.0f);
                    t = new float3(+0.0f, +0.0f, -1.0f);
                }
                else if (cam.cameraType == CameraType.SceneView)
                {
                    s = new float3(+1.0f, -1.0f, -1.0f);
                    t = new float3(+0.0f, +0.0f, -1.0f);
                }
            }
        }

        if (correct || toTex)
        {
            M.c0.x = s.x;
            M.c1.y = s.y;
            M.c2.z = s.z;
            M.c3 = new float4(s * t, 1.0f);

            V = math.mul(V, M);
        }

        return V;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetMat_SfromW0(
       Transform trCam,
       Camera cam,
       out float4x4 S, out float4x4 CV)
    {
        float3 posS = float3.zero;

        float3 pos = trCam.position;
        quaternion rot = trCam.rotation;
        float fov = cam.fieldOfView;
        float aspect = cam.aspect;
        float near = cam.nearClipPlane;
        float far = cam.farClipPlane;
        float hv = cam.orthographicSize;

        Rect pRect = cam.pixelRect;
        float x = pRect.x;
        float y = pRect.y;
        float w = pRect.width;
        float h = pRect.height;
        bool isOrtho = cam.orthographic;

        float4x4 V = float4x4.identity;
        {
            float3x3 R = new float3x3(rot);

            float3 t = -new float3(math.dot(R.c0, pos), math.dot(R.c1, pos), math.dot(R.c2, pos));
            R = math.transpose(R);
            V.c0 = new float4(R.c0, 0.0f);
            V.c1 = new float4(R.c1, 0.0f);
            V.c2 = new float4(R.c2, 0.0f);
            V.c3 = new float4(t, 1.0f);
        }

        float4x4 C = float4x4.identity;
        {
            if (!isOrtho)
            {
                float cotFov = 1.0f / math.tan(math.radians(fov / 2.0f));
                C.c0.x = (1.0f / aspect) * cotFov;
                C.c1.y = cotFov;

                if (gdt == GraphicsDeviceType.Direct3D11 || gdt == GraphicsDeviceType.Direct3D12)
                {
                    C.c2.z = +far / (far - near);
                    C.c3.z = -(far * near / (far - near));
                    C.c2.w = 1.0f;
                    C.c3.w = 0.0f;
                }
                else if (gdt == GraphicsDeviceType.OpenGLCore || gdt == GraphicsDeviceType.OpenGLES3)
                {
                    C.c2.z = (far + near) / (far - near);
                    C.c3.z = -(2 * far * near) / (far - near);
                    C.c2.w = 1.0f;
                    C.c3.w = 0.0f;
                }
                else if (gdt == GraphicsDeviceType.Vulkan)
                {
                    C.c2.z = +far / (far - near);
                    C.c3.z = -(far * near / (far - near));
                    C.c2.w = 1.0f;
                    C.c3.w = 0.0f;
                }
            }
            else
            {
                float3 s = float3.zero;
                float3 t = float3.zero;

                if (gdt == GraphicsDeviceType.Direct3D11 || gdt == GraphicsDeviceType.Direct3D12)
                {
                    s = new float3(1.0f, 1.0f, 1.0f) / new float3(aspect * hv, hv, far - near);
                    t = new float3(0.0f, 0.0f, -near);
                }
                else if (gdt == GraphicsDeviceType.OpenGLCore || gdt == GraphicsDeviceType.OpenGLES3)
                {
                    s = new float3(1.0f, 1.0f, 2.0f) / new float3(aspect * hv, hv, far - near);
                    t = new float3(0.0f, 0.0f, (far + near) * (-0.5f));
                }
                else if (gdt == GraphicsDeviceType.Vulkan)
                {
                    s = new float3(1.0f, 1.0f, 1.0f) / new float3(aspect * hv, hv, far - near);
                    t = new float3(0.0f, 0.0f, -near);
                }

                C.c0.x = s.x;
                C.c1.y = s.y;
                C.c2.z = s.z;
                C.c3 = new float4(s * t, 1.0f);
            }
        }

        S = float4x4.identity;
        {
            float3 s = float3.zero;
            float3 t = float3.zero;
            if (gdt == GraphicsDeviceType.Direct3D11 || gdt == GraphicsDeviceType.Direct3D12)
            {
                t = new float3(new float2(x, y) + new float2(w, h) * 0.5f, 0.0f);
                s = new float3(new float2(w, h) * 0.5f, 1.0f);
            }
            else if (gdt == GraphicsDeviceType.OpenGLCore || gdt == GraphicsDeviceType.OpenGLES3)
            {
                t = new float3(new float2(x, y) + new float2(w, h) * 0.5f, 0.5f);
                s = new float3(new float2(w, h) * 0.5f, 0.5f);
            }
            else if (gdt == GraphicsDeviceType.Vulkan)
            {
                t = new float3(new float2(x, y) + new float2(w, h) * 0.5f, 0.0f);
                s = new float3(new float2(w, h) * 0.5f, 1.0f);
            }

            S.c3 = new float4(t, 1.0f);
            S.c0.x = s.x;
            S.c1.y = s.y;
            S.c2.z = s.z;
        }

        CV = math.mul(C, V);
    }

}


public class BufferBase<T> where T : struct
{
    public int count
    {
        get; set;
    }

    public T[] data
    {
        get; set;
    }

    public ComputeBuffer value
    {
        get; set;
    }

    public BufferBase(int count)
    {
        this.count = count;
        data = new T[count];
    }

    public void Release()
    {
        if (value != null)
        {
            value.Release();
            //value.Dispose();
        }
    }

    public static void Release(BufferBase<T> buffer)
    {
        if(buffer != null)
        {
            buffer.Release();            
            buffer = null;
        }
    }
}

public class COBuffer<T> : BufferBase<T> where T : struct
{
    public COBuffer(int count) : base(count)
    {
        value = new ComputeBuffer(count, Marshal.SizeOf<T>(), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
    }

    public void Write()
    {
        value.SetData(data);
    }
}

public class ROBuffer<T> : BufferBase<T> where T : struct
{
    public ROBuffer(int count) : base(count)
    {
        value = new ComputeBuffer(count, Marshal.SizeOf<T>(), ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write()
    {
        var na = value.BeginWrite<T>(0, count);
        for (int i = 0; i < count; i++)
        {
            na[i] = data[i];
        }
        value.EndWrite<T>(count);
    }
}

public class RWBuffer<T> : BufferBase<T> where T : struct
{
    public RWBuffer(int count) : base(count)
    {
        value = new ComputeBuffer(count, Marshal.SizeOf<T>());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write()
    {
        value.SetData(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Read()
    {
        //value.GetData(data);

        var read = AsyncGPUReadback.Request(value);
        read.WaitForCompletion();

        var na = read.GetData<T>(0);
        for (int i = 0; i < count; i++)
        {
            data[i] = na[i];
        }

        int a = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Read(CommandBuffer cmd)
    {
        cmd.RequestAsyncReadback(value,
            (read) =>
            {
                var na = read.GetData<T>(0);
                for (int i = 0; i < count; i++)
                {
                    data[i] = na[i];
                }
            });
    }
}


public class ColliderRender
{  
    Shader gshader;
    ComputeShader cshader;
    public int insCount = 1;   
    public Color color { get; set; } = Color.green;

    [Serializable]
    public enum Type
    {
        Box, Sphere, Capsule, Cylinder
    }

    public Type type;

    public ColliderRender(Transform[] trs, Type type)
    {
        {
            this.trs = trs;
            insCount = trs.Length;

            this.type = type;
        }        
    }

    public void Init(Shader gshader, ComputeShader cshader)
    {
        this.gshader = gshader;
        this.cshader = cshader;

        InitObject();

        InitCS();
        InitRender();
    }

    public void Enable()
    {        
        RenderGOM.OnRenderCamDebug += Render;
    }

    public void Disable()
    {        
        RenderGOM.OnRenderCamDebug -= Render;
    }
    
    public void Begin()
    {

    }
    
    public void Destroy()
    {
        ReleaseResource();
    }

    Mesh mesh;
    Material mte;
    MaterialPropertyBlock mpb;
    int pass;

    int[] baseVtx;

    void InitRender()
    {
        if (type == Type.Box)
        {            
            mesh = RenderUtil.CreateBoxMeshWirePartsDetail_Normal(new float3(0.5f, 0.5f, 0.5f), out baseVtx);
        }
        else if (type == Type.Sphere)
        {           
            mesh = RenderUtil.CreateSphereMeshWirePartsDetail_Normal(1.0f, 12, 12, out baseVtx);
        }
        else if (type == Type.Capsule)
        {           
            mesh = RenderUtil.CreateCapsuleMeshWirePartsDetail_Normal(1.0f, 4.0f, 12, 12, 1, out baseVtx);
        }
        else if (type == Type.Cylinder)
        {                      
            mesh = RenderUtil.CreateCylinderMeshWirePartsDetail_Normal(1.0f, 1.0f, 4, 12, 1, out baseVtx);
        }

        {
            mte = new Material(gshader);
            mpb = new MaterialPropertyBlock();
            pass = mte.FindPass("Capsule_Test");
        }

        {
            Vector4 _baseVtx = new Vector4(baseVtx[0], baseVtx[1], baseVtx[2], baseVtx[3]);

            mpb.SetVector("baseVtx", _baseVtx);

            mpb.SetBuffer("W0_Buffer", W0_Buffer.value);
            mpb.SetBuffer("W1_Buffer", W1_Buffer.value);
            mpb.SetBuffer("W2_Buffer", W2_Buffer.value);
        }
    }
   
    public Transform[] trs { get; set; }

    public BoxCollider[] boxCols    {get; set;}
    public SphereCollider[] spCols  {get; set;}
    public CapsuleCollider[] capCols{get; set;}
    public NavMeshAgent[] cydCols { get; set; }

    void InitObject()
    {    
        if (type == Type.Box)
        {
            boxCols = new BoxCollider[insCount];
        }
        else if (type == Type.Sphere)
        {
            spCols = new SphereCollider[insCount];
        }
        else if (type == Type.Capsule)
        {
            capCols = new CapsuleCollider[insCount];
        }
        else if (type == Type.Cylinder)
        {
            cydCols = new NavMeshAgent[insCount];
        }       

        for (int i = 0; i < insCount; i++)
        {
            if (type == Type.Box)
            {
                boxCols[i] = trs[i].GetComponent<BoxCollider>();
                boxCols[i].size = new Vector3(1.0f, 1.0f, 1.0f);
            }
            else if (type == Type.Sphere)
            {
                spCols[i] = trs[i].GetComponent<SphereCollider>();
            }
            else if (type == Type.Capsule)
            {
                capCols[i] = trs[i].GetComponent<CapsuleCollider>();
            }
            else if (type == Type.Cylinder)
            {
                cydCols[i] = trs[i].GetComponent<NavMeshAgent>();
            }
        }
    }


    int kidx;
    const int grCount = 64;
    int dvCount;

    float4 countInfo;

    void InitCS()
    {
        {

            if (type == Type.Box)
            {
                kidx = cshader.FindKernel("CS_Box_W");
            }
            else if (type == Type.Sphere)
            {
                kidx = cshader.FindKernel("CS_Sphere_W");
            }
            else if (type == Type.Capsule)
            {
                kidx = cshader.FindKernel("CS_Capsule_W");
            }
            else if (type == Type.Cylinder)
            {
                kidx = cshader.FindKernel("CS_Cylinder_W");
            }

            dvCount = (insCount % grCount == 0) ? (insCount / grCount) : (insCount / grCount + 1);
            countInfo.x = (float)insCount;          
        }

        InitResource();
    }
   
    ROBuffer<float4x4> data0_Buffer;
    ROBuffer<float4x4> data1_Buffer;

    RWBuffer<float4x4> W0_Buffer;
    RWBuffer<float4x4> W1_Buffer;
    RWBuffer<float4x4> W2_Buffer;

    void InitResource()
    {       
        {
            data0_Buffer = new ROBuffer<float4x4>(insCount);
            data1_Buffer = new ROBuffer<float4x4>(insCount);

            W0_Buffer = new RWBuffer<float4x4>(insCount);
            W1_Buffer = new RWBuffer<float4x4>(insCount);
            W2_Buffer = new RWBuffer<float4x4>(insCount);
        }      
    }
       

    public void Compute(ScriptableRenderContext context, CommandBuffer cmd)
    {
        if(bRender)
        {
            WriteToResource(context, cmd);
            DispatchCompute(context, cmd);
            //ReadFromResource(context, cmd);
        }
    }

    void WriteToResource(ScriptableRenderContext context, CommandBuffer cmd)
    {        
        {
            var array = data0_Buffer.data;
            for (int i = 0; i < insCount; i++)
            {
                float4x4 data = float4x4.zero;

                if (type == Type.Box)
                {
                    float3 c = boxCols[i].center;
                    float3 size = boxCols[i].size;

                    data.c0 = new float4(c, 0.0f);
                    data.c1 = new float4(size, 0.0f);
                }
                else if (type == Type.Sphere)
                {
                    float3 c = spCols[i].center;
                    float r = spCols[i].radius;

                    data.c0 = new float4(c, r);
                }
                else if (type == Type.Capsule)
                {
                    float3 c = capCols[i].center;
                    float d = (float)capCols[i].direction;
                    float r = capCols[i].radius;
                    float h = capCols[i].height;
                    float hh = 0.5f * h - r;
                    hh = (hh < 0.0f) ? (0.0f) : (hh);

                    data.c0 = new float4(c, 0.0f);
                    data.c1 = new float4(d, r, h, hh);
                }
                else if (type == Type.Cylinder)
                {
                    float o = cydCols[i].baseOffset;
                    float r = cydCols[i].radius;
                    float h = cydCols[i].height;

                    data.c0 = new float4(o, r, h, 0.0f);
                }
                
                array[i] = data;
            }
            data0_Buffer.Write();
        }

        {
            var array = data1_Buffer.data;
            for (int i = 0; i < insCount; i++)
            {
                float4x4 data = float4x4.zero;
                data.c0 = new float4(trs[i].position, 0.0f);
                data.c1 = ((quaternion)(trs[i].rotation)).value;
                data.c2 = new float4(trs[i].localScale, 0.0f);
                
                array[i] = data;
            }
            data1_Buffer.Write();
        }
    }

    void DispatchCompute(ScriptableRenderContext context, CommandBuffer cmd)
    {              
        {
            cmd.SetComputeVectorParam(cshader, "countInfo", countInfo);

            cmd.SetComputeBufferParam(cshader, kidx, "data0_Buffer", data0_Buffer.value);
            cmd.SetComputeBufferParam(cshader, kidx, "data1_Buffer", data1_Buffer.value);

            cmd.SetComputeBufferParam(cshader, kidx, "W0_Buffer", W0_Buffer.value);
            cmd.SetComputeBufferParam(cshader, kidx, "W1_Buffer", W1_Buffer.value);
            cmd.SetComputeBufferParam(cshader, kidx, "W2_Buffer", W2_Buffer.value);
        }

        cmd.DispatchCompute(cshader, kidx, dvCount, 1, 1);     
    }

    void ReadFromResource(ScriptableRenderContext context, CommandBuffer cmd)
    {
        W0_Buffer.Read(cmd);
        W1_Buffer.Read(cmd);
        W2_Buffer.Read(cmd);        
    }

    void ReleaseResource()
    {
        Action<ComputeBuffer> ReleaseBuffer =
        (buffer) =>
        {
            if (buffer != null)
            {
                buffer.Release();
                buffer = null;
            }
        };

        BufferBase<float4x4>.Release(data0_Buffer);
        BufferBase<float4x4>.Release(data1_Buffer);
        BufferBase<float4x4>.Release(W0_Buffer   );
        BufferBase<float4x4>.Release(W1_Buffer   );
        BufferBase<float4x4>.Release(W2_Buffer   );        
    }

    public bool bRender = true;

    void Render(ScriptableRenderContext context, CommandBuffer cmd, Camera cam, RenderGOM.PerCamera perCam)
    {
        if(bRender)
        {
            {
                Compute(context, cmd);
            }

            {
                //color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                mpb.SetColor("color", color);
                mpb.SetMatrix("CV", perCam.CV);
                mpb.SetVector("dirW_view", new float4(math.rotate(cam.transform.rotation, new float3(0.0f, 0.0f, 1.0f)), 0.0f));
                mpb.SetVector("posW_view", cam.transform.position);
            }

            {
                cmd.DrawMeshInstancedProcedural(mesh, 0, mte, pass, insCount, mpb);
            }
        }       
    }
}


//Test
class Texture3DBase<T> where T : struct
{
    public int3 dim;
    public T[,,] data;

    public Texture3DBase(int3 dim)
    {
        this.dim = dim;
        data = new T[dim.z, dim.y, dim.x];
    }
}

class ROTexture3D<T> : Texture3DBase<T> where T : struct
{
    public Texture3D value;

    public ROTexture3D(int3 dim) : base(dim)
    {
        value = new Texture3D(dim.x, dim.y, dim.z, TextureFormat.RGBAFloat, false);
    }

    public void Write()
    {
        
    }
}

class RWTexture3D<T> : Texture3DBase<T> where T : struct
{
    public RWTexture3D(int3 dim) : base(dim)
    {

    }
}