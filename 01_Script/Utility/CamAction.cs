using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;

using UnityEngine;

public class CamAction : MonoBehaviour
{
    public int mode = 0;

    public Camera cam;
    public float zoomPace = 15.0f;   

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        {
            camMove = new CamAction.CamMove();
        
            StartCoroutine(camMove.ZoomInOut(transform, zoomPace));
            //StartCoroutine(camMove.MoveParallel(transform, mode));
            StartCoroutine(camMove.MoveParallel_XZ(transform, delta));
        
            StartCoroutine(camMove.RotSpin(transform));
            StartCoroutine(camMove.RotOrbit(transform));
        }

        //{
        //    camMove = new CamAction.CamMove();
        //    routine = new IEnumerator[4];
        //
        //    routine[0] = camMove.ZoomInOut(transform, zoomPace);            
        //    routine[1] = camMove.MoveParallel_XZ(transform, delta);
        //    routine[2] = camMove.RotSpin(transform);
        //    routine[3] = camMove.RotOrbit(transform);
        //}

        //{
        //    camMove = new CamAction.CamMove();
        //    routine = new IEnumerator[2];
        //
        //    routine[0] = camMove.ZoomInOut(transform, zoomPace);
        //    routine[1] = camMove.MoveParallel_XZ(transform, delta);
        //
        //    StartCoroutine(camMove.RotSpin(transform));
        //    StartCoroutine(camMove.RotOrbit(transform));
        //}
    }


    IEnumerator[] routine;

    void FixedUpdate()
    {
        //foreach(var r in routine)
        //{
        //    r.MoveNext();
        //}
    }



    public bool bFarCompute = true;
    void Update()
    {
        Compute_FarPlane(cam);
    }

    static bool bFocus = true;

    private void OnApplicationFocus(bool focus)
    {
        bFocus = focus;
    }



    CamMove camMove;

    public static KeyCode key_orbit = KeyCode.Z;    //KeyCode.V     //KeyCode.LeftShift
    public static KeyCode key_spin = KeyCode.C;       //KeyCode.B     //KeyCode.LeftAlt
    public static int delta = 75;  //50

    public static float pacePlane
    { get; set; } = 50.0f;

    //public static float paceZoom
    //{ get; set; } = 3.0f;

    public static bool useMouseForMove
    { get; set; } = false;

    public class CamMove
    {
        public bool activeZoom
        { get; set; } = true;

        public bool activeMoveParallel
        { get; set; } = true;

        public bool activeRotOrbit
        { get; set; } = true;

        public bool activeRotSpin
        { get; set; } = true;

        //public float paceZoom
        //{ get; set; } = 1.0f;
        //
        //public float pacePlane
        //{ get; set; } = 1.0f;


        //Play
        public IEnumerator ZoomInOut(Transform camTrans, float pace = 15.0f)
        {
            while (true)
            {
                //float delta = Input.GetAxis("Mouse ScrollWheel") * 400.0f * pace * Time.deltaTime;
                float delta = Input.GetAxis("Mouse ScrollWheel") * 400.0f * pace * Time.fixedDeltaTime;

                float3 zaxis = math.rotate(camTrans.rotation, new float3(0.0f, 0.0f, 1.0f));
                float3 pos = camTrans.position;
                pos = pos + zaxis * delta;

                camTrans.position = pos;

                yield return null;
            }
        }

        public IEnumerator MoveParallel(Transform camTrans, int mode = 0)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            while (true)
            {
                Vector3 camPos = Vector3.zero;
                float camDelta = 0.05f;

                float y0 = 0.025f;

                //float yScale = bUseYScale ? camTrans.position.y : 1.0f;

                //camDelta = y0 + (0.025f * CamAction.pacePlane) * camTrans.position.y * Time.deltaTime;
                //camDelta = y0 + (0.025f * CamAction.pacePlane) * yScale * Time.deltaTime;

                camDelta = y0 + (0.025f * CamAction.pacePlane) * math.abs(camTrans.position.y) * Time.deltaTime;

                //camDelta = 1.0f;

                {
                    if (Input.GetKey(KeyCode.W))
                    {
                        camPos.y += camDelta;
                    }

                    if (Input.GetKey(KeyCode.A))
                    {
                        camPos.x -= camDelta;
                    }

                    if (Input.GetKey(KeyCode.S))
                    {
                        camPos.y -= camDelta;
                    }

                    if (Input.GetKey(KeyCode.D))
                    {
                        camPos.x += camDelta;
                    }
                }

                float3 xaxis;
                float3 yaxis;
                float3 zaxis;

                if (mode == 0)
                {
                    xaxis = math.rotate(camTrans.rotation, new float3(1.0f, 0.0f, 0.0f));
                    zaxis = math.rotate(camTrans.rotation, new float3(0.0f, 0.0f, 1.0f));
                }
                else
                {
                    xaxis = math.rotate(camTrans.rotation, new float3(1.0f, 0.0f, 0.0f));
                    yaxis = new float3(0.0f, 1.0f, 0.0f);
                    zaxis = math.cross(xaxis, yaxis);
                }

                float3 localPos = xaxis * camPos.x + zaxis * camPos.y;
                camTrans.position += (Vector3)localPos;


                yield return null;
            }
        }

        public IEnumerator RotSpin(Transform camTrans)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            float3 prePos = float3.zero;
            float3 curPos = float3.zero;

            float3 angle = float3.zero;

            bool down = false;

            quaternion q = quaternion.identity;
            quaternion preRot = quaternion.identity;

            while (true)
            {
                if (Input.GetMouseButtonDown(1) && Input.GetKey(key_spin))
                {
                    prePos = Input.mousePosition;
                    preRot = camTrans.rotation;
                    down = true;
                }

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

                if (Input.GetMouseButtonUp(1) || !Input.GetKey(key_spin))
                {
                    down = false;
                }

                yield return null;
            }

        }

        public IEnumerator RotOrbit(Transform camTrans)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            float3 prePos = float3.zero;
            float3 curPos = float3.zero;

            float3 angle = float3.zero;

            bool down = false;

            Ray ray = new Ray();
            float3 center = float3.zero;
            float radius = 1.0f;
            float3 centerNormal = float3.zero;

            quaternion preRot = quaternion.identity;

            float3 rayDirInView = float3.zero;
            float3 rayDirInWorld = float3.zero;

            quaternion q = quaternion.identity;

            while (true)
            {
                if (Input.GetMouseButtonDown(1) && Input.GetKey(key_orbit))
                {
                    prePos = Input.mousePosition;
                    preRot = camTrans.rotation;

                    ray = cam.ScreenPointToRay(prePos);

                    float3x3 mat = new float3x3(camTrans.rotation);
                    float3 rayDir = ray.direction.normalized;
                    rayDirInView = math.mul(math.transpose(mat), rayDir);

                    {
                        float3 n = new float3(0.0f, -1.0f, 0.0f);
                        float3 p0 = new float3(0.0f, 0.0f, 0.0f);

                        float3 d = math.normalize(ray.direction);
                        float3 l0 = ray.origin;

                        float k0 = math.dot(n, d);
                        float k1 = math.dot(n, (p0 - l0));

                        float t = 0.0f;
                        t = (math.abs(k0) < 0.1f) ?  10.0f : (k1 / k0);                                                
                        
                        float3 l1 = l0 + t * d;
                        radius = math.distance(l1, camTrans.position);
                        center = l1;                       

                        down = true;
                    }

                    //{
                    //    float3 n = new float3(0.0f, -1.0f, 0.0f);
                    //    float3 p0 = new float3(0.0f, 0.0f, 0.0f);
                    //
                    //    float3 d = math.normalize(ray.direction);
                    //    float3 l0 = ray.origin;
                    //
                    //    float t = (math.dot(n, (p0 - l0)) / math.dot(n, d));
                    //    float3 l1 = l0 + t * d;
                    //    radius = math.distance(l1, camTrans.position);
                    //    center = l1;
                    //
                    //    //Debug.Log("radius : " + radius.ToString());
                    //    //Debug.Log("center : " + center.ToString());
                    //
                    //    down = true;
                    //}
                }

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

                if (Input.GetMouseButtonUp(1) || !Input.GetKey(key_orbit))
                {
                    down = false;
                }

                yield return null;
            }
        }

        //
        public static void ZoomInOut1(Transform camTrans)
        {
            float pace = 15.0f;
            float delta = Input.GetAxis("Mouse ScrollWheel") * 400.0f * pace * Time.deltaTime;

            float3 zaxis = math.rotate(camTrans.rotation, new float3(0.0f, 0.0f, 1.0f));
            float3 pos = camTrans.position;
            pos = pos + zaxis * delta;

            camTrans.position = pos;
        }

        public static void MoveParallel1(Transform camTrans, int mode = 0)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            Vector3 camPos = Vector3.zero;
            float camDelta = 0.05f;

            float y0 = 0.1f;
            //camDelta = y0 + (0.025f * CamAction.pacePlane) * camTrans.position.y * Time.fixedDeltaTime;
            //camDelta = 1.0f;
            //camDelta = 100.0f * Time.deltaTime;
            camDelta = y0 + (0.025f * CamAction.pacePlane) * camTrans.position.y * Time.deltaTime;

            {
                if (Input.GetKey(KeyCode.W))
                {
                    camPos.y += camDelta;
                }

                if (Input.GetKey(KeyCode.A))
                {
                    camPos.x -= camDelta;
                }

                if (Input.GetKey(KeyCode.S))
                {
                    camPos.y -= camDelta;
                }

                if (Input.GetKey(KeyCode.D))
                {
                    camPos.x += camDelta;
                }
            }

            float3 xaxis;
            float3 yaxis;
            float3 zaxis;

            if (mode == 0)
            {
                xaxis = math.rotate(camTrans.rotation, new float3(1.0f, 0.0f, 0.0f));
                zaxis = math.rotate(camTrans.rotation, new float3(0.0f, 0.0f, 1.0f));
            }
            else
            {
                xaxis = math.rotate(camTrans.rotation, new float3(1.0f, 0.0f, 0.0f));
                yaxis = new float3(0.0f, 1.0f, 0.0f);
                zaxis = math.cross(xaxis, yaxis);
            }

            float3 localPos = xaxis * camPos.x + zaxis * camPos.y;
            camTrans.position += (Vector3)localPos;
        }

        //
        public IEnumerator MoveParallel_XZ(Transform camTrans, float pace, int delta = 20)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            while (true)
            {
                if (activeMoveParallel)
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

                    //{
                    //    if (Input.GetKey(KeyCode.W))
                    //    {
                    //        UKey = true;
                    //    }
                    //
                    //    if (Input.GetKey(KeyCode.A))
                    //    {
                    //        Lkey = true;
                    //    }
                    //
                    //    if (Input.GetKey(KeyCode.S))
                    //    {
                    //        DKey = true;
                    //    }
                    //
                    //    if (Input.GetKey(KeyCode.D))
                    //    {
                    //        RKey = true;
                    //    }
                    //
                    //    if (((xmin <= pos.x && pos.x <= xmin + delta)
                    //        && (ymin <= pos.y && pos.y <= ymin + delta))
                    //        || (Lkey && !RKey && !UKey && DKey))
                    //    {
                    //        //1
                    //        camPos.x -= camDelta;
                    //        camPos.y -= camDelta;
                    //    }
                    //    else if (((xmin + delta < pos.x && pos.x < xmax - delta)
                    //        && (ymin <= pos.y && pos.y <= ymin + delta))
                    //        || (!Lkey && !RKey && !UKey && DKey))
                    //    {
                    //        //2
                    //        camPos.y -= camDelta;
                    //    }
                    //    else if (((xmax - delta <= pos.x && pos.x <= xmax)
                    //        && (ymin <= pos.y && pos.y <= ymin + delta))
                    //        || (!Lkey && RKey && !UKey && DKey))
                    //    {
                    //        //3
                    //        camPos.x += camDelta;
                    //        camPos.y -= camDelta;
                    //    }
                    //    else if (((xmin <= pos.x && pos.x <= xmin + delta)
                    //         && (ymin + delta < pos.y && pos.y < ymax - delta))
                    //         || (Lkey && !RKey && !UKey && !DKey))
                    //    {
                    //        //4
                    //        camPos.x -= camDelta;
                    //    }
                    //    //else if ((xmin + delta < pos.x && pos.x < xmax - delta)
                    //    //     && (ymin + delta < pos.y && pos.y < ymax - delta)
                    //    //     )
                    //    //{
                    //    //    //5
                    //    //}
                    //    else if (((xmax - delta <= pos.x && pos.x <= xmax)
                    //        && (ymin + delta < pos.y && pos.y < ymax - delta))
                    //        || (!Lkey && RKey && !UKey && !DKey))
                    //    {
                    //        //6
                    //        camPos.x += camDelta;
                    //    }
                    //    else if (((xmin <= pos.x && pos.x <= xmin + delta)
                    //        && (ymax - delta <= pos.y && pos.y <= ymax))
                    //        || (Lkey && !RKey && UKey && !DKey))
                    //    {
                    //        //7
                    //        camPos.x -= camDelta;
                    //        camPos.y += camDelta;
                    //    }
                    //    else if (((xmin + delta < pos.x && pos.x < xmax - delta)
                    //         && (ymax - delta <= pos.y && pos.y <= ymax))
                    //         || (!Lkey && !RKey && UKey && !DKey))
                    //    {
                    //        //8                   
                    //        camPos.y += camDelta;
                    //    }
                    //    else if (((xmax - delta <= pos.x && pos.x <= xmax)
                    //        && (ymax - delta <= pos.y && pos.y <= ymax))
                    //        || (!Lkey && RKey && UKey && !DKey))
                    //    {
                    //        //9
                    //        camPos.x += camDelta;
                    //        camPos.y += camDelta;
                    //    }
                    //
                    //    //Vector3 localPos =
                    //    //    (camTrans.rotation * Vector3.right).normalized * camPos.x +
                    //    //    (camTrans.rotation * Vector3.up).normalized * camPos.y;
                    //    //camTrans.position = camTrans.position + localPos;
                    //}

                    {
                        if (Input.GetKey(KeyCode.W) || (ymax - delta <= pos.y && pos.y <= ymax))
                        {
                            UKey = true;
                            camPos.y += camDelta;
                        }

                        if (Input.GetKey(KeyCode.A) || (xmin <= pos.x && pos.x <= xmin + delta))
                        {
                            Lkey = true;
                            camPos.x -= camDelta;
                        }

                        if (Input.GetKey(KeyCode.S) || (ymin <= pos.y && pos.y <= ymin + delta))
                        {
                            DKey = true;
                            camPos.y -= camDelta;
                        }

                        if (Input.GetKey(KeyCode.D) || (xmax - delta <= pos.x && pos.x <= xmax))
                        {
                            RKey = true;
                            camPos.x += camDelta;
                        }
                    }



                    float3 xaxis = math.rotate(camTrans.rotation, new float3(1.0f, 0.0f, 0.0f));
                    float3 yaxis = new float3(0.0f, 1.0f, 0.0f);
                    float3 zaxis = math.cross(xaxis, yaxis);

                    float3 localPos = xaxis * camPos.x + zaxis * camPos.y;
                    camTrans.position += (Vector3)localPos;

                    //camTrans.localPosition = camPos;
                }

                yield return null;
            }
        }

        public IEnumerator MoveParallel_XZ(Transform camTrans, int delta = 20)
        {
            Camera cam = camTrans.gameObject.GetComponent<Camera>();

            while (true)
            {
                if(!bFocus)
                {
                    yield return null;   
                }

                if (activeMoveParallel)
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
                    float y0 = 0.025f;
                    //camDelta = y0 + (0.025f * CamAction.pacePlane) * camTrans.position.y * Time.deltaTime;
                    camDelta = y0 + (0.025f * CamAction.pacePlane) * camTrans.position.y * Time.fixedDeltaTime;

                    bool RKey = false;
                    bool Lkey = false;
                    bool UKey = false;
                    bool DKey = false;

                    if (CamAction.useMouseForMove)
                    {
                        if ((Input.GetKey(CamAction.key_orbit) || Input.GetKey(CamAction.key_spin)) && Input.GetMouseButton(1))
                        {

                        }
                        else
                        {
                            if (Input.GetKey(KeyCode.W) || ((ymax - delta <= pos.y && pos.y <= ymax) && (xmin <= pos.x && pos.x <= xmax)))
                            {
                                UKey = true;
                                camPos.y += camDelta;
                            }

                            if (Input.GetKey(KeyCode.A) || ((xmin <= pos.x && pos.x <= xmin + delta) && (ymin <= pos.y && pos.y <= ymax)))
                            {
                                Lkey = true;
                                camPos.x -= camDelta;
                            }

                            if (Input.GetKey(KeyCode.S) || ((ymin <= pos.y && pos.y <= ymin + delta) && (xmin <= pos.x && pos.x <= xmax)))
                            {
                                DKey = true;
                                camPos.y -= camDelta;
                            }

                            if (Input.GetKey(KeyCode.D) || ((xmax - delta <= pos.x && pos.x <= xmax) && (ymin <= pos.y && pos.y <= ymax)))
                            {
                                RKey = true;
                                camPos.x += camDelta;
                            }
                        }
                    }
                    else
                    {
                        if (Input.GetKey(KeyCode.W))
                        {
                            UKey = true;
                            camPos.y += camDelta;
                        }

                        if (Input.GetKey(KeyCode.A))
                        {
                            Lkey = true;
                            camPos.x -= camDelta;
                        }

                        if (Input.GetKey(KeyCode.S))
                        {
                            DKey = true;
                            camPos.y -= camDelta;
                        }

                        if (Input.GetKey(KeyCode.D))
                        {
                            RKey = true;
                            camPos.x += camDelta;
                        }
                    }



                    float3 xaxis = math.rotate(camTrans.rotation, new float3(1.0f, 0.0f, 0.0f));
                    float3 yaxis = new float3(0.0f, 1.0f, 0.0f);
                    float3 zaxis = math.cross(xaxis, yaxis);

                    float3 localPos = xaxis * camPos.x + zaxis * camPos.y;
                    camTrans.position += (Vector3)localPos;

                    //camTrans.localPosition = camPos;
                }

                yield return null;
            }
        }

        public IEnumerator RotOrbitRandomFixed_range(
            Transform camTrans, string planeTag, string planeLayer,
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
                if (activeRotOrbit)
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
                    }
                }

                yield return null;

                if (!activeRotOrbit)
                {
                    down = false;
                }

            }


        }

        public IEnumerator RotSpin_range(
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
                if (activeRotSpin)
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
                }

                yield return null;

                if (!activeRotSpin)
                {
                    down = false;
                }
            }

        }

    }


    //FarPalne
    public void Compute_FarPlane(Camera cam)
    {
        if (cam.cameraType == CameraType.Game && bFarCompute)
        {
            float fov = cam.fieldOfView;
            float aspect = cam.aspect;
            float near = cam.nearClipPlane;
            float far = cam.farClipPlane;

            float3 pos = cam.transform.position;

            float yn = near * math.tan(math.radians(fov * 0.5f));
            float xn = aspect * yn;

            //float3 forward = near * math.normalize(math.rotate(cam.transform.rotation, new float3(0.0f, 0.0f, 1.0f)));
            float4x4 W = cam.transform.localToWorldMatrix;
            float4 forward = W.c2;
            float4x4 vecs = float4x4.zero;
            vecs.c0 = new float4(+xn, +yn, near, 0.0f);
            vecs.c1 = new float4(-xn, +yn, near, 0.0f);
            vecs.c2 = new float4(-xn, -yn, near, 0.0f);
            vecs.c3 = new float4(+xn, -yn, near, 0.0f);
            vecs = math.mul(W, vecs);

            float3x2 plane;
            plane.c0 = new float3(0.0f, -0.1f, 0.0f);
            plane.c1 = new float3(0.0f, 1.0f, 0.0f);
            float3x2 line;
            line.c0 = pos;

            float maxDist = 0.0f;
            int maxIndex = 0;
            unsafe
            {
                float* dist = stackalloc float[4];
                float3x2* lines = stackalloc float3x2[4];

                lines[0].c1 = vecs.c0.xyz;
                lines[1].c1 = vecs.c1.xyz;
                lines[2].c1 = vecs.c2.xyz;
                lines[3].c1 = vecs.c3.xyz;

                for (int i = 0; i < 4; i++)
                {
                    lines[i].c0 = pos;
                    dist[i] = Compute_Dist(plane, lines[i]);
                    //Debug.Log("dist" + i.ToString() + " : " + dist[i].ToString());
                }

                maxDist = max(dist, 4, &maxIndex);

                far = Compute_maxDist(forward.xyz, maxDist, lines[maxIndex].c1);

            }

            cam.farClipPlane = 1.1f * far;
            //cam.farClipPlane = far;
        }
    }

    static float Compute_Dist(float3x2 plane, float3x2 line)
    {
        const float maxDist = 1000.0f;

        float dist = 0.0f;
        float3 n = -math.normalize(plane.c1);
        float l = math.length(line.c1);
        float3 dir = line.c1 / l;
        float nDotDir = math.dot(n, dir);
        //if (nDotDir > 0.25f)
        if (nDotDir > 0.0001f)
        {
            //dist = math.length((math.dot(n, plane.c0 - line.c0) / math.max(0.0001f, math.dot(n, line.c1))) * line.c1);
            dist = math.length((math.dot(n, plane.c0 - line.c0) / (l * nDotDir) * line.c1));
        }
        else
        {
            //dist = 50.0f;
            //dist = 100.0f;
            dist = maxDist;
        }

        //return math.min(dist, 200.0f);
        return math.min(dist, maxDist);
    }

    static float Compute_maxDist(float3 dir, float maxDist, float3 lineDir)
    {
        float max = 0.0f;
        float3 n = math.normalize(dir);
        max = math.dot(n, maxDist * math.normalize(lineDir));

        return max;
    }

    static unsafe float max(float* ptr, int num, int* index)
    {
        float max = 0.0f;
        max = *ptr;
        *index = 0;
        for (int i = 0; i < num; i++)
        {
            if (max < *(ptr + i))
            {
                max = *(ptr + i);
                *index = i;
            }
        }

        return max;
    }
}