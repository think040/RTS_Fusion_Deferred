using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using UnityEngine.Timeline;
using System.ComponentModel.Design;
using UnityEngine.LowLevel;
using UnityEngine.Playables;

namespace Utility_JSB
{
    public enum GraphicsAPI
    {
        OPENGL = 0,
        DIRECT3D = 1
    }

    public static class Coordinates
    {
        static Coordinates()
        {
            //GraphicsDeviceType[] gdt = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows);

            GraphicsDeviceType[] gdt = new GraphicsDeviceType[4];
            gdt[0] = SystemInfo.graphicsDeviceType;

            Debug.Log(gdt[0]);
            if (gdt[0] == GraphicsDeviceType.OpenGLCore)
            {
                GAPI = GraphicsAPI.OPENGL;
                //Debug.Log("GraphicsAPI.OPEGL");
            }
            else if (gdt[0] == GraphicsDeviceType.Direct3D11)
            {
                GAPI = GraphicsAPI.DIRECT3D;
                //Debug.Log("GraphicsAPI.DIRECT3D");
            }

        }

        public static GraphicsAPI GAPI;

        //Local To Screen
        public static Matrix4x4 GetLocalToWorldMatrix(Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            Matrix4x4 mat = Matrix4x4.identity;
            Matrix4x4 matTranslation = Matrix4x4.identity;
            Matrix4x4 matRotation = Matrix4x4.identity;
            Matrix4x4 matScale = Matrix4x4.identity;

            //Translation
            matTranslation.m03 = translation.x;
            matTranslation.m13 = translation.y;
            matTranslation.m23 = translation.z;

            //Rotation
            matRotation = Matrix4x4.Rotate(rotation);

            //Scale
            matScale.m00 = scale.x;
            matScale.m11 = scale.y;
            matScale.m22 = scale.z;

            //Multiplication
            mat = matTranslation * matRotation * matScale;

            return mat;
        }

        public static Matrix4x4 GetWorldToViewMatrix(Vector3 translation, Quaternion rotation, GraphicsAPI GAPI)
        {
            Matrix4x4 mat = Matrix4x4.identity;
            Matrix4x4 matTranslation = Matrix4x4.identity;
            Matrix4x4 matRotation = Matrix4x4.identity;
            Matrix4x4 matZReflection = Matrix4x4.identity;

            //Translation
            matTranslation.m03 = translation.x;
            matTranslation.m13 = translation.y;
            matTranslation.m23 = translation.z;

            //Rotation
            matRotation = Matrix4x4.Rotate(rotation);

            //ZReflection
            if (GAPI == GraphicsAPI.OPENGL)
            {
                matZReflection.m22 = -1.0f;
            }
            else if (GAPI == GraphicsAPI.DIRECT3D)
            {
                matZReflection.m22 = 1.0f;
            }
            else
            {
                matZReflection.m22 = 1.0f;
            }

            mat = Matrix4x4.Inverse(matTranslation * matRotation * matZReflection);

            return mat;
        }

        public static Matrix4x4 GetViewToClipMatrix(float near, float far, float aspect, float fov, GraphicsAPI GAPI)
        {
            Matrix4x4 mat = Matrix4x4.identity;
            Matrix4x4 mat1 = Matrix4x4.identity;
            mat1.m11 = -1.0f; mat1.m22 = -1.0f; mat1.m23 = 1.0f;

            if (GAPI == GraphicsAPI.OPENGL)
            {
                mat.m00 = (1.0f / (aspect * Mathf.Tan(fov / 2.0f * Mathf.Deg2Rad)));
                mat.m01 = 0.0f;
                mat.m02 = 0.0f;
                mat.m03 = 0.0f;

                mat.m10 = 0.0f;
                mat.m11 = (1.0f / (Mathf.Tan(fov / 2.0f * Mathf.Deg2Rad)));
                mat.m12 = 0.0f;
                mat.m13 = 0.0f;

                mat.m20 = 0.0f;
                mat.m21 = 0.0f;
                mat.m22 = -(far + near) / (far - near);
                mat.m23 = -(2 * far * near) / (far - near);

                mat.m30 = 0.0f;
                mat.m31 = 0.0f;
                mat.m32 = -1.0f;
                mat.m33 = 0.0f;
            }
            else if (GAPI == GraphicsAPI.DIRECT3D)
            {
                mat.m00 = (1.0f / (aspect * Mathf.Tan(fov / 2.0f * Mathf.Deg2Rad)));
                mat.m01 = 0.0f;
                mat.m02 = 0.0f;
                mat.m03 = 0.0f;

                mat.m10 = 0.0f;
                mat.m11 = (1.0f / (Mathf.Tan(fov / 2.0f * Mathf.Deg2Rad)));
                mat.m12 = 0.0f;
                mat.m13 = 0.0f;

                mat.m20 = 0.0f;
                mat.m21 = 0.0f;
                mat.m22 = (far) / (far - near);
                mat.m23 = -(far * near) / (far - near);

                //mat.m22 = -((far) / (far - near)) + 1;
                //mat.m23 = (far * near) / (far - near);

                //mat.m22 = (far + near) / (far - near);
                //mat.m23 = -(2 * far * near) / (far - near);

                mat.m30 = 0.0f;
                mat.m31 = 0.0f;
                mat.m32 = 1.0f;
                mat.m33 = 0.0f;

                mat = mat1 * mat;
            }

            return mat;
        }

        public static Matrix4x4 GetNDCtoScreenMatrix(Vector2 size, Rect normalRect, GraphicsAPI GAPI)
        {
            Matrix4x4 mat = Matrix4x4.identity;
            Matrix4x4 mat1 = Matrix4x4.identity;
            mat1.m11 = -1.0f; mat1.m22 = -1.0f; mat1.m23 = 1.0f;
            Matrix4x4 translationMat = Matrix4x4.identity;
            Matrix4x4 scaleMat = Matrix4x4.identity;

            Vector2 vpos = new Vector2(normalRect.position.x * size.x, normalRect.position.y * size.y);
            Vector2 vsize = new Vector2(normalRect.size.x * size.x, normalRect.size.y * size.y);

            if (GAPI == GraphicsAPI.OPENGL)
            {
                //translation
                translationMat.m03 = vpos.x + vsize.x / 2.0f;
                translationMat.m13 = vpos.y + vsize.y / 2.0f;
                translationMat.m23 = 0.5f;

                //scale
                scaleMat.m00 = vsize.x / 2.0f;
                scaleMat.m11 = vsize.y / 2.0f;
                scaleMat.m22 = 0.5f;

                //Multiplication
                mat = translationMat * scaleMat;
            }
            else if (GAPI == GraphicsAPI.DIRECT3D)
            {
                vpos.y = (1.0f - (normalRect.position.y + normalRect.size.y)) * size.y;

                //translation
                translationMat.m03 = vpos.x + vsize.x / 2.0f;
                translationMat.m13 = vpos.y + vsize.y / 2.0f;

                //scale
                scaleMat.m00 = vsize.x / 2.0f;
                scaleMat.m11 = vsize.y / 2.0f;

                //Multiplication
                mat = translationMat * scaleMat;
                //mat = translationMat * scaleMat * mat1;
            }


            return mat;
        }

        //Screen To Local
        public static Matrix4x4 GetScreenToNDCmatrix(Vector2 size, Rect normalRect, GraphicsAPI GAPI)
        {

            Matrix4x4 mat = Matrix4x4.identity;
            Matrix4x4 translationMatIv = Matrix4x4.identity;
            Matrix4x4 scaleMatIv = Matrix4x4.identity;
            Matrix4x4 mat1 = Matrix4x4.identity;

            Vector2 vpos = new Vector2(normalRect.position.x * size.x, normalRect.position.y * size.y);
            Vector2 vsize = new Vector2(normalRect.size.x * size.x, normalRect.size.y * size.y);

            if (GAPI == GraphicsAPI.OPENGL)
            {
                //scaleIv
                scaleMatIv.m00 = 2.0f / vsize.x;
                scaleMatIv.m11 = 2.0f / vsize.y;
                scaleMatIv.m22 = 2.0f;

                //translationIv
                translationMatIv.m03 = -(vpos.x + vsize.x / 2.0f);
                translationMatIv.m13 = -(vpos.y + vsize.y / 2.0f);
                translationMatIv.m23 = -0.5f;

            }
            else if (GAPI == GraphicsAPI.DIRECT3D)
            {
                vpos.y = (1.0f - (normalRect.position.y + normalRect.size.y)) * size.y;

                //scaleIv
                scaleMatIv.m00 = 2.0f / vsize.x;
                scaleMatIv.m11 = 2.0f / vsize.y;

                //translationIv
                translationMatIv.m03 = -(vpos.x + vsize.x / 2.0f);
                translationMatIv.m13 = -(vpos.y + vsize.y / 2.0f);
            }

            //Multiplication
            mat = scaleMatIv * translationMatIv;

            return mat;
        }

        public static Matrix4x4 GetClipToViewMatrix(float near, float far, float aspect, float fov, GraphicsAPI GAPI)
        {
            Matrix4x4 mat = Matrix4x4.identity;
            Matrix4x4 mat1 = Matrix4x4.identity;
            mat1.m11 = -1.0f; mat1.m22 = -1.0f; mat1.m23 = 1.0f;

            if (GAPI == GraphicsAPI.OPENGL)
            {
                mat.m00 = aspect * Mathf.Tan(fov / 2.0f * Mathf.Deg2Rad);
                mat.m11 = Mathf.Tan(fov / 2.0f * Mathf.Deg2Rad);

                mat.m22 = 0.0f;
                mat.m23 = -1.0f;
                mat.m32 = -(far - near) / (2.0f * far * near);
                mat.m33 = (far + near) / (2.0f * far * near);

            }
            else if (GAPI == GraphicsAPI.DIRECT3D)
            {
                mat.m00 = aspect * Mathf.Tan(fov / 2.0f * Mathf.Deg2Rad);
                mat.m11 = Mathf.Tan(fov / 2.0f * Mathf.Deg2Rad);

                mat.m22 = 0.0f;
                mat.m23 = 1.0f;
                mat.m32 = -(far - near) / (far * near);
                mat.m33 = (far) / (far * near);

                mat = mat * mat1;
            }

            return mat;

            //Camera.main.rect.
        }

        public static Matrix4x4 GetViewToWorldMatrix(Vector3 translation, Quaternion rotation, GraphicsAPI GAPI)
        {
            Matrix4x4 mat = Matrix4x4.identity;
            Matrix4x4 matTranslation = Matrix4x4.identity;
            Matrix4x4 matRotation = Matrix4x4.identity;
            Matrix4x4 matZReflection = Matrix4x4.identity;

            //Translation
            matTranslation.m03 = translation.x;
            matTranslation.m13 = translation.y;
            matTranslation.m23 = translation.z;

            //Rotation
            matRotation = Matrix4x4.Rotate(rotation);

            //ZReflection
            if (GAPI == GraphicsAPI.OPENGL)
            {
                matZReflection.m22 = -1.0f;
            }
            else if (GAPI == GraphicsAPI.DIRECT3D)
            {
                matZReflection.m22 = 1.0f;
            }
            else
            {
                matZReflection.m22 = 1.0f;
            }

            mat = matTranslation * matRotation * matZReflection;

            return mat;
        }

        public static Matrix4x4 GetWorldToLocalMatrix(Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            Matrix4x4 mat = Matrix4x4.identity;

            return mat;
        }

        //Directional Shadow
        public static Matrix4x4 GetViewtoClipMatrix_Ortho(float near, float far, float left, float right, float bottom, float top, GraphicsAPI GAPI)
        {
            Matrix4x4 mat = Matrix4x4.identity;
            Matrix4x4 mat1 = Matrix4x4.identity;
            Matrix4x4 mat2 = Matrix4x4.identity;

            if (GAPI == GraphicsAPI.OPENGL)
            {
                mat1.m03 = -(right + left) / 2.0f;
                mat1.m13 = -(top + bottom) / 2.0f;
                mat1.m23 = -((-far) + (-near)) / 2.0f;

                mat.m00 = 2.0f / (right - left);
                mat.m11 = 2.0f / (top - bottom);
                mat.m22 = 2.0f / ((-far) - (-near));

                mat = mat * mat1;
            }
            else if (GAPI == GraphicsAPI.DIRECT3D)
            {
                mat1.m03 = -(right + left) / 2.0f;
                mat1.m13 = -(top + bottom) / 2.0f;
                mat1.m23 = -(far + near) / 2.0f;

                mat.m00 = 2.0f / (right - left);
                mat.m11 = 2.0f / (top - bottom);
                mat.m22 = 2.0f / (far - near);

                mat2.m11 = -1.0f; mat2.m22 = -0.5f; mat2.m23 = 0.5f;
                //mat2.m22 = 0.5f;     mat2.m23 = 0.5f;

                mat = mat2 * mat * mat1;
            }

            return mat;
        }

        //RayCasting
        public static Ray ScreenPointToRayCustom(Vector3 scPoint, Camera cam, GraphicsAPI GAPI)
        {
            //Camera cam = Camera.main;
            Ray ray = new Ray();
            Vector3 origin = Vector3.zero;
            Vector3 direction = Vector3.zero;
            Matrix4x4 mat = Matrix4x4.identity;
            mat.m11 = -1.0f;
            mat.m13 = cam.pixelRect.height;
            mat.m22 = -1.0f;
            mat.m23 = 1.0f;

            Vector4 pos = new Vector4(scPoint.x, scPoint.y, scPoint.z, 1.0f);

            if (GAPI == GraphicsAPI.OPENGL)
            {
                pos = new Vector4(scPoint.x, scPoint.y, scPoint.z, 1.0f);
            }
            else if (GAPI == GraphicsAPI.DIRECT3D)
            {
                pos = mat * pos;
            }

            Vector4 pos1 = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

            Matrix4x4 NS = Coordinates.GetScreenToNDCmatrix(cam.pixelRect.size, cam.rect, Coordinates.GAPI);
            Matrix4x4 pVN = Coordinates.GetClipToViewMatrix(
                cam.nearClipPlane, cam.farClipPlane, cam.aspect, cam.fieldOfView, Coordinates.GAPI);
            Matrix4x4 WV = Coordinates.GetViewToWorldMatrix(
                cam.transform.position, cam.transform.rotation, Coordinates.GAPI);

            pos = NS * pos;
            pos = pVN * pos;
            pos = new Vector4(pos.x / pos.w, pos.y / pos.w, pos.z / pos.w, 1.0f);
            pos = WV * pos;
            pos1 = WV * pos1;

            origin = new Vector3(pos1.x, pos1.y, pos1.z);
            direction = new Vector3(pos.x - pos1.x, pos.y - pos1.y, pos.z - pos1.z);

            ray.origin = origin;
            ray.direction = direction;

            return ray;
        }

        //Signed Normalized To UnSigned Normalized
        public static Matrix4x4 GetSignedNormalToUnSignedNormal(GraphicsAPI GAPI)
        {
            Matrix4x4 mat = Matrix4x4.identity;
            Matrix4x4 mat1 = Matrix4x4.identity;
            if (GAPI == GraphicsAPI.OPENGL)
            {
                mat.m03 = 0.5f; mat.m13 = 0.5f; mat.m23 = 0.5f;
                mat.m00 = 0.5f; mat.m11 = 0.5f; mat.m22 = 0.5f;
            }
            else if (GAPI == GraphicsAPI.DIRECT3D)
            {
                /*
                mat.m03 = 0.5f;     mat.m13 = 0.5f;     mat.m23 = 1.0f;
                mat.m00 = 0.5f;     mat.m11 = -0.5f;    mat.m22 = -1.0f;

                mat1.m13 = 1.0f;
                mat1.m11 = -1.0f;

                mat = mat1 * mat; */

                mat.m03 = 0.5f; mat.m13 = 0.5f;
                mat.m00 = 0.5f; mat.m11 = 0.5f;

                mat1.m13 = 1.0f;
                mat1.m11 = -1.0f;

                mat = mat1 * mat;
            }

            return mat;
        }

        public static void CompareAxis_ByQuaternionOrMatrix(Transform tr)
        {
            Matrix4x4 mat = Matrix4x4.identity;
            Matrix4x4 mat1 = Matrix4x4.identity;
            Vector3 vec = Vector3.zero;

            //By Quaternion
            vec = tr.rotation * Vector3.right;
            mat.m00 = vec.x; mat.m10 = vec.y; mat.m20 = vec.z;
            vec = tr.rotation * Vector3.up;
            mat.m01 = vec.x; mat.m11 = vec.y; mat.m21 = vec.z;
            vec = tr.rotation * Vector3.forward;
            mat.m02 = vec.x; mat.m12 = vec.y; mat.m22 = vec.z;
            vec = tr.position;
            mat.m03 = vec.x; mat.m13 = vec.y; mat.m23 = vec.z;
            Debug.Log("By Quaternion");
            Debug.Log(mat.ToString());

            //By Matrix
            mat1 = tr.localToWorldMatrix;
            Debug.Log("By Matrix");
            Debug.Log(mat1.ToString());
        }

        public static Matrix4x4 GetLocalToWorld(Transform tr)
        {
            Matrix4x4 mat = Matrix4x4.identity;
            mat = Matrix4x4.Rotate(tr.localRotation);
            mat.m03 = tr.localPosition.x; mat.m13 = tr.localPosition.y; mat.m23 = tr.localPosition.z;

            Matrix4x4 mat1 = Matrix4x4.identity;
            mat1.m00 = tr.localScale.x; mat1.m11 = tr.localScale.y; mat1.m22 = tr.localScale.z;
            mat = mat * mat1;

            if (tr.parent != null)
            {
                Matrix4x4 mat2 = Matrix4x4.identity;
                mat2 = GetLocalToWorld(tr.parent);
                mat = mat2 * mat;
            }

            return mat;
        }

        public static Matrix4x4 GetWorldToLocal(Transform tr)
        {
            Matrix4x4 mat = GetLocalToWorld(tr);
            return Matrix4x4.Inverse(mat);
        }

        /*
        [MenuItem("SceneView/Draw CustomShader")]
        static void SceneViewCustomSceneMode()
        {
            Shader sd = Shader.Find("Unlit/PN_Triangle_Shader");
            Debug.Log(sd.name);
            UnityEditor.ShaderUtil.RegisterShader(sd);

            SceneView sv = SceneView.currentDrawingSceneView;
            //sv.SetSceneViewShaderReplace(sd, null);


        }
        */
    }

    class TransformList : IEnumerable
    {
        public class Node
        {
            public int index;   //index in List
            public int parent;  //parent index in List
            public int height;  //tree height position of each node
            public Transform tr;
        }

        private List<Node> TrList;
        int list_index = -1;

        public TransformList(Transform tr)
        {
            this.TrList = new List<Node>();
            this.AddTrNode(tr, -1, -1);
        }

        void AddTrNode(Transform tr, int parent, int height)
        {
            int tree_height = ++height;
            int list_index = ++this.list_index;

            Node node = new Node();
            node.index = list_index;
            node.parent = parent;
            node.height = height;
            node.tr = tr;

            this.TrList.Add(node);

            for (int i = 0; i < tr.childCount; i++)
            {
                AddTrNode(tr.GetChild(i), list_index, tree_height);
            }
        }

        public Node this[int i]
        {
            get
            {
                IEnumerator e = TrList.GetEnumerator();
                int n = 0;
                e.Reset();
                while (e.MoveNext())
                {
                    if (n == i)
                    {
                        return (Node)e.Current;
                    }
                    n++;
                }

                return null;
            }
        }

        public int Count
        {
            get
            {
                return this.TrList.Count;
            }
        }

        public IEnumerator GetEnumerator()
        {
            IEnumerator e = this.Traveler();

            return e;
        }

        public IEnumerator Traveler()
        {
            foreach (Node node in this.TrList)
            {
                yield return node;
            }
        }
    }

    public static class Initialization
    {
        public static Mesh CreateTriangleMesh(float normalAngle, Mesh inMesh = null)
        {
            Mesh outMesh;
            if (inMesh != null)
            {
                outMesh = inMesh;
            }
            else
            {
                outMesh = new Mesh();
            }

            Vector3[] vertices = new Vector3[3];
            int[] indices = new int[3];
            Vector3[] normals = new Vector3[3];

            //Set vertices
            float size = 1.0f;
            vertices[0] = new Vector3(
                0.0f,
                size * Mathf.Sin(60.0f * Mathf.Deg2Rad) * (2.0f / 3.0f),
                0.0f);
            vertices[1] = Quaternion.AngleAxis(120.0f, new Vector3(0.0f, 0.0f, -1.0f)) * vertices[0];
            vertices[2] = Quaternion.AngleAxis(-120.0f, new Vector3(0.0f, 0.0f, -1.0f)) * vertices[0];

            //Set indices
            indices[0] = 0;
            indices[1] = 1;
            indices[2] = 2;

            //Set normals         
            normals[0] = Quaternion.AngleAxis(normalAngle, Vector3.Cross(Vector3.back, vertices[0])) * Vector3.back;
            normals[1] = Quaternion.AngleAxis(normalAngle, Vector3.Cross(Vector3.back, vertices[1])) * Vector3.back;
            normals[2] = Quaternion.AngleAxis(normalAngle, Vector3.Cross(Vector3.back, vertices[2])) * Vector3.back;

            //Applaying vertices indices normals
            List<Vector3> ListVertices = new List<Vector3>();
            foreach (Vector3 temp in vertices)
            {
                ListVertices.Add(temp);
            }

            List<Vector3> ListNormals = new List<Vector3>();
            foreach (Vector3 temp in normals)
            {
                ListNormals.Add(temp);
            }

            outMesh.SetVertices(ListVertices);
            outMesh.SetIndices(indices, MeshTopology.Triangles, 0);
            outMesh.SetNormals(ListNormals);

            return outMesh;
        }

        public static void InitMeshComponent(
            GameObject GObj,
            ref MeshFilter mf, ref Mesh mesh,
            ref MeshRenderer mr, ref Material material, Shader shader)
        {
            //MeshFilter Test
            if (GObj.GetComponent<MeshFilter>() != null)
            {
                mf = GObj.GetComponent<MeshFilter>();
                if (mf.mesh != null)
                {
                    mesh = mf.mesh;
                }
                else
                {
                    mesh = new Mesh();
                    mf.mesh = mesh;
                }
            }
            else
            {
                mf = GObj.AddComponent<MeshFilter>();
                if (mf.mesh != null)
                {
                    mesh = mf.mesh;
                }
                else
                {
                    mesh = new Mesh();
                    mf.mesh = mesh;
                }
            }

            //MeshRenderer Test
            if (GObj.GetComponent<MeshRenderer>() != null)
            {
                mr = GObj.GetComponent<MeshRenderer>();
                if (mr.material != null)
                {
                    material = mr.material;
                    material.shader = shader;
                }
                else
                {
                    material = new Material(shader);
                    mr.material = material;
                }
            }
            else
            {
                mr = GObj.AddComponent<MeshRenderer>();
                if (mr.material != null)
                {
                    material = mr.material;
                    material.shader = shader;
                }
                else
                {
                    material = new Material(shader);
                    mr.material = material;
                }
            }
        }

        public static void SendShaderVariable_TransformCameraLight(
            Material material,
            Transform tr, Camera cam, Light light)
        {
            Matrix4x4 W = Coordinates.GetLocalToWorldMatrix(
                   tr.position, tr.rotation, tr.localScale);
            Matrix4x4 V = Coordinates.GetWorldToViewMatrix(
                cam.gameObject.transform.position, cam.gameObject.transform.rotation, Coordinates.GAPI);
            Matrix4x4 C = Coordinates.GetViewToClipMatrix(
                cam.nearClipPlane, cam.farClipPlane, cam.aspect, cam.fieldOfView,
                Coordinates.GAPI);

            Matrix4x4 CVW_Camera = C * V * W;
            material.SetMatrix(Shader.PropertyToID("CVW_Camera"), CVW_Camera);

            Matrix4x4 VW_Camera = V * W;
            material.SetMatrix(Shader.PropertyToID("VW_Camera"), VW_Camera);

            Matrix4x4 VW_IT_Camera = Matrix4x4.Transpose(Matrix4x4.Inverse(V * W));
            material.SetMatrix(Shader.PropertyToID("VW_IT_Camera"), VW_IT_Camera);

            Vector4 vec = new Vector4(
                light.transform.position.x,
                light.transform.position.y,
                light.transform.position.z, 1);
            material.SetVector(
                Shader.PropertyToID("LightPosInView"),
                    V * vec);
        }

        public static void SendShaderVariable_TransformCameraLight(
            MaterialPropertyBlock mpb,
            Transform tr, Camera cam, Light light)
        {
            Matrix4x4 W = Coordinates.GetLocalToWorldMatrix(
                   tr.position, tr.rotation, tr.localScale);
            Matrix4x4 V = Coordinates.GetWorldToViewMatrix(
                cam.gameObject.transform.position, cam.gameObject.transform.rotation, Coordinates.GAPI);
            Matrix4x4 C = Coordinates.GetViewToClipMatrix(
                cam.nearClipPlane, cam.farClipPlane, cam.aspect, cam.fieldOfView,
                Coordinates.GAPI);

            Matrix4x4 CVW_Camera = C * V * W;
            mpb.SetMatrix(Shader.PropertyToID("CVW_Camera"), CVW_Camera);

            Matrix4x4 VW_Camera = V * W;
            mpb.SetMatrix(Shader.PropertyToID("VW_Camera"), VW_Camera);

            Matrix4x4 VW_IT_Camera = Matrix4x4.Transpose(Matrix4x4.Inverse(V * W));
            mpb.SetMatrix(Shader.PropertyToID("VW_IT_Camera"), VW_IT_Camera);

            Vector4 vec = new Vector4(
                light.transform.position.x,
                light.transform.position.y,
                light.transform.position.z, 1);
            mpb.SetVector(
                Shader.PropertyToID("LightPosInView"),
                    V * vec);
        }

        public static void SendShaderVariable_Matrix_ForRendering(
            MaterialPropertyBlock mpb,
            Transform tr, Camera cam, Light light)
        {
            Matrix4x4 W = Coordinates.GetLocalToWorldMatrix(
                   tr.position, tr.rotation, tr.localScale);
            Matrix4x4 V = Coordinates.GetWorldToViewMatrix(
                cam.gameObject.transform.position, cam.gameObject.transform.rotation, Coordinates.GAPI);
            Matrix4x4 C = Coordinates.GetViewToClipMatrix(
                cam.nearClipPlane, cam.farClipPlane, cam.aspect, cam.fieldOfView,
                Coordinates.GAPI);

            Matrix4x4 CVW_Camera = C * V * W;
            mpb.SetMatrix(Shader.PropertyToID("CVW_Camera"), CVW_Camera);

            Matrix4x4 VW_Camera = V * W;
            mpb.SetMatrix(Shader.PropertyToID("VW_Camera"), VW_Camera);

            Matrix4x4 VW_IT_Camera = Matrix4x4.Transpose(Matrix4x4.Inverse(V * W));
            mpb.SetMatrix(Shader.PropertyToID("VW_IT_Camera"), VW_IT_Camera);
            //
            Matrix4x4 V_Camera = V;
            mpb.SetMatrix(Shader.PropertyToID("V_Camera"), V_Camera);

            Matrix4x4 V_IT_Camera = Matrix4x4.Transpose(Matrix4x4.Inverse(V));
            mpb.SetMatrix(Shader.PropertyToID("V_IT_Camera"), V_IT_Camera);

            //Light
            Transform tr_light = light.gameObject.GetComponent<Transform>();
            Camera cam_light = light.gameObject.GetComponent<Camera>();

            Matrix4x4 V_Light = Coordinates.GetWorldToViewMatrix(
                tr_light.position, tr_light.rotation, Coordinates.GAPI);
            Matrix4x4 C_Light = Coordinates.GetViewToClipMatrix(
                cam_light.nearClipPlane, cam_light.farClipPlane, cam_light.aspect, cam_light.fieldOfView,
                Coordinates.GAPI);
            Matrix4x4 C_Light_Ortho = Coordinates.GetViewtoClipMatrix_Ortho(
                cam_light.nearClipPlane, cam_light.farClipPlane,
                -cam_light.orthographicSize, cam_light.orthographicSize,
                -cam_light.orthographicSize, cam_light.orthographicSize, Coordinates.GAPI);
            Matrix4x4 T = Coordinates.GetSignedNormalToUnSignedNormal(Coordinates.GAPI);

            //for point & spot light position and for directional light direction
            Matrix4x4 V_I_Light = Matrix4x4.Inverse(V_Light);
            mpb.SetMatrix(Shader.PropertyToID("V_I_Light"), V_I_Light);

            if (light.type == LightType.Point || light.type == LightType.Spot)
            {
                Matrix4x4 TCVW_Light = T * C_Light * V_Light * W;
                mpb.SetMatrix(Shader.PropertyToID("TCVW_Light"), TCVW_Light);
            }
            else if (light.type == LightType.Directional)
            {
                Matrix4x4 TCVW_Light = T * C_Light_Ortho * V_Light * W;
                mpb.SetMatrix(Shader.PropertyToID("TCVW_Light"), TCVW_Light);
            }
        }

        public static void SendShaderVariable_Matrix_ForShadowing(
            MaterialPropertyBlock mpb,
            Transform tr, Light light)
        {
            Matrix4x4 W = Coordinates.GetLocalToWorldMatrix(
                   tr.position, tr.rotation, tr.localScale);

            //Light
            Transform tr_light = light.gameObject.GetComponent<Transform>();
            Camera cam_light = light.gameObject.GetComponent<Camera>();

            Matrix4x4 V_Light = Coordinates.GetWorldToViewMatrix(
                tr_light.position, tr_light.rotation, Coordinates.GAPI);
            Matrix4x4 C_Light = Coordinates.GetViewToClipMatrix(
                cam_light.nearClipPlane, cam_light.farClipPlane, cam_light.aspect, cam_light.fieldOfView,
                Coordinates.GAPI);
            Matrix4x4 C_Light_Ortho = Coordinates.GetViewtoClipMatrix_Ortho(
                cam_light.nearClipPlane, cam_light.farClipPlane,
                -cam_light.orthographicSize, cam_light.orthographicSize,
                -cam_light.orthographicSize, cam_light.orthographicSize, Coordinates.GAPI);

            //for point & spot light position and for directional light direction
            Matrix4x4 V_I_Light = Matrix4x4.Inverse(V_Light);
            mpb.SetMatrix(Shader.PropertyToID("V_I_Light"), V_I_Light);

            if (light.type == LightType.Point || light.type == LightType.Spot)
            {
                Matrix4x4 CVW_Light = C_Light * V_Light * W;
                mpb.SetMatrix(Shader.PropertyToID("CVW_Light"), CVW_Light);
            }
            else if (light.type == LightType.Directional)
            {
                Matrix4x4 CVW_Light = C_Light_Ortho * V_Light * W;
                mpb.SetMatrix(Shader.PropertyToID("CVW_Light"), CVW_Light);
            }
        }

        public static void SendShaderVariable_SkinnedMesh(
            SkinnedMeshRenderer skmr)
        {
            Mesh mesh = skmr.sharedMesh;

            //Send BoneIndex and BoneWeight to Mesh
            List<Vector4> boneI = new List<Vector4>();
            List<Vector4> boneW = new List<Vector4>();

            for (int i = 0; i < mesh.vertexCount; i++)
            {
                BoneWeight boneWeight = mesh.boneWeights[i];
                Vector4 index = new Vector4(
                    (float)boneWeight.boneIndex0, (float)boneWeight.boneIndex1,
                    (float)boneWeight.boneIndex2, (float)boneWeight.boneIndex3);
                Vector4 weight = new Vector4(
                    boneWeight.weight0, boneWeight.weight1,
                    boneWeight.weight2, boneWeight.weight3);

                boneI.Add(index); boneW.Add(weight);
            }
            mesh.SetUVs(4, boneI);
            mesh.SetUVs(5, boneW);


        }

        public static void SendShaderVariable_Matrix_SkinnedMesh_ForRendering(
            MaterialPropertyBlock mpb, SkinnedMeshRenderer skmr,
            Camera cam, Light light)
        {
            Mesh mesh = skmr.sharedMesh;
            Transform[] bone = skmr.bones;
            Matrix4x4[] bindPose = mesh.bindposes;

            //Send WorldBoneMatrix to MaterialPropertyBlock
            int count = bone.Length;
            Matrix4x4[] W_Bone = new Matrix4x4[count];
            Matrix4x4[] W_IT_Bone = new Matrix4x4[count];
            for (int i = 0; i < count; i++)
            {
                W_Bone[i] = bone[i].localToWorldMatrix * bindPose[i];
                W_IT_Bone[i] = Matrix4x4.Transpose(Matrix4x4.Inverse(W_Bone[i]));
            }

            mpb.SetMatrixArray(Shader.PropertyToID("W_Bone"), W_Bone);
            mpb.SetMatrixArray(Shader.PropertyToID("W_IT_Bone"), W_IT_Bone);

            //
            Matrix4x4 V = Coordinates.GetWorldToViewMatrix(
                cam.gameObject.transform.position, cam.gameObject.transform.rotation, Coordinates.GAPI);
            Matrix4x4 C = Coordinates.GetViewToClipMatrix(
                cam.nearClipPlane, cam.farClipPlane, cam.aspect, cam.fieldOfView,
                Coordinates.GAPI);

            Matrix4x4 CV_Camera = C * V;
            mpb.SetMatrix(Shader.PropertyToID("CV_Camera"), CV_Camera);
            //
            Matrix4x4 V_Camera = V;
            mpb.SetMatrix(Shader.PropertyToID("V_Camera"), V_Camera);

            Matrix4x4 V_IT_Camera = Matrix4x4.Transpose(Matrix4x4.Inverse(V));
            mpb.SetMatrix(Shader.PropertyToID("V_IT_Camera"), V_IT_Camera);

            //Light
            Transform tr_light = light.gameObject.GetComponent<Transform>();
            Camera cam_light = light.gameObject.GetComponent<Camera>();

            Matrix4x4 V_Light = Coordinates.GetWorldToViewMatrix(
                tr_light.position, tr_light.rotation, Coordinates.GAPI);
            Matrix4x4 C_Light = Coordinates.GetViewToClipMatrix(
                cam_light.nearClipPlane, cam_light.farClipPlane, cam_light.aspect, cam_light.fieldOfView,
                Coordinates.GAPI);
            Matrix4x4 C_Light_Ortho = Coordinates.GetViewtoClipMatrix_Ortho(
                cam_light.nearClipPlane, cam_light.farClipPlane,
                -cam_light.orthographicSize, cam_light.orthographicSize,
                -cam_light.orthographicSize, cam_light.orthographicSize, Coordinates.GAPI);
            Matrix4x4 T = Coordinates.GetSignedNormalToUnSignedNormal(Coordinates.GAPI);

            //for point & spot light position and for directional light direction
            Matrix4x4 V_I_Light = Matrix4x4.Inverse(V_Light);
            mpb.SetMatrix(Shader.PropertyToID("V_I_Light"), V_I_Light);

            if (light.type == LightType.Point || light.type == LightType.Spot)
            {
                Matrix4x4 TCV_Light = T * C_Light * V_Light;
                mpb.SetMatrix(Shader.PropertyToID("TCV_Light"), TCV_Light);
            }
            else if (light.type == LightType.Directional)
            {
                Matrix4x4 TCV_Light = T * C_Light_Ortho * V_Light;
                mpb.SetMatrix(Shader.PropertyToID("TCV_Light"), TCV_Light);
            }
        }

        public static void SendShaderVariable_Matrix_SkinnedMesh_ForShadowing(
            MaterialPropertyBlock mpb, SkinnedMeshRenderer skmr,
            Light light)
        {
            Mesh mesh = skmr.sharedMesh;
            Transform[] bone = skmr.bones;
            Matrix4x4[] bindPose = mesh.bindposes;

            //Send WorldBoneMatrix to MaterialPropertyBlock
            int count = bone.Length;
            Matrix4x4[] W_Bone = new Matrix4x4[count];

            for (int i = 0; i < count; i++)
            {
                W_Bone[i] = bone[i].localToWorldMatrix * bindPose[i];
            }

            mpb.SetMatrixArray(Shader.PropertyToID("W_Bone"), W_Bone);

            //Light
            Transform tr_light = light.gameObject.GetComponent<Transform>();
            Camera cam_light = light.gameObject.GetComponent<Camera>();

            Matrix4x4 V_Light = Coordinates.GetWorldToViewMatrix(
                tr_light.position, tr_light.rotation, Coordinates.GAPI);
            Matrix4x4 C_Light = Coordinates.GetViewToClipMatrix(
                cam_light.nearClipPlane, cam_light.farClipPlane, cam_light.aspect, cam_light.fieldOfView,
                Coordinates.GAPI);
            Matrix4x4 C_Light_Ortho = Coordinates.GetViewtoClipMatrix_Ortho(
                cam_light.nearClipPlane, cam_light.farClipPlane,
                -cam_light.orthographicSize, cam_light.orthographicSize,
                -cam_light.orthographicSize, cam_light.orthographicSize, Coordinates.GAPI);

            //for point & spot light position and for directional light direction
            Matrix4x4 V_I_Light = Matrix4x4.Inverse(V_Light);
            mpb.SetMatrix(Shader.PropertyToID("V_I_Light"), V_I_Light);

            if (light.type == LightType.Point || light.type == LightType.Spot)
            {
                Matrix4x4 CV_Light = C_Light * V_Light;
                mpb.SetMatrix(Shader.PropertyToID("CV_Light"), CV_Light);
            }
            else if (light.type == LightType.Directional)
            {
                Matrix4x4 CV_Light = C_Light_Ortho * V_Light;
                mpb.SetMatrix(Shader.PropertyToID("CV_Light"), CV_Light);
            }
        }

    }

    public static class HermiteSpline
    {
        public static Vector3 Curve(Vector3 p0, Vector3 p1, Vector3 dp0, Vector3 dp1, float u)
        {
            Matrix4x4 matU = Matrix4x4.zero;
            Matrix4x4 matM = Matrix4x4.zero;
            Matrix4x4 matG = Matrix4x4.zero;
            Matrix4x4 matR = Matrix4x4.zero;
            Vector3 Position = Vector3.zero;

            //matU
            matU.m00 = u * u * u; matU.m01 = u * u; matU.m02 = u; matU.m03 = 1;

            //matM
            matM.m00 = 2; matM.m01 = -2; matM.m02 = 1; matM.m03 = 1;
            matM.m10 = -3; matM.m11 = 3; matM.m12 = -2; matM.m13 = -1;
            matM.m20 = 0; matM.m21 = 0; matM.m22 = 1; matM.m23 = 0;
            matM.m30 = 1; matM.m31 = 0; matM.m32 = 0; matM.m33 = 0;

            //matG
            matG.m00 = p0.x; matG.m01 = p0.y; matG.m02 = p0.z; matG.m03 = 0;
            matG.m10 = p1.x; matG.m11 = p1.y; matG.m12 = p1.z; matG.m13 = 0;
            matG.m20 = dp0.x; matG.m21 = dp0.y; matG.m22 = dp0.z; matG.m23 = 0;
            matG.m30 = dp1.x; matG.m31 = dp1.y; matG.m32 = dp1.z; matG.m33 = 0;

            //multiplication
            matR = matU * matM * matG;

            Position = new Vector3(matR.m00, matR.m01, matR.m02);
            return Position;
        }

        public static float3 Curve_f(float3 p0, float3 p1, float3 dp0, float3 dp1, float u)
        {
            float4 U = float4.zero;
            float4x4 M = float4x4.zero;
            float4x3 G = float4x3.zero;
            float3 pos = float3.zero;

            U = new float4(math.pow(u, 3.0f), math.pow(u, 2.0f), u, 1.0f);

            M.c0 = new float4(+2.0f, -3.0f, +0.0f, +1.0f);
            M.c1 = new float4(-2.0f, +3.0f, +0.0f, +0.0f);
            M.c2 = new float4(+1.0f, -2.0f, +1.0f, +0.0f);
            M.c3 = new float4(+1.0f, -1.0f, +0.0f, +0.0f);

            G.c0 = new float4(p0.x, p1.x, dp0.x, dp1.x);
            G.c1 = new float4(p0.y, p1.y, dp0.y, dp1.y);
            G.c2 = new float4(p0.z, p1.z, dp0.z, dp1.z);

            pos = math.mul(math.mul(U, M), G);

            return pos;
        }

        public static double3 Curve_d(double3 p0, double3 p1, double3 dp0, double3 dp1, double u)
        {
            double4 U = double4.zero;
            double4x4 M = double4x4.zero;
            double4x3 G = double4x3.zero;
            double3 pos = double3.zero;

            U = new double4(math.pow(u, 3.0), math.pow(u, 2.0), u, 1.0);

            M.c0 = new double4(+2.0, -3.0, +0.0, +1.0);
            M.c1 = new double4(-2.0, +3.0, +0.0, +0.0);
            M.c2 = new double4(+1.0, -2.0, +1.0, +0.0);
            M.c3 = new double4(+1.0, -1.0, +0.0, +0.0);

            G.c0 = new double4(p0.x, p1.x, dp0.x, dp1.x);
            G.c1 = new double4(p0.y, p1.y, dp0.y, dp1.y);
            G.c2 = new double4(p0.z, p1.z, dp0.z, dp1.z);

            pos = math.mul(math.mul(U, M), G);

            return pos;
        }


        public static Vector3 TCurve(Vector3 p0, Vector3 p1, Vector3 dp0, Vector3 dp1, float u)
        {
            Matrix4x4 matU = Matrix4x4.zero;
            Matrix4x4 matM = Matrix4x4.zero;
            Matrix4x4 matG = Matrix4x4.zero;
            Matrix4x4 matR = Matrix4x4.zero;
            Vector3 Tanget = Vector3.zero;

            //matU
            matU.m00 = 3 * u * u; matU.m01 = 2 * u; matU.m02 = 1; matU.m03 = 0;

            //matM
            matM.m00 = 2; matM.m01 = -2; matM.m02 = 1; matM.m03 = 1;
            matM.m10 = -3; matM.m11 = 3; matM.m12 = -2; matM.m13 = -1;
            matM.m20 = 0; matM.m21 = 0; matM.m22 = 1; matM.m23 = 0;
            matM.m30 = 1; matM.m31 = 0; matM.m32 = 0; matM.m33 = 0;

            //matG
            matG.m00 = p0.x; matG.m01 = p0.y; matG.m02 = p0.z; matG.m03 = 0;
            matG.m10 = p1.x; matG.m11 = p1.y; matG.m12 = p1.z; matG.m13 = 0;
            matG.m20 = dp0.x; matG.m21 = dp0.y; matG.m22 = dp0.z; matG.m23 = 0;
            matG.m30 = dp1.x; matG.m31 = dp1.y; matG.m32 = dp1.z; matG.m33 = 0;

            //multiplication
            matR = matU * matM * matG;

            Tanget = new Vector3(matR.m00, matR.m01, matR.m02);
            return Tanget;
        }

        public static float3 TCurve_f(float3 p0, float3 p1, float3 dp0, float3 dp1, float u)
        {
            float4 U = float4.zero;
            float4x4 M = float4x4.zero;
            float4x3 G = float4x3.zero;
            float3 tan = float3.zero;

            U = new float4(3.0f * math.pow(u, 2.0f), 2.0f * u, 1.0f, 0.0f);

            M.c0 = new float4(+2.0f, -3.0f, +0.0f, +1.0f);
            M.c1 = new float4(-2.0f, +3.0f, +0.0f, +0.0f);
            M.c2 = new float4(+1.0f, -2.0f, +1.0f, +0.0f);
            M.c3 = new float4(+1.0f, -1.0f, +0.0f, +0.0f);

            G.c0 = new float4(p0.x, p1.x, dp0.x, dp1.x);
            G.c1 = new float4(p0.y, p1.y, dp0.y, dp1.y);
            G.c2 = new float4(p0.z, p1.z, dp0.z, dp1.z);

            tan = math.mul(math.mul(U, M), G);

            return tan;
        }

        public static double3 TCurve_d(double3 p0, double3 p1, double3 dp0, double3 dp1, double u)
        {
            double4 U = double4.zero;
            double4x4 M = double4x4.zero;
            double4x3 G = double4x3.zero;
            double3 tan = double3.zero;

            U = new double4(3.0 * math.pow(u, 2.0), 2.0 * u, 1.0, 0.0);

            M.c0 = new double4(+2.0, -3.0, +0.0, +1.0);
            M.c1 = new double4(-2.0, +3.0, +0.0, +0.0);
            M.c2 = new double4(+1.0, -2.0, +1.0, +0.0);
            M.c3 = new double4(+1.0, -1.0, +0.0, +0.0);

            G.c0 = new double4(p0.x, p1.x, dp0.x, dp1.x);
            G.c1 = new double4(p0.y, p1.y, dp0.y, dp1.y);
            G.c2 = new double4(p0.z, p1.z, dp0.z, dp1.z);

            tan = math.mul(math.mul(U, M), G);

            return tan;
        }


        public static Vector3 NCurve(Vector3 p0, Vector3 p1, Vector3 dp0, Vector3 dp1, float u)
        {
            Matrix4x4 matU = Matrix4x4.zero;
            Matrix4x4 matM = Matrix4x4.zero;
            Matrix4x4 matG = Matrix4x4.zero;
            Matrix4x4 matR = Matrix4x4.zero;
            Vector3 Normal = Vector3.zero;

            //matU
            matU.m00 = 3 * 2 * u; matU.m01 = 2 * 1; matU.m02 = 0; matU.m03 = 0;

            //matM
            matM.m00 = 2; matM.m01 = -2; matM.m02 = 1; matM.m03 = 1;
            matM.m10 = -3; matM.m11 = 3; matM.m12 = -2; matM.m13 = -1;
            matM.m20 = 0; matM.m21 = 0; matM.m22 = 1; matM.m23 = 0;
            matM.m30 = 1; matM.m31 = 0; matM.m32 = 0; matM.m33 = 0;

            //matG
            matG.m00 = p0.x; matG.m01 = p0.y; matG.m02 = p0.z; matG.m03 = 0;
            matG.m10 = p1.x; matG.m11 = p1.y; matG.m12 = p1.z; matG.m13 = 0;
            matG.m20 = dp0.x; matG.m21 = dp0.y; matG.m22 = dp0.z; matG.m23 = 0;
            matG.m30 = dp1.x; matG.m31 = dp1.y; matG.m32 = dp1.z; matG.m33 = 0;

            //multiplication
            matR = matU * matM * matG;

            Normal = new Vector3(matR.m00, matR.m01, matR.m02);
            return Normal;
        }

        public static float3 NCurve_f(float3 p0, float3 p1, float3 dp0, float3 dp1, float u)
        {
            float4 U = float4.zero;
            float4x4 M = float4x4.zero;
            float4x3 G = float4x3.zero;
            float3 normal = float3.zero;

            U = new float4(6.0f * u, 2.0f, 0.0f, 0.0f);

            M.c0 = new float4(+2.0f, -3.0f, +0.0f, +1.0f);
            M.c1 = new float4(-2.0f, +3.0f, +0.0f, +0.0f);
            M.c2 = new float4(+1.0f, -2.0f, +1.0f, +0.0f);
            M.c3 = new float4(+1.0f, -1.0f, +0.0f, +0.0f);

            G.c0 = new float4(p0.x, p1.x, dp0.x, dp1.x);
            G.c1 = new float4(p0.y, p1.y, dp0.y, dp1.y);
            G.c2 = new float4(p0.z, p1.z, dp0.z, dp1.z);

            normal = math.mul(math.mul(U, M), G);

            return normal;
        }

        public static double3 NCurve_d(double3 p0, double3 p1, double3 dp0, double3 dp1, double u)
        {
            double4 U = double4.zero;
            double4x4 M = double4x4.zero;
            double4x3 G = double4x3.zero;
            double3 normal = double3.zero;

            U = new double4(6.0 * u, 2.0, 0.0, 0.0);

            M.c0 = new double4(+2.0, -3.0, +0.0, +1.0);
            M.c1 = new double4(-2.0, +3.0, +0.0, +0.0);
            M.c2 = new double4(+1.0, -2.0, +1.0, +0.0);
            M.c3 = new double4(+1.0, -1.0, +0.0, +0.0);

            G.c0 = new double4(p0.x, p1.x, dp0.x, dp1.x);
            G.c1 = new double4(p0.y, p1.y, dp0.y, dp1.y);
            G.c2 = new double4(p0.z, p1.z, dp0.z, dp1.z);

            normal = math.mul(math.mul(U, M), G);

            return normal;
        }


        public static Vector3 BCurve(Vector3 p0, Vector3 p1, Vector3 dp0, Vector3 dp1, float u)
        {
            Vector3 BNormal = Vector3.zero;

            BNormal = Vector3.Cross(
                HermiteSpline.TCurve(p0, p1, dp0, dp1, u),
                HermiteSpline.NCurve(p0, p1, dp0, dp1, u));
            return BNormal;
        }

        public static Quaternion LookRotation(Vector3 forward, Vector3 upward)
        {
            Matrix4x4 mat = Matrix4x4.zero;
            Vector4 vec4 = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            Vector3 vec3 = Vector3.zero;

            mat.m00 = Vector3.Cross(upward.normalized, forward.normalized).normalized.x;
            mat.m10 = Vector3.Cross(upward.normalized, forward.normalized).normalized.y;
            mat.m20 = Vector3.Cross(upward.normalized, forward.normalized).normalized.z;
            mat.m30 = 0.0f;

            mat.m01 = upward.normalized.x;
            mat.m11 = upward.normalized.y;
            mat.m21 = upward.normalized.z;
            mat.m31 = 0.0f;

            mat.m02 = forward.normalized.x;
            mat.m12 = forward.normalized.y;
            mat.m22 = forward.normalized.z;
            mat.m32 = 0.0f;

            mat.m03 = 0.0f;
            mat.m13 = 0.0f;
            mat.m23 = 0.0f;
            mat.m33 = 1.0f;

            Quaternion q = Quaternion.identity;
            float trace = mat.m00 + mat.m11 + mat.m22;

            if (trace >= 0)
            {
                q.w = 0.5f * Mathf.Sqrt(trace + 1);
                q.z = (+mat.m10 - mat.m01) / (4 * q.w);
                q.y = (-mat.m20 + mat.m02) / (4 * q.w);
                q.x = (+mat.m21 - mat.m12) / (4 * q.w);
            }
            else
            {
                float[] traceArray = new float[3];

                traceArray[0] = mat.m00;
                traceArray[1] = mat.m11;
                traceArray[2] = mat.m22;

                if (Mathf.Max(traceArray) == mat.m00)
                {
                    q.x = (0.5f) * Mathf.Sqrt(+mat.m00 - mat.m11 - mat.m22 + 1);
                    q.y = (+mat.m10 + mat.m01) / (4 * q.x);
                    q.z = (+mat.m20 + mat.m02) / (4 * q.x);
                    q.w = (+mat.m21 - mat.m12) / (4 * q.x);
                }
                else if (Mathf.Max(traceArray) == mat.m11)
                {
                    q.y = (0.5f) * Mathf.Sqrt(-mat.m00 + mat.m11 - mat.m22 + 1);
                    q.x = (+mat.m10 + mat.m01) / (4 * q.y);
                    q.w = (-mat.m20 + mat.m02) / (4 * q.y);
                    q.z = (+mat.m12 + mat.m21) / (4 * q.y);
                }
                else if (Mathf.Max(traceArray) == mat.m22)
                {
                    q.z = (0.5f) * Mathf.Sqrt(-mat.m00 - mat.m11 + mat.m22 + 1);
                    q.w = (+mat.m10 - mat.m01) / (4 * q.z);
                    q.x = (+mat.m20 + mat.m02) / (4 * q.z);
                    q.y = (+mat.m21 + mat.m12) / (4 * q.z);
                }

            }

            return q;
        }


        public static float GetArcLength(Vector3 p0, Vector3 p1, Vector3 dp0, Vector3 dp1, float delta)
        {
            float length = 0.0f;
            Vector3 pos1 = Vector3.zero;
            Vector3 pos2 = Vector3.zero;
            float u = 0.0f;

            while (u <= 1.0f)
            {
                pos1 = HermiteSpline.Curve(p0, p1, dp0, dp1, u);
                u = u + delta;
                pos2 = HermiteSpline.Curve(p0, p1, dp0, dp1, u);
                length = length + Vector3.Distance(pos1, pos2);
            }

            return length;
        }

        public static float GetArcLength_f(float3 p0, float3 p1, float3 dp0, float3 dp1, float delta)
        {
            float length = 0.0f;
            float3 pos1 = float3.zero;
            float3 pos2 = float3.zero;
            float u = 0.0f;

            while (u <= 1.0f)
            {
                pos1 = HermiteSpline.Curve_f(p0, p1, dp0, dp1, u);
                u = u + delta;
                pos2 = HermiteSpline.Curve_f(p0, p1, dp0, dp1, u);
                length = length + math.distance(pos1, pos2);
            }

            return length;
        }


        //
        public static float4x3 getMG(float3 p0, float3 p1, float3 dp0, float3 dp1)
        {
            float4x3 MG = float4x3.zero;
            float4x4 M = float4x4.zero;
            float4x3 G = float4x3.zero;

            M.c0 = new float4(+2.0f, -3.0f, +0.0f, +1.0f);
            M.c1 = new float4(-2.0f, +3.0f, +0.0f, +0.0f);
            M.c2 = new float4(+1.0f, -2.0f, +1.0f, +0.0f);
            M.c3 = new float4(+1.0f, -1.0f, +0.0f, +0.0f);

            G.c0 = new float4(p0.x, p1.x, dp0.x, dp1.x);
            G.c1 = new float4(p0.y, p1.y, dp0.y, dp1.y);
            G.c2 = new float4(p0.z, p1.z, dp0.z, dp1.z);

            MG = math.mul(M, G);

            return MG;
        }

        public static float3 CurveMG(float u, float4x3 MG)
        {
            float3 pos = float3.zero;
            float4 U = float4.zero;

            U = new float4(math.pow(u, 3.0f), math.pow(u, 2.0f), u, 1.0f);

            pos = math.mul(U, MG);

            return pos;
        }

        public static float3 TCurveMG(float u, float4x3 MG)
        {
            float3 pos = float3.zero;
            float4 U = float4.zero;

            U = new float4(3.0f * math.pow(u, 2.0f), 2.0f * u, 1.0f, 0.0f);

            pos = math.mul(U, MG);

            return pos;
        }

        public static float3 NCurveMG(float u, float4x3 MG)
        {
            float3 pos = float3.zero;
            float4 U = float4.zero;

            U = new float4(6.0f * u, 2.0f, 0.0f, 0.0f);

            pos = math.mul(U, MG);

            return pos;
        }


        public static float3 BCurveGM(float u, float4x3 MG)
        {
            float3 T;
            float3 N;
            float3 B;

            T = TCurveMG(u, MG);
            N = NCurveMG(u, MG);
            B = math.cross(T, N);

            return B;
        }

        public static float3 BCurveTN(float3 T, float3 N, float u)
        {
            float3 B;

            B = math.cross(T, N);

            return B;
        }


        public static float GetArcLengthMG(float4x3 MG, float delta)
        {
            float length = 0.0f;
            float3 pos1 = float3.zero;

            float3 pos2 = float3.zero;
            float u = 0.0f;

            while (u <= 1.0f)
            {
                pos1 = CurveMG(u, MG);
                u = u + delta;
                pos2 = CurveMG(u, MG);
                length = length + math.distance(pos1, pos2);
            }

            return length;
        }
    }

    public static class CameraMove
    {
        public static IEnumerator ZoomInOut(Transform camTrans)
        {
            Vector3 prePos = Vector3.zero;
            Vector3 curPos = Vector3.zero;

            while (true)
            {
                float delta = Input.GetAxis("Mouse ScrollWheel") * 4.0f;

                Matrix4x4 temp = camTrans.localToWorldMatrix;
                Vector4 pos = temp * new Vector4(0.0f, 0.0f, delta, 1.0f);

                camTrans.position = (Vector3)pos;

                prePos = curPos;

                yield return null;
            }
        }

        public static IEnumerator ZoomInOut(Transform camTrans, float pace)
        {
            while (true)
            {
                float delta = Input.GetAxis("Mouse ScrollWheel") * 400.0f * pace * Time.deltaTime;

                float3 zaxis = math.rotate(camTrans.rotation, new float3(0.0f, 0.0f, 1.0f));
                float3 pos = camTrans.position;
                pos = pos + zaxis * delta;

                camTrans.position = pos;

                yield return null;
            }
        }

        public static IEnumerator ZoomInOut_range(Transform camTrans, float pace, float r0, float r1, float3 center)
        {
            while (true)
            {
                float delta = Input.GetAxis("Mouse ScrollWheel") * 400.0f * pace * Time.deltaTime;

                float3 zaxis = math.rotate(camTrans.rotation, new float3(0.0f, 0.0f, 1.0f));
                float3 pos = camTrans.position;
                pos = pos + zaxis * delta;

                float r = math.length(center - pos);

                if (r0 < r && r < r1)
                {
                    camTrans.position = pos;
                }

                yield return null;
            }
        }

        public static IEnumerator ZoomInOut_NonCenter_range(Transform camTrans, float3 center, float pace, float r0, float r1)
        {
            Camera cam = camTrans.GetComponent<Camera>();

            //float3 center = float3.zero;

            float3 dir;
            dir = math.normalize(center - (float3)camTrans.position);
            float3x3 mat = new float3x3(camTrans.rotation);
            float3 rayDirInView = math.mul(math.transpose(mat), dir);
            float3 rayDirInWorld;

            while (true)
            {
                //float3 centerInView = new float3((float)cam.pixelWidth, (float)cam.pixelHeight, 0.0f) * rc;
                //Ray ray = cam.ScreenPointToRay(centerInView);
                //dir = ray.direction.normalized;

                mat = new float3x3(camTrans.rotation);
                rayDirInWorld = math.normalize(math.mul(mat, rayDirInView));

                float delta = Input.GetAxis("Mouse ScrollWheel") * 400.0f * pace * Time.deltaTime;

                float3 pos = camTrans.position;
                pos = pos + rayDirInWorld * delta;

                float r = math.length(center - pos);

                if (r0 < r && r < r1)
                {
                    camTrans.position = pos;
                }

                yield return null;
            }
        }

        public static IEnumerator RotOrbit(Transform camTrans, string planeTag, string layerMask, int mode)
        {

            Vector3 prePos = Vector3.zero;
            Vector3 curPos = Vector3.zero;
            Vector3 dir = Vector3.zero;
            bool down = false;


            while (true)
            {

                if (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftControl) && down == false)
                {
                    prePos = Input.mousePosition;
                    down = true;

                }

                if (down)
                {
                    Matrix4x4 mat = Matrix4x4.zero;
                    Matrix4x4 mat1 = Matrix4x4.zero;
                    Matrix4x4 mat2 = Matrix4x4.zero;

                    //radius calculation                
                    float radius = 1.0f;

                    Matrix4x4 temp = camTrans.localToWorldMatrix;
                    Vector3 rayOrigin = new Vector3(temp.m03, temp.m13, temp.m23);
                    Vector3 rayDir = new Vector3(temp.m02, temp.m12, temp.m22);

                    Ray ray = new Ray(rayOrigin, rayDir);
                    RaycastHit[] hits;
                    hits = Physics.RaycastAll(ray, 100.0f, LayerMask.GetMask(layerMask));
                    //Debug.Log(LayerMask.LayerToName(8).ToString());  


                    for (int i = 0; i < hits.GetLength(0); i++)
                    {
                        if (hits[i].collider != null)
                        {
                            if (hits[i].collider.gameObject != null)
                            {
                                if (hits[i].collider.gameObject.tag == planeTag)
                                {
                                    radius = Vector3.Distance(hits[i].point, rayOrigin);
                                    //Debug.Log(radius.ToString());
                                    break;
                                }
                            }
                        }
                    }

                    if (mode == 0)
                    {
                        mat = Matrix4x4.TRS(
                        (Vector3)(camTrans.localToWorldMatrix * new Vector4(0.0f, 0.0f, 0.0f, 1.0f)),
                        Quaternion.identity,
                        Vector3.one);
                        mat1 = Matrix4x4.TRS(
                            (Vector3)(camTrans.localToWorldMatrix * new Vector4(0.0f, 0.0f, radius, 1.0f)),
                            Quaternion.identity,
                            Vector3.one);
                        mat2 = mat1.inverse * mat;

                        curPos = Input.mousePosition;
                        dir = curPos - prePos;

                        float dTheta = -dir.y * 0.01f;
                        float dPhi = -dir.x * 0.01f;

                        Vector3 R = new Vector3(mat2.m03, mat2.m13, mat2.m23);
                        float r = R.magnitude;
                        float theta = Mathf.Acos(R.y / r) + dTheta;
                        float phi = 0.0f;
                        if (R.x >= 0.0f)
                        {
                            phi = Mathf.Acos(R.z / Mathf.Sqrt(R.z * R.z + R.x * R.x)) + dPhi;
                        }
                        else
                        {
                            phi = 2 * Mathf.PI - Mathf.Acos(R.z / Mathf.Sqrt(R.z * R.z + R.x * R.x)) + dPhi;
                        }


                        if (theta <= Mathf.Deg2Rad * 10) theta = Mathf.Deg2Rad * 10;
                        if (theta >= Mathf.Deg2Rad * 170) theta = Mathf.Deg2Rad * 170;
                        //if (phi <= Mathf.Deg2Rad * 10) phi = Mathf.Deg2Rad * 10;
                        //if (phi >= Mathf.Deg2Rad * 355) phi = Mathf.Deg2Rad * 355;

                        R.x = r * Mathf.Sin(theta) * Mathf.Sin(phi);
                        R.y = r * Mathf.Cos(theta);
                        R.z = r * Mathf.Sin(theta) * Mathf.Cos(phi);

                        Vector3 forward = -R.normalized;
                        Vector3 upward = new Vector3(
                            r * Mathf.Cos(theta) * Mathf.Sin(phi),
                            r * (-Mathf.Sin(theta)),
                            r * Mathf.Cos(theta) * Mathf.Cos(phi)).normalized * (-1);
                        Vector3 right = new Vector3(
                            r * Mathf.Sin(theta) * Mathf.Cos(phi),
                            0.0f,
                            r * Mathf.Sin(theta) * (-Mathf.Sin(phi))
                            ).normalized * (-1);

                        mat2.m03 = R.x;
                        mat2.m13 = R.y;
                        mat2.m23 = R.z;
                        mat2.m33 = 1.0f;

                        Matrix4x4 mat3 = mat1 * mat2;
                        camTrans.position = new Vector3(mat3.m03, mat3.m13, mat3.m23);
                        camTrans.rotation =
                            Quaternion.LookRotation(forward, upward);
                    }
                    else if (mode == 1)
                    {
                        mat = camTrans.localToWorldMatrix;
                        mat1 = Matrix4x4.TRS(
                            (Vector3)(camTrans.localToWorldMatrix * new Vector4(0.0f, 0.0f, radius, 1.0f)),
                            camTrans.rotation,
                            Vector3.one);
                        mat2 = mat1.inverse * mat;

                        curPos = Input.mousePosition;
                        dir = curPos - prePos;

                        float dTheta = -dir.y * 0.01f;
                        float dPhi = -dir.x * 0.01f;

                        Vector3 R = new Vector3(mat2.m03, mat2.m13, mat2.m23);
                        float r = R.magnitude;
                        float theta = Mathf.Acos(R.y / r) + dTheta;
                        float phi = 0.0f;
                        if (R.x >= 0.0f)
                        {
                            phi = Mathf.Acos(R.z / Mathf.Sqrt(R.z * R.z + R.x * R.x)) + dPhi;
                        }
                        else
                        {
                            phi = 2 * Mathf.PI - Mathf.Acos(R.z / Mathf.Sqrt(R.z * R.z + R.x * R.x)) + dPhi;
                        }


                        if (theta <= Mathf.Deg2Rad * 10) theta = Mathf.Deg2Rad * 10;
                        if (theta >= Mathf.Deg2Rad * 170) theta = Mathf.Deg2Rad * 170;
                        //if (phi <= Mathf.Deg2Rad * 10) phi = Mathf.Deg2Rad * 10;
                        //if (phi >= Mathf.Deg2Rad * 355) phi = Mathf.Deg2Rad * 355;

                        R.x = r * Mathf.Sin(theta) * Mathf.Sin(phi);
                        R.y = r * Mathf.Cos(theta);
                        R.z = r * Mathf.Sin(theta) * Mathf.Cos(phi);

                        Vector3 forward = -R.normalized;
                        Vector3 upward = new Vector3(
                            r * Mathf.Cos(theta) * Mathf.Sin(phi),
                            r * (-Mathf.Sin(theta)),
                            r * Mathf.Cos(theta) * Mathf.Cos(phi)).normalized * (-1);
                        Vector3 right = new Vector3(
                            r * Mathf.Sin(theta) * Mathf.Cos(phi),
                            0.0f,
                            r * Mathf.Sin(theta) * (-Mathf.Sin(phi))
                            ).normalized * (-1);

                        mat2.m03 = R.x;
                        mat2.m13 = R.y;
                        mat2.m23 = R.z;
                        mat2.m33 = 1.0f;

                        Matrix4x4 mat3 = mat1 * mat2;
                        camTrans.position = new Vector3(mat3.m03, mat3.m13, mat3.m23);
                        camTrans.rotation =
                            Quaternion.LookRotation(
                                new Vector3(mat1.m02, mat1.m12, mat1.m22),
                                new Vector3(mat1.m01, mat1.m11, mat1.m21)) *
                            Quaternion.LookRotation(forward, upward);
                    }

                    prePos = curPos;

                }

                if (Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.LeftControl))
                {
                    down = false;
                }

                yield return null;
            }
        }

        public static IEnumerator RotOrbitCenterFixed(Transform camTrans, string planeTag, string planeLayer, GameObject centerObject)
        {
            Vector3 prePos = Vector3.zero;
            Vector3 curPos = Vector3.zero;
            Vector3 angle = Vector3.zero;

            bool down = false;
            float radius = 1.0f;
            Ray ray = new Ray();
            RaycastHit[] hits;
            Vector3 center = Vector3.zero;
            Vector3 centerNormal = Vector3.zero;

            Quaternion q = Quaternion.identity;

            while (true)
            {
                if (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftShift) && down == false)
                {
                    down = true;
                    prePos = Input.mousePosition;

                    ray.direction = camTrans.rotation * Vector3.forward;
                    ray.origin = camTrans.position;

                    hits = Physics.RaycastAll(ray, 100.0f, LayerMask.GetMask(planeLayer));

                    for (int i = 0; i < hits.GetLength(0); i++)
                    {
                        if (hits[i].collider != null)
                        {
                            if (hits[i].collider.gameObject != null)
                            {
                                if (hits[i].collider.gameObject.tag == planeTag)
                                {
                                    center = hits[i].point;
                                    centerNormal = hits[i].normal;
                                    radius = Vector3.Distance(center, ray.origin);
                                    break;
                                }
                            }
                        }
                    }

                    centerObject.SetActive(true);
                    //centerObject.transform.position = center + centerNormal.normalized * 0.5f;
                    centerObject.transform.position = center;
                    centerObject.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    //centerObject.GetComponent<MeshRenderer>().material.color = Color.red;
                    //centerObject.transform.rotation =
                    //Quaternion.LookRotation(centerNormal, -ray.direction.normalized);

                    //Coordinates.CompareAxis_ByQuaternionOrMatrix(camTrans);
                }

                if (down)
                {
                    curPos = Input.mousePosition;
                    angle = curPos - prePos;

                    q =
                        Quaternion.AngleAxis(-angle.x, Vector3.up) *
                        Quaternion.LookRotation(ray.direction.normalized, Vector3.up) *
                        Quaternion.AngleAxis(angle.y, Vector3.right);

                    camTrans.rotation = q;
                    camTrans.position = center + radius * (q * (-Vector3.forward));
                    //centerObject.transform.rotation =
                    //Quaternion.LookRotation(centerNormal, (q * (-Vector3.forward)));

                    Debug.Log((q * Vector3.right).ToString());
                    //Coordinates.CompareAxis_ByQuaternionOrMatrix(camTrans);
                }

                if (Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.LeftShift))
                {
                    down = false;
                    centerObject.SetActive(false);
                }

                yield return null;
            }
        }

        public static IEnumerator RotOrbitRandomFixed(Transform camTrans, string planeTag, string planeLayer, GameObject centerObject)
        {
            Vector3 prePos = Vector3.zero;
            Vector3 curPos = Vector3.zero;
            Vector3 angle = Vector3.zero;

            Camera cam = camTrans.gameObject.GetComponent<Camera>();
            bool down = false;
            float radius = 1.0f;
            Ray ray = new Ray();
            Vector3 rayDirInView = Vector3.zero;
            Vector3 rayDirInWorld = Vector3.zero;
            Quaternion firstRotation = Quaternion.identity;
            Vector3 firstForward = Vector3.zero;
            RaycastHit[] hits;
            Vector3 center = Vector3.zero;
            Vector3 centerNormal = Vector3.zero;

            Quaternion q = Quaternion.identity;

            while (true)
            {
                //if (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftShift) && down == false)
                if (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftShift) && down == false)
                {
                    down = true;
                    prePos = Input.mousePosition;
                    firstRotation = camTrans.rotation;
                    firstForward = camTrans.rotation * Vector3.forward;

                    ray = cam.ScreenPointToRay(prePos);

                    rayDirInView.x = Vector3.Dot(
                        (camTrans.rotation * Vector3.right).normalized, ray.direction.normalized);
                    rayDirInView.y = Vector3.Dot(
                        (camTrans.rotation * Vector3.up).normalized, ray.direction.normalized);
                    rayDirInView.z = Vector3.Dot(
                        (camTrans.rotation * Vector3.forward).normalized, ray.direction.normalized);

                    hits = Physics.RaycastAll(ray, 100.0f, LayerMask.GetMask(planeLayer));

                    for (int i = 0; i < hits.GetLength(0); i++)
                    {
                        if (hits[i].collider != null)
                        {
                            if (hits[i].collider.gameObject != null)
                            {
                                if (hits[i].collider.gameObject.tag == planeTag)
                                {
                                    center = hits[i].point;
                                    centerNormal = hits[i].normal;
                                    radius = Vector3.Distance(center, camTrans.position);
                                    //Debug.Log("Plane is hited");
                                    break;
                                }
                            }
                        }
                    }

                    centerObject.SetActive(true);
                    //centerObject.transform.position = center + centerNormal.normalized * 0.5f;
                    centerObject.transform.position = center;
                    centerObject.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    //centerObject.GetComponent<MeshRenderer>().material.color = Color.red;
                    //centerObject.transform.rotation =
                    //Quaternion.LookRotation(centerNormal, -ray.direction.normalized);

                    //Coordinates.CompareAxis_ByQuaternionOrMatrix(camTrans);
                }

                if (down)
                {
                    curPos = Input.mousePosition;
                    angle = (curPos - prePos) * 0.1f;

                    q =
                        Quaternion.AngleAxis(-angle.x, Vector3.up) *
                        Quaternion.LookRotation(firstForward, Vector3.up) *
                        Quaternion.AngleAxis(angle.y, Vector3.right);

                    camTrans.rotation = q;

                    rayDirInWorld =
                        ((q * Vector3.right).normalized * rayDirInView.x +
                        (q * Vector3.up).normalized * rayDirInView.y +
                        (q * Vector3.forward).normalized * rayDirInView.z).normalized;
                    camTrans.position = center + radius * (-rayDirInWorld);
                    //centerObject.transform.rotation =
                    //Quaternion.LookRotation(centerNormal, (q * (-Vector3.forward)));

                    //Debug.Log((q * Vector3.right).ToString());
                    //Coordinates.CompareAxis_ByQuaternionOrMatrix(camTrans);
                }

                if (Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.LeftShift))
                {
                    down = false;
                    centerObject.SetActive(false);
                }

                yield return null;
                //yield return new WaitForSeconds(0.0f);
            }
        }

        public static IEnumerator RotOrbitRandomFixed(Transform camTrans, string planeTag, string planeLayer, GameObject centerObject, KeyCode key)
        {
            Vector3 prePos = Vector3.zero;
            Vector3 curPos = Vector3.zero;
            Vector3 angle = Vector3.zero;

            Camera cam = camTrans.gameObject.GetComponent<Camera>();
            bool down = false;
            float radius = 1.0f;
            Ray ray = new Ray();
            Vector3 rayDirInView = Vector3.zero;
            Vector3 rayDirInWorld = Vector3.zero;
            Quaternion firstRotation = Quaternion.identity;
            Vector3 firstForward = Vector3.zero;
            RaycastHit[] hits;
            Vector3 center = Vector3.zero;
            Vector3 centerNormal = Vector3.zero;

            Quaternion q = Quaternion.identity;

            while (true)
            {
                //if (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftShift) && down == false)
                if (Input.GetMouseButtonDown(1) && Input.GetKey(key) && down == false)
                {
                    down = true;
                    prePos = Input.mousePosition;
                    firstRotation = camTrans.rotation;
                    firstForward = camTrans.rotation * Vector3.forward;

                    ray = cam.ScreenPointToRay(prePos);

                    rayDirInView.x = Vector3.Dot(
                        (camTrans.rotation * Vector3.right).normalized, ray.direction.normalized);
                    rayDirInView.y = Vector3.Dot(
                        (camTrans.rotation * Vector3.up).normalized, ray.direction.normalized);
                    rayDirInView.z = Vector3.Dot(
                        (camTrans.rotation * Vector3.forward).normalized, ray.direction.normalized);

                    hits = Physics.RaycastAll(ray, 100.0f, LayerMask.GetMask(planeLayer));

                    for (int i = 0; i < hits.GetLength(0); i++)
                    {
                        if (hits[i].collider != null)
                        {
                            if (hits[i].collider.gameObject != null)
                            {
                                if (hits[i].collider.gameObject.tag == planeTag)
                                {
                                    center = hits[i].point;
                                    centerNormal = hits[i].normal;
                                    radius = Vector3.Distance(center, camTrans.position);
                                    //Debug.Log("Plane is hited");
                                    break;
                                }
                            }
                        }
                    }

                    centerObject.SetActive(true);
                    //centerObject.transform.position = center + centerNormal.normalized * 0.5f;
                    centerObject.transform.position = center;
                    centerObject.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    //centerObject.GetComponent<MeshRenderer>().material.color = Color.red;
                    //centerObject.transform.rotation =
                    //Quaternion.LookRotation(centerNormal, -ray.direction.normalized);

                    //Coordinates.CompareAxis_ByQuaternionOrMatrix(camTrans);
                }

                if (down)
                {
                    curPos = Input.mousePosition;
                    angle = (curPos - prePos) * 0.1f;

                    q =
                        Quaternion.AngleAxis(-angle.x, Vector3.up) *
                        Quaternion.LookRotation(firstForward, Vector3.up) *
                        Quaternion.AngleAxis(angle.y, Vector3.right);

                    camTrans.rotation = q;

                    rayDirInWorld =
                        ((q * Vector3.right).normalized * rayDirInView.x +
                        (q * Vector3.up).normalized * rayDirInView.y +
                        (q * Vector3.forward).normalized * rayDirInView.z).normalized;
                    camTrans.position = center + radius * (-rayDirInWorld);
                    //centerObject.transform.rotation =
                    //Quaternion.LookRotation(centerNormal, (q * (-Vector3.forward)));

                    //Debug.Log((q * Vector3.right).ToString());
                    //Coordinates.CompareAxis_ByQuaternionOrMatrix(camTrans);
                }

                if (Input.GetMouseButtonUp(1) || Input.GetKeyUp(key))
                {
                    down = false;
                    centerObject.SetActive(false);
                }

                yield return null;
                //yield return new WaitForSeconds(0.0f);
            }
        }

        public static IEnumerator RotOrbitRandomFixed_range(
            Transform camTrans, string planeTag, string planeLayer, GameObject centerObject,
            float theta0 = 45.0f, float theta1 = 85.0f, KeyCode key = KeyCode.LeftShift)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            float3 prePos = float3.zero;
            float3 curPos = float3.zero;

            float3 angle = float3.zero;

            bool down = false;

            Ray ray = new Ray();
            RaycastHit[] hits;

            float3 center = float3.zero;
            float radius = 1.0f;
            float3 centerNormal = float3.zero;

            quaternion preRot = quaternion.identity;

            float3 rayDirInView = float3.zero;
            float3 rayDirInWorld = float3.zero;

            quaternion q = quaternion.identity;

            float cosp = 0.0f;
            float sinp = 0.0f;
            float cost = 0.0f;
            float sint = 0.0f;

            float prePhi = 0.0f;
            float preTheta = 0.0f;

            float3x3 m = float3x3.identity;
            float3 xaxis = float3.zero;
            float3 yaxis = float3.zero;
            float3 zaxis = float3.zero;


            while (true)
            {
                if (Input.GetMouseButtonDown(1) && Input.GetKey(key))
                {
                    prePos = Input.mousePosition;
                    preRot = camTrans.rotation;

                    ray = cam.ScreenPointToRay(prePos);

                    float3x3 mat = new float3x3(camTrans.rotation);
                    float3 rayDir = ray.direction.normalized;
                    rayDirInView = math.mul(math.transpose(mat), rayDir);

                    hits = Physics.RaycastAll(ray, 100.0f, LayerMask.GetMask(planeLayer));
                    for (int i = 0; i < hits.GetLength(0); i++)
                    {
                        if (hits[i].collider != null)
                        {
                            if (hits[i].collider.gameObject != null)
                            {
                                if (hits[i].collider.gameObject.tag == planeTag)
                                {
                                    center = hits[i].point;
                                    centerNormal = hits[i].normal;
                                    radius = math.distance(center, camTrans.position);
                                    down = true;

                                    {
                                        m = new float3x3(preRot);
                                        xaxis = m.c0;
                                        yaxis = m.c1;
                                        zaxis = m.c2;

                                        {
                                            cosp = math.dot(new float3(1.0f, 0.0f, 0.0f), xaxis);
                                            sinp = math.dot(new float3(0.0f, 1.0f, 0.0f), math.cross(new float3(1.0f, 0.0f, 0.0f), xaxis));
                                            prePhi = math.acos(cosp);
                                            if (sinp < 0)
                                            {
                                                prePhi *= (-1.0f);
                                            }

                                        }

                                        {
                                            cost = math.dot(new float3(0.0f, 1.0f, 0.0f), yaxis);
                                            preTheta = math.acos(cost);
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }

                    centerObject.SetActive(true);
                    centerObject.transform.position = center;
                }


                if (down)
                {
                    curPos = Input.mousePosition;
                    //angle = (curPos - prePos) * 0.1f;
                    angle = (curPos - prePos) * 0.001f;

                    {
                        float phi = 0.0f;
                        phi = prePhi - angle.x;

                        float theta = 0.0f;
                        theta = math.clamp(preTheta + angle.y, math.radians(theta0), math.radians(theta1));

                        q = math.mul(
                            quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), phi),
                            quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), theta));
                        camTrans.rotation = q;
                    }

                    {
                        float3x3 mat = new float3x3(q);
                        rayDirInWorld = math.mul(mat, rayDirInView);

                        camTrans.position = center + radius * (-rayDirInWorld);
                    }
                }

                if (Input.GetMouseButtonUp(1) || !Input.GetKey(key))
                {
                    down = false;
                    centerObject.SetActive(false);
                }

                yield return null;

            }


        }

        public static IEnumerator RotOrbitConstCenterFixed_range(
            Transform camTrans, float3 center,
            int delta = 10, float theta0 = 45.0f, float theta1 = 85.0f, KeyCode key = KeyCode.LeftShift)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            float3 prePos = float3.zero;
            float3 curPos = float3.zero;

            float3 angle = float3.zero;

            bool down = false;

            //Ray ray = new Ray();
            //RaycastHit[] hits;

            //float3 center = float3.zero;
            float radius = 1.0f;
            float3 centerNormal = float3.zero;

            quaternion preRot = quaternion.identity;

            //float3 rayDirInView = float3.zero;
            //float3 rayDirInWorld = float3.zero;

            quaternion q = quaternion.identity;

            Rect pRect = cam.pixelRect;
            Vector2 min = pRect.min;
            Vector2 size = pRect.size;
            Vector2 vDelta = new Vector2(delta, delta);
            Rect rect = new Rect(min + vDelta, size - 2.0f * vDelta);

            float cosp = 0.0f;
            float sinp = 0.0f;
            float cost = 0.0f;
            float sint = 0.0f;

            float prePhi = 0.0f;
            float preTheta = 0.0f;

            float3x3 m = float3x3.identity;
            float3 xaxis = float3.zero;
            float3 yaxis = float3.zero;
            float3 zaxis = float3.zero;


            while (true)
            {
                if (Input.GetMouseButtonDown(1) && Input.GetKey(key))
                {
                    prePos = Input.mousePosition;
                    {
                        down = true;
                        {
                            zaxis = center - (float3)(camTrans.position);
                            radius = math.length(zaxis);

                            //if (radius > 0.5f)
                            {
                                zaxis = math.normalize(zaxis);
                                xaxis = math.normalize(math.cross(new float3(0.0f, 1.0f, 0.0f), zaxis));
                                yaxis = math.cross(zaxis, xaxis);

                                //preRot = new quaternion(new float3x3(xaxis, yaxis, zaxis));                                                                                
                                //camTrans.rotation = preRot;
                            }
                        }

                        {
                            cosp = math.dot(new float3(1.0f, 0.0f, 0.0f), xaxis);
                            sinp = math.dot(new float3(0.0f, 1.0f, 0.0f), math.cross(new float3(1.0f, 0.0f, 0.0f), xaxis));
                            prePhi = math.acos(cosp);
                            if (sinp < 0)
                            {
                                prePhi *= (-1.0f);
                            }

                        }

                        {
                            cost = math.dot(new float3(0.0f, 1.0f, 0.0f), yaxis);
                            preTheta = math.acos(cost);
                        }
                    }

                    //centerObject.SetActive(true);
                    //centerObject.transform.position = center;
                }

                if (rect.Contains(Input.mousePosition))
                {
                    if (down)
                    {
                        curPos = Input.mousePosition;
                        //angle = (curPos - prePos) * 0.1f;
                        angle = (curPos - prePos) * 0.01f;

                        {
                            float phi = 0.0f;
                            phi = prePhi - angle.x;

                            float theta = 0.0f;
                            theta = math.clamp(preTheta + angle.y, math.radians(theta0), math.radians(theta1));

                            q = math.mul(
                                quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), phi),
                                quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), theta));
                            camTrans.rotation = q;
                        }

                        {
                            zaxis = math.rotate(q, new float3(0.0f, 0.0f, 1.0f));
                            camTrans.position = center + radius * (-zaxis);
                        }
                    }
                }
                else
                {
                    down = false;
                }

                if (Input.GetMouseButtonUp(1) || !Input.GetKey(key))
                {
                    down = false;
                    //centerObject.SetActive(false);
                }

                yield return null;

            }
        }


        public static IEnumerator RotOrbitConstNonCenterFixed_range(
            Transform camTrans, float3 center,
            float theta0 = 45.0f, float theta1 = 85.0f)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            float3 prePos = float3.zero;
            float3 curPos = float3.zero;

            float3 angle = float3.zero;

            bool down = false;

            //Ray ray = new Ray();
            //RaycastHit[] hits;

            //float3 center = float3.zero;
            //float3 centerInView = float3.zero;
            //float3 rc = new float3(0.2f, 0.5f, 0.0f);

            float radius = 1.0f;
            //float3 centerNormal = float3.zero;

            quaternion preRot = quaternion.identity;

            float3 rayDirInView = float3.zero;
            float3 rayDirInWorld = float3.zero;

            quaternion q = quaternion.identity;

            float cosp = 0.0f;
            float sinp = 0.0f;
            float cost = 0.0f;
            float sint = 0.0f;

            float prePhi = 0.0f;
            float preTheta = 0.0f;

            float3x3 m = float3x3.identity;
            float3 xaxis = float3.zero;
            float3 yaxis = float3.zero;
            float3 zaxis = float3.zero;


            float3 dir;
            dir = math.normalize(center - (float3)camTrans.position);
            float3x3 mat = new float3x3(camTrans.rotation);
            rayDirInView = math.mul(math.transpose(mat), dir);

            while (true)
            {
                if (Input.GetMouseButtonDown(1) && down == false)
                {
                    prePos = Input.mousePosition;
                    preRot = camTrans.rotation;

                    //centerInView = new float3((float)cam.pixelWidth, (float)cam.pixelHeight, 0.0f) * rc;
                    //ray = cam.ScreenPointToRay(centerInView);
                    //
                    //mat = new float3x3(camTrans.rotation);
                    //float3 rayDir = ray.direction.normalized;
                    //rayDirInView = math.mul(math.transpose(mat), rayDir);

                    radius = math.distance(center, camTrans.position);
                    down = true;

                    {
                        m = new float3x3(preRot);
                        xaxis = m.c0;
                        yaxis = m.c1;
                        zaxis = m.c2;

                        {
                            cosp = math.dot(new float3(1.0f, 0.0f, 0.0f), xaxis);
                            sinp = math.dot(new float3(0.0f, 1.0f, 0.0f), math.cross(new float3(1.0f, 0.0f, 0.0f), xaxis));
                            prePhi = math.acos(cosp);
                            if (sinp < 0)
                            {
                                prePhi *= (-1.0f);
                            }

                        }

                        {
                            cost = math.dot(new float3(0.0f, 1.0f, 0.0f), yaxis);
                            preTheta = math.acos(cost);
                        }
                    }

                }

                if (down)
                {
                    curPos = Input.mousePosition;
                    //angle = (curPos - prePos) * 0.1f;
                    angle = (curPos - prePos) * 0.001f;

                    {
                        float phi = 0.0f;
                        phi = prePhi - angle.x;

                        float theta = 0.0f;
                        theta = math.clamp(preTheta + angle.y, math.radians(theta0), math.radians(theta1));

                        q = math.mul(
                            quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), phi),
                            quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), theta));
                        camTrans.rotation = q;
                    }

                    {
                        mat = new float3x3(q);
                        rayDirInWorld = math.normalize(math.mul(mat, rayDirInView));

                        camTrans.position = center + radius * (-rayDirInWorld);
                    }
                }

                if (Input.GetMouseButtonUp(1))
                {
                    down = false;
                }


                yield return null;
            }

        }

        public static IEnumerator RotSpin(Transform camTrans, int mode)
        {
            Vector3 prePos = Vector3.zero;
            Vector3 curPos = Vector3.zero;
            Vector3 dir = Vector3.zero;
            bool down = false;

            while (true)
            {
                if (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftAlt) && down == false)
                {
                    prePos = Input.mousePosition;
                    down = true;
                }

                if (down)
                {
                    curPos = Input.mousePosition;
                    dir = curPos - prePos;
                    Vector3 axisScr = new Vector3(-dir.y, dir.x, 0.0f);

                    if (mode == 0)
                    {
                        Vector3 axisLocal = new Vector3(axisScr.x, 0.0f, 0.0f);
                        Vector3 axisGlobal = new Vector3(0.0f, axisScr.y, 0.0f);
                        float angleLocal = axisLocal.magnitude * 0.1f;
                        float angleGlobal = axisGlobal.magnitude * 0.1f;
                        if (angleLocal >= 360.0f)
                        {
                            angleLocal = 0.0f;
                        }

                        if (angleGlobal >= 360.0f)
                        {
                            angleGlobal = 0.0f;
                        }

                        camTrans.rotation =
                            Quaternion.AngleAxis(angleGlobal, axisGlobal.normalized) *
                            camTrans.rotation *
                            Quaternion.AngleAxis(angleLocal, axisLocal.normalized);
                    }
                    else if (mode == 1)
                    {
                        float angle = axisScr.magnitude * 0.1f;

                        if (angle >= 360.0f)
                        {
                            angle = 0.0f;
                        }

                        camTrans.rotation =
                            camTrans.rotation *
                            Quaternion.AngleAxis(angle, axisScr.normalized);
                    }

                    prePos = curPos;
                }

                if (Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.LeftAlt))
                {
                    down = false;
                }

                yield return null;
            }
        }

        public static IEnumerator RotSpin(Transform camTrans, int mode, KeyCode key)
        {
            Vector3 prePos = Vector3.zero;
            Vector3 curPos = Vector3.zero;
            Vector3 dir = Vector3.zero;
            bool down = false;

            while (true)
            {
                if (Input.GetMouseButtonDown(1) && Input.GetKey(key) && down == false)
                {
                    prePos = Input.mousePosition;
                    down = true;
                }

                if (down)
                {
                    curPos = Input.mousePosition;
                    dir = curPos - prePos;
                    Vector3 axisScr = new Vector3(-dir.y, dir.x, 0.0f);

                    if (mode == 0)
                    {
                        Vector3 axisLocal = new Vector3(axisScr.x, 0.0f, 0.0f);
                        Vector3 axisGlobal = new Vector3(0.0f, axisScr.y, 0.0f);
                        float angleLocal = axisLocal.magnitude * 0.1f;
                        float angleGlobal = axisGlobal.magnitude * 0.1f;
                        if (angleLocal >= 360.0f)
                        {
                            angleLocal = 0.0f;
                        }

                        if (angleGlobal >= 360.0f)
                        {
                            angleGlobal = 0.0f;
                        }

                        camTrans.rotation =
                            Quaternion.AngleAxis(angleGlobal, axisGlobal.normalized) *
                            camTrans.rotation *
                            Quaternion.AngleAxis(angleLocal, axisLocal.normalized);
                    }
                    else if (mode == 1)
                    {
                        float angle = axisScr.magnitude * 0.1f;

                        if (angle >= 360.0f)
                        {
                            angle = 0.0f;
                        }

                        camTrans.rotation =
                            camTrans.rotation *
                            Quaternion.AngleAxis(angle, axisScr.normalized);
                    }

                    prePos = curPos;
                }

                if (Input.GetMouseButtonUp(1) || Input.GetKeyUp(key))
                {
                    down = false;
                }

                yield return null;
            }
        }


        public static IEnumerator RotSpin_range(
            Transform camTrans,
            float theta0 = 45.0f, float theta1 = 85.0f, KeyCode key = KeyCode.LeftAlt)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            float3 prePos = float3.zero;
            float3 curPos = float3.zero;

            float3 angle = float3.zero;

            bool down = false;

            quaternion q = quaternion.identity;
            quaternion preRot = quaternion.identity;

            float cosp = 0.0f;
            float sinp = 0.0f;
            float cost = 0.0f;
            float sint = 0.0f;

            float prePhi = 0.0f;
            float preTheta = 0.0f;

            float3x3 m = float3x3.identity;
            float3 xaxis = float3.zero;
            float3 yaxis = float3.zero;
            float3 zaxis = float3.zero;

            while (true)
            {
                if (Input.GetMouseButtonDown(1) && Input.GetKey(key))
                {
                    prePos = Input.mousePosition;
                    preRot = camTrans.rotation;
                    down = true;

                    {
                        m = new float3x3(preRot);
                        xaxis = m.c0;
                        yaxis = m.c1;
                        zaxis = m.c2;

                        {
                            cosp = math.dot(new float3(1.0f, 0.0f, 0.0f), xaxis);
                            sinp = math.dot(new float3(0.0f, 1.0f, 0.0f), math.cross(new float3(1.0f, 0.0f, 0.0f), xaxis));
                            prePhi = math.acos(cosp);
                            if (sinp < 0)
                            {
                                prePhi *= (-1.0f);
                            }
                        }

                        {
                            cost = math.dot(new float3(0.0f, 1.0f, 0.0f), yaxis);
                            preTheta = math.acos(cost);
                        }
                    }
                }

                if (down)
                {
                    curPos = Input.mousePosition;

                    angle = (curPos - prePos) * 0.001f;
                    {
                        float phi = 0.0f;
                        phi = prePhi + angle.x;

                        float theta = 0.0f;
                        theta = math.clamp(preTheta - angle.y, math.radians(theta0), math.radians(theta1));

                        q = math.mul(
                            quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), phi),
                            quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), theta));
                        camTrans.rotation = q;
                    }
                }

                if (Input.GetMouseButtonUp(1) || !Input.GetKey(key))
                {
                    down = false;
                }

                yield return null;
            }

        }

        public static IEnumerator MoveParallel(Transform camTrans)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            while (true)
            {
                Rect rect = cam.pixelRect;

                Vector3 posf = Input.mousePosition;
                Vector3Int pos = new Vector3Int((int)posf.x, (int)posf.y, (int)posf.z);
                int delta = 20;
                int xmin = (int)(rect.min.x);
                int xmax = (int)(rect.max.x);
                int ymin = (int)(rect.min.y);
                int ymax = (int)(rect.max.y);

                Vector3 camPos = Vector3.zero;
                float camDelta = 0.05f;

                if ((xmin <= pos.x && pos.x <= xmin + delta)
                    && (ymin <= pos.y && pos.y <= ymin + delta))
                {
                    //1
                    camPos.x -= camDelta;
                    camPos.y -= camDelta;
                }
                else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                    && (ymin <= pos.y && pos.y <= ymin + delta))
                {
                    //2
                    camPos.y -= camDelta;
                }
                else if ((xmax - delta <= pos.x && pos.x <= xmax)
                    && (ymin <= pos.y && pos.y <= ymin + delta))
                {
                    //3
                    camPos.x += camDelta;
                    camPos.y -= camDelta;
                }
                else if ((xmin <= pos.x && pos.x <= xmin + delta)
                     && (ymin + delta < pos.y && pos.y < ymax - delta))
                {
                    //4
                    camPos.x -= camDelta;
                }
                else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                     && (ymin + delta < pos.y && pos.y < ymax - delta))
                {
                    //5
                }
                else if ((xmax - delta <= pos.x && pos.x <= xmax)
                    && (ymin + delta < pos.y && pos.y < ymax - delta))
                {
                    //6
                    camPos.x += camDelta;
                }
                else if ((xmin <= pos.x && pos.x <= xmin + delta)
                    && (ymax - delta <= pos.y && pos.y <= ymax))
                {
                    //7
                    camPos.x -= camDelta;
                    camPos.y += camDelta;
                }
                else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                     && (ymax - delta <= pos.y && pos.y <= ymax))
                {
                    //8                   
                    camPos.y += camDelta;
                }
                else if ((xmax - delta <= pos.x && pos.x <= xmax)
                    && (ymax - delta <= pos.y && pos.y <= ymax))
                {
                    //9
                    camPos.x += camDelta;
                    camPos.y += camDelta;
                }

                Vector3 localPos =
                    (camTrans.rotation * Vector3.right).normalized * camPos.x +
                    (camTrans.rotation * Vector3.up).normalized * camPos.y;
                camTrans.position = camTrans.position + localPos;

                //camTrans.localPosition = camPos;

                yield return null;
            }
        }

        public static IEnumerator MoveParallel_XZ(Transform camTrans)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            while (true)
            {
                Rect rect = cam.pixelRect;

                Vector3 posf = Input.mousePosition;
                Vector3Int pos = new Vector3Int((int)posf.x, (int)posf.y, (int)posf.z);
                int delta = 20;
                int xmin = (int)(rect.min.x);
                int xmax = (int)(rect.max.x);
                int ymin = (int)(rect.min.y);
                int ymax = (int)(rect.max.y);

                Vector3 camPos = Vector3.zero;
                float camDelta = 0.05f;

                if ((xmin <= pos.x && pos.x <= xmin + delta)
                    && (ymin <= pos.y && pos.y <= ymin + delta))
                {
                    //1
                    camPos.x -= camDelta;
                    camPos.y -= camDelta;
                }
                else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                    && (ymin <= pos.y && pos.y <= ymin + delta))
                {
                    //2
                    camPos.y -= camDelta;
                }
                else if ((xmax - delta <= pos.x && pos.x <= xmax)
                    && (ymin <= pos.y && pos.y <= ymin + delta))
                {
                    //3
                    camPos.x += camDelta;
                    camPos.y -= camDelta;
                }
                else if ((xmin <= pos.x && pos.x <= xmin + delta)
                     && (ymin + delta < pos.y && pos.y < ymax - delta))
                {
                    //4
                    camPos.x -= camDelta;
                }
                else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                     && (ymin + delta < pos.y && pos.y < ymax - delta))
                {
                    //5
                }
                else if ((xmax - delta <= pos.x && pos.x <= xmax)
                    && (ymin + delta < pos.y && pos.y < ymax - delta))
                {
                    //6
                    camPos.x += camDelta;
                }
                else if ((xmin <= pos.x && pos.x <= xmin + delta)
                    && (ymax - delta <= pos.y && pos.y <= ymax))
                {
                    //7
                    camPos.x -= camDelta;
                    camPos.y += camDelta;
                }
                else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                     && (ymax - delta <= pos.y && pos.y <= ymax))
                {
                    //8                   
                    camPos.y += camDelta;
                }
                else if ((xmax - delta <= pos.x && pos.x <= xmax)
                    && (ymax - delta <= pos.y && pos.y <= ymax))
                {
                    //9
                    camPos.x += camDelta;
                    camPos.y += camDelta;
                }

                //Vector3 localPos =
                //    (camTrans.rotation * Vector3.right).normalized * camPos.x +
                //    (camTrans.rotation * Vector3.up).normalized * camPos.y;
                //camTrans.position = camTrans.position + localPos;

                float3 xaxis = math.rotate(camTrans.rotation, new float3(1.0f, 0.0f, 0.0f));
                float3 yaxis = new float3(0.0f, 1.0f, 0.0f);
                float3 zaxis = math.cross(xaxis, yaxis);

                float3 localPos = xaxis * camPos.x + zaxis * camPos.y;
                camTrans.position += (Vector3)localPos;

                //camTrans.localPosition = camPos;

                yield return null;
            }
        }

        public static IEnumerator MoveParallel_XZ(Transform camTrans, float pace, int delta = 20)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            while (true)
            {
                Rect rect = cam.pixelRect;

                Vector3 posf = Input.mousePosition;
                Vector3Int pos = new Vector3Int((int)posf.x, (int)posf.y, (int)posf.z);

                int xmin = (int)(rect.min.x);
                int xmax = (int)(rect.max.x);
                int ymin = (int)(rect.min.y);
                int ymax = (int)(rect.max.y);

                Vector3 camPos = Vector3.zero;
                float camDelta = 0.05f;

                //float k = 0.025f;
                float y0 = 0.1f;
                camDelta = y0 + pace * camTrans.position.y * Time.deltaTime;

                bool RKey = false;
                bool Lkey = false;
                bool UKey = false;
                bool DKey = false;

                if (Input.GetKey(KeyCode.W))
                {
                    UKey = true;
                }

                if (Input.GetKey(KeyCode.A))
                {
                    Lkey = true;
                }

                if (Input.GetKey(KeyCode.S))
                {
                    DKey = true;
                }

                if (Input.GetKey(KeyCode.D))
                {
                    RKey = true;
                }

                if (((xmin <= pos.x && pos.x <= xmin + delta)
                    && (ymin <= pos.y && pos.y <= ymin + delta))
                    || (Lkey && !RKey && !UKey && DKey))
                {
                    //1
                    camPos.x -= camDelta;
                    camPos.y -= camDelta;
                }
                else if (((xmin + delta < pos.x && pos.x < xmax - delta)
                    && (ymin <= pos.y && pos.y <= ymin + delta))
                    || (!Lkey && !RKey && !UKey && DKey))
                {
                    //2
                    camPos.y -= camDelta;
                }
                else if (((xmax - delta <= pos.x && pos.x <= xmax)
                    && (ymin <= pos.y && pos.y <= ymin + delta))
                    || (!Lkey && RKey && !UKey && DKey))
                {
                    //3
                    camPos.x += camDelta;
                    camPos.y -= camDelta;
                }
                else if (((xmin <= pos.x && pos.x <= xmin + delta)
                     && (ymin + delta < pos.y && pos.y < ymax - delta))
                     || (Lkey && !RKey && !UKey && !DKey))
                {
                    //4
                    camPos.x -= camDelta;
                }
                //else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                //     && (ymin + delta < pos.y && pos.y < ymax - delta)
                //     )
                //{
                //    //5
                //}
                else if (((xmax - delta <= pos.x && pos.x <= xmax)
                    && (ymin + delta < pos.y && pos.y < ymax - delta))
                    || (!Lkey && RKey && !UKey && !DKey))
                {
                    //6
                    camPos.x += camDelta;
                }
                else if (((xmin <= pos.x && pos.x <= xmin + delta)
                    && (ymax - delta <= pos.y && pos.y <= ymax))
                    || (Lkey && !RKey && UKey && !DKey))
                {
                    //7
                    camPos.x -= camDelta;
                    camPos.y += camDelta;
                }
                else if (((xmin + delta < pos.x && pos.x < xmax - delta)
                     && (ymax - delta <= pos.y && pos.y <= ymax))
                     || (!Lkey && !RKey && UKey && !DKey))
                {
                    //8                   
                    camPos.y += camDelta;
                }
                else if (((xmax - delta <= pos.x && pos.x <= xmax)
                    && (ymax - delta <= pos.y && pos.y <= ymax))
                    || (!Lkey && RKey && UKey && !DKey))
                {
                    //9
                    camPos.x += camDelta;
                    camPos.y += camDelta;
                }

                //Vector3 localPos =
                //    (camTrans.rotation * Vector3.right).normalized * camPos.x +
                //    (camTrans.rotation * Vector3.up).normalized * camPos.y;
                //camTrans.position = camTrans.position + localPos;

                float3 xaxis = math.rotate(camTrans.rotation, new float3(1.0f, 0.0f, 0.0f));
                float3 yaxis = new float3(0.0f, 1.0f, 0.0f);
                float3 zaxis = math.cross(xaxis, yaxis);

                float3 localPos = xaxis * camPos.x + zaxis * camPos.y;
                camTrans.position += (Vector3)localPos;

                //camTrans.localPosition = camPos;

                yield return null;
            }
        }
    }

    public class CameraMoveTouch
    {

        public static IEnumerator RotOrbitRandomFixed(Transform camTrans, string planeTag, string planeLayer, GameObject centerObject, int delta = 10)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            float3 prePos = float3.zero;
            float3 curPos = float3.zero;

            float3 angle = float3.zero;

            bool down = false;

            Ray ray = new Ray();
            RaycastHit[] hits;

            float3 center = float3.zero;
            float radius = 1.0f;
            float3 centerNormal = float3.zero;

            quaternion preRot = quaternion.identity;

            float3 rayDirInView = float3.zero;
            float3 rayDirInWorld = float3.zero;

            quaternion q = quaternion.identity;

            Rect pRect = cam.pixelRect;
            Vector2 min = pRect.min;
            Vector2 size = pRect.size;
            Vector2 vDelta = new Vector2(delta, delta);
            Rect rect = new Rect(min + vDelta, size - 2.0f * vDelta);

            if (Input.touchSupported)
            {
                while (true)
                {
                    if (Input.touchCount > 0)
                    {
                        var touches = Input.touches;
                        var touch = touches[0];

                        if (touch.phase == TouchPhase.Began)
                        {

                            float2 pos = touch.position;
                            prePos = new float3(pos, 0.0f);
                            preRot = camTrans.rotation;

                            ray = cam.ScreenPointToRay(prePos);

                            float3x3 mat = new float3x3(camTrans.rotation);
                            float3 rayDir = ray.direction.normalized;
                            rayDirInView = math.mul(math.transpose(mat), rayDir);

                            hits = Physics.RaycastAll(ray, 100.0f, LayerMask.GetMask(planeLayer));
                            for (int i = 0; i < hits.GetLength(0); i++)
                            {
                                if (hits[i].collider != null)
                                {
                                    if (hits[i].collider.gameObject != null)
                                    {
                                        if (hits[i].collider.gameObject.tag == planeTag)
                                        {
                                            center = hits[i].point;
                                            centerNormal = hits[i].normal;
                                            radius = math.distance(center, camTrans.position);
                                            down = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            centerObject.SetActive(true);
                            centerObject.transform.position = center;
                        }

                        if (rect.Contains(touch.position))
                        {
                            if (down)
                            {
                                float2 pos = touch.position;
                                curPos = new float3(pos, 0.0f);
                                angle = (curPos - prePos) * 0.1f;

                                q = math.mul(math.mul(
                                    quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(-angle.x)),
                                    preRot),
                                    quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.radians(angle.y)));

                                camTrans.rotation = q;

                                float3x3 mat = new float3x3(q);
                                rayDirInWorld = math.mul(mat, rayDirInView);

                                camTrans.position = center + radius * (-rayDirInWorld);
                            }
                        }
                        else
                        {
                            down = false;
                        }

                        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        {
                            down = false;
                            centerObject.SetActive(false);
                        }


                    }

                    yield return null;
                }
            }
            else
            {
                while (true)
                {
                    {
                        if (Input.GetMouseButtonDown(1))
                        {
                            prePos = Input.mousePosition;
                            preRot = camTrans.rotation;

                            ray = cam.ScreenPointToRay(prePos);

                            float3x3 mat = new float3x3(camTrans.rotation);
                            float3 rayDir = ray.direction.normalized;
                            rayDirInView = math.mul(math.transpose(mat), rayDir);

                            hits = Physics.RaycastAll(ray, 100.0f, LayerMask.GetMask(planeLayer));
                            for (int i = 0; i < hits.GetLength(0); i++)
                            {
                                if (hits[i].collider != null)
                                {
                                    if (hits[i].collider.gameObject != null)
                                    {
                                        if (hits[i].collider.gameObject.tag == planeTag)
                                        {
                                            center = hits[i].point;
                                            centerNormal = hits[i].normal;
                                            radius = math.distance(center, camTrans.position);
                                            down = true;

                                            break;
                                        }
                                    }
                                }
                            }

                            centerObject.SetActive(true);
                            centerObject.transform.position = center;
                        }

                        if (rect.Contains(Input.mousePosition))
                        {
                            if (down)
                            {
                                curPos = Input.mousePosition;
                                angle = (curPos - prePos) * 0.1f;

                                q = math.mul(math.mul(
                                    quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(-angle.x)),
                                    preRot),
                                    quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.radians(angle.y)));

                                camTrans.rotation = q;

                                float3x3 mat = new float3x3(q);
                                rayDirInWorld = math.mul(mat, rayDirInView);

                                camTrans.position = center + radius * (-rayDirInWorld);
                            }
                        }
                        else
                        {
                            down = false;
                        }

                        if (Input.GetMouseButtonUp(1))
                        {
                            down = false;
                            centerObject.SetActive(false);
                        }
                    }

                    yield return null;
                }
            }


        }

        public static IEnumerator RotSpin(Transform camTrans, int delta = 100)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            float3 prePos = float3.zero;
            float3 curPos = float3.zero;

            float3 angle = float3.zero;

            bool down = false;

            quaternion q = quaternion.identity;
            quaternion preRot = quaternion.identity;

            Rect pRect = cam.pixelRect;
            Vector2 min = pRect.min;
            Vector2 size = pRect.size;
            Vector2 vDelta = new Vector2(delta, delta);
            Rect rect = new Rect(min + vDelta, size - 2.0f * vDelta);

            if (Input.touchSupported)
            {
                while (true)
                {
                    if (Input.touchCount > 0)
                    {
                        var touches = Input.touches;
                        var touch = touches[0];

                        if (touch.phase == TouchPhase.Began)
                        {
                            prePos = new float3((float2)touch.position, 0.0f);
                            preRot = camTrans.rotation;
                            down = true;
                        }

                        if (rect.Contains(touch.position))
                        {
                            if (down)
                            {
                                curPos = new float3((float2)touch.position, 0.0f);
                                angle = (curPos - prePos) * 0.1f;

                                q = math.mul(math.mul(
                                    quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(angle.x)),
                                    preRot),
                                    quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.radians(-angle.y)));

                                camTrans.rotation = q;
                            }
                        }
                        else
                        {
                            down = false;
                        }

                        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        {
                            down = false;
                        }
                    }

                    yield return null;
                }
            }
            else
            {
                while (true)
                {
                    {
                        if (Input.GetMouseButtonDown(1))
                        {
                            prePos = Input.mousePosition;
                            preRot = camTrans.rotation;
                            down = true;
                        }

                        if (rect.Contains(Input.mousePosition))
                        {
                            if (down)
                            {
                                curPos = Input.mousePosition;
                                angle = (curPos - prePos) * 0.1f;

                                q = math.mul(math.mul(
                                    quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(angle.x)),
                                    preRot),
                                    quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.radians(-angle.y)));

                                camTrans.rotation = q;
                            }
                        }
                        else
                        {
                            down = false;
                        }

                        if (Input.GetMouseButtonUp(1))
                        {
                            down = false;
                        }
                    }

                    yield return null;
                }
            }

        }

        public static IEnumerator MoveParallel(Transform camTrans, int delta = 100)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();
            Rect rect = cam.pixelRect;

            quaternion preRot = quaternion.identity;

            float3 posf;
            int3 pos;
            //int delta = 20;
            int xmin;
            int xmax;
            int ymin;
            int ymax;

            float3 camPos = float3.zero;
            float camDelta = 0.05f;

            if (Input.touchSupported)
            {
                while (true)
                {
                    if (Input.touchCount > 0)
                    {
                        var touches = Input.touches;
                        var touch = touches[0];

                        posf = new float3((float2)touch.position, 0.0f);
                        pos = new int3((int)posf.x, (int)posf.y, (int)posf.z);
                        //delta = 200;
                        xmin = (int)(rect.min.x);
                        xmax = (int)(rect.max.x);
                        ymin = (int)(rect.min.y);
                        ymax = (int)(rect.max.y);

                        camPos = float3.zero;
                        camDelta = 0.05f;

                        if ((xmin <= pos.x && pos.x <= xmin + delta)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //1
                            camPos.x -= camDelta;
                            camPos.y -= camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //2
                            camPos.y -= camDelta;
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //3
                            camPos.x += camDelta;
                            camPos.y -= camDelta;
                        }
                        else if ((xmin <= pos.x && pos.x <= xmin + delta)
                             && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //4
                            camPos.x -= camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                             && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //5
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //6
                            camPos.x += camDelta;
                        }
                        else if ((xmin <= pos.x && pos.x <= xmin + delta)
                            && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //7
                            camPos.x -= camDelta;
                            camPos.y += camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                             && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //8                   
                            camPos.y += camDelta;
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //9
                            camPos.x += camDelta;
                            camPos.y += camDelta;
                        }

                        float3x3 mat = new float3x3(camTrans.rotation);
                        float3 localPos = math.mul(mat, camPos);
                        camTrans.position += (Vector3)localPos;
                    }

                    yield return null;
                }
            }
            else
            {
                while (true)
                {
                    {
                        posf = Input.mousePosition;
                        pos = new int3((int)posf.x, (int)posf.y, (int)posf.z);
                        //delta = 20;
                        xmin = (int)(rect.min.x);
                        xmax = (int)(rect.max.x);
                        ymin = (int)(rect.min.y);
                        ymax = (int)(rect.max.y);

                        camPos = float3.zero;
                        camDelta = 0.05f;

                        if ((xmin <= pos.x && pos.x <= xmin + delta)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //1
                            camPos.x -= camDelta;
                            camPos.y -= camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //2
                            camPos.y -= camDelta;
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //3
                            camPos.x += camDelta;
                            camPos.y -= camDelta;
                        }
                        else if ((xmin <= pos.x && pos.x <= xmin + delta)
                             && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //4
                            camPos.x -= camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                             && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //5
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //6
                            camPos.x += camDelta;
                        }
                        else if ((xmin <= pos.x && pos.x <= xmin + delta)
                            && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //7
                            camPos.x -= camDelta;
                            camPos.y += camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                             && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //8                   
                            camPos.y += camDelta;
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //9
                            camPos.x += camDelta;
                            camPos.y += camDelta;
                        }

                        float3x3 mat = new float3x3(camTrans.rotation);
                        float3 localPos = math.mul(mat, camPos);
                        camTrans.position += (Vector3)localPos;
                    }

                    yield return null;
                }
            }
        }

        public static IEnumerator ZoomInOut(Transform camTrans, int delta = 100)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            float3 prePos = float3.zero;
            float3 curPos = float3.zero;
            float preR = 0.0f;
            float curR = 0.0f;

            bool down = false;

            float4x4 preW = float4x4.identity;

            Rect pRect = cam.pixelRect;
            Vector2 min = pRect.min;
            Vector2 size = pRect.size;
            Vector2 vDelta = new Vector2(delta, delta);
            Rect rect = new Rect(min + vDelta, size - 2.0f * vDelta);

            if (Input.touchSupported)
            {
                while (true)
                {
                    if (Input.touchCount > 1)
                    {
                        var touches = Input.touches;
                        var touch0 = touches[0];
                        var touch1 = touches[1];


                        if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
                        {
                            down = true;
                            prePos = new float3((float2)(touch0.position + touch1.position) * 0.5f, 0.0f);
                            preR = 0.5f * (math.distance(prePos, new float3((float2)touch0.position, 0.0f)) + math.distance(prePos, new float3((float2)touch1.position, 0.0f)));
                            preW = camTrans.localToWorldMatrix;
                        }

                        if (rect.Contains(touch0.position) && rect.Contains(touch1.position))
                        {
                            if (down)
                            {
                                //curPos = new float3((float2)(touch0.position + touch1.position) * 0.5f, 0.0f);
                                curR = 0.5f * (math.distance(prePos, new float3((float2)touch0.position, 0.0f)) + math.distance(prePos, new float3((float2)touch1.position, 0.0f)));
                                float zDelta = (curR - preR) * 0.01f;
                                float3 camPos = new float3(0.0f, 0.0f, zDelta);

                                camTrans.position = math.mul(preW, new float4(camPos, 1.0f)).xyz;
                            }
                        }
                        else
                        {
                            down = false;
                        }

                        if (touch0.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Ended)
                        {
                            down = false;
                        }

                    }

                    //if (Input.touchCount > 0)
                    //{
                    //    var touches = Input.touches;
                    //    var touch = touches[0];
                    //
                    //    if (touch.phase == TouchPhase.Began)
                    //    {
                    //        down = true;
                    //        prePos = new float3((float2)touch.position, 0.0f);
                    //        preW = camTrans.localToWorldMatrix;
                    //    }
                    //
                    //    if (rect.Contains(touch.position))
                    //    {
                    //        if (down)
                    //        {
                    //            curPos = new float3((float2)touch.position, 0.0f);
                    //            float zDelta = (curPos - prePos).x * 0.01f;
                    //            float3 camPos = new float3(0.0f, 0.0f, zDelta);
                    //
                    //            camTrans.position = math.mul(preW, new float4(camPos, 1.0f)).xyz;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        down = false;
                    //    }
                    //
                    //    if (touch.phase == TouchPhase.Ended)
                    //    {
                    //        down = false;
                    //    }
                    //
                    //}

                    yield return null;
                }
            }
            else
            {
                while (true)
                {
                    {
                        if (Input.GetMouseButtonDown(1))
                        {
                            down = true;
                            prePos = Input.mousePosition;
                            preW = camTrans.localToWorldMatrix;
                        }

                        if (rect.Contains(Input.mousePosition))
                        {
                            if (down)
                            {
                                curPos = Input.mousePosition;
                                float zDelta = (curPos - prePos).x * 0.01f;
                                float3 camPos = new float3(0.0f, 0.0f, zDelta);

                                camTrans.position = math.mul(preW, new float4(camPos, 1.0f)).xyz;
                            }
                        }
                        else
                        {
                            down = false;
                        }

                        if (Input.GetMouseButtonUp(1))
                        {
                            down = false;
                        }
                    }

                    yield return null;
                }
            }
        }

        //
        public bool activeRotOrbit = false;
        public bool activeRotSpin = false;
        public bool activeMoveParallel = true;
        public bool activeZoom = false;

        public IEnumerator RotOrbitRandomFixedIns(Transform camTrans, string planeTag, string planeLayer, GameObject centerObject, int delta = 10)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            float3 prePos = float3.zero;
            float3 curPos = float3.zero;

            float3 angle = float3.zero;

            bool down = false;

            Ray ray = new Ray();
            RaycastHit[] hits;

            float3 center = float3.zero;
            float radius = 1.0f;
            float3 centerNormal = float3.zero;

            quaternion preRot = quaternion.identity;

            float3 rayDirInView = float3.zero;
            float3 rayDirInWorld = float3.zero;

            quaternion q = quaternion.identity;

            Rect pRect = cam.pixelRect;
            Vector2 min = pRect.min;
            Vector2 size = pRect.size;
            Vector2 vDelta = new Vector2(delta, delta);
            Rect rect = new Rect(min + vDelta, size - 2.0f * vDelta);

            if (Input.touchSupported)
            {
                while (true)
                {
                    if (Input.touchCount > 0 && activeRotOrbit)
                    {
                        var touches = Input.touches;
                        var touch = touches[0];

                        if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Stationary)
                        {

                            float2 pos = touch.position;
                            prePos = new float3(pos, 0.0f);
                            preRot = camTrans.rotation;

                            ray = cam.ScreenPointToRay(prePos);

                            float3x3 mat = new float3x3(camTrans.rotation);
                            float3 rayDir = ray.direction.normalized;
                            rayDirInView = math.mul(math.transpose(mat), rayDir);

                            hits = Physics.RaycastAll(ray, 100.0f, LayerMask.GetMask(planeLayer));
                            for (int i = 0; i < hits.GetLength(0); i++)
                            {
                                if (hits[i].collider != null)
                                {
                                    if (hits[i].collider.gameObject != null)
                                    {
                                        if (hits[i].collider.gameObject.tag == planeTag)
                                        {
                                            center = hits[i].point;
                                            centerNormal = hits[i].normal;
                                            radius = math.distance(center, camTrans.position);
                                            down = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            centerObject.SetActive(true);
                            centerObject.transform.position = center;
                        }

                        if (rect.Contains(touch.position))
                        {
                            if (down)
                            {
                                float2 pos = touch.position;
                                curPos = new float3(pos, 0.0f);
                                angle = (curPos - prePos) * 0.1f;

                                q = math.mul(math.mul(
                                    quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(-angle.x)),
                                    preRot),
                                    quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.radians(angle.y)));

                                camTrans.rotation = q;

                                float3x3 mat = new float3x3(q);
                                rayDirInWorld = math.mul(mat, rayDirInView);

                                camTrans.position = center + radius * (-rayDirInWorld);
                            }
                        }
                        else
                        {
                            down = false;
                        }

                        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        {
                            down = false;
                            centerObject.SetActive(false);
                        }
                    }

                    yield return null;
                    if (!activeRotOrbit)
                    {
                        down = false;
                    }
                }
            }
            else
            {
                while (true)
                {
                    if (activeRotOrbit)
                    {
                        if (Input.GetMouseButtonDown(1))
                        {
                            prePos = Input.mousePosition;
                            preRot = camTrans.rotation;

                            ray = cam.ScreenPointToRay(prePos);

                            float3x3 mat = new float3x3(camTrans.rotation);
                            float3 rayDir = ray.direction.normalized;
                            rayDirInView = math.mul(math.transpose(mat), rayDir);

                            hits = Physics.RaycastAll(ray, 100.0f, LayerMask.GetMask(planeLayer));
                            for (int i = 0; i < hits.GetLength(0); i++)
                            {
                                if (hits[i].collider != null)
                                {
                                    if (hits[i].collider.gameObject != null)
                                    {
                                        if (hits[i].collider.gameObject.tag == planeTag)
                                        {
                                            center = hits[i].point;
                                            centerNormal = hits[i].normal;
                                            radius = math.distance(center, camTrans.position);
                                            down = true;

                                            break;
                                        }
                                    }
                                }
                            }

                            centerObject.SetActive(true);
                            centerObject.transform.position = center;
                        }

                        if (rect.Contains(Input.mousePosition))
                        {
                            if (down)
                            {
                                curPos = Input.mousePosition;
                                angle = (curPos - prePos) * 0.1f;

                                q = math.mul(math.mul(
                                    quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(-angle.x)),
                                    preRot),
                                    quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.radians(angle.y)));

                                camTrans.rotation = q;

                                float3x3 mat = new float3x3(q);
                                rayDirInWorld = math.mul(mat, rayDirInView);

                                camTrans.position = center + radius * (-rayDirInWorld);
                            }
                        }
                        else
                        {
                            down = false;
                        }

                        if (Input.GetMouseButtonUp(1))
                        {
                            down = false;
                            centerObject.SetActive(false);
                        }
                    }

                    yield return null;
                    if (!activeRotOrbit)
                    {
                        down = false;
                    }
                }
            }


        }

        public IEnumerator RotSpinIns(Transform camTrans, int delta = 100)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            float3 prePos = float3.zero;
            float3 curPos = float3.zero;

            float3 angle = float3.zero;

            bool down = false;

            quaternion q = quaternion.identity;
            quaternion preRot = quaternion.identity;

            Rect pRect = cam.pixelRect;
            Vector2 min = pRect.min;
            Vector2 size = pRect.size;
            Vector2 vDelta = new Vector2(delta, delta);
            Rect rect = new Rect(min + vDelta, size - 2.0f * vDelta);

            if (Input.touchSupported)
            {
                while (true)
                {
                    if (Input.touchCount > 0 && activeRotSpin)
                    {
                        var touches = Input.touches;
                        var touch = touches[0];

                        if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Stationary)
                        {
                            prePos = new float3((float2)touch.position, 0.0f);
                            preRot = camTrans.rotation;
                            down = true;
                        }

                        if (rect.Contains(touch.position))
                        {
                            if (down)
                            {
                                curPos = new float3((float2)touch.position, 0.0f);
                                angle = (curPos - prePos) * 0.1f;

                                q = math.mul(math.mul(
                                    quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(angle.x)),
                                    preRot),
                                    quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.radians(-angle.y)));

                                camTrans.rotation = q;
                            }
                        }
                        else
                        {
                            down = false;
                        }

                        if (touch.phase == TouchPhase.Ended)
                        {
                            down = false;
                        }
                    }

                    yield return null;
                    if (!activeRotSpin)
                    {
                        down = false;
                    }
                }
            }
            else
            {
                while (true)
                {
                    if (activeRotSpin)
                    {
                        if (Input.GetMouseButtonDown(1))
                        {
                            prePos = Input.mousePosition;
                            preRot = camTrans.rotation;
                            down = true;
                        }

                        if (rect.Contains(Input.mousePosition))
                        {
                            if (down)
                            {
                                curPos = Input.mousePosition;
                                angle = (curPos - prePos) * 0.1f;

                                q = math.mul(math.mul(
                                    quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), math.radians(angle.x)),
                                    preRot),
                                    quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), math.radians(-angle.y)));

                                camTrans.rotation = q;
                            }
                        }
                        else
                        {
                            down = false;
                        }

                        if (Input.GetMouseButtonUp(1))
                        {
                            down = false;
                        }
                    }

                    yield return null;
                    if (!activeRotSpin)
                    {
                        down = false;
                    }
                }
            }

        }

        public IEnumerator MoveParallelIns(Transform camTrans, int delta = 100)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();
            Rect rect = cam.pixelRect;

            quaternion preRot = quaternion.identity;

            float3 posf;
            int3 pos;
            //int delta = 20;
            int xmin;
            int xmax;
            int ymin;
            int ymax;

            float3 camPos = float3.zero;
            float camDelta = 1.0f;

            if (Input.touchSupported)
            {
                while (true)
                {
                    if (Input.touchCount > 0 && activeMoveParallel)
                    {
                        var touches = Input.touches;
                        var touch = touches[0];

                        posf = new float3((float2)touch.position, 0.0f);
                        pos = new int3((int)posf.x, (int)posf.y, (int)posf.z);
                        //delta = 200;
                        xmin = (int)(rect.min.x);
                        xmax = (int)(rect.max.x);
                        ymin = (int)(rect.min.y);
                        ymax = (int)(rect.max.y);

                        camPos = float3.zero;
                        camDelta = 0.5f;

                        if ((xmin <= pos.x && pos.x <= xmin + delta)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //1
                            camPos.x -= camDelta;
                            camPos.y -= camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //2
                            camPos.y -= camDelta;
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //3
                            camPos.x += camDelta;
                            camPos.y -= camDelta;
                        }
                        else if ((xmin <= pos.x && pos.x <= xmin + delta)
                             && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //4
                            camPos.x -= camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                             && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //5
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //6
                            camPos.x += camDelta;
                        }
                        else if ((xmin <= pos.x && pos.x <= xmin + delta)
                            && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //7
                            camPos.x -= camDelta;
                            camPos.y += camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                             && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //8                   
                            camPos.y += camDelta;
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //9
                            camPos.x += camDelta;
                            camPos.y += camDelta;
                        }

                        float3x3 mat = new float3x3(camTrans.rotation);
                        float3 localPos = math.mul(mat, camPos);
                        camTrans.position += (Vector3)localPos;
                    }

                    yield return null;

                }
            }
            else
            {
                while (true)
                {
                    if (activeMoveParallel)
                    {
                        posf = Input.mousePosition;
                        pos = new int3((int)posf.x, (int)posf.y, (int)posf.z);
                        //delta = 20;
                        xmin = (int)(rect.min.x);
                        xmax = (int)(rect.max.x);
                        ymin = (int)(rect.min.y);
                        ymax = (int)(rect.max.y);

                        camPos = float3.zero;
                        camDelta = 0.2f;

                        if ((xmin <= pos.x && pos.x <= xmin + delta)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //1
                            camPos.x -= camDelta;
                            camPos.y -= camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //2
                            camPos.y -= camDelta;
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //3
                            camPos.x += camDelta;
                            camPos.y -= camDelta;
                        }
                        else if ((xmin <= pos.x && pos.x <= xmin + delta)
                             && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //4
                            camPos.x -= camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                             && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //5
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //6
                            camPos.x += camDelta;
                        }
                        else if ((xmin <= pos.x && pos.x <= xmin + delta)
                            && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //7
                            camPos.x -= camDelta;
                            camPos.y += camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                             && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //8                   
                            camPos.y += camDelta;
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //9
                            camPos.x += camDelta;
                            camPos.y += camDelta;
                        }

                        float3x3 mat = new float3x3(camTrans.rotation);
                        float3 localPos = math.mul(mat, camPos);
                        camTrans.position += (Vector3)localPos;
                    }

                    yield return null;
                }
            }
        }

        public IEnumerator ZoomInOutIns(Transform camTrans, int delta = 100)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            float3 prePos = float3.zero;
            float3 curPos = float3.zero;
            float preR = 0.0f;
            float curR = 0.0f;

            bool down = false;

            float4x4 preW = float4x4.identity;

            Rect pRect = cam.pixelRect;
            Vector2 min = pRect.min;
            Vector2 size = pRect.size;
            Vector2 vDelta = new Vector2(delta, delta);
            Rect rect = new Rect(min + vDelta, size - 2.0f * vDelta);

            if (Input.touchSupported)
            {
                while (true)
                {
                    if (Input.touchCount > 1 && activeZoom)
                    {
                        var touches = Input.touches;
                        var touch0 = touches[0];
                        var touch1 = touches[1];


                        if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
                        {
                            down = true;
                            prePos = new float3((float2)(touch0.position + touch1.position) * 0.5f, 0.0f);
                            preR = 0.5f * (math.distance(prePos, new float3((float2)touch0.position, 0.0f)) + math.distance(prePos, new float3((float2)touch1.position, 0.0f)));
                            preW = camTrans.localToWorldMatrix;
                        }

                        if (rect.Contains(touch0.position) && rect.Contains(touch1.position))
                        {
                            if (down)
                            {
                                //curPos = new float3((float2)(touch0.position + touch1.position) * 0.5f, 0.0f);
                                curR = 0.5f * (math.distance(prePos, new float3((float2)touch0.position, 0.0f)) + math.distance(prePos, new float3((float2)touch1.position, 0.0f)));
                                float zDelta = (curR - preR) * 0.01f;
                                float3 camPos = new float3(0.0f, 0.0f, zDelta);

                                camTrans.position = math.mul(preW, new float4(camPos, 1.0f)).xyz;
                            }
                        }
                        else
                        {
                            down = false;
                        }

                        if (touch0.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Ended)
                        {
                            down = false;
                        }

                    }

                    //if (Input.touchCount > 0)
                    //{
                    //    var touches = Input.touches;
                    //    var touch = touches[0];
                    //
                    //    if (touch.phase == TouchPhase.Began)
                    //    {
                    //        down = true;
                    //        prePos = new float3((float2)touch.position, 0.0f);
                    //        preW = camTrans.localToWorldMatrix;
                    //    }
                    //
                    //    if (rect.Contains(touch.position))
                    //    {
                    //        if (down)
                    //        {
                    //            curPos = new float3((float2)touch.position, 0.0f);
                    //            float zDelta = (curPos - prePos).x * 0.01f;
                    //            float3 camPos = new float3(0.0f, 0.0f, zDelta);
                    //
                    //            camTrans.position = math.mul(preW, new float4(camPos, 1.0f)).xyz;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        down = false;
                    //    }
                    //
                    //    if (touch.phase == TouchPhase.Ended)
                    //    {
                    //        down = false;
                    //    }
                    //
                    //}

                    yield return null;
                    if (!activeZoom)
                    {
                        down = false;
                    }
                }
            }
            else
            {
                while (true)
                {
                    if (activeZoom)
                    {
                        if (Input.GetMouseButtonDown(1))
                        {
                            down = true;
                            prePos = Input.mousePosition;
                            preW = camTrans.localToWorldMatrix;
                        }

                        if (rect.Contains(Input.mousePosition))
                        {
                            if (down)
                            {
                                curPos = Input.mousePosition;
                                float zDelta = (curPos - prePos).x * 0.01f;
                                float3 camPos = new float3(0.0f, 0.0f, zDelta);

                                camTrans.position = math.mul(preW, new float4(camPos, 1.0f)).xyz;
                            }
                        }
                        else
                        {
                            down = false;
                        }

                        if (Input.GetMouseButtonUp(1))
                        {
                            down = false;
                        }
                    }

                    yield return null;
                    if (!activeZoom)
                    {
                        down = false;
                    }
                }
            }
        }



        public IEnumerator RotOrbitRandomFixedIns_range(
            Transform camTrans, string planeTag, string planeLayer, GameObject centerObject,
            int delta = 10, float theta0 = 45.0f, float theta1 = 85.0f)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            float3 prePos = float3.zero;
            float3 curPos = float3.zero;

            float3 angle = float3.zero;

            bool down = false;

            Ray ray = new Ray();
            RaycastHit[] hits;

            float3 center = float3.zero;
            float radius = 1.0f;
            float3 centerNormal = float3.zero;

            quaternion preRot = quaternion.identity;

            float3 rayDirInView = float3.zero;
            float3 rayDirInWorld = float3.zero;

            quaternion q = quaternion.identity;

            Rect pRect = cam.pixelRect;
            Vector2 min = pRect.min;
            Vector2 size = pRect.size;
            Vector2 vDelta = new Vector2(delta, delta);
            Rect rect = new Rect(min + vDelta, size - 2.0f * vDelta);

            float cosp = 0.0f;
            float sinp = 0.0f;
            float cost = 0.0f;
            float sint = 0.0f;

            float prePhi = 0.0f;
            float preTheta = 0.0f;

            float3x3 m = float3x3.identity;
            float3 xaxis = float3.zero;
            float3 yaxis = float3.zero;
            float3 zaxis = float3.zero;

            if (Input.touchSupported)
            {
                while (true)
                {
                    if (Input.touchCount > 0 && activeRotOrbit)
                    {
                        var touches = Input.touches;
                        var touch = touches[0];

                        if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Stationary)
                        {

                            float2 pos = touch.position;
                            prePos = new float3(pos, 0.0f);
                            preRot = camTrans.rotation;

                            ray = cam.ScreenPointToRay(prePos);

                            float3x3 mat = new float3x3(camTrans.rotation);
                            float3 rayDir = ray.direction.normalized;
                            rayDirInView = math.mul(math.transpose(mat), rayDir);

                            hits = Physics.RaycastAll(ray, 100.0f, LayerMask.GetMask(planeLayer));
                            for (int i = 0; i < hits.GetLength(0); i++)
                            {
                                if (hits[i].collider != null)
                                {
                                    if (hits[i].collider.gameObject != null)
                                    {
                                        if (hits[i].collider.gameObject.tag == planeTag)
                                        {
                                            center = hits[i].point;
                                            centerNormal = hits[i].normal;
                                            radius = math.distance(center, camTrans.position);
                                            down = true;

                                            {
                                                m = new float3x3(preRot);
                                                xaxis = m.c0;
                                                yaxis = m.c1;
                                                zaxis = m.c2;

                                                {
                                                    cosp = math.dot(new float3(1.0f, 0.0f, 0.0f), xaxis);
                                                    sinp = math.dot(new float3(0.0f, 1.0f, 0.0f), math.cross(new float3(1.0f, 0.0f, 0.0f), xaxis));
                                                    prePhi = math.acos(cosp);
                                                    if (sinp < 0)
                                                    {
                                                        prePhi *= (-1.0f);
                                                    }
                                                }

                                                {
                                                    cost = math.dot(new float3(0.0f, 1.0f, 0.0f), yaxis);
                                                    preTheta = math.acos(cost);
                                                }
                                            }
                                            break;
                                        }
                                    }
                                }
                            }

                            centerObject.SetActive(true);
                            centerObject.transform.position = center;
                        }

                        if (rect.Contains(touch.position))
                        {
                            if (down)
                            {
                                float2 pos = touch.position;
                                curPos = new float3(pos, 0.0f);
                                //angle = (curPos - prePos) * 0.1f;
                                angle = (curPos - prePos) * 0.001f;

                                {
                                    float phi = 0.0f;
                                    phi = prePhi - angle.x;

                                    float theta = 0.0f;
                                    theta = math.clamp(preTheta + angle.y, math.radians(theta0), math.radians(theta1));

                                    q = math.mul(
                                        quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), phi),
                                        quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), theta));
                                    camTrans.rotation = q;
                                }

                                {
                                    float3x3 mat = new float3x3(q);
                                    rayDirInWorld = math.mul(mat, rayDirInView);

                                    camTrans.position = center + radius * (-rayDirInWorld);
                                }
                            }
                        }
                        else
                        {
                            down = false;
                        }

                        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        {
                            down = false;
                            centerObject.SetActive(false);
                        }
                    }

                    yield return null;
                    if (!activeRotOrbit)
                    {
                        down = false;
                    }
                }
            }
            else
            {
                while (true)
                {
                    if (activeRotOrbit)
                    {
                        if (Input.GetMouseButtonDown(1))
                        {
                            prePos = Input.mousePosition;
                            preRot = camTrans.rotation;

                            ray = cam.ScreenPointToRay(prePos);

                            float3x3 mat = new float3x3(camTrans.rotation);
                            float3 rayDir = ray.direction.normalized;
                            rayDirInView = math.mul(math.transpose(mat), rayDir);

                            hits = Physics.RaycastAll(ray, 100.0f, LayerMask.GetMask(planeLayer));
                            for (int i = 0; i < hits.GetLength(0); i++)
                            {
                                if (hits[i].collider != null)
                                {
                                    if (hits[i].collider.gameObject != null)
                                    {
                                        if (hits[i].collider.gameObject.tag == planeTag)
                                        {
                                            center = hits[i].point;
                                            centerNormal = hits[i].normal;
                                            radius = math.distance(center, camTrans.position);
                                            down = true;

                                            {
                                                m = new float3x3(preRot);
                                                xaxis = m.c0;
                                                yaxis = m.c1;
                                                zaxis = m.c2;

                                                {
                                                    cosp = math.dot(new float3(1.0f, 0.0f, 0.0f), xaxis);
                                                    sinp = math.dot(new float3(0.0f, 1.0f, 0.0f), math.cross(new float3(1.0f, 0.0f, 0.0f), xaxis));
                                                    prePhi = math.acos(cosp);
                                                    if (sinp < 0)
                                                    {
                                                        prePhi *= (-1.0f);
                                                    }

                                                }

                                                {
                                                    cost = math.dot(new float3(0.0f, 1.0f, 0.0f), yaxis);
                                                    preTheta = math.acos(cost);
                                                }
                                            }
                                            break;
                                        }
                                    }
                                }
                            }

                            centerObject.SetActive(true);
                            centerObject.transform.position = center;
                        }

                        if (rect.Contains(Input.mousePosition))
                        {
                            if (down)
                            {
                                curPos = Input.mousePosition;
                                //angle = (curPos - prePos) * 0.1f;
                                angle = (curPos - prePos) * 0.001f;

                                {
                                    float phi = 0.0f;
                                    phi = prePhi - angle.x;

                                    float theta = 0.0f;
                                    theta = math.clamp(preTheta + angle.y, math.radians(theta0), math.radians(theta1));

                                    q = math.mul(
                                        quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), phi),
                                        quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), theta));
                                    camTrans.rotation = q;
                                }

                                {
                                    float3x3 mat = new float3x3(q);
                                    rayDirInWorld = math.mul(mat, rayDirInView);

                                    camTrans.position = center + radius * (-rayDirInWorld);
                                }
                            }
                        }
                        else
                        {
                            down = false;
                        }

                        if (Input.GetMouseButtonUp(1))
                        {
                            down = false;
                            centerObject.SetActive(false);
                        }
                    }

                    yield return null;
                    if (!activeRotOrbit)
                    {
                        down = false;
                    }
                }
            }


        }

        public IEnumerator RotSpinIns_range(
            Transform camTrans,
            int delta = 100, float theta0 = 45.0f, float theta1 = 85.0f)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            float3 prePos = float3.zero;
            float3 curPos = float3.zero;

            float3 angle = float3.zero;

            bool down = false;

            quaternion q = quaternion.identity;
            quaternion preRot = quaternion.identity;

            Rect pRect = cam.pixelRect;
            Vector2 min = pRect.min;
            Vector2 size = pRect.size;
            Vector2 vDelta = new Vector2(delta, delta);
            Rect rect = new Rect(min + vDelta, size - 2.0f * vDelta);

            float cosp = 0.0f;
            float sinp = 0.0f;
            float cost = 0.0f;
            float sint = 0.0f;

            float prePhi = 0.0f;
            float preTheta = 0.0f;

            float3x3 m = float3x3.identity;
            float3 xaxis = float3.zero;
            float3 yaxis = float3.zero;
            float3 zaxis = float3.zero;

            if (Input.touchSupported)
            {
                while (true)
                {
                    if (Input.touchCount > 0 && activeRotSpin)
                    {
                        var touches = Input.touches;
                        var touch = touches[0];

                        if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Stationary)
                        {
                            prePos = new float3((float2)touch.position, 0.0f);
                            preRot = camTrans.rotation;
                            down = true;

                            {
                                m = new float3x3(preRot);
                                xaxis = m.c0;
                                yaxis = m.c1;
                                zaxis = m.c2;

                                {
                                    cosp = math.dot(new float3(1.0f, 0.0f, 0.0f), xaxis);
                                    sinp = math.dot(new float3(0.0f, 1.0f, 0.0f), math.cross(new float3(1.0f, 0.0f, 0.0f), xaxis));
                                    prePhi = math.acos(cosp);
                                    if (sinp < 0)
                                    {
                                        prePhi *= (-1.0f);
                                    }
                                }

                                {
                                    cost = math.dot(new float3(0.0f, 1.0f, 0.0f), yaxis);
                                    preTheta = math.acos(cost);
                                }
                            }
                        }

                        if (rect.Contains(touch.position))
                        {
                            if (down)
                            {
                                curPos = new float3((float2)touch.position, 0.0f);

                                angle = (curPos - prePos) * 0.001f;
                                {
                                    float phi = 0.0f;
                                    phi = prePhi + angle.x;

                                    float theta = 0.0f;
                                    theta = math.clamp(preTheta - angle.y, math.radians(theta0), math.radians(theta1));

                                    q = math.mul(
                                        quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), phi),
                                        quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), theta));
                                    camTrans.rotation = q;
                                }
                            }
                        }
                        else
                        {
                            down = false;
                        }

                        if (touch.phase == TouchPhase.Ended)
                        {
                            down = false;
                        }
                    }

                    yield return null;
                    if (!activeRotSpin)
                    {
                        down = false;
                    }
                }
            }
            else
            {
                while (true)
                {
                    if (activeRotSpin)
                    {
                        if (Input.GetMouseButtonDown(1))
                        {
                            prePos = Input.mousePosition;
                            preRot = camTrans.rotation;
                            down = true;

                            {
                                m = new float3x3(preRot);
                                xaxis = m.c0;
                                yaxis = m.c1;
                                zaxis = m.c2;

                                {
                                    cosp = math.dot(new float3(1.0f, 0.0f, 0.0f), xaxis);
                                    sinp = math.dot(new float3(0.0f, 1.0f, 0.0f), math.cross(new float3(1.0f, 0.0f, 0.0f), xaxis));
                                    prePhi = math.acos(cosp);
                                    if (sinp < 0)
                                    {
                                        prePhi *= (-1.0f);
                                    }
                                }

                                {
                                    cost = math.dot(new float3(0.0f, 1.0f, 0.0f), yaxis);
                                    preTheta = math.acos(cost);
                                }
                            }
                        }

                        if (rect.Contains(Input.mousePosition))
                        {
                            if (down)
                            {
                                curPos = Input.mousePosition;

                                angle = (curPos - prePos) * 0.001f;
                                {
                                    float phi = 0.0f;
                                    phi = prePhi + angle.x;

                                    float theta = 0.0f;
                                    theta = math.clamp(preTheta - angle.y, math.radians(theta0), math.radians(theta1));

                                    q = math.mul(
                                        quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), phi),
                                        quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), theta));
                                    camTrans.rotation = q;
                                }
                            }
                        }
                        else
                        {
                            down = false;
                        }

                        if (Input.GetMouseButtonUp(1))
                        {
                            down = false;
                        }
                    }

                    yield return null;
                    if (!activeRotSpin)
                    {
                        down = false;
                    }
                }
            }

        }

        public IEnumerator MoveParallelIns_pace(Transform camTrans, int delta = 100)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();
            Rect rect = cam.pixelRect;

            quaternion preRot = quaternion.identity;

            float3 posf;
            int3 pos;
            //int delta = 20;
            int xmin;
            int xmax;
            int ymin;
            int ymax;

            float3 camPos = float3.zero;
            float camDelta = 1.0f;

            if (Input.touchSupported)
            {
                while (true)
                {
                    if (Input.touchCount > 0 && activeMoveParallel)
                    {
                        var touches = Input.touches;
                        var touch = touches[0];

                        posf = new float3((float2)touch.position, 0.0f);
                        pos = new int3((int)posf.x, (int)posf.y, (int)posf.z);
                        //delta = 200;
                        xmin = (int)(rect.min.x);
                        xmax = (int)(rect.max.x);
                        ymin = (int)(rect.min.y);
                        ymax = (int)(rect.max.y);

                        camPos = float3.zero;
                        //camDelta = 0.2f;

                        float k = 0.025f;
                        float y0 = 0.01f;
                        camDelta = y0 + k * camTrans.position.y;
                        //camDelta = k * camTrans.position.y;
                        if ((xmin <= pos.x && pos.x <= xmin + delta)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //1
                            camPos.x -= camDelta;
                            camPos.y -= camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //2
                            camPos.y -= camDelta;
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //3
                            camPos.x += camDelta;
                            camPos.y -= camDelta;
                        }
                        else if ((xmin <= pos.x && pos.x <= xmin + delta)
                             && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //4
                            camPos.x -= camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                             && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //5
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //6
                            camPos.x += camDelta;
                        }
                        else if ((xmin <= pos.x && pos.x <= xmin + delta)
                            && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //7
                            camPos.x -= camDelta;
                            camPos.y += camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                             && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //8                   
                            camPos.y += camDelta;
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //9
                            camPos.x += camDelta;
                            camPos.y += camDelta;
                        }

                        //float3x3 mat = new float3x3(camTrans.rotation);
                        //float3 localPos = math.mul(mat, camPos);

                        float3 xaxis = math.rotate(camTrans.rotation, new float3(1.0f, 0.0f, 0.0f));
                        float3 yaxis = new float3(0.0f, 1.0f, 0.0f);
                        float3 zaxis = math.cross(xaxis, yaxis);

                        float3 localPos = xaxis * camPos.x + zaxis * camPos.y;
                        camTrans.position += (Vector3)localPos;
                    }

                    yield return null;

                }
            }
            else
            {
                while (true)
                {
                    if (activeMoveParallel)
                    {
                        posf = Input.mousePosition;
                        pos = new int3((int)posf.x, (int)posf.y, (int)posf.z);
                        //delta = 20;
                        xmin = (int)(rect.min.x);
                        xmax = (int)(rect.max.x);
                        ymin = (int)(rect.min.y);
                        ymax = (int)(rect.max.y);

                        camPos = float3.zero;
                        //camDelta = 0.1f;

                        float k = 0.025f;
                        float y0 = 0.1f;
                        camDelta = y0 + k * camTrans.position.y;
                        //camDelta = k * camTrans.position.y;

                        if ((xmin <= pos.x && pos.x <= xmin + delta)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //1
                            camPos.x -= camDelta;
                            camPos.y -= camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //2
                            camPos.y -= camDelta;
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //3
                            camPos.x += camDelta;
                            camPos.y -= camDelta;
                        }
                        else if ((xmin <= pos.x && pos.x <= xmin + delta)
                             && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //4
                            camPos.x -= camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                             && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //5
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //6
                            camPos.x += camDelta;
                        }
                        else if ((xmin <= pos.x && pos.x <= xmin + delta)
                            && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //7
                            camPos.x -= camDelta;
                            camPos.y += camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                             && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //8                   
                            camPos.y += camDelta;
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //9
                            camPos.x += camDelta;
                            camPos.y += camDelta;
                        }

                        //float3x3 mat = new float3x3(camTrans.rotation);
                        //float3x3 mat1 = float3x3.identity;
                        //mat1.c0 = mat.c0;
                        //mat1.c1 = new float3(0.0f, 1.0f, 0.0f);
                        //float3 localPos = math.mul(mat, camPos);

                        float3 xaxis = math.rotate(camTrans.rotation, new float3(1.0f, 0.0f, 0.0f));
                        float3 yaxis = new float3(0.0f, 1.0f, 0.0f);
                        float3 zaxis = math.cross(xaxis, yaxis);

                        float3 localPos = xaxis * camPos.x + zaxis * camPos.y;
                        camTrans.position += (Vector3)localPos;
                    }

                    yield return null;
                }
            }
        }

        public IEnumerator ZoomInOutIns_pace(Transform camTrans, int delta = 100)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            float3 prePos = float3.zero;
            float3 curPos = float3.zero;
            float preR = 0.0f;
            float curR = 0.0f;

            bool down = false;

            float4x4 preW = float4x4.identity;

            Rect pRect = cam.pixelRect;
            Vector2 min = pRect.min;
            Vector2 size = pRect.size;
            Vector2 vDelta = new Vector2(delta, delta);
            Rect rect = new Rect(min + vDelta, size - 2.0f * vDelta);

            if (Input.touchSupported)
            {
                while (true)
                {
                    if (Input.touchCount > 1 && activeZoom)
                    {
                        var touches = Input.touches;
                        var touch0 = touches[0];
                        var touch1 = touches[1];


                        if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
                        {
                            down = true;
                            prePos = new float3((float2)(touch0.position + touch1.position) * 0.5f, 0.0f);
                            preR = 0.5f * (math.distance(prePos, new float3((float2)touch0.position, 0.0f)) + math.distance(prePos, new float3((float2)touch1.position, 0.0f)));
                            preW = camTrans.localToWorldMatrix;
                        }

                        if (rect.Contains(touch0.position) && rect.Contains(touch1.position))
                        {
                            if (down)
                            {
                                //curPos = new float3((float2)(touch0.position + touch1.position) * 0.5f, 0.0f);
                                curR = 0.5f * (math.distance(prePos, new float3((float2)touch0.position, 0.0f)) + math.distance(prePos, new float3((float2)touch1.position, 0.0f)));
                                //float zDelta = (curR - preR) * 0.01f;

                                float k = 0.4f;
                                float zDelta = (curR - preR) * 0.005f * k * camTrans.position.y;

                                float3 camPos = new float3(0.0f, 0.0f, zDelta);
                                camTrans.position = math.mul(preW, new float4(camPos, 1.0f)).xyz;
                            }
                        }
                        else
                        {
                            down = false;
                        }

                        if (touch0.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Ended)
                        {
                            down = false;
                        }

                    }

                    //if (Input.touchCount > 0)
                    //{
                    //    var touches = Input.touches;
                    //    var touch = touches[0];
                    //
                    //    if (touch.phase == TouchPhase.Began)
                    //    {
                    //        down = true;
                    //        prePos = new float3((float2)touch.position, 0.0f);
                    //        preW = camTrans.localToWorldMatrix;
                    //    }
                    //
                    //    if (rect.Contains(touch.position))
                    //    {
                    //        if (down)
                    //        {
                    //            curPos = new float3((float2)touch.position, 0.0f);
                    //            float zDelta = (curPos - prePos).x * 0.01f;
                    //            float3 camPos = new float3(0.0f, 0.0f, zDelta);
                    //
                    //            camTrans.position = math.mul(preW, new float4(camPos, 1.0f)).xyz;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        down = false;
                    //    }
                    //
                    //    if (touch.phase == TouchPhase.Ended)
                    //    {
                    //        down = false;
                    //    }
                    //
                    //}

                    yield return null;
                    if (!activeZoom)
                    {
                        down = false;
                    }
                }
            }
            else
            {
                while (true)
                {
                    if (activeZoom)
                    {
                        if (Input.GetMouseButtonDown(1))
                        {
                            down = true;
                            prePos = Input.mousePosition;
                            preW = camTrans.localToWorldMatrix;
                        }

                        if (rect.Contains(Input.mousePosition))
                        {
                            if (down)
                            {
                                curPos = Input.mousePosition;
                                //float zDelta = (curPos - prePos).x * 0.01f;

                                float k = 0.2f;
                                //float y0;
                                float zDelta = (curPos - prePos).x * 0.005f * k * camTrans.position.y;
                                float3 camPos = new float3(0.0f, 0.0f, zDelta);

                                camTrans.position = math.mul(preW, new float4(camPos, 1.0f)).xyz;
                            }
                        }
                        else
                        {
                            down = false;
                        }

                        if (Input.GetMouseButtonUp(1))
                        {
                            down = false;
                        }
                    }

                    yield return null;
                    if (!activeZoom)
                    {
                        down = false;
                    }
                }
            }
        }



        public IEnumerator MoveParallelInsDown_pace(Transform camTrans, int delta = 100)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();
            Rect rect = cam.pixelRect;

            quaternion preRot = quaternion.identity;

            float3 posf;
            int3 pos;
            //int delta = 20;
            int xmin;
            int xmax;
            int ymin;
            int ymax;

            float3 camPos = float3.zero;
            float camDelta = 1.0f;

            bool down = false;

            while (true)
            {
                if (activeMoveParallel)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        down = true;
                    }

                    if (down)
                    {
                        posf = Input.mousePosition;
                        pos = new int3((int)posf.x, (int)posf.y, (int)posf.z);
                        //delta = 20;
                        xmin = (int)(rect.min.x);
                        xmax = (int)(rect.max.x);
                        ymin = (int)(rect.min.y);
                        ymax = (int)(rect.max.y);

                        camPos = float3.zero;
                        //camDelta = 0.1f;

                        float k = 0.025f;
                        float y0 = 0.1f;
                        camDelta = y0 + k * camTrans.position.y;
                        //camDelta = k * camTrans.position.y;

                        if ((xmin <= pos.x && pos.x <= xmin + delta)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //1
                            camPos.x -= camDelta;
                            camPos.y -= camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //2
                            camPos.y -= camDelta;
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //3
                            camPos.x += camDelta;
                            camPos.y -= camDelta;
                        }
                        else if ((xmin <= pos.x && pos.x <= xmin + delta)
                             && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //4
                            camPos.x -= camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                             && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //5
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //6
                            camPos.x += camDelta;
                        }
                        else if ((xmin <= pos.x && pos.x <= xmin + delta)
                            && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //7
                            camPos.x -= camDelta;
                            camPos.y += camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                             && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //8                   
                            camPos.y += camDelta;
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //9
                            camPos.x += camDelta;
                            camPos.y += camDelta;
                        }

                        //float3x3 mat = new float3x3(camTrans.rotation);
                        //float3x3 mat1 = float3x3.identity;
                        //mat1.c0 = mat.c0;
                        //mat1.c1 = new float3(0.0f, 1.0f, 0.0f);
                        //float3 localPos = math.mul(mat, camPos);

                        float3 xaxis = math.rotate(camTrans.rotation, new float3(1.0f, 0.0f, 0.0f));
                        float3 yaxis = new float3(0.0f, 1.0f, 0.0f);
                        float3 zaxis = math.cross(xaxis, yaxis);

                        float3 localPos = xaxis * camPos.x + zaxis * camPos.y;
                        camTrans.position += (Vector3)localPos;
                    }

                    if (Input.GetMouseButtonUp(0))
                    {
                        down = false;
                    }
                }

                yield return null;
            }



            if (Input.touchSupported)
            {
                while (true)
                {
                    if (Input.touchCount > 0 && activeMoveParallel)
                    {
                        var touches = Input.touches;
                        var touch = touches[0];

                        posf = new float3((float2)touch.position, 0.0f);
                        pos = new int3((int)posf.x, (int)posf.y, (int)posf.z);
                        //delta = 200;
                        xmin = (int)(rect.min.x);
                        xmax = (int)(rect.max.x);
                        ymin = (int)(rect.min.y);
                        ymax = (int)(rect.max.y);

                        camPos = float3.zero;
                        //camDelta = 0.2f;

                        float k = 0.025f;
                        float y0 = 0.01f;
                        camDelta = y0 + k * camTrans.position.y;
                        //camDelta = k * camTrans.position.y;
                        if ((xmin <= pos.x && pos.x <= xmin + delta)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //1
                            camPos.x -= camDelta;
                            camPos.y -= camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //2
                            camPos.y -= camDelta;
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //3
                            camPos.x += camDelta;
                            camPos.y -= camDelta;
                        }
                        else if ((xmin <= pos.x && pos.x <= xmin + delta)
                             && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //4
                            camPos.x -= camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                             && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //5
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //6
                            camPos.x += camDelta;
                        }
                        else if ((xmin <= pos.x && pos.x <= xmin + delta)
                            && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //7
                            camPos.x -= camDelta;
                            camPos.y += camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                             && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //8                   
                            camPos.y += camDelta;
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //9
                            camPos.x += camDelta;
                            camPos.y += camDelta;
                        }

                        //float3x3 mat = new float3x3(camTrans.rotation);
                        //float3 localPos = math.mul(mat, camPos);

                        float3 xaxis = math.rotate(camTrans.rotation, new float3(1.0f, 0.0f, 0.0f));
                        float3 yaxis = new float3(0.0f, 1.0f, 0.0f);
                        float3 zaxis = math.cross(xaxis, yaxis);

                        float3 localPos = xaxis * camPos.x + zaxis * camPos.y;
                        camTrans.position += (Vector3)localPos;
                    }

                    yield return null;

                }
            }
            else
            {
                while (true)
                {
                    if (activeMoveParallel)
                    {
                        posf = Input.mousePosition;
                        pos = new int3((int)posf.x, (int)posf.y, (int)posf.z);
                        //delta = 20;
                        xmin = (int)(rect.min.x);
                        xmax = (int)(rect.max.x);
                        ymin = (int)(rect.min.y);
                        ymax = (int)(rect.max.y);

                        camPos = float3.zero;
                        //camDelta = 0.1f;

                        float k = 0.025f;
                        float y0 = 0.1f;
                        camDelta = y0 + k * camTrans.position.y;
                        //camDelta = k * camTrans.position.y;

                        if ((xmin <= pos.x && pos.x <= xmin + delta)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //1
                            camPos.x -= camDelta;
                            camPos.y -= camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //2
                            camPos.y -= camDelta;
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            //3
                            camPos.x += camDelta;
                            camPos.y -= camDelta;
                        }
                        else if ((xmin <= pos.x && pos.x <= xmin + delta)
                             && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //4
                            camPos.x -= camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                             && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //5
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymin + delta < pos.y && pos.y < ymax - delta))
                        {
                            //6
                            camPos.x += camDelta;
                        }
                        else if ((xmin <= pos.x && pos.x <= xmin + delta)
                            && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //7
                            camPos.x -= camDelta;
                            camPos.y += camDelta;
                        }
                        else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                             && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //8                   
                            camPos.y += camDelta;
                        }
                        else if ((xmax - delta <= pos.x && pos.x <= xmax)
                            && (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            //9
                            camPos.x += camDelta;
                            camPos.y += camDelta;
                        }

                        //float3x3 mat = new float3x3(camTrans.rotation);
                        //float3x3 mat1 = float3x3.identity;
                        //mat1.c0 = mat.c0;
                        //mat1.c1 = new float3(0.0f, 1.0f, 0.0f);
                        //float3 localPos = math.mul(mat, camPos);

                        float3 xaxis = math.rotate(camTrans.rotation, new float3(1.0f, 0.0f, 0.0f));
                        float3 yaxis = new float3(0.0f, 1.0f, 0.0f);
                        float3 zaxis = math.cross(xaxis, yaxis);

                        float3 localPos = xaxis * camPos.x + zaxis * camPos.y;
                        camTrans.position += (Vector3)localPos;
                    }

                    yield return null;
                }
            }
        }

    }

    public static class LightCamera
    {
        //orthogonal
        public static void AdjustOrthoFrustum(Camera lightCam, Camera mainCam)
        {
            lightCam.orthographic = true;

            Vector3[] vertices = new Vector3[8];
            LightCamera.GetVerticesOfPerspFrustum(Camera.main, vertices);
            Vector3 center = LightCamera.GetCenterOfFrustum(vertices);

            float[] radius = new float[8];
            for (int i = 0; i < 8; i++)
            {
                radius[i] = Vector3.Distance(center, vertices[i]);
            }
            float maxRadius = Mathf.Max(radius);

            Matrix4x4 mat = lightCam.gameObject.transform.root.localToWorldMatrix;
            //Vector4 position = mainCam.cameraToWorldMatrix * new Vector4(center.x, center.y, -center.z, 1.0f);
            Vector4 position = mainCam.gameObject.transform.localToWorldMatrix *
                new Vector4(center.x, center.y, center.z, 1.0f);
            //Debug.DrawRay(new Vector3(position.x, position.y, position.z), 2.0f * Vector3.up, Color.red, 1000.0f);
            mat.m03 = position.x;
            mat.m13 = position.y;
            mat.m23 = position.z;
            lightCam.gameObject.transform.root.position = (Vector3)(mat * new Vector4(0.0f, 0.0f, -1.0f * maxRadius, 1.0f));

            lightCam.farClipPlane = 2.0f * maxRadius;
            lightCam.nearClipPlane = 0.0f * maxRadius;
            lightCam.orthographicSize = maxRadius;

        }

        //perspective
        public static void AdjustPerspFrustum(Camera lightCam, Camera mainCam)
        {
            lightCam.orthographic = false;

            Vector3[] vertices = new Vector3[8];
            LightCamera.GetVerticesOfPerspFrustum(mainCam, vertices);
            Vector3 center = LightCamera.GetCenterOfFrustum(vertices);
            center = (Vector3)(mainCam.transform.localToWorldMatrix * new Vector4(center.x, center.y, center.z, 1.0f));

            Vector3 forward = center - lightCam.gameObject.transform.root.position;
            lightCam.gameObject.transform.root.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = (Vector3)(lightCam.gameObject.transform.root.worldToLocalMatrix * mainCam.transform.localToWorldMatrix *
                    new Vector4(vertices[i].x, vertices[i].y, vertices[i].z, 1.0f));
            }

            float[] factors = new float[4];
            LightCamera.SetPerspFrustum(vertices, factors);

            lightCam.farClipPlane = factors[0];
            lightCam.nearClipPlane = factors[1];
            lightCam.fieldOfView = 2.0f * factors[2];
            lightCam.aspect = factors[3];
        }

        public static void GetVerticesOfPerspFrustum(Camera cam, Vector3[] outVertices)
        {
            float aspect = cam.aspect;
            float far = cam.farClipPlane;
            float near = cam.nearClipPlane;
            float top = near * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float bottom = -top;
            float right = top * cam.aspect;
            float left = -right;
            float zAspect = far / near;

            outVertices[0] = new Vector3(right, top, near);
            outVertices[1] = new Vector3(-right, top, near);
            outVertices[2] = new Vector3(right, -top, near);
            outVertices[3] = new Vector3(-right, -top, near);
            outVertices[4] = new Vector3(right * zAspect, top * zAspect, far);
            outVertices[5] = new Vector3(-right * zAspect, top * zAspect, far);
            outVertices[6] = new Vector3(right * zAspect, -top * zAspect, far);
            outVertices[7] = new Vector3(-right * zAspect, -top * zAspect, far);
        }

        public static Vector3 GetCenterOfFrustum(Vector3[] points)
        {
            Vector3 result = Vector3.zero;

            for (int i = 0; i < points.Length; i++)
            {
                result = result + points[i];
            }

            result = (1 / (float)points.Length) * result;

            return result;
        }

        public static void SetOrthoFrustum(Vector3[] vertices, float[] faces)
        {
            Vector3[] normals = new Vector3[6];
            normals[0] = Vector3.forward;
            normals[1] = Vector3.back;
            normals[2] = Vector3.up;
            normals[3] = Vector3.down;
            normals[4] = Vector3.right;
            normals[5] = Vector3.left;


            float[] distances = new float[8];
            for (int i = 0; i < normals.Length; i++)
            {
                for (int j = 0; j < vertices.Length; j++)
                {
                    distances[j] = Vector3.Dot(normals[i], vertices[j]);
                }

                faces[i] = Mathf.Max(distances);
            }
        }

        public static void SetPerspFrustum(Vector3[] vertices, float[] factors)
        {
            float[] distances = new float[8];
            float[] angles = new float[8];

            for (int i = 0; i < vertices.Length; i++)
            {
                distances[i] = Vector3.Dot(Vector3.forward, vertices[i]);

                Vector3 temp;
                temp = new Vector3(vertices[i].x, 0.0f, vertices[i].z);
                float angleX = Mathf.Abs(Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(Vector3.forward, temp.normalized)));
                temp = new Vector3(0.0f, vertices[i].y, vertices[i].z);
                float angleY = Mathf.Abs(Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(Vector3.forward, temp.normalized)));
                angles[i] = Mathf.Max(angleX, angleY);
            }
            factors[0] = Mathf.Max(distances);  //farClipPlane
            factors[1] = Mathf.Min(distances);  //nearClipPlane                   
            factors[2] = Mathf.Max(angles);  //fieldOfView
            factors[3] = 1.0f;  //aspectRadio
        }

    }

    public class PVFMesh
    {
        static Mesh cubeMesh;
        private Mesh frusMesh;
        public float4 frustumInfo;

        private List<Vector3> vertex;
        private List<Vector3> normal;

        static PVFMesh()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            PVFMesh.cubeMesh = cube.GetComponent<MeshFilter>().mesh;
            GameObject.DestroyImmediate(cube);
        }

        public PVFMesh(float4 frustumInfo)
        {
            this.frusMesh = UnityEngine.Object.Instantiate<Mesh>(PVFMesh.cubeMesh);
            this.vertex = new List<Vector3>();
            this.normal = new List<Vector3>();
            for (int i = 0; i < PVFMesh.cubeMesh.vertexCount; i++)
            {
                PVFMesh.cubeMesh.GetVertices(this.vertex);
                PVFMesh.cubeMesh.GetNormals(this.normal);
            }
            ChangeCubeToPVF(frustumInfo);
        }

        public void ChangeCubeToPVF(float4 frustumInfo)
        {
            //frustumInfo.x = math.clamp(frustumInfo.x, 10.0f, 170.0f);
            //frustumInfo.y = math.clamp(frustumInfo.y, 0.2f, 5.0f);
            //frustumInfo.z = math.clamp(frustumInfo.z, 0.2f, 50.0f);
            //frustumInfo.w = math.clamp(frustumInfo.w, frustumInfo.z + 1.0f, 300.0f);

            //frustumInfo.w = math.clamp(frustumInfo.w, frustumInfo.z + 1.0f, 100.0f);


            this.frustumInfo = frustumInfo;
            float fov = frustumInfo.x;
            float aspect = frustumInfo.y;
            float near = frustumInfo.z;
            float far = frustumInfo.w;

            Matrix4x4 m = Matrix4x4.identity;
            m.m11 = Mathf.Tan(fov / 2.0f * Mathf.Deg2Rad);
            m.m00 = aspect * m.m11;
            m.m22 = 0.0f;
            m.m23 = 1.0f;
            m.m32 = -(far - near) / (2 * far * near);
            m.m33 = +(far + near) / (2 * far * near);

            float3 nr = new float3(+1.0f, +0.0f, -m.m00);
            float3 nl = new float3(-1.0f, +0.0f, -m.m00);
            float3 nu = new float3(+0.0f, +1.0f, -m.m11);
            float3 nd = new float3(+0.0f, -1.0f, -m.m11);

            for (int i = 0; i < PVFMesh.cubeMesh.vertexCount; i++)
            {
                Vector3 v3 = 2.0f * PVFMesh.cubeMesh.vertices[i];
                Vector4 v4 = new Vector4(v3.x, v3.y, v3.z, 1.0f);
                v4 = m * v4;
                v4 = (1.0f / v4.w) * v4;
                v3 = new Vector3(v4.x, v4.y, v4.z);
                vertex[i] = v3;
                if (Vector3.Dot(PVFMesh.cubeMesh.normals[i], new Vector3(+1.0f, +0.0f, 0.0f)) > 0.0f)
                {
                    normal[i] = nr;
                }
                else if (Vector3.Dot(PVFMesh.cubeMesh.normals[i], new Vector3(-1.0f, +0.0f, 0.0f)) > 0.0f)
                {
                    normal[i] = nl;
                }
                else if (Vector3.Dot(PVFMesh.cubeMesh.normals[i], new Vector3(+0.0f, +1.0f, 0.0f)) > 0.0f)
                {
                    normal[i] = nu;
                }
                else if (Vector3.Dot(PVFMesh.cubeMesh.normals[i], new Vector3(+0.0f, -1.0f, 0.0f)) > 0.0f)
                {
                    normal[i] = nd;
                }
            }
            frusMesh.SetVertices(vertex);
            frusMesh.SetNormals(normal);
        }

        public Mesh mesh
        {
            get
            {
                return this.frusMesh;
            }
        }

    }

    public unsafe class PVFMesh_Multi
    {
        static Mesh cubeMesh;
        List<Vector3> pos;
        List<Vector3> normal;
        Vector3[] tpos;
        Vector3[] tnom;

        static PVFMesh_Multi()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            PVFMesh_Multi.cubeMesh = cube.GetComponent<MeshFilter>().mesh;
            GameObject.DestroyImmediate(cube);
        }

        NativeArray<ConstantData> cdata;
        NativeArray<InputData> idata;
        NativeArray<OutputData> odata;

        int count;
        public Mesh[] mesh
        {
            get; private set;
        }

        public float4[] fi
        {
            get; set;
        }

        ActionJob job;

        public PVFMesh_Multi()
        {

        }

        public void DisposeNA()
        {
            if (cdata.IsCreated) cdata.Dispose();
            if (idata.IsCreated) idata.Dispose();
            if (odata.IsCreated) odata.Dispose();
        }

        public IEnumerator Schedule(int count)
        {
            Init(count);
            while (true)
            {
                WriteToNA();
                Execute();
                yield return null;
                ReadFromNA();
            }

        }

        public void Init(int count)
        {
            this.count = count;

            cdata = new NativeArray<ConstantData>(1, Allocator.Persistent);
            idata = new NativeArray<InputData>(count, Allocator.Persistent);
            odata = new NativeArray<OutputData>(count, Allocator.Persistent);

            mesh = new Mesh[count];
            fi = new float4[count];
            for (int i = 0; i < count; i++)
            {
                mesh[i] = Component.Instantiate<Mesh>(cubeMesh);
                fi[i] = new float4(60.0f, 1.0f, 1.0f, 50.0f);
            }

            job = new ActionJob();
            job.cdata = cdata;
            job.idata = idata;
            job.odata = odata;

            pos = new List<Vector3>();
            normal = new List<Vector3>();
            tpos = new Vector3[24];
            tnom = new Vector3[24];
            cubeMesh.GetVertices(pos);
            cubeMesh.GetNormals(normal);

            ConstantData cd = new ConstantData();
            for (int i = 0; i < 24; i++)
            {
                float3* pPos = (float3*)cd.posL;
                float3* pNom = (float3*)cd.normalL;
                pPos[i] = pos[i];
                pNom[i] = normal[i];
            }
            cdata[0] = cd;
        }

        public void WriteToNA()
        {
            InputData id = new InputData();
            for (int i = 0; i < count; i++)
            {
                id.fi = fi[i];
                idata[i] = id;
            }
        }

        public void Execute()
        {
            job.Schedule<ActionJob>(count, 1).Complete();
        }

        public void ReadFromNA()
        {
            for (int i = 0; i < count; i++)
            {
                OutputData od = new OutputData();
                od = odata[i];
                for (int j = 0; j < 24; j++)
                {
                    float3* pPos = (float3*)od.posL;
                    float3* pNom = (float3*)od.normalL;
                    tpos[j] = pPos[j];
                    tnom[j] = pNom[j];
                }
                mesh[i].SetVertices(tpos);
                mesh[i].SetNormals(tnom);
            }
        }

        [BurstCompile]
        struct ActionJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<ConstantData> cdata;
            [ReadOnly]
            public NativeArray<InputData> idata;
            public NativeArray<OutputData> odata;

            public void Execute(int i)
            {
                float4 fi = idata[i].fi;
                float fov = fi.x;
                float aspect = fi.y;
                float near = fi.z;
                float far = fi.w;

                float4x4 m = float4x4.zero;
                m.c1.y = math.tan(0.5f * math.radians(fov));
                m.c0.x = aspect * m.c1.y;
                m.c2.z = 0.0f;
                m.c3.z = 1.0f;
                m.c2.w = -(far - near) / (2 * far * near);
                m.c3.w = +(far + near) / (2 * far * near);

                float3 nr = new float3(+1.0f, +0.0f, -m.c0.x);
                float3 nl = new float3(-1.0f, +0.0f, -m.c0.x);
                float3 nu = new float3(+0.0f, +1.0f, -m.c1.y);
                float3 nd = new float3(+0.0f, -1.0f, -m.c1.y);

                ConstantData constData = cdata[0];
                float3* pv0 = (float3*)constData.posL;
                float3* pn0 = (float3*)constData.normalL;

                OutputData outData = new OutputData();
                float3* pv1 = (float3*)outData.posL;
                float3* pn1 = (float3*)outData.normalL;

                for (int j = 0; j < 24; j++)
                {
                    float4 v4 = new float4(2.0f * pv0[j], 1.0f);
                    v4 = math.mul(m, v4);
                    v4 = (1.0f / v4.w) * v4;
                    pv1[j] = v4.xyz;

                    pn1[j] = pn0[j];
                    if (math.dot(pn0[j], new float3(+1.0f, +0.0f, +0.0f)) > 0.0f)
                    {
                        pn1[j] = nr;
                    }
                    else
                    if (math.dot(pn0[j], new float3(-1.0f, +0.0f, +0.0f)) > 0.0f)
                    {
                        pn1[j] = nl;
                    }
                    else
                    if (math.dot(pn0[j], new float3(+0.0f, +1.0f, +0.0f)) > 0.0f)
                    {
                        pn1[j] = nu;
                    }
                    else
                    if (math.dot(pn0[j], new float3(+0.0f, -1.0f, +0.0f)) > 0.0f)
                    {
                        pn1[j] = nd;
                    }
                }

                odata[i] = outData;
            }
        }

        unsafe struct ConstantData
        {
            public fixed float posL[3 * 24];       // 3 * 4 * 6
            public fixed float normalL[3 * 24];    // 3 * 4 * 6    
        }

        struct InputData
        {
            public float4 fi;
        }

        unsafe struct OutputData
        {
            public fixed float posL[3 * 24];       // 3 * 4 * 6
            public fixed float normalL[3 * 24];    // 3 * 4 * 6 
        }
    }

    public unsafe class PVFCollider_Multi
    {
        static int[] sIndices;
        static float3[] sPos;
        static float3 sCenter;
        static float3x2[] sPlane;

        static PVFCollider_Multi()
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh boxMesh = box.GetComponent<MeshFilter>().mesh;
            GameObject.DestroyImmediate(box);

            sIndices = new int[36];
            int[] indices = boxMesh.GetIndices(0);
            for (int i = 0; i < 36; i++)
            {
                sIndices[i] = indices[i];
            }

            sPos = new float3[24];
            List<Vector3> vertices = new List<Vector3>();
            boxMesh.GetVertices(vertices);
            for (int i = 0; i < 24; i++)
            {
                sPos[i] = vertices[i];
            }

            sCenter = new float3(0.0f);

            sPlane = new float3x2[6];
            sPlane[0].c0 = new float3(+1.0f, +0.0f, +0.0f);
            sPlane[0].c1 = new float3(+0.5f, +0.0f, +0.0f);
            sPlane[1].c0 = new float3(-1.0f, +0.0f, +0.0f);
            sPlane[1].c1 = new float3(-0.5f, +0.0f, +0.0f);
            sPlane[2].c0 = new float3(+0.0f, +1.0f, +0.0f);
            sPlane[2].c1 = new float3(+0.0f, +0.5f, +0.0f);
            sPlane[3].c0 = new float3(+0.0f, -1.0f, +0.0f);
            sPlane[3].c1 = new float3(+0.0f, -0.5f, +0.0f);
            sPlane[4].c0 = new float3(+0.0f, +0.0f, +1.0f);
            sPlane[4].c1 = new float3(+0.0f, +0.0f, +0.5f);
            sPlane[5].c0 = new float3(+0.0f, +0.0f, -1.0f);
            sPlane[5].c1 = new float3(+0.0f, +0.0f, -0.5f);
        }

        NativeArray<ConstantData> cdata;
        NativeArray<InputData> idata;
        NativeArray<OutputData> odata;

        int count;

        public float4[] fi
        {
            get; set;
        }

        public int[] indice
        {
            get { return sIndices; }
        }

        public Vector4[] pos
        {
            get; private set;
        }

        public Vector4[] center
        {
            get; private set;
        }

        public Vector4[] plane
        {
            get; private set;
        }

        ActionJob job;

        public void DisposeNA()
        {
            if (cdata.IsCreated) cdata.Dispose();
            if (idata.IsCreated) idata.Dispose();
            if (odata.IsCreated) odata.Dispose();
        }

        public IEnumerator Schedule(int count)
        {
            Init(count);
            while (true)
            {
                WriteToNA();
                Execute();
                yield return null;
                ReadFromNA();
            }
            //DisposeNA();

        }

        public void Init(int count)
        {
            this.count = count;

            cdata = new NativeArray<ConstantData>(1, Allocator.Persistent);
            idata = new NativeArray<InputData>(count, Allocator.Persistent);
            odata = new NativeArray<OutputData>(count, Allocator.Persistent);

            fi = new float4[count];
            for (int i = 0; i < count; i++)
            {
                fi[i] = new float4(60.0f, 1.0f, 1.0f, 50.0f);
            }

            pos = new Vector4[4 * 6 * count];
            center = new Vector4[count];
            plane = new Vector4[2 * 6 * count];

            job = new ActionJob();
            job.cdata = cdata;
            job.idata = idata;
            job.odata = odata;

            ConstantData cd = new ConstantData();
            for (int i = 0; i < 24; i++)
            {
                float3* pPos = (float3*)cd.pos;
                pPos[i] = sPos[i];
            }

            for (int i = 0; i < 6; i++)
            {
                float3x2* pPlane = (float3x2*)cd.plane;
                pPlane[i] = sPlane[i];
            }

            cdata[0] = cd;

        }

        public void WriteToNA()
        {
            InputData id = new InputData();
            for (int i = 0; i < count; i++)
            {
                id.fi = fi[i];
                idata[i] = id;
            }
        }

        public void Execute()
        {
            job.Schedule<ActionJob>(count, 1).Complete();
        }

        public void ReadFromNA()
        {
            for (int i = 0; i < count; i++)
            {
                OutputData od = odata[i];
                float3* pPos = (float3*)od.pos;
                float3x2* pPlane = (float3x2*)od.plane;

                Vector3 v3;
                for (int j = 0; j < 24; j++)
                {
                    v3 = pPos[j];
                    pos[i * 24 + j] = new Vector4(v3.x, v3.y, v3.z, 0.0f);
                }

                v3 = od.center;
                center[i] = new Vector4(v3.x, v3.y, v3.z, 0.0f);

                for (int j = 0; j < 6; j++)
                {
                    v3 = pPlane[j].c0;
                    plane[i * 12 + 2 * j + 0] = new Vector4(v3.x, v3.y, v3.z, 0.0f);
                    v3 = pPlane[j].c1;
                    plane[i * 12 + 2 * j + 1] = new Vector4(v3.x, v3.y, v3.z, 0.0f);
                }
            }

        }

        struct ActionJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<ConstantData> cdata;
            [ReadOnly]
            public NativeArray<InputData> idata;
            public NativeArray<OutputData> odata;

            public void Execute(int i)
            {
                float4 fi = idata[i].fi;
                float fov = fi.x;
                float aspect = fi.y;
                float near = fi.z;
                float far = fi.w;

                float4x4 m = float4x4.zero;
                m.c1.y = math.tan(0.5f * math.radians(fov));
                m.c0.x = aspect * m.c1.y;
                m.c2.z = 0.0f;
                m.c3.z = 1.0f;
                m.c2.w = -(far - near) / (2 * far * near);
                m.c3.w = +(far + near) / (2 * far * near);

                float3 nr = new float3(+1.0f, +0.0f, -m.c0.x);
                float3 nl = new float3(-1.0f, +0.0f, -m.c0.x);
                float3 nu = new float3(+0.0f, +1.0f, -m.c1.y);
                float3 nd = new float3(+0.0f, -1.0f, -m.c1.y);

                float xn = m.c0.x * near;
                float yn = m.c1.y * near;
                float xf = m.c0.x * far;
                float yf = m.c1.y * far;

                //
                ConstantData cd = cdata[0];
                float3* v0 = (float3*)cd.pos;
                float3* c0 = &cd.center;
                float3x2* p0 = (float3x2*)cd.pos;

                OutputData od = new OutputData();
                float3* v1 = (float3*)od.pos;
                float3* c1 = &od.center;
                float3x2* p1 = (float3x2*)od.plane;

                for (int j = 0; j < 24; j++)
                {
                    float4 vec = new float4(2.0f * v0[j].xyz, 1.0f);
                    vec = math.mul(m, vec);
                    vec = (1.0f / vec.w) * vec;
                    v1[j] = vec.xyz;
                }

                *c1 = (1.0f / 8.0f) * (
                   new float3(+xn, +yn, near) +
                   new float3(-xn, +yn, near) +
                   new float3(+xn, -yn, near) +
                   new float3(-xn, -yn, near) +
                   new float3(+xf, +yf, far) +
                   new float3(-xf, +yf, far) +
                   new float3(+xf, -yf, far) +
                   new float3(-xf, -yf, far));

                p1[0].c0 = nr;
                p1[0].c1 = new float3(0.0f);
                p1[1].c0 = nl;
                p1[1].c1 = new float3(0.0f);
                p1[2].c0 = nu;
                p1[2].c1 = new float3(0.0f);
                p1[3].c0 = nd;
                p1[3].c1 = new float3(0.0f);
                p1[4].c0 = new float3(0.0f, 0.0f, +1.0f);
                p1[4].c1 = new float3(0.0f, 0.0f, far);
                p1[5].c0 = new float3(0.0f, 0.0f, -1.0f);
                p1[5].c1 = new float3(0.0f, 0.0f, near);

                odata[i] = od;
            }
        }

        unsafe struct ConstantData
        {
            public fixed float pos[3 * 24];        //3 * 4 * 6
            public float3 center;
            public fixed float plane[6 * 6];       //3 * 2 * 6
        }

        struct InputData
        {
            public float4 fi;
        }

        unsafe struct OutputData
        {
            public fixed float pos[3 * 24];        //3 * 4 * 6
            public float3 center;
            public fixed float plane[6 * 6];       //3 * 2 * 6
        }
    }

    public class OVFTransform
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 ChangeBoxToElement(float4x4 input, float4 frustumInfo)
        {
            float4x4 output = float4x4.identity;

            float3 pos0 = input.c0.xyz;
            quaternion rot0 = new quaternion(input.c1);
            float3 sca0 = input.c2.xyz;

            float hv = frustumInfo.x;
            float aspect = frustumInfo.y;
            float near = frustumInfo.z;
            float far = frustumInfo.w;
            float tv = 2.0f * hv;
            float far_near = far - near;

            float3 pos1 = pos0 + math.rotate(rot0, new float3(0.0f, 0.0f, sca0.z * (near + 0.5f * (far_near))));
            quaternion rot1 = rot0;
            float3 sca1 = new float3(tv * aspect, tv, far_near) * sca0;

            output.c0 = new float4(pos1, 1.0f);
            output.c1 = rot1.value;
            output.c2 = new float4(sca1, 0.0f);

            return output;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 ChangeBoxToElement(Transform tr, float4 frustumInfo)
        {
            float4x4 output = float4x4.identity;

            float3 pos0 = tr.position;
            quaternion rot0 = tr.rotation;
            float3 sca0 = tr.localScale;

            float hv = frustumInfo.x;
            float aspect = frustumInfo.y;
            float near = frustumInfo.z;
            float far = frustumInfo.w;
            float tv = 2.0f * hv;
            float far_near = far - near;

            float3 pos1 = pos0 + math.rotate(rot0, new float3(0.0f, 0.0f, sca0.z * (near + 0.5f * (far_near))));
            quaternion rot1 = rot0;
            float3 sca1 = new float3(tv * aspect, tv, far_near) * sca0;

            output.c0 = new float4(pos1, 1.0f);
            output.c1 = rot1.value;
            output.c2 = new float4(sca1, 0.0f);

            return output;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 ChangeBoxToMatrix(float4x4 input, float4 frustumInfo)
        {
            float4x4 output = float4x4.identity;

            float3 pos0 = input.c0.xyz;
            quaternion rot0 = new quaternion(input.c1);
            float3 sca0 = input.c2.xyz;

            float hv = frustumInfo.x;
            float aspect = frustumInfo.y;
            float near = frustumInfo.z;
            float far = frustumInfo.w;
            float tv = 2.0f * hv;
            float far_near = far - near;

            float3 pos1 = pos0 + math.rotate(rot0, new float3(0.0f, 0.0f, sca0.z * (near + 0.5f * (far_near))));
            quaternion rot1 = rot0;
            float3 sca1 = new float3(tv * aspect, tv, far_near) * sca0;

            float3x3 R = new float3x3(rot1);
            output.c0 = new float4(sca1.x * R.c0, 0.0f);
            output.c1 = new float4(sca1.y * R.c1, 0.0f);
            output.c2 = new float4(sca1.z * R.c2, 0.0f);
            output.c3 = new float4(pos1, 1.0f);

            return output;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 ChangeBoxToMatrix(Camera cam)
        {
            float4x4 output = float4x4.identity;

            Transform tr = cam.transform;

            float3 pos0 = tr.position;
            quaternion rot0 = tr.rotation;
            float3 sca0 = tr.localScale;

            float hv = cam.orthographicSize;
            float aspect = cam.aspect;
            float near = cam.nearClipPlane;
            float far = cam.farClipPlane;
            float tv = 2.0f * hv;
            float far_near = far - near;

            float3 pos1 = pos0 + math.rotate(rot0, new float3(0.0f, 0.0f, sca0.z * (near + 0.5f * (far_near))));
            quaternion rot1 = rot0;
            float3 sca1 = new float3(tv * aspect, tv, far_near) * sca0;

            float3x3 R = new float3x3(rot1);
            output.c0 = new float4(sca1.x * R.c0, 0.0f);
            output.c1 = new float4(sca1.y * R.c1, 0.0f);
            output.c2 = new float4(sca1.z * R.c2, 0.0f);
            output.c3 = new float4(pos1, 1.0f);

            return output;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 ChangeBoxToMatrix(Transform tr, float4 frustumInfo)
        {
            float4x4 output = float4x4.identity;

            float3 pos0 = tr.position;
            quaternion rot0 = tr.rotation;
            float3 sca0 = tr.localScale;

            float hv = frustumInfo.x;
            float aspect = frustumInfo.y;
            float near = frustumInfo.z;
            float far = frustumInfo.w;
            float tv = 2.0f * hv;
            float far_near = far - near;

            float3 pos1 = pos0 + math.rotate(rot0, new float3(0.0f, 0.0f, sca0.z * (near + 0.5f * (far_near))));
            quaternion rot1 = rot0;
            float3 sca1 = new float3(tv * aspect, tv, far_near) * sca0;

            float3x3 R = new float3x3(rot1);
            output.c0 = new float4(sca1.x * R.c0, 0.0f);
            output.c1 = new float4(sca1.y * R.c1, 0.0f);
            output.c2 = new float4(sca1.z * R.c2, 0.0f);
            output.c3 = new float4(pos1, 1.0f);

            return output;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ChangeBoxToElement_Matrix(float4x4 input, float4 frustumInfo, out float4x4 E_cull, out float4x4 W_cull)
        {
            E_cull = float4x4.identity;
            W_cull = float4x4.identity;

            float3 pos0 = input.c0.xyz;
            quaternion rot0 = new quaternion(input.c1);
            float3 sca0 = input.c2.xyz;

            float hv = frustumInfo.x;
            float aspect = frustumInfo.y;
            float near = frustumInfo.z;
            float far = frustumInfo.w;
            float tv = 2.0f * hv;
            float far_near = far - near;

            float3 pos1 = pos0 + math.rotate(rot0, new float3(0.0f, 0.0f, sca0.z * (near + 0.5f * (far_near))));
            quaternion rot1 = rot0;
            float3 sca1 = new float3(tv * aspect, tv, far_near) * sca0;

            E_cull.c0 = new float4(pos1, 1.0f);
            E_cull.c1 = rot1.value;
            E_cull.c2 = new float4(sca1, 0.0f);

            float3x3 R = new float3x3(rot1);
            W_cull.c0 = new float4(sca1.x * R.c0, 0.0f);
            W_cull.c1 = new float4(sca1.y * R.c1, 0.0f);
            W_cull.c2 = new float4(sca1.z * R.c2, 0.0f);
            W_cull.c3 = new float4(pos1, 1.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ChangeBoxToElement_Matrix(float4x4 input, float4 frustumInfo, out float4x4 E_cull, out float4x4 W_cull, out float4x4 Wn_cull)
        {
            E_cull = float4x4.identity;
            W_cull = float4x4.identity;
            Wn_cull = float4x4.identity;

            float3 pos0 = input.c0.xyz;
            quaternion rot0 = new quaternion(input.c1);
            float3 sca0 = input.c2.xyz;

            float hv = frustumInfo.x;
            float aspect = frustumInfo.y;
            float near = frustumInfo.z;
            float far = frustumInfo.w;
            float tv = 2.0f * hv;            
            float far_near = far - near;

            float3 pos1 = pos0 + math.rotate(rot0, new float3(0.0f, 0.0f, sca0.z * (near + 0.5f * (far_near))));
            quaternion rot1 = rot0;
            float3 sca1 = new float3(tv * aspect, tv, far_near) * sca0;

            E_cull.c0 = new float4(pos1, 1.0f);
            E_cull.c1 = rot1.value;
            E_cull.c2 = new float4(sca1, 0.0f);

            float3x3 R = new float3x3(rot1);
            W_cull.c0 = new float4(sca1.x * R.c0, 0.0f);
            W_cull.c1 = new float4(sca1.y * R.c1, 0.0f);
            W_cull.c2 = new float4(sca1.z * R.c2, 0.0f);
            W_cull.c3 = new float4(pos1, 1.0f);

            float3 rs = 1.0f / sca1;
            float3 r0 = rs.x * R.c0;
            float3 r1 = rs.y * R.c1;
            float3 r2 = rs.z * R.c2;
            Wn_cull.c0 = new float4(r0, math.dot(r0, -pos1));
            Wn_cull.c1 = new float4(r1, math.dot(r1, -pos1));
            Wn_cull.c2 = new float4(r2, math.dot(r2, -pos1));
            Wn_cull.c3 = new float4(0.0f, 0.0f, 0.0f, 1.0f);
        }
    }


    public class SphereVF_Cull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void fromPVF(Transform tr, float4 fi, float4[] planes)
        {
            float3 pos = tr.position;
            quaternion rot = tr.rotation;

            float3x3 R = new float3x3(rot);
            float3 dirZ = R.c2;

            float fov = fi.x;
            float aspect = fi.y;
            float near = fi.z;
            float far = fi.w;

            float tany = math.tan(math.radians(0.5f * fov));
            float tanx = aspect * tany;

            unsafe
            {
                float4* ps = stackalloc float4[6];

                ps[0].xyz = math.mul(R, math.normalize(new float3(+1.0f, +0.0f, -tanx)));    //+x
                ps[1].xyz = math.mul(R, math.normalize(new float3(-1.0f, +0.0f, -tanx)));    //-x
                ps[2].xyz = math.mul(R, math.normalize(new float3(+0.0f, +1.0f, -tany)));    //+y
                ps[3].xyz = math.mul(R, math.normalize(new float3(+0.0f, -1.0f, -tany)));    //-y                
                ps[4].xyz = math.mul(R, new float3(0.0f, 0.0f, +1.0f));                      //+z
                ps[5].xyz = math.mul(R, new float3(0.0f, 0.0f, -1.0f));                      //-z

                ps[0].w = -math.dot(ps[0].xyz, pos);
                ps[1].w = -math.dot(ps[1].xyz, pos);
                ps[2].w = -math.dot(ps[2].xyz, pos);
                ps[3].w = -math.dot(ps[3].xyz, pos);
                ps[4].w = -math.dot(ps[4].xyz, pos + dirZ * far);
                ps[5].w = -math.dot(ps[5].xyz, pos + dirZ * near);

                for (int i = 0; i < 6; i++)
                {
                    planes[i] = ps[i];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void fromPVF(float3 pos, quaternion rot, float4 fi, float* ptrPlane)
        {
            float3x3 R = new float3x3(rot);
            float3 dirZ = R.c2;

            float fov = fi.x;
            float aspect = fi.y;
            float near = fi.z;
            float far = fi.w;

            float tany = math.tan(math.radians(0.5f * fov));
            float tanx = aspect * tany;

            unsafe
            {
                float4* ps = stackalloc float4[6];

                ps[0].xyz = math.mul(R, math.normalize(new float3(+1.0f, +0.0f, -tanx)));    //+x
                ps[1].xyz = math.mul(R, math.normalize(new float3(-1.0f, +0.0f, -tanx)));    //-x
                ps[2].xyz = math.mul(R, math.normalize(new float3(+0.0f, +1.0f, -tany)));    //+y
                ps[3].xyz = math.mul(R, math.normalize(new float3(+0.0f, -1.0f, -tany)));    //-y                
                ps[4].xyz = math.mul(R, new float3(0.0f, 0.0f, +1.0f));                      //+z
                ps[5].xyz = math.mul(R, new float3(0.0f, 0.0f, -1.0f));                      //-z

                ps[0].w = -math.dot(ps[0].xyz, pos);
                ps[1].w = -math.dot(ps[1].xyz, pos);
                ps[2].w = -math.dot(ps[2].xyz, pos);
                ps[3].w = -math.dot(ps[3].xyz, pos);
                ps[4].w = -math.dot(ps[4].xyz, pos + dirZ * far);
                ps[5].w = -math.dot(ps[5].xyz, pos + dirZ * near);

                for (int i = 0; i < 6; i++)
                {
                    float4* ptr = (float4*)(ptrPlane + 4 * i);
                    *ptr = ps[i];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void fromOVF(Transform tr, float4 fi, float4[] planes)
        {
            float3 pos = tr.position;
            quaternion rot = tr.rotation;

            float3x3 R = new float3x3(rot);
            float3 dirX = R.c0;
            float3 dirY = R.c1;
            float3 dirZ = R.c2;

            float hv = fi.x;
            float aspect = fi.y;
            float near = fi.z;
            float far = fi.w;

            float ny = hv;
            float nx = hv * aspect;

            unsafe
            {
                float4* ps = stackalloc float4[6];

                ps[0].xyz = math.mul(R, new float3(+1.0f, +0.0f, +0.0f));    //+x
                ps[1].xyz = math.mul(R, new float3(-1.0f, +0.0f, +0.0f));    //-x
                ps[2].xyz = math.mul(R, new float3(+0.0f, +1.0f, +0.0f));    //+y
                ps[3].xyz = math.mul(R, new float3(+0.0f, -1.0f, +0.0f));    //-y
                ps[4].xyz = math.mul(R, new float3(+0.0f, +0.0f, +1.0f));    //+z
                ps[5].xyz = math.mul(R, new float3(+0.0f, +0.0f, -1.0f));    //-z

                float3 posZ = pos + dirZ * near;
                float3 posXp = dirX * (+nx);
                float3 posXn = -posXp;
                float3 posYp = dirY * (+ny);
                float3 posYn = -posYp;

                ps[0].w = -math.dot(ps[0].xyz, posZ + posXp);
                ps[1].w = -math.dot(ps[1].xyz, posZ + posXn);
                ps[2].w = -math.dot(ps[2].xyz, posZ + posYp);
                ps[3].w = -math.dot(ps[3].xyz, posZ + posYn);
                ps[4].w = -math.dot(ps[4].xyz, pos + dirZ * far);
                ps[5].w = -math.dot(ps[5].xyz, pos + dirZ * near);

                for (int i = 0; i < 6; i++)
                {
                    planes[i] = ps[i];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void fromOVF(float3 pos, quaternion rot, float4 fi, float* ptrPlane)
        {
            float3x3 R = new float3x3(rot);
            float3 dirX = R.c0;
            float3 dirY = R.c1;
            float3 dirZ = R.c2;

            float hv = fi.x;
            float aspect = fi.y;
            float near = fi.z;
            float far = fi.w;

            float ny = hv;
            float nx = hv * aspect;

            unsafe
            {
                float4* ps = stackalloc float4[6];

                ps[0].xyz = math.mul(R, new float3(+1.0f, +0.0f, +0.0f));    //+x
                ps[1].xyz = math.mul(R, new float3(-1.0f, +0.0f, +0.0f));    //-x
                ps[2].xyz = math.mul(R, new float3(+0.0f, +1.0f, +0.0f));    //+y
                ps[3].xyz = math.mul(R, new float3(+0.0f, -1.0f, +0.0f));    //-y
                ps[4].xyz = math.mul(R, new float3(+0.0f, +0.0f, +1.0f));    //+z
                ps[5].xyz = math.mul(R, new float3(+0.0f, +0.0f, -1.0f));    //-z

                float3 posZ = pos + dirZ * near;
                float3 posXp = dirX * (+nx);
                float3 posXn = -posXp;
                float3 posYp = dirY * (+ny);
                float3 posYn = -posYp;

                ps[0].w = -math.dot(ps[0].xyz, posZ + posXp);
                ps[1].w = -math.dot(ps[1].xyz, posZ + posXn);
                ps[2].w = -math.dot(ps[2].xyz, posZ + posYp);
                ps[3].w = -math.dot(ps[3].xyz, posZ + posYn);
                ps[4].w = -math.dot(ps[4].xyz, pos + dirZ * far);
                ps[5].w = -math.dot(ps[5].xyz, pos + dirZ * near);

                for (int i = 0; i < 6; i++)
                {
                    float4* ptr = (float4*)(ptrPlane + 4 * i);
                    *ptr = ps[i];
                }
            }
        }

        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void fromPVF(Transform tr, float4 fi, float4[] vertex, int2[] eid, float4[] plane)
        {
            float3 pos = tr.position;
            quaternion rot = tr.rotation;

            float3x3 R = new float3x3(rot);
            float3 dirZ = R.c2;
            float4x4 W = float4x4.identity;
            W.c0 = new float4(R.c0, 0.0f);
            W.c1 = new float4(R.c1, 0.0f);
            W.c2 = new float4(R.c2, 0.0f);
            W.c3 = new float4(pos, 1.0f);

            float fov = fi.x;
            float aspect = fi.y;
            float near = fi.z;
            float far = fi.w;

            float tany = math.tan(math.radians(0.5f * fov));
            float tanx = aspect * tany;

            float xn = tanx * near;
            float yn = tany * near;
            float xf = tanx * far;
            float yf = tany * far;

            unsafe
            {
                float4* vs = stackalloc float4[8];

                vs[0] = math.mul(W, new float4(new float3(+xn, +yn, near), 1.0f));
                vs[1] = math.mul(W, new float4(new float3(-xn, +yn, near), 1.0f));
                vs[2] = math.mul(W, new float4(new float3(-xn, -yn, near), 1.0f));
                vs[3] = math.mul(W, new float4(new float3(+xn, -yn, near), 1.0f));
                vs[4] = math.mul(W, new float4(new float3(+xf, +yf, far), 1.0f));
                vs[5] = math.mul(W, new float4(new float3(-xf, +yf, far), 1.0f));
                vs[6] = math.mul(W, new float4(new float3(-xf, -yf, far), 1.0f));
                vs[7] = math.mul(W, new float4(new float3(+xf, -yf, far), 1.0f));

                for (int i = 0; i < 8; i++)
                {
                    vertex[i] = vs[i];
                }
            }

            unsafe
            {
                int2* e = stackalloc int2[12];
                e[0].x = 0;
                e[0].y = 1;
                e[1].x = 1;
                e[1].y = 2;
                e[2].x = 2;
                e[2].y = 3;
                e[3].x = 3;
                e[3].y = 0;

                e[4].x = 4;
                e[4].y = 5;
                e[5].x = 5;
                e[5].y = 6;
                e[6].x = 6;
                e[6].y = 7;
                e[7].x = 7;
                e[7].y = 4;

                e[8].x = 0;
                e[8].y = 4;
                e[9].x = 1;
                e[9].y = 5;
                e[10].x = 2;
                e[10].y = 6;
                e[11].x = 3;
                e[11].y = 7;

                for (int i = 0; i < 12; i++)
                {
                    eid[i] = e[i];
                }
            }

            unsafe
            {
                float4* ps = stackalloc float4[6];

                ps[0].xyz = math.mul(R, -math.normalize(new float3(+1.0f, +0.0f, -tanx)));    //+x
                ps[1].xyz = math.mul(R, -math.normalize(new float3(-1.0f, +0.0f, -tanx)));    //-x
                ps[2].xyz = math.mul(R, -math.normalize(new float3(+0.0f, +1.0f, -tany)));    //+y
                ps[3].xyz = math.mul(R, -math.normalize(new float3(+0.0f, -1.0f, -tany)));    //-y                
                ps[4].xyz = math.mul(R, -new float3(0.0f, 0.0f, +1.0f));                      //+z
                ps[5].xyz = math.mul(R, -new float3(0.0f, 0.0f, -1.0f));                      //-z

                ps[0].w = -math.dot(ps[0].xyz, pos);
                ps[1].w = -math.dot(ps[1].xyz, pos);
                ps[2].w = -math.dot(ps[2].xyz, pos);
                ps[3].w = -math.dot(ps[3].xyz, pos);
                ps[4].w = -math.dot(ps[4].xyz, pos + dirZ * far);
                ps[5].w = -math.dot(ps[5].xyz, pos + dirZ * near);

                for (int i = 0; i < 6; i++)
                {
                    plane[i] = ps[i];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Cull(float4 sphere, float4[] vertex, int2[] eid, float4[] plane)
        {
            bool isCull = true;

            float3 center = sphere.xyz;
            float radius = sphere.w;

            for (int i = 0; i < 8; i++)
            {
                if (math.distance(center, vertex[i].xyz) <= radius)
                {
                    isCull = false;
                    break;
                }
            }

            for (int i = 0; i < 12; i++)
            {
                float3 v0 = vertex[eid[i].x].xyz;
                float3 v1 = vertex[eid[i].y].xyz;
                float3 d0 = math.normalize(v1 - v0);
                float3 d1 = math.normalize(v0 - v1);

                float3 p0 = center - v0;
                float l0 = math.dot(d0, p0);

                float3 p1 = center - v1;
                float l1 = math.dot(d1, p1);

                float d = math.length(p0 - d0 * l0);

                if (l0 >= 0.0f && l1 >= 0.0f && d <= radius)
                {
                    isCull = false;
                }
            }

            {
                float xp0 = math.dot(plane[0].xyz, center) + plane[0].w;
                float xn0 = math.dot(plane[1].xyz, center) + plane[1].w;
                float yp0 = math.dot(plane[2].xyz, center) + plane[2].w;
                float yn0 = math.dot(plane[3].xyz, center) + plane[3].w;
                float zp0 = math.dot(plane[4].xyz, center) + plane[4].w;
                float zn0 = math.dot(plane[5].xyz, center) + plane[5].w;

                float xp1 = xp0 + radius;
                float xn1 = xn0 + radius;
                float yp1 = yp0 + radius;
                float yn1 = yn0 + radius;
                float zp1 = zp0 + radius;
                float zn1 = zn0 + radius;

                if (xp1 >= 0.0f && xn1 >= 0.0f && yp0 >= 0.0f && yn0 >= 0.0f && zp0 >= 0.0f && zn0 >= 0.0f)
                {
                    isCull = false;
                }
                else
                if (xp0 >= 0.0f && xn0 >= 0.0f && yp1 >= 0.0f && yn1 >= 0.0f && zp0 >= 0.0f && zn0 >= 0.0f)
                {
                    isCull = false;
                }
                else
                if (xp0 >= 0.0f && xn0 >= 0.0f && yp0 >= 0.0f && yn0 >= 0.0f && zp1 >= 0.0f && zn1 >= 0.0f)
                {
                    isCull = false;
                }

                //if (xp0 >= 0.0f && xn0 >= 0.0f && yp0 >= 0.0f && yn0 >= 0.0f && zp0 >= 0.0f && zn0 >= 0.0f)
                //{
                //    isCull = false;
                //}

            }

            return isCull;
        }

        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void fromPVF(Transform tr, float4 fi, float4[] vertex, int2[] eid, float4[] plane, int4[] fid)
        {
            float3 pos = tr.position;
            quaternion rot = tr.rotation;

            float3x3 R = new float3x3(rot);
            float3 dirZ = R.c2;
            float4x4 W = float4x4.identity;
            W.c0 = new float4(R.c0, 0.0f);
            W.c1 = new float4(R.c1, 0.0f);
            W.c2 = new float4(R.c2, 0.0f);
            W.c3 = new float4(pos, 1.0f);

            float fov = fi.x;
            float aspect = fi.y;
            float near = fi.z;
            float far = fi.w;

            float tany = math.tan(math.radians(0.5f * fov));
            float tanx = aspect * tany;

            float xn = tanx * near;
            float yn = tany * near;
            float xf = tanx * far;
            float yf = tany * far;

            unsafe
            {
                float4* vs = stackalloc float4[8];

                vs[0] = math.mul(W, new float4(new float3(+xn, +yn, near), 1.0f));
                vs[1] = math.mul(W, new float4(new float3(-xn, +yn, near), 1.0f));
                vs[2] = math.mul(W, new float4(new float3(-xn, -yn, near), 1.0f));
                vs[3] = math.mul(W, new float4(new float3(+xn, -yn, near), 1.0f));
                vs[4] = math.mul(W, new float4(new float3(+xf, +yf, far), 1.0f));
                vs[5] = math.mul(W, new float4(new float3(-xf, +yf, far), 1.0f));
                vs[6] = math.mul(W, new float4(new float3(-xf, -yf, far), 1.0f));
                vs[7] = math.mul(W, new float4(new float3(+xf, -yf, far), 1.0f));

                for (int i = 0; i < 8; i++)
                {
                    vertex[i] = vs[i];
                }
            }

            unsafe
            {
                int2* e = stackalloc int2[12];
                e[0].x = 0;
                e[0].y = 1;
                e[1].x = 1;
                e[1].y = 2;
                e[2].x = 2;
                e[2].y = 3;
                e[3].x = 3;
                e[3].y = 0;

                e[4].x = 4;
                e[4].y = 5;
                e[5].x = 5;
                e[5].y = 6;
                e[6].x = 6;
                e[6].y = 7;
                e[7].x = 7;
                e[7].y = 4;

                e[8].x = 0;
                e[8].y = 4;
                e[9].x = 1;
                e[9].y = 5;
                e[10].x = 2;
                e[10].y = 6;
                e[11].x = 3;
                e[11].y = 7;

                for (int i = 0; i < 12; i++)
                {
                    eid[i] = e[i];
                }
            }

            unsafe
            {
                float4* ps = stackalloc float4[6];

                ps[0].xyz = math.mul(R, -math.normalize(new float3(+1.0f, +0.0f, -tanx)));    //+x
                ps[1].xyz = math.mul(R, -math.normalize(new float3(-1.0f, +0.0f, -tanx)));    //-x
                ps[2].xyz = math.mul(R, -math.normalize(new float3(+0.0f, +1.0f, -tany)));    //+y
                ps[3].xyz = math.mul(R, -math.normalize(new float3(+0.0f, -1.0f, -tany)));    //-y                
                ps[4].xyz = math.mul(R, -new float3(0.0f, 0.0f, +1.0f));                      //+z
                ps[5].xyz = math.mul(R, -new float3(0.0f, 0.0f, -1.0f));                      //-z

                ps[0].w = -math.dot(ps[0].xyz, pos);
                ps[1].w = -math.dot(ps[1].xyz, pos);
                ps[2].w = -math.dot(ps[2].xyz, pos);
                ps[3].w = -math.dot(ps[3].xyz, pos);
                ps[4].w = -math.dot(ps[4].xyz, pos + dirZ * far);
                ps[5].w = -math.dot(ps[5].xyz, pos + dirZ * near);

                for (int i = 0; i < 6; i++)
                {
                    plane[i] = ps[i];
                }
            }

            unsafe
            {
                int4* fs = stackalloc int4[6];

                //+x
                fs[0].x = 0;
                fs[0].y = 4;
                fs[0].z = 7;
                fs[0].w = 3;

                //-x
                fs[1].x = 1;
                fs[1].y = 2;
                fs[1].z = 6;
                fs[1].w = 5;

                //+y
                fs[2].x = 0;
                fs[2].y = 1;
                fs[2].z = 5;
                fs[2].w = 4;

                //-y
                fs[3].x = 3;
                fs[3].y = 7;
                fs[3].z = 6;
                fs[3].w = 2;

                //+z far
                fs[4].x = 4;
                fs[4].y = 5;
                fs[4].z = 6;
                fs[4].w = 7;

                //-z near
                fs[5].x = 0;
                fs[5].y = 3;
                fs[5].z = 2;
                fs[5].w = 1;

                for (int i = 0; i < 6; i++)
                {
                    fid[i] = fs[i];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void fromOVF(Transform tr, float4 fi, float4[] vertex, int2[] eid, float4[] plane, int4[] fid)
        {
            float3 pos = tr.position;
            quaternion rot = tr.rotation;

            float3x3 R = new float3x3(rot);
            float3 dirX = R.c0;
            float3 dirY = R.c1;
            float3 dirZ = R.c2;
            float4x4 W = float4x4.identity;
            W.c0 = new float4(R.c0, 0.0f);
            W.c1 = new float4(R.c1, 0.0f);
            W.c2 = new float4(R.c2, 0.0f);
            W.c3 = new float4(pos, 1.0f);

            float hv = fi.x;
            float aspect = fi.y;
            float near = fi.z;
            float far = fi.w;

            float ny = hv;
            float nx = hv * aspect;

            float xn = nx;
            float yn = ny;
            float xf = nx;
            float yf = ny;

            unsafe
            {
                float4* vs = stackalloc float4[8];

                vs[0] = math.mul(W, new float4(new float3(+xn, +yn, near), 1.0f));
                vs[1] = math.mul(W, new float4(new float3(-xn, +yn, near), 1.0f));
                vs[2] = math.mul(W, new float4(new float3(-xn, -yn, near), 1.0f));
                vs[3] = math.mul(W, new float4(new float3(+xn, -yn, near), 1.0f));
                vs[4] = math.mul(W, new float4(new float3(+xf, +yf, far), 1.0f));
                vs[5] = math.mul(W, new float4(new float3(-xf, +yf, far), 1.0f));
                vs[6] = math.mul(W, new float4(new float3(-xf, -yf, far), 1.0f));
                vs[7] = math.mul(W, new float4(new float3(+xf, -yf, far), 1.0f));

                for (int i = 0; i < 8; i++)
                {
                    vertex[i] = vs[i];
                }
            }

            unsafe
            {
                int2* e = stackalloc int2[12];
                e[0].x = 0;
                e[0].y = 1;
                e[1].x = 1;
                e[1].y = 2;
                e[2].x = 2;
                e[2].y = 3;
                e[3].x = 3;
                e[3].y = 0;

                e[4].x = 4;
                e[4].y = 5;
                e[5].x = 5;
                e[5].y = 6;
                e[6].x = 6;
                e[6].y = 7;
                e[7].x = 7;
                e[7].y = 4;

                e[8].x = 0;
                e[8].y = 4;
                e[9].x = 1;
                e[9].y = 5;
                e[10].x = 2;
                e[10].y = 6;
                e[11].x = 3;
                e[11].y = 7;

                for (int i = 0; i < 12; i++)
                {
                    eid[i] = e[i];
                }
            }

            unsafe
            {
                float4* ps = stackalloc float4[6];

                ps[0].xyz = math.mul(R, -new float3(+1.0f, +0.0f, +0.0f));    //+x
                ps[1].xyz = math.mul(R, -new float3(-1.0f, +0.0f, +0.0f));    //-x
                ps[2].xyz = math.mul(R, -new float3(+0.0f, +1.0f, +0.0f));    //+y
                ps[3].xyz = math.mul(R, -new float3(+0.0f, -1.0f, +0.0f));    //-y
                ps[4].xyz = math.mul(R, -new float3(+0.0f, +0.0f, +1.0f));    //+z
                ps[5].xyz = math.mul(R, -new float3(+0.0f, +0.0f, -1.0f));    //-z

                float3 posZ = pos + dirZ * near;
                float3 posXp = dirX * (+nx);
                float3 posXn = -posXp;
                float3 posYp = dirY * (+ny);
                float3 posYn = -posYp;

                ps[0].w = -math.dot(ps[0].xyz, posZ + posXp);
                ps[1].w = -math.dot(ps[1].xyz, posZ + posXn);
                ps[2].w = -math.dot(ps[2].xyz, posZ + posYp);
                ps[3].w = -math.dot(ps[3].xyz, posZ + posYn);
                ps[4].w = -math.dot(ps[4].xyz, pos + dirZ * far);
                ps[5].w = -math.dot(ps[5].xyz, pos + dirZ * near);

                for (int i = 0; i < 6; i++)
                {
                    plane[i] = ps[i];
                }
            }

            unsafe
            {
                int4* fs = stackalloc int4[6];

                //+x
                fs[0].x = 0;
                fs[0].y = 4;
                fs[0].z = 7;
                fs[0].w = 3;

                //-x
                fs[1].x = 1;
                fs[1].y = 2;
                fs[1].z = 6;
                fs[1].w = 5;

                //+y
                fs[2].x = 0;
                fs[2].y = 1;
                fs[2].z = 5;
                fs[2].w = 4;

                //-y
                fs[3].x = 3;
                fs[3].y = 7;
                fs[3].z = 6;
                fs[3].w = 2;

                //+z far
                fs[4].x = 4;
                fs[4].y = 5;
                fs[4].z = 6;
                fs[4].w = 7;

                //-z near
                fs[5].x = 0;
                fs[5].y = 3;
                fs[5].z = 2;
                fs[5].w = 1;

                for (int i = 0; i < 6; i++)
                {
                    fid[i] = fs[i];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Cull(float4 sphere, float4[] vertex, int2[] eid, float4[] plane, int4[] fid)
        {
            bool isCull = true;

            float3 center = sphere.xyz;
            float radius = sphere.w;

            for (int i = 0; i < 8; i++)
            {
                if (math.distance(center, vertex[i].xyz) <= radius)
                {
                    isCull = false;
                    return isCull;
                }
            }

            for (int i = 0; i < 12; i++)
            {
                float3 v0 = vertex[eid[i].x].xyz;
                float3 v1 = vertex[eid[i].y].xyz;
                float3 d0 = math.normalize(v1 - v0);
                float3 d1 = math.normalize(v0 - v1);

                float3 p0 = center - v0;
                float l0 = math.dot(d0, p0);

                float3 p1 = center - v1;
                float l1 = math.dot(d1, p1);

                float d = math.length(p0 - d0 * l0);

                if (l0 >= 0.0f && l1 >= 0.0f && d <= radius)
                {
                    isCull = false;
                    return isCull;
                }
            }

            unsafe
            {
                float* ts = stackalloc float[6];

                int k = 0;
                for (int i = 0; i < 6; i++)
                {
                    ts[i] = math.dot(plane[i].xyz, center) + plane[i].w;

                    if (ts[i] >= 0.0f)
                    {
                        k++;
                    }
                }

                if (k == 6)
                {
                    isCull = false;
                    return isCull;
                }


                //float xp0 = math.dot(plane[0].xyz, center) + plane[0].w;
                //float xn0 = math.dot(plane[1].xyz, center) + plane[1].w;
                //float yp0 = math.dot(plane[2].xyz, center) + plane[2].w;
                //float yn0 = math.dot(plane[3].xyz, center) + plane[3].w;
                //float zp0 = math.dot(plane[4].xyz, center) + plane[4].w;
                //float zn0 = math.dot(plane[5].xyz, center) + plane[5].w;
                //
                //if (xp0 >= 0.0f && xn0 >= 0.0f && yp0 >= 0.0f && yn0 >= 0.0f && zp0 >= 0.0f && zn0 >= 0.0f)
                //{
                //    isCull = false;
                //    return isCull;
                //}
            }

            unsafe
            {
                {
                    float4* ps = stackalloc float4[6];
                    float3* vs = stackalloc float3[4];
                    float3* es = stackalloc float3[4];
                    float3* ns = stackalloc float3[4];
                    float* ts = stackalloc float[6];

                    for (int i = 0; i < 6; i++)
                    {
                        ps[4] = -plane[i];
                        ps[5] = new float4(plane[i].xyz, plane[i].w + radius);

                        vs[0] = vertex[fid[i].x].xyz;
                        vs[1] = vertex[fid[i].y].xyz;
                        vs[2] = vertex[fid[i].z].xyz;
                        vs[3] = vertex[fid[i].w].xyz;

                        es[0] = vs[1] - vs[0];
                        es[1] = vs[2] - vs[1];
                        es[2] = vs[3] - vs[2];
                        es[3] = vs[0] - vs[3];

                        ns[0] = math.cross(ps[4].xyz, es[0]);
                        ns[1] = math.cross(ps[4].xyz, es[1]);
                        ns[2] = math.cross(ps[4].xyz, es[2]);
                        ns[3] = math.cross(ps[4].xyz, es[3]);

                        ps[0] = new float4(ns[0], -math.dot(ns[0], vs[0]));
                        ps[1] = new float4(ns[1], -math.dot(ns[1], vs[1]));
                        ps[2] = new float4(ns[2], -math.dot(ns[2], vs[2]));
                        ps[3] = new float4(ns[3], -math.dot(ns[3], vs[3]));

                        int k = 0;
                        for (int j = 0; j < 6; j++)
                        {
                            ts[j] = math.dot(ps[j].xyz, center) + ps[j].w;
                            if (ts[j] >= 0.0f)
                            {
                                k++;
                            }
                        }

                        if (k == 6)
                        {
                            isCull = false;
                            return isCull;
                        }
                    }
                }
            }

            return isCull;
        }
    }

    public static class Triangle
    {
        public static Mesh CreateTriangleMesh(float normalAngle, Mesh inMesh = null)
        {
            Mesh outMesh;
            if (inMesh != null)
            {
                outMesh = inMesh;
            }
            else
            {
                outMesh = new Mesh();
            }

            Vector3[] vertices = new Vector3[3];
            int[] indices = new int[3];
            Vector3[] normals = new Vector3[3];

            //Set vertices
            float size = 1.0f;
            vertices[0] = new Vector3(
                0.0f,
                size * Mathf.Sin(60.0f * Mathf.Deg2Rad) * (2.0f / 3.0f),
                0.0f);
            vertices[1] = Quaternion.AngleAxis(120.0f, new Vector3(0.0f, 0.0f, -1.0f)) * vertices[0];
            vertices[2] = Quaternion.AngleAxis(-120.0f, new Vector3(0.0f, 0.0f, -1.0f)) * vertices[0];

            //Set indices
            indices[0] = 0;
            indices[1] = 1;
            indices[2] = 2;

            //Set normals         
            normals[0] = Quaternion.AngleAxis(normalAngle, Vector3.Cross(Vector3.back, vertices[0])) * Vector3.back;
            normals[1] = Quaternion.AngleAxis(normalAngle, Vector3.Cross(Vector3.back, vertices[1])) * Vector3.back;
            normals[2] = Quaternion.AngleAxis(normalAngle, Vector3.Cross(Vector3.back, vertices[2])) * Vector3.back;

            //Applaying vertices indices normals
            List<Vector3> ListVertices = new List<Vector3>();
            foreach (Vector3 temp in vertices)
            {
                ListVertices.Add(temp);
            }

            List<Vector3> ListNormals = new List<Vector3>();
            foreach (Vector3 temp in normals)
            {
                ListNormals.Add(temp);
            }

            outMesh.SetVertices(ListVertices);
            outMesh.SetIndices(indices, MeshTopology.Triangles, 0);
            outMesh.SetNormals(ListNormals);

            return outMesh;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TestTriangleToTriangle(float3x3 triA, float3x3 triB, float4x4 AfromB)
        {
            triB.c0 = math.mul(AfromB, new float4(triB.c0, 1.0f)).xyz;
            triB.c1 = math.mul(AfromB, new float4(triB.c1, 1.0f)).xyz;
            triB.c2 = math.mul(AfromB, new float4(triB.c2, 1.0f)).xyz;

            bool inPlane = false;
            bool bParallel = false;
            bool bIntersected = false;

            bIntersected = TestTriangleToTriangleInPlane(triA, triB, out inPlane, out bParallel);
            if (inPlane)
            {
                if (bIntersected)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (bParallel)
                {
                    return false;
                }
            }

            if (TestTriangleToTriangleOutPlane(triA, triB))
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TestTriangleToTriangleInPlane(float3x3 triA, float3x3 triB, out bool inPlane, out bool bParallel)
        {
            const float epsilon = 0.0001f;
            inPlane = false;
            bParallel = false;
            float3x3 edgeA;
            edgeA.c0 = triA.c1 - triA.c0;
            edgeA.c1 = triA.c2 - triA.c1;
            edgeA.c2 = triA.c0 - triA.c2;
            float3x3 edgeB;
            edgeB.c0 = triB.c1 - triB.c0;
            edgeB.c1 = triB.c2 - triB.c1;
            edgeB.c2 = triB.c0 - triB.c2;

            float3 na = math.cross(edgeA.c0, edgeA.c1);
            float3 nb = math.cross(edgeB.c0, edgeB.c1);

            float3 vec;
            vec.x = math.abs(math.dot(na, triB.c0 - triA.c0));
            vec.y = math.abs(math.dot(na, triB.c1 - triA.c0));
            vec.z = math.abs(math.dot(na, triB.c2 - triA.c0));
            if ((vec.x > epsilon) ||
                (vec.y > epsilon) ||
                (vec.z > epsilon))
            {
                inPlane = false;
                bParallel = false;
                if (math.lengthsq(math.cross(na, nb)) < epsilon)
                {
                    bParallel = true;
                }
                return false;
            }
            inPlane = true;
            bParallel = true;

            //// vertex to triangle Test ////
            float3x3 neA;
            neA.c0 = math.cross(na, edgeA.c0);
            neA.c1 = math.cross(na, edgeA.c1);
            neA.c2 = math.cross(na, edgeA.c2);
            float3x3 neB;
            neB.c0 = math.cross(nb, edgeB.c0);
            neB.c1 = math.cross(nb, edgeB.c1);
            neB.c2 = math.cross(nb, edgeB.c2);

            //point triA and triB
            if (TestPointToTriangleInPlane(triA.c0, triB, neB))
            {
                return true;
            }

            if (TestPointToTriangleInPlane(triA.c1, triB, neB))
            {
                return true;
            }

            if (TestPointToTriangleInPlane(triA.c2, triB, neB))
            {
                return true;
            }
            //point triB and triA
            if (TestPointToTriangleInPlane(triB.c0, triA, neA))
            {
                return true;
            }

            if (TestPointToTriangleInPlane(triB.c1, triA, neA))
            {
                return true;
            }

            if (TestPointToTriangleInPlane(triB.c2, triA, neA))
            {
                return true;
            }

            //// edge to edge Test ////
            float3x2 lineSeg1;
            float3x2 lineSeg2;
            float4 result;

            // triA edge0 and triB
            lineSeg1.c0 = triA.c0;
            lineSeg1.c1 = triA.c1;

            lineSeg2.c0 = triB.c0;
            lineSeg2.c1 = triB.c1;
            if (TestLineSegmentToLineSegment(lineSeg1, lineSeg2, out result))
            {
                return true;
            }

            lineSeg2.c0 = triB.c1;
            lineSeg2.c1 = triB.c2;
            if (TestLineSegmentToLineSegment(lineSeg1, lineSeg2, out result))
            {
                return true;
            }

            lineSeg2.c0 = triB.c2;
            lineSeg2.c1 = triB.c0;
            if (TestLineSegmentToLineSegment(lineSeg1, lineSeg2, out result))
            {
                return true;
            }

            // triA edge1 and triB
            lineSeg1.c0 = triA.c1;
            lineSeg1.c1 = triA.c2;

            lineSeg2.c0 = triB.c0;
            lineSeg2.c1 = triB.c1;
            if (TestLineSegmentToLineSegment(lineSeg1, lineSeg2, out result))
            {
                return true;
            }

            lineSeg2.c0 = triB.c1;
            lineSeg2.c1 = triB.c2;
            if (TestLineSegmentToLineSegment(lineSeg1, lineSeg2, out result))
            {
                return true;
            }

            lineSeg2.c0 = triB.c2;
            lineSeg2.c1 = triB.c0;
            if (TestLineSegmentToLineSegment(lineSeg1, lineSeg2, out result))
            {
                return true;
            }

            // triA edge2 and triB
            lineSeg1.c0 = triA.c2;
            lineSeg1.c1 = triA.c0;

            lineSeg2.c0 = triB.c0;
            lineSeg2.c1 = triB.c1;
            if (TestLineSegmentToLineSegment(lineSeg1, lineSeg2, out result))
            {
                return true;
            }

            lineSeg2.c0 = triB.c1;
            lineSeg2.c1 = triB.c2;
            if (TestLineSegmentToLineSegment(lineSeg1, lineSeg2, out result))
            {
                return true;
            }

            lineSeg2.c0 = triB.c2;
            lineSeg2.c1 = triB.c0;
            if (TestLineSegmentToLineSegment(lineSeg1, lineSeg2, out result))
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TestPointToTriangleInPlane(float3 point, float3x3 triA, float3x3 neA)
        {
            if (
                (math.dot(neA.c0, point - triA.c0)) >= 0.0f &&
                (math.dot(neA.c1, point - triA.c1)) >= 0.0f &&
                (math.dot(neA.c2, point - triA.c2)) >= 0.0f)
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TestLineSegmentToLineSegment(float3x2 lineSeg1, float3x2 lineSeg2, out float4 result)
        {
            result = new float4(-1.0f);
            float s = 0.0f;
            float t = 0.0f;
            float k = 0.0f;
            float3 ls = new float3(0.0f);
            float3 lt = new float3(0.0f);
            float3 d1 = lineSeg1.c1 - lineSeg1.c0;
            float3 d2 = lineSeg2.c1 - lineSeg2.c0;
            float d1d1 = math.dot(d1, d1);
            float d2d2 = math.dot(d2, d2);
            float d1d2 = math.dot(d1, d2);
            float3 p1 = lineSeg1.c0 - lineSeg2.c0;
            float3 p2 = lineSeg1.c1 - lineSeg2.c0;
            float3 q1 = lineSeg2.c0 - lineSeg1.c0;
            float3 q2 = lineSeg2.c1 - lineSeg1.c0;
            float p1d1 = math.dot(p1, d1);
            float p1d2 = math.dot(p1, d2);

            float d1xd2 = d1d1 * d2d2 - d1d2 * d1d2;
            if (math.abs(d1xd2) < 0.001f &&
                math.lengthsq(math.cross(p1, d1)) < 0.001f)
            {
                float3 d1_d1d1 = d1 / d1d1;
                float3 d2_d2d2 = d2 / d2d2;
                float s1 = math.dot(q1, d1_d1d1); result.x = s1;
                float s2 = math.dot(q2, d1_d1d1); result.y = s2;
                float t1 = math.dot(p1, d2_d2d2); result.z = t1;
                float t2 = math.dot(p2, d2_d2d2); result.w = t2;
                if (
                    (s1 >= 0.0f && s1 <= 1.0f) ||
                    (s2 >= 0.0f && s2 <= 1.0f) ||
                    (t1 >= 0.0f && t1 <= 1.0f) ||
                    (t2 >= 0.0f && t2 <= 1.0f))
                {
                    return true;
                }
            }

            float2x2 m = new float2x2(0.0f);
            m.c0.x = +d1d1; // +math.dot(lineSeg1.c1 - lineSeg1.c0, lineSeg1.c1 - lineSeg1.c0);
            m.c0.y = +d1d2; // +math.dot(lineSeg1.c1 - lineSeg1.c0, lineSeg2.c1 - lineSeg2.c0);
            m.c1.x = -d1d2; // -math.dot(lineSeg2.c1 - lineSeg2.c0, lineSeg1.c1 - lineSeg1.c0);
            m.c1.y = -d2d2; // -math.dot(lineSeg2.c1 - lineSeg2.c0, lineSeg2.c1 - lineSeg2.c0);
            float2 v = new float2(0.0f);
            v.x = -p1d1; // -math.dot(lineSeg1.c1 - lineSeg1.c0, lineSeg1.c0 - lineSeg2.c0);
            v.y = -p1d2; // -math.dot(lineSeg2.c1 - lineSeg2.c0, lineSeg1.c0 - lineSeg2.c0);

            float det = -d1xd2; // m.c0.x * m.c1.y - m.c1.x * m.c0.y;

            s = (+m.c1.y * v.x - m.c1.x * v.y) / det;
            t = (-m.c0.y * v.x + m.c0.x * v.y) / det;
            ls = lineSeg1.c0 + s * d1; // (lineSeg1.c1 - lineSeg1.c0);
            lt = lineSeg2.c0 + t * d2; // (lineSeg2.c1 - lineSeg2.c0);
            k = math.dot(ls - lt, ls - lt);
            if (
                (s >= 0.0f && s <= 1.0f) &&
                (t >= 0.0f && t <= 1.0f) &&
                (k >= 0.0f && k <= 0.0001f))
            {
                result.x = s;
                result.z = t;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TestTriangleToTriangleOutPlane(float3x3 triA, float3x3 triB)
        {
            float3 result = new float3(0.0f);
            float3x2 lineSeg;

            //lineSegA_triB
            lineSeg.c0 = triA.c0;
            lineSeg.c1 = triA.c1;
            if (TestLineSegmentToTriangle(lineSeg, triB, out result))
            {
                return true;
            }

            lineSeg.c0 = triA.c1;
            lineSeg.c1 = triA.c2;
            if (TestLineSegmentToTriangle(lineSeg, triB, out result))
            {
                return true;
            }

            lineSeg.c0 = triA.c2;
            lineSeg.c1 = triA.c0;
            if (TestLineSegmentToTriangle(lineSeg, triB, out result))
            {
                return true;
            }

            //triA_lineSegB
            lineSeg.c0 = triB.c0;
            lineSeg.c1 = triB.c1;
            if (TestLineSegmentToTriangle(lineSeg, triA, out result))
            {
                return true;
            }

            lineSeg.c0 = triB.c1;
            lineSeg.c1 = triB.c2;
            if (TestLineSegmentToTriangle(lineSeg, triA, out result))
            {
                return true;
            }

            lineSeg.c0 = triB.c2;
            lineSeg.c1 = triB.c0;
            if (TestLineSegmentToTriangle(lineSeg, triA, out result))
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TestLineSegmentToTriangle(float3x2 lineSeg, float3x3 triangle, out float3 result)
        {
            float3 r = new float3(0.0f);
            float3x3 M = new float3x3(0.0f);
            M.c0 = lineSeg.c0 - lineSeg.c1;
            M.c1 = triangle.c1 - triangle.c0;
            M.c2 = triangle.c2 - triangle.c0;
            float3 v = lineSeg.c0 - triangle.c0;

            //optimazed
            float3 area1 = math.cross(M.c1, M.c2);
            float3 area2 = math.cross(M.c0, v);
            float det = math.dot(M.c0, area1);
            r.x = math.dot(v, area1) / det;
            r.y = math.dot(M.c2, area2) / det;
            r.z = math.dot(M.c1, -area2) / det;

            //non-optimazed
            /*float det = math.dot(M.c0, math.cross(M.c1, M.c2)) + 0.0001f;
            r.x = math.dot(v, math.cross(M.c1, M.c2)) / det;
            r.y = math.dot(v, math.cross(M.c2, M.c0)) / det;
            r.z = math.dot(v, math.cross(M.c0, M.c1)) / det;*/
            result = r;
            if ((r.x >= 0.0f && r.x <= 1.0f) &&
                (r.y >= 0.0f && r.y <= 1.0f) &&
                (r.z >= 0.0f && r.z <= 1.0f) &&
                (r.y + r.z >= 0.0f && r.y + r.z <= 1.0f))
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TestTriangleToTriangleSimple(float3x3 triA, float3x3 triB, float4x4 AfromB)
        {
            triB.c0 = math.mul(AfromB, new float4(triB.c0, 1.0f)).xyz;
            triB.c1 = math.mul(AfromB, new float4(triB.c1, 1.0f)).xyz;
            triB.c2 = math.mul(AfromB, new float4(triB.c2, 1.0f)).xyz;

            if (TestTriangleToTriangleOutPlane(triA, triB))
            {
                return true;
            }

            return false;
        }

    }

    //HexaHedron_Collider
    public class HHCollider
    {
        static int[] sIndices;
        static float3[] sPos;
        static float3[] sNom;
        static float3 sCenter;
        static float3x2[] sPlanes;

        public int[] indices;
        public float3[] pos;
        public float3[] nom;
        public float3 center;
        public float3x2[] planes;
        public float4x4 W;
        public float4x4 M;
        public bool bCulled = false;

        static HHCollider()
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh boxMesh = box.GetComponent<MeshFilter>().mesh;
            GameObject.DestroyImmediate(box);

            HHCollider.sIndices = new int[36];
            int[] indices = boxMesh.GetIndices(0);
            for (int i = 0; i < 36; i++)
            {
                HHCollider.sIndices[i] = indices[i];
            }

            HHCollider.sPos = new float3[24];
            List<Vector3> vertices = new List<Vector3>();
            boxMesh.GetVertices(vertices);
            for (int i = 0; i < 24; i++)
            {
                HHCollider.sPos[i] = vertices[i];
            }

            HHCollider.sNom = new float3[24];
            List<Vector3> normals = new List<Vector3>();
            boxMesh.GetNormals(normals);
            for (int i = 0; i < 24; i++)
            {
                HHCollider.sNom[i] = normals[i];
            }

            HHCollider.sCenter = new float3(0.0f);

            HHCollider.sPlanes = new float3x2[6];
            HHCollider.sPlanes[0].c0 = new float3(+1.0f, +0.0f, +0.0f);
            HHCollider.sPlanes[0].c1 = new float3(+0.5f, +0.0f, +0.0f);
            HHCollider.sPlanes[1].c0 = new float3(-1.0f, +0.0f, +0.0f);
            HHCollider.sPlanes[1].c1 = new float3(-0.5f, +0.0f, +0.0f);
            HHCollider.sPlanes[2].c0 = new float3(+0.0f, +1.0f, +0.0f);
            HHCollider.sPlanes[2].c1 = new float3(+0.0f, +0.5f, +0.0f);
            HHCollider.sPlanes[3].c0 = new float3(+0.0f, -1.0f, +0.0f);
            HHCollider.sPlanes[3].c1 = new float3(+0.0f, -0.5f, +0.0f);
            HHCollider.sPlanes[4].c0 = new float3(+0.0f, +0.0f, +1.0f);
            HHCollider.sPlanes[4].c1 = new float3(+0.0f, +0.0f, +0.5f);
            HHCollider.sPlanes[5].c0 = new float3(+0.0f, +0.0f, -1.0f);
            HHCollider.sPlanes[5].c1 = new float3(+0.0f, +0.0f, -0.5f);
        }

        public HHCollider()
        {
            this.indices = new int[36];
            this.pos = new float3[24];
            this.nom = new float3[24];
            this.center = new float3(0.0f);
            this.planes = new float3x2[6];
        }

        public HHCollider(float4x4 W)
        {
            this.indices = new int[36];
            this.pos = new float3[24];
            this.center = new float3(0.0f);
            this.planes = new float3x2[6];
            this.W = W;
        }

        public HHCollider(float3 t, quaternion r, float3 s)
        {
            this.indices = new int[36];
            this.pos = new float3[24];
            this.center = new float3(0.0f);
            this.planes = new float3x2[6];

            this.M.c0 = new float4(t, 0.0f);
            this.M.c1 = new float4(r.value.x, r.value.y, r.value.z, r.value.w);
            this.M.c2 = new float4(s, 0.0f);
        }

        public void InitBox()
        {
            this.indices = HHCollider.sIndices;

            for (int i = 0; i < 24; i++)
            {
                this.pos[i] = HHCollider.sPos[i];
                this.nom[i] = HHCollider.sNom[i];
            }

            this.center = HHCollider.sCenter;

            for (int i = 0; i < 6; i++)
            {
                this.planes[i] = HHCollider.sPlanes[i];
            }

        }

        public void InitBox(float4x4 W)
        {
            this.InitBox();
            this.W = W;
        }

        public void InitBox(float3 t, quaternion r, float3 s)
        {
            this.InitBox();

            this.M.c0 = new float4(t, 0.0f);
            this.M.c1 = new float4(r.value.x, r.value.y, r.value.z, r.value.w);
            this.M.c2 = new float4(s, 0.0f);
        }

        public void UpdateBox(float3 t, quaternion r, float3 s)
        {
            this.M.c0 = new float4(t, 0.0f);
            this.M.c1 = new float4(r.value.x, r.value.y, r.value.z, r.value.w);
            this.M.c2 = new float4(s, 0.0f);
        }

        public void InitPVF(float4 frustumInfo)
        {
            ///index
            this.indices = HHCollider.sIndices;

            frustumInfo.x = math.clamp(frustumInfo.x, 10.0f, 170.0f);
            frustumInfo.y = math.clamp(frustumInfo.y, 0.2f, 5.0f);
            frustumInfo.z = math.clamp(frustumInfo.z, 0.2f, 50.0f);
            frustumInfo.w = math.clamp(frustumInfo.w, frustumInfo.z + 1.0f, 300.0f);
            //frustumInfo.w = math.clamp(frustumInfo.w, frustumInfo.z + 1.0f, 100.0f);


            float fov = frustumInfo.x;
            float aspect = frustumInfo.y;
            float near = frustumInfo.z;
            float far = frustumInfo.w;

            float4x4 m = new float4x4(0.0f);
            m.c1.y = math.tan(math.radians(fov / 2.0f)); //  Mathf.Tan(fov / 2.0f * Mathf.Deg2Rad);
            m.c0.x = aspect * m.c1.y;
            m.c2.z = 0.0f;
            m.c3.z = 1.0f;
            m.c2.w = -(far - near) / (2 * far * near);
            m.c3.w = +(far + near) / (2 * far * near);

            ///pos
            for (int i = 0; i < 24; i++)
            {
                float3 v3 = 2.0f * HHCollider.sPos[i];
                float4 v4 = new float4(v3, 1.0f);
                v4 = math.mul(m, v4);
                v3 = ((1.0f / v4.w) * v4).xyz;
                this.pos[i] = v3;
            }

            ///center
            //float xn = m.c0.x * near;
            //float yn = m.c1.x * near;
            //float xf = m.c0.x * far;
            //float yf = m.c1.x * far;

            float xn = m.c0.x * near;
            float yn = m.c1.y * near;
            float xf = m.c0.x * far;
            float yf = m.c1.y * far;
            this.center = (1.0f / 8.0f) * (
               new float3(+xn, +yn, near) +
               new float3(-xn, +yn, near) +
               new float3(+xn, -yn, near) +
               new float3(-xn, -yn, near) +
               new float3(+xf, +yf, far) +
               new float3(-xf, +yf, far) +
               new float3(+xf, -yf, far) +
               new float3(-xf, -yf, far));

            ///plane
            float3 nr = new float3(+1.0f, +0.0f, -m.c0.x);
            float3 nl = new float3(-1.0f, +0.0f, -m.c0.x);
            float3 nu = new float3(+0.0f, +1.0f, -m.c1.y);
            float3 nd = new float3(+0.0f, -1.0f, -m.c1.y);

            this.planes[0].c0 = nr;
            this.planes[0].c1 = new float3(0.0f);
            this.planes[1].c0 = nl;
            this.planes[1].c1 = new float3(0.0f);
            this.planes[2].c0 = nu;
            this.planes[2].c1 = new float3(0.0f);
            this.planes[3].c0 = nd;
            this.planes[3].c1 = new float3(0.0f);
            this.planes[4].c0 = new float3(0.0f, 0.0f, +1.0f);
            this.planes[4].c1 = new float3(0.0f, 0.0f, far);
            this.planes[5].c0 = new float3(0.0f, 0.0f, -1.0f);
            this.planes[5].c1 = new float3(0.0f, 0.0f, near);
        }

        public void InitPVF(float4 frustumInfo, float4x4 W)
        {
            this.InitPVF(frustumInfo);
            this.W = W;
        }

        public void InitPVF(float4 frustumInfo, float3 t, quaternion r, float3 s)
        {
            this.InitPVF(frustumInfo);

            this.M.c0 = new float4(t, 0.0f);
            this.M.c1 = new float4(r.value.x, r.value.y, r.value.z, r.value.w);
            this.M.c2 = new float4(s, 0.0f);
        }

        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TestHCtoHC(HHCollider A, HHCollider B, float4x4 AfromB, float4x4 AfromBnormal)
        {
            if (HHCollider.TestCentersToBoundingPlanes(
                A.center, B.center, AfromB,
                A.planes, B.planes, AfromBnormal))
            {
                return true;
            }
            else if (HHCollider.TestMeshToMesh(
                A.pos, B.pos,
                A.indices, B.indices, AfromB))
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TestCentersToBoundingPlanes(
                        float3 centerA, float3 centerB, float4x4 AfromB,
                        float3x2[] planesA, float3x2[] planesB, float4x4 AfromBnormal)
        {
            int a = 0;
            int b = 0;
            int numPlaneA = planesA.GetLength(0);
            int numPlaneB = planesB.GetLength(0);

            float3 center = math.mul(AfromB, new float4(centerB, 1.0f)).xyz;
            for (int i = 0; i < numPlaneA; i++)
            {
                if (HHCollider.TestPointToPlaneInOut(center, planesA[i]))
                {
                    a = a + 1;
                }
                else
                {
                    break;
                }

            }

            for (int i = 0; i < numPlaneB; i++)
            {
                float3x2 plane;
                plane.c0 = math.mul(AfromBnormal, new float4(planesB[i].c0, 0.0f)).xyz;
                plane.c1 = math.mul(AfromB, new float4(planesB[i].c1, 1.0f)).xyz;
                if (HHCollider.TestPointToPlaneInOut(centerA, plane))
                {
                    b = b + 1;
                }
                else
                {
                    break;
                }
            }

            if ((a == numPlaneA) || (b == numPlaneB))
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TestPointToPlaneInOut(float3 point, float3x2 plane)
        {
            if (math.dot(plane.c0, point - plane.c1) <= 0.0f)
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TestMeshToMesh(
            float3[] posA, float3[] posB,
            int[] indiA, int[] indiB,
            float4x4 AfromB)
        {
            float3x3 triA;
            float3x3 triB;

            for (int i = 0; i < indiA.GetLength(0); i = i + 3)
            {
                for (int j = 0; j < indiB.GetLength(0); j = j + 3)
                {
                    triA.c0 = posA[indiA[i + 0]];
                    triA.c1 = posA[indiA[i + 1]];
                    triA.c2 = posA[indiA[i + 2]];

                    triB.c0 = posB[indiB[j + 0]];
                    triB.c1 = posB[indiB[j + 1]];
                    triB.c2 = posB[indiB[j + 2]];

                    if (Triangle.TestTriangleToTriangleSimple(triA, triB, AfromB))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    public class BoxWire
    {
        public static int[] sIndices;
        public static float3[] sPos;
        public static int vtxCount;
        public static int idxCount;
        public static MeshTopology type;
        public static Mesh mesh;

        static BoxWire()
        {
            mesh = RenderUtil.CreateCubeMeshWire();

            List<Vector3> vertices = new List<Vector3>();
            mesh.GetVertices(vertices);
            sPos = new float3[mesh.vertexCount];
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                sPos[i] = vertices[i];
            }

            sIndices = mesh.GetIndices(0);

            vtxCount = mesh.vertexCount;
            idxCount = sIndices.Length;

            type = mesh.GetTopology(0);
        }
    }

    public static class TransformUtil
    {
        public static void GetComponentInChild<T>(Transform tr, List<Transform> list) where T : Component
        {
            T component = tr.GetComponent<T>();
            if (component != null)
            {
                list.Add(tr);
            }

            for (int i = 0; i < tr.childCount; i++)
            {
                Transform child = tr.GetChild(i);
                GetComponentInChild<T>(child, list);
            }
        }

        public static void GetComponentInChild<T>(Transform tr, List<T> list) where T : Component
        {
            T component = tr.GetComponent<T>();
            if (component != null)
            {
                list.Add(component);
            }

            for (int i = 0; i < tr.childCount; i++)
            {
                Transform child = tr.GetChild(i);
                GetComponentInChild<T>(child, list);
            }
        }

        public static List<T> GetComponentInChild<T>(Transform root) where T : Component
        {
            List<T> list = new List<T>();
            GetComponentInChild<T>(root, list);

            return list;
        }
    }

    public static class UserTimer
    {
        static UserTimer()
        {

        }

        public static void ReSet()
        {


        }

        public static void Start()
        {
            ct = DateTime.Now;
            pt = ct;
        }

        public static void Update()
        {
            ct = DateTime.Now;
            dt = (ct - pt).Milliseconds / 1000.0f;
            pt = ct;
        }

        static DateTime ct;
        static DateTime pt;
        public static float dt;
    }

    public static class CollisionUtil
    {
        public static bool TestOBBtoOBB(float4x4 A, float4x4 B)
        {
            const float epsilon = 0.001f;

            float3 ta = new float3(A.c3.x, A.c3.y, A.c3.z);
            float3 tb = new float3(B.c3.x, B.c3.y, B.c3.z);

            float3 col0 = float3.zero; float3 col1 = float3.zero; float3 col2 = float3.zero;
            col0 = new float3(A.c0.x, A.c0.y, A.c0.z);
            col1 = new float3(A.c1.x, A.c1.y, A.c1.z);
            col2 = new float3(A.c2.x, A.c2.y, A.c2.z);
            float3x3 Ra = new float3x3(col0, col1, col2);
            col0 = new float3(B.c0.x, B.c0.y, B.c0.z);
            col1 = new float3(B.c1.x, B.c1.y, B.c1.z);
            col2 = new float3(B.c2.x, B.c2.y, B.c2.z);
            float3x3 Rb = new float3x3(col0, col1, col2);

            float3 sa = math.abs(
                0.5f * new float3(math.length(Ra.c0), math.length(Ra.c1), math.length(Ra.c2)));
            float3 sb = math.abs(
                0.5f * new float3(math.length(Rb.c0), math.length(Rb.c1), math.length(Rb.c2)));

            float3x3 R = math.mul(math.transpose(Ra), Rb);
            float3x3 absR = new float3x3(math.abs(R.c0), math.abs(R.c1), math.abs(R.c2)) + new float3x3(epsilon);
            float3 c = math.mul(Ra, tb - ta);

            float ra = 0.0f;
            float rb = 0.0f;
            float rc = 0.0f;
            //Ra
            rc = math.abs(c.x);
            ra = sa.x;
            rb = math.mul(absR.c0.x, sb.x) + math.mul(absR.c1.x, sb.y) + math.mul(absR.c2.x, sb.z);
            if (rc > ra + rb) return false;

            rc = math.abs(c.y);
            ra = sa.y;
            rb = math.mul(absR.c0.y, sb.x) + math.mul(absR.c1.y, sb.y) + math.mul(absR.c2.y, sb.z);
            if (rc > ra + rb) return false;

            rc = math.abs(c.z);
            ra = sa.z;
            rb = math.mul(absR.c0.z, sb.x) + math.mul(absR.c1.z, sb.y) + math.mul(absR.c2.z, sb.z);
            if (rc > ra + rb) return false;
            //Rb
            rc = math.abs(math.mul(c.x, R.c0.x) + math.mul(c.y, R.c0.y) + math.mul(c.z, R.c0.z));
            ra = math.mul(sa.x, absR.c0.x) + math.mul(sa.y, absR.c0.y) + math.mul(sa.z, absR.c0.z);
            rb = sb.x;
            if (rc > ra + rb) return false;

            rc = math.abs(math.mul(c.x, R.c1.x) + math.mul(c.y, R.c1.y) + math.mul(c.z, R.c1.z));
            ra = math.mul(sa.x, absR.c1.x) + math.mul(sa.y, absR.c1.y) + math.mul(sa.z, absR.c1.z);
            rb = sb.y;
            if (rc > ra + rb) return false;

            rc = math.abs(math.mul(c.x, R.c2.x) + math.mul(c.y, R.c2.y) + math.mul(c.z, R.c2.z));
            ra = math.mul(sa.x, absR.c2.x) + math.mul(sa.y, absR.c2.y) + math.mul(sa.z, absR.c2.z);
            rb = sb.z;
            if (rc > ra + rb) return false;
            //Ra0 x Rb
            rc = math.abs(math.mul(c.y, -R.c0.z) + math.mul(c.z, R.c0.y));
            ra = math.mul(sa.y, absR.c0.z) + math.mul(sa.z, absR.c0.y);
            rb = math.mul(absR.c2.x, sb.y) + math.mul(absR.c1.x, sb.z);
            if (rc > ra + rb) return false;

            rc = math.abs(math.mul(c.y, -R.c1.z) + math.mul(c.z, R.c1.y));
            ra = math.mul(sa.y, absR.c1.z) + math.mul(sa.z, absR.c1.y);
            rb = math.mul(absR.c2.x, sb.x) + math.mul(absR.c0.x, sb.z);
            if (rc > ra + rb) return false;

            rc = math.abs(math.mul(c.y, -R.c2.z) + math.mul(c.z, R.c2.y));
            ra = math.mul(sa.y, absR.c2.z) + math.mul(sa.z, absR.c2.y);
            rb = math.mul(absR.c1.x, sb.x) + math.mul(absR.c0.x, sb.y);
            if (rc > ra + rb) return false;
            //Ra1 x Rb
            rc = math.abs(math.mul(c.x, R.c0.z) + math.mul(c.z, -R.c0.x));
            ra = math.mul(sa.x, absR.c0.z) + math.mul(sa.z, absR.c0.x);
            rb = math.mul(absR.c2.y, sb.y) + math.mul(absR.c1.y, sb.z);
            if (rc > ra + rb) return false;

            rc = math.abs(math.mul(c.x, R.c1.z) + math.mul(c.z, -R.c1.x));
            ra = math.mul(sa.x, absR.c1.z) + math.mul(sa.z, absR.c1.x);
            rb = math.mul(absR.c2.y, sb.x) + math.mul(absR.c0.y, sb.z);
            if (rc > ra + rb) return false;

            rc = math.abs(math.mul(c.x, R.c2.z) + math.mul(c.z, -R.c2.x));
            ra = math.mul(sa.x, absR.c2.z) + math.mul(sa.z, absR.c2.x);
            rb = math.mul(absR.c1.y, sb.x) + math.mul(absR.c0.y, sb.y);
            if (rc > ra + rb) return false;
            //Ra2 x Rb
            rc = math.abs(math.mul(c.x, -R.c0.y) + math.mul(c.y, R.c0.x));
            ra = math.mul(sa.x, absR.c0.y) + math.mul(sa.y, absR.c0.x);
            rb = math.mul(absR.c2.z, sb.y) + math.mul(absR.c1.z, sb.z);
            if (rc > ra + rb) return false;

            rc = math.abs(math.mul(c.x, -R.c1.y) + math.mul(c.y, R.c1.x));
            ra = math.mul(sa.x, absR.c1.y) + math.mul(sa.y, absR.c1.x);
            rb = math.mul(absR.c2.z, sb.x) + math.mul(absR.c0.z, sb.z);
            if (rc > ra + rb) return false;

            rc = math.abs(math.mul(c.x, -R.c2.y) + math.mul(c.y, R.c2.x));
            ra = math.mul(sa.x, absR.c2.y) + math.mul(sa.y, absR.c2.x);
            rb = math.mul(absR.c1.z, sb.x) + math.mul(absR.c0.z, sb.y);
            if (rc > ra + rb) return false;

            return true;
        }
    }

    [System.Serializable]
    public struct DualQuaternion
    {
        public float4 real;
        public float4 dual;

        public DualQuaternion(quaternion real, quaternion dual)
        {
            this.real = real.value;
            this.dual = dual.value;
        }

        public DualQuaternion(quaternion r, float3 t)
        {
            this.real = float4.zero;
            this.dual = float4.zero;
            fromRigidParam(r.value, t);
        }

        public DualQuaternion(float3x3 R, float3 t)
        {
            this.real = float4.zero;
            this.dual = float4.zero;
            fromRigidParam(R, t);
        }

        public DualQuaternion(float4x4 M)
        {
            this.real = float4.zero;
            this.dual = float4.zero;
            fromRigidParam(M);
        }

        public DualQuaternion(float o, float d, float3 l, float3 m)
        {
            this.real = float4.zero;
            this.dual = float4.zero;
            fromScrewParam(o, d, l, m);
        }

        public bool bUnit
        {
            get
            {
                if (math.dot(real, real) == 1.0f && math.dot(real, dual) == 0.0f)
                { return true; }
                else
                { return false; }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void toRigidParam(out float4 r, out float3 t)
        {
            r = float4.zero;
            t = float3.zero;

            {
                r = real;
                float4 _real = new float4(-real.xyz, +real.w);
                t = 2.0f * new float3(math.cross(dual.xyz, _real.xyz) + _real.xyz * dual.w + dual.xyz * _real.w);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void toRigidParam(out float3x3 R, out float3 t)
        {
            R = float3x3.zero;
            t = float3.zero;

            //if(isUnit)
            {
                float4 r = float4.zero;
                toRigidParam(out r, out t);
                R = new float3x3(new quaternion(r));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void toRigidParam(out float4x4 M)
        {
            float3x3 R = float3x3.zero;
            float3 t = float3.zero;
            M = float4x4.zero;

            {
                toRigidParam(out R, out t);
                M.c0 = new float4(R.c0, 0.0f);
                M.c1 = new float4(R.c1, 0.0f);
                M.c2 = new float4(R.c2, 0.0f);
                M.c3 = new float4(t, 1.0f);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void fromRigidParam(float4 r, float3 t)
        {
            {
                real = r;
                dual = 0.5f * math.mul(new quaternion(new float4(t, 0.0f)), new quaternion(r)).value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void fromRigidParam(float3x3 R, float3 t)
        {
            {
                //float4 r = quaternion.LookRotation(R.c2, R.c1).value;
                float4 r = new quaternion(R).value;
                fromRigidParam(r, t);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void fromRigidParam(float4x4 M)
        {
            {
                float3x3 R = float3x3.zero;
                float3 t = float3.zero;
                R.c0 = M.c0.xyz;
                R.c1 = M.c1.xyz;
                R.c2 = M.c2.xyz;
                t = M.c3.xyz;
                fromRigidParam(R, t);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void toScrewParam(out float o, out float d, out float3 l, out float3 m)
        {
            o = 0.0f;
            d = 0.0f;
            l = float3.zero;
            m = float3.zero;

            {
                float i_s = 1.0f / math.length(real.xyz);

                o = +2.0f * math.acos(real.w);
                d = -2.0f * dual.w * i_s;
                l = real.xyz * i_s;
                m = (dual.xyz - l * d * real.w * 0.5f) * i_s;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void fromScrewParam(float o, float d, float3 l, float3 m)
        {
            float o_2 = 0.5f * o;
            float d_2 = 0.5f * d;
            float sin = 0.0f;
            float cos = 0.0f;

            {
                math.sincos(o_2, out sin, out cos);

                real.xyz = sin * l;
                real.w = cos;

                dual.xyz = sin * m + d_2 * cos * l;
                dual.w = -d_2 * sin;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DualQuaternion scLerp(DualQuaternion a, DualQuaternion b, float t)
        {
            DualQuaternion q = new DualQuaternion();
            DualQuaternion dq = new DualQuaternion();
            float o = 0.0f; float d = 0.0f; float3 l = float3.zero; float3 m = float3.zero;

            dq = ~a * (math.dot(a.real, b.real) >= 0.0f ? b : (-1.0f) * b);

            dq.toScrewParam(out o, out d, out l, out m);
            o = o * t;
            d = d * t;
            dq.fromScrewParam(o, d, l, m);

            q = a * dq;

            return q;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DualQuaternion scLerp_debug(DualQuaternion a, DualQuaternion b, float t, out float3 rp, out float3 rb, out float3 rl)
        {
            DualQuaternion q = new DualQuaternion();
            DualQuaternion dq = new DualQuaternion();
            float o = 0.0f; float d = 0.0f; float3 l = float3.zero; float3 m = float3.zero;

            dq = ~a * (math.dot(a.real, b.real) >= 0.0f ? b : (-1.0f) * b);

            dq.toScrewParam(out o, out d, out l, out m);
            o = o * t;
            d = d * t;
            dq.fromScrewParam(o, d, l, m);

            q = a * dq;

            //Debug
            float4 rot;
            float3 tb;
            float3 p;
            p = math.cross(l, m);
            dq.toRigidParam(out rot, out tb);

            rp = a * p;
            rb = a * (tb - d * l);
            rl = a * (p + d * l);

            return q;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DualQuaternion operator ~(DualQuaternion q)
        {
            DualQuaternion p = new DualQuaternion();
            p.real = new float4(-q.real.xyz, +q.real.w);
            p.dual = new float4(-q.dual.xyz, +q.dual.w);
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DualQuaternion operator !(DualQuaternion q)
        {
            DualQuaternion p = new DualQuaternion();
            p.real = new float4(-q.real.xyz, +q.real.w);
            p.dual = new float4(+q.dual.xyz, -q.dual.w);
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DualQuaternion operator *(DualQuaternion a, DualQuaternion b)
        {
            DualQuaternion p = new DualQuaternion();

            p.real =
                math.mul(new quaternion(a.real), new quaternion(b.real)).value;
            p.dual =
                math.mul(new quaternion(a.real), new quaternion(b.dual)).value +
                math.mul(new quaternion(a.dual), new quaternion(b.real)).value;
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 operator *(DualQuaternion q, float3 v)
        {
            {
                //DualQuaternion p = new DualQuaternion();
                //p.real = new float4(0.0f, 0.0f, 0.0f, 1.0f);
                //p.dual = new float4(v, 0.0f);            
                //v = (q * p * (!q)).dual.xyz;
            }

            {
                float4 r;
                float3 t;
                q.toRigidParam(out r, out t);
                v = math.rotate(r, v) + t;
            }

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DualQuaternion operator *(float s, DualQuaternion a)
        {
            DualQuaternion p = new DualQuaternion();

            p.real = s * a.real;
            p.dual = s * a.dual;
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DualQuaternion operator *(DualQuaternion a, float s)
        {
            DualQuaternion p = new DualQuaternion();

            p.real = a.real * s;
            p.dual = a.dual * s;
            return p;
        }
    }

    [System.Serializable]
    public struct dualquaternion
    {
        public float4 real;
        public float4 dual;

        public dualquaternion(quaternion real, quaternion dual)
        {
            this.real = real.value;
            this.dual = dual.value;
        }

        public dualquaternion(quaternion r, float3 t)
        {
            this.real = float4.zero;
            this.dual = float4.zero;
            fromRigidParam(r.value, t);
        }

        public dualquaternion(float3x3 R, float3 t)
        {
            this.real = float4.zero;
            this.dual = float4.zero;
            fromRigidParam(R, t);
        }

        public dualquaternion(float4x4 M)
        {
            this.real = float4.zero;
            this.dual = float4.zero;
            fromRigidParam(M);
        }

        public dualquaternion(float o, float d, float3 l, float3 m)
        {
            this.real = float4.zero;
            this.dual = float4.zero;
            fromScrewParam(o, d, l, m);
        }

        public static dualquaternion identity
        {
            get { return new dualquaternion(new quaternion(0.0f, 0.0f, 0.0f, 1.0f), new quaternion(0.0f, 0.0f, 0.0f, 0.0f)); }
        }

        public bool bUnit
        {
            get
            {
                if (math.dot(real, real) == 1.0f && math.dot(real, dual) == 0.0f)
                { return true; }
                else
                { return false; }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void toRigidParam(out float4 r, out float3 t)
        {
            r = float4.zero;
            t = float3.zero;

            {
                r = real;
                float4 _real = new float4(-real.xyz, +real.w);
                t = 2.0f * new float3(math.cross(dual.xyz, _real.xyz) + _real.xyz * dual.w + dual.xyz * _real.w);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void toRigidParam(out float3x3 R, out float3 t)
        {
            R = float3x3.zero;
            t = float3.zero;

            //if(isUnit)
            {
                float4 r = float4.zero;
                toRigidParam(out r, out t);
                R = new float3x3(new quaternion(r));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void toRigidParam(out float4x4 M)
        {
            float3x3 R = float3x3.zero;
            float3 t = float3.zero;
            M = float4x4.zero;

            {
                toRigidParam(out R, out t);
                M.c0 = new float4(R.c0, 0.0f);
                M.c1 = new float4(R.c1, 0.0f);
                M.c2 = new float4(R.c2, 0.0f);
                M.c3 = new float4(t, 1.0f);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4x4 toMat()
        {
            float4x4 M;
            toRigidParam(out M);
            return M;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void fromRigidParam(float4 r, float3 t)
        {
            {
                real = r;
                dual = 0.5f * math.mul(new quaternion(new float4(t, 0.0f)), new quaternion(r)).value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void fromRigidParam(float3x3 R, float3 t)
        {
            {
                //float4 r = quaternion.LookRotation(R.c2, R.c1).value;
                float4 r = new quaternion(R).value;
                fromRigidParam(r, t);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void fromRigidParam(float4x4 M)
        {
            {
                float3x3 R = float3x3.zero;
                float3 t = float3.zero;
                R.c0 = M.c0.xyz;
                R.c1 = M.c1.xyz;
                R.c2 = M.c2.xyz;
                t = M.c3.xyz;
                fromRigidParam(R, t);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void toScrewParam(out float o, out float d, out float3 l, out float3 m)
        {
            o = 0.0f;
            d = 0.0f;
            l = float3.zero;
            m = float3.zero;

            {
                //float i_s = 1.0f / math.length(real.xyz);
                float i_s = math.rsqrt(1.0f - real.w * real.w);

                o = +2.0f * math.acos(real.w);
                d = -2.0f * dual.w * i_s;
                l = real.xyz * i_s;
                m = (dual.xyz - l * d * real.w * 0.5f) * i_s;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void fromScrewParam(float o, float d, float3 l, float3 m)
        {
            float o_2 = 0.5f * o;
            float d_2 = 0.5f * d;
            float sin = 0.0f;
            float cos = 0.0f;

            {
                math.sincos(o_2, out sin, out cos);

                real.xyz = sin * l;
                real.w = cos;

                dual.xyz = sin * m + d_2 * cos * l;
                dual.w = -d_2 * sin;
            }
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static dualquaternion scLerp(dualquaternion a, dualquaternion b, float t)
        {
            dualquaternion q;
            //q = scLerpTest(a, b, t);
            q = scLerp3(a, b, t);
            //q = scLerp1(a, b, t);

            return q;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static dualquaternion scLerp1(dualquaternion a, dualquaternion b, float t)
        {
            dualquaternion q = new dualquaternion();
            dualquaternion dq = new dualquaternion();

            if (dualquaternion.dot(a, b) < 0.998f) //0.998f
            {
                float o = 0.0f; float d = 0.0f; float3 l = float3.zero; float3 m = float3.zero;

                dq = ~a * (math.dot(a.real, b.real) >= 0.0f ? b : (-1.0f) * b);

                dq.toScrewParam(out o, out d, out l, out m);
                o = o * t;
                d = d * t;
                dq.fromScrewParam(o, d, l, m);

                q = a * dq;
            }
            else
            {
                q = Lerp(a, b, t);
            }

            return q;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static dualquaternion scLerp2(dualquaternion a, dualquaternion b, float t)
        {
            dualquaternion q = new dualquaternion();
            dualquaternion dq = new dualquaternion();

            float dot = dualquaternion.dot(a, b);
            if (0.0f < dot && dot < 0.998f) //0.998f            
            {
                float o = 0.0f; float d = 0.0f; float3 l = float3.zero; float3 m = float3.zero;

                dq = ~a * (math.dot(a.real, b.real) >= 0.0f ? b : (-1.0f) * b);

                dq.toScrewParam(out o, out d, out l, out m);
                o = o * t;
                d = d * t;
                dq.fromScrewParam(o, d, l, m);

                q = a * dq;
            }
            else
            {
                q = Lerp(a, b, t);
            }

            return q;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static dualquaternion scLerp3(dualquaternion a, dualquaternion b, float t)
        {
            dualquaternion q = new dualquaternion();
            dualquaternion dq = new dualquaternion();

            dq = ~a * (math.dot(a.real, b.real) < 0.0f ? (-1.0f) * b : b);

            if (dq.real.w < 1.0f)
            {
                float o = 0.0f; float d = 0.0f; float3 l = float3.zero; float3 m = float3.zero;

                dq.toScrewParam(out o, out d, out l, out m);
                o = o * t;
                d = d * t;
                dq.fromScrewParam(o, d, l, m);

                q = a * dq;

                debugInfo.x++;
            }
            else
            {
                q = Lerp(a, b, t);

                debugInfo.y++;
            }

            debugInfo.zw = debugInfo.xy / (debugInfo.x + debugInfo.y);

            return q;
        }


        public static float4 debugInfo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static dualquaternion scLerpTest(dualquaternion a, dualquaternion b, float t)
        {
            dualquaternion q = new dualquaternion();
            dualquaternion dq = new dualquaternion();

            float dot = dualquaternion.dot(a, b);
            //sDot = dot;
            //if (0.0f < dot && dot < 0.998f) //0.998f            
            //{
            //    float o = 0.0f; float d = 0.0f; float3 l = float3.zero; float3 m = float3.zero;
            //
            //    dq = ~a * (math.dot(a.real, b.real) >= 0.0f ? b : (-1.0f) * b);
            //
            //    dq.toScrewParam(out o, out d, out l, out m);
            //    o = o * t;
            //    d = d * t;
            //    dq.fromScrewParam(o, d, l, m);
            //
            //    q = a * dq;
            //
            //    countInfo.x++;
            //}
            //else
            //{
            //    q = Lerp(a, b, t);
            //
            //    countInfo.y++;
            //}

            //if (dot <= 0.0f)
            //{
            //    //countNegZeroDot++;
            //    debugInfo.x++;
            //}


            //if(math.abs(0.0f - dot) < 0.0000001f || math.abs(1.0f - dot) < 0.0005f)
            //if (dot < 0.1f || dot > 0.9999999f)
            //if (dot <= 0.0f || dot > 0.99999f)
            dq = ~a * (math.dot(a.real, b.real) >= 0.0f ? b : (-1.0f) * b);

            //if(math.dot(a.real, b.real) < 0.0f)
            //{
            //    dq = ~a * ((-1.0f) * b);
            //}



            //if (dot > 0.99999f)
            //{
            //    q = Lerp(a, b, t);
            //
            //    countInfo.x++;
            //}


            if (dq.real.w >= 1.0f)
            {
                q = Lerp(a, b, t);

                debugInfo.x++;
            }
            else
            {
                float o = 0.0f; float d = 0.0f; float3 l = float3.zero; float3 m = float3.zero;

                //dq = ~a * (math.dot(a.real, b.real) >= 0.0f ? b : (-1.0f) * b);

                //dq = ~a * b;
                //dq = Normalize(dq);
                //dq = (math.dot(dq.real, dq.real) >= 0.0f ? dq : (-1.0f) * dq);

                //if(dq.real.w >= 1.0f)
                //{
                //    q = Lerp(a, b, t);
                //
                //    countInfo.x++;
                //}
                //else
                {
                    dq.toScrewParam(out o, out d, out l, out m);
                    o = o * t;
                    d = d * t;
                    dq.fromScrewParam(o, d, l, m);

                    q = a * dq;

                    //q = Normalize(q);

                    //countInfo.y++;
                    debugInfo.y++;
                }

            }

            debugInfo.zw = debugInfo.xy / (debugInfo.x + debugInfo.y);

            return q;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float dot(dualquaternion a, dualquaternion b)
        {
            return math.dot(a.real, b.real);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static dualquaternion Normalize(dualquaternion q0)
        {
            dualquaternion q1 = q0;

            float mag = math.dot(q0.real, q0.real);

            if (mag > 0.000001f)
            {
                //assert
            }
            q1.real *= 1.0f / mag;
            q1.dual *= 1.0f / mag;

            return q1;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static dualquaternion Lerp(dualquaternion a, dualquaternion b, float dt)
        {
            dualquaternion q = new dualquaternion();

            float3 ta;
            quaternion ra;
            float3 tb;
            quaternion rb;
            float3 t;
            quaternion r;

            a.toRigidParam(out ra.value, out ta);
            b.toRigidParam(out rb.value, out tb);

            t = math.lerp(ta, tb, dt);
            r = math.slerp(ra, rb, dt);
            //r = math.normalize(r);

            q.fromRigidParam(r.value, t);

            return q;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static dualquaternion scLerp0(dualquaternion a, dualquaternion b, float t)
        {
            dualquaternion q = new dualquaternion();
            dualquaternion dq = new dualquaternion();
            float o = 0.0f; float d = 0.0f; float3 l = float3.zero; float3 m = float3.zero;

            dq = ~a * (math.dot(a.real, b.real) >= 0.0f ? b : (-1.0f) * b);

            dq.toScrewParam(out o, out d, out l, out m);
            o = o * t;
            d = d * t;
            dq.fromScrewParam(o, d, l, m);

            q = a * dq;

            return q;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static dualquaternion scLerp_debug(dualquaternion a, dualquaternion b, float t, out float3 rp, out float3 rb, out float3 rl)
        {
            dualquaternion q = new dualquaternion();
            dualquaternion dq = new dualquaternion();
            float o = 0.0f; float d = 0.0f; float3 l = float3.zero; float3 m = float3.zero;

            dq = ~a * (math.dot(a.real, b.real) >= 0.0f ? b : (-1.0f) * b);

            dq.toScrewParam(out o, out d, out l, out m);
            o = o * t;
            d = d * t;
            dq.fromScrewParam(o, d, l, m);

            q = a * dq;

            //Debug
            float4 rot;
            float3 tb;
            float3 p;
            p = math.cross(l, m);
            dq.toRigidParam(out rot, out tb);

            rp = a * p;
            rb = a * (tb - d * l);
            rl = a * (p + d * l);

            return q;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static dualquaternion operator ~(dualquaternion q)
        {
            dualquaternion p = new dualquaternion();
            p.real = new float4(-q.real.xyz, +q.real.w);
            p.dual = new float4(-q.dual.xyz, +q.dual.w);
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static dualquaternion operator !(dualquaternion q)
        {
            dualquaternion p = new dualquaternion();
            p.real = new float4(-q.real.xyz, +q.real.w);
            p.dual = new float4(+q.dual.xyz, -q.dual.w);
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static dualquaternion operator *(dualquaternion a, dualquaternion b)
        {
            dualquaternion p = new dualquaternion();

            p.real =
                math.mul(new quaternion(a.real), new quaternion(b.real)).value;
            p.dual =
                math.mul(new quaternion(a.real), new quaternion(b.dual)).value +
                math.mul(new quaternion(a.dual), new quaternion(b.real)).value;
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 operator *(dualquaternion q, float3 v)
        {
            {
                //dualquaternion p = new dualquaternion();
                //p.real = new float4(0.0f, 0.0f, 0.0f, 1.0f);
                //p.dual = new float4(v, 0.0f);            
                //v = (q * p * (!q)).dual.xyz;
            }

            {
                float4 r;
                float3 t;
                q.toRigidParam(out r, out t);
                v = math.rotate(r, v) + t;
            }

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static dualquaternion operator *(float s, dualquaternion a)
        {
            dualquaternion p = new dualquaternion();

            p.real = s * a.real;
            p.dual = s * a.dual;
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static dualquaternion operator *(dualquaternion a, float s)
        {
            dualquaternion p = new dualquaternion();

            p.real = a.real * s;
            p.dual = a.dual * s;
            return p;
        }
    }

}
