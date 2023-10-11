using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }



    public RectTransform[] rectTrs;
    public bool[] bRects;
    static Image[] images;
    public static bool[] bTouches;
    public static bool[] bToggleValues;
    public static bool[] bButtonClick;

    public float delta = 125.0f;
    static int count;

    RectTransform L;
    RectTransform R;
    RectTransform B;
    RectTransform T;
    RectTransform LB;
    RectTransform RB;
    RectTransform LT;
    RectTransform RT;
    RectTransform C;

    RectWorld rectWorld;
    public RectOut[] rectOut;

    IEnumerator[] routines;

    public void Init()
    {
        {
            //rectTrs = new RectTransform[count];
            count = rectTrs.Length;
        }

        {
            rectWorld = new RectWorld();
            rectWorld.Init(rectTrs, bRects);
        }

        {
            images = new Image[count];
            bTouches = new bool[count];
            bToggleValues = new bool[count];
            bButtonClick = new bool[count];
        }
    }

    public void Begin()
    {
        {
            L = rectTrs[0];
            R = rectTrs[1];
            B = rectTrs[2];
            T = rectTrs[3];

            LB = rectTrs[4];
            RB = rectTrs[5];
            LT = rectTrs[6];
            RT = rectTrs[7];

            C = rectTrs[8];
        }

        {
            for (int i = 0; i < count; i++)
            {
                images[i] = rectTrs[i].gameObject.GetComponent<Image>();
                bTouches[i] = false;
                bToggleValues[i] = false;
                bButtonClick[i] = false;
            }
        }

        {
            SetUpBoundaryMove();
        }

        {
            //routines = new IEnumerator[3];
            //routines[0] = rectWorld.Schedule();
            //routines[1] = CheckMoveTouch();
            //routines[2] = CheckToggleTouch1();



            routines = new IEnumerator[2];
            routines[0] = rectWorld.Schedule();
            routines[1] = CheckBoundary();

            //StartCoroutine(rectWorld.Schedule());
            rectOut = rectWorld.rectOutData;




            //StartCoroutine(CheckToggleSettingMode());
            //StartCoroutine(CheckBoundary());
            //StartCoroutine(rectWorld.Schedule());

            //StartCoroutine(CheckToggleTouch1());
        }


    }

    // Update is called once per frame
    void Update()
    {
        if(!GameManager.bUpdate)
        {
            return;
        }

        for (int i = 0; i < routines.Length; i++)
        {
            routines[i].MoveNext();
        }
    }

    void OnDestroy()
    {
        if (rectWorld != null) rectWorld.ReleaseResourece();
    }

    void SetUpBoundaryMove()
    {
        delta = CamAction.delta;

        L.anchorMin = new Vector2(0.0f, 0.0f);
        L.anchorMax = new Vector2(0.0f, 1.0f);
        L.pivot = new Vector2(0.0f, 0.5f);
        L.offsetMin = new Vector2(0.0f, +delta);
        L.offsetMax = new Vector2(+delta, -delta);

        R.anchorMin = new Vector2(1.0f, 0.0f);
        R.anchorMax = new Vector2(1.0f, 1.0f);
        R.pivot = new Vector2(1.0f, 0.5f);
        R.offsetMin = new Vector2(-delta, +delta);
        R.offsetMax = new Vector2(0.0f, -delta);

        B.anchorMin = new Vector2(0.0f, 0.0f);
        B.anchorMax = new Vector2(1.0f, 0.0f);
        B.pivot = new Vector2(0.5f, 0.0f);
        B.offsetMin = new Vector2(+delta, 0.0f);
        B.offsetMax = new Vector2(-delta, +delta);

        T.anchorMin = new Vector2(0.0f, 1.0f);
        T.anchorMax = new Vector2(1.0f, 1.0f);
        T.pivot = new Vector2(0.5f, 1.0f);
        T.offsetMin = new Vector2(+delta, -delta);
        T.offsetMax = new Vector2(-delta, 0.0f);

        //
        LB.anchorMin = new Vector2(0.0f, 0.0f);
        LB.anchorMax = new Vector2(0.0f, 0.0f);
        LB.pivot = new Vector2(0.0f, 0.0f);
        LB.offsetMin = new Vector2(0.0f, 0.0f);
        LB.offsetMax = new Vector2(+delta, +delta);

        RB.anchorMin = new Vector2(1.0f, 0.0f);
        RB.anchorMax = new Vector2(1.0f, 0.0f);
        RB.pivot = new Vector2(1.0f, 0.0f);
        RB.offsetMin = new Vector2(-delta, 0.0f);
        RB.offsetMax = new Vector2(0.0f, +delta);

        LT.anchorMin = new Vector2(0.0f, 1.0f);
        LT.anchorMax = new Vector2(0.0f, 1.0f);
        LT.pivot = new Vector2(0.0f, 1.0f);
        LT.offsetMin = new Vector2(0.0f, -delta);
        LT.offsetMax = new Vector2(+delta, 0.0f);

        RT.anchorMin = new Vector2(1.0f, 1.0f);
        RT.anchorMax = new Vector2(1.0f, 1.0f);
        RT.pivot = new Vector2(1.0f, 1.0f);
        RT.offsetMin = new Vector2(-delta, -delta);
        RT.offsetMax = new Vector2(0.0f, 0.0f);

        //
        C.anchorMin = new Vector2(0.0f, 0.0f);
        C.anchorMax = new Vector2(1.0f, 1.0f);
        C.pivot = new Vector2(0.5f, 0.5f);
        C.offsetMin = new Vector2(+delta, +delta);
        C.offsetMax = new Vector2(-delta, -delta);

        //
        //C.anchorMin = new Vector2(0.5f, 0.5f);
        //C.anchorMax = new Vector2(0.5f, 0.5f);
        //C.pivot = new Vector2(0.5f, 0.5f);
        //C.offsetMin = new Vector2(-delta, -delta);
        //C.offsetMax = new Vector2(+delta, +delta);
    }

    IEnumerator CheckBoundary()
    {
        bool[,] bound = new bool[3, 3];
        Image[,] img = new Image[3, 3];
        int2[] idx = new int2[9];

        {
            img[0, 1] = images[0];
            img[2, 1] = images[1];
            img[1, 0] = images[2];
            img[1, 2] = images[3];

            img[0, 0] = images[4];
            img[2, 0] = images[5];
            img[0, 2] = images[6];
            img[2, 2] = images[7];

            img[1, 1] = images[8];
        }

        {
            idx[0] = new int2(0, 1);
            idx[1] = new int2(2, 1);
            idx[2] = new int2(1, 0);
            idx[3] = new int2(1, 2);

            idx[4] = new int2(0, 0);
            idx[5] = new int2(2, 0);
            idx[6] = new int2(0, 2);
            idx[7] = new int2(2, 2);

            idx[8] = new int2(1, 1);
        }

        while (true)
        {
            if ((Input.GetKey(CamAction.key_orbit) || Input.GetKey(CamAction.key_spin)) && Input.GetMouseButton(1))
            {
                for (int i = 0; i < 8; i++)
                {
                    Image image = images[i];

                    {
                        image.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
                        bTouches[i] = false;
                        //bButtonClick[i] = false;
                    }
                }
            }
            else
            {
                //for (int i = 0; i < 8; i++)
                //{
                //    Image image = images[i];
                //
                //    if (rectOut[i].bIn)
                //    {
                //        image.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
                //        bTouches[i] = true;
                //        //bButtonClick[i] = true;
                //    }
                //    else
                //    {
                //        image.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
                //        bTouches[i] = false;
                //        //bButtonClick[i] = false;
                //    }
                //}

                int xi = 1;
                int yi = 1;

                int2 id = new int2(1, 1);
                for (int i = 0; i < 8; i++)
                {
                    Image image = images[i];

                    {
                        image.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
                        //bTouches[i] = false;
                        //bButtonClick[i] = false;
                    }
                }

                if (CamAction.useMouseForMove)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        Image image = images[i];

                        if (rectOut[i].bIn)
                        {
                            //image.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
                            //xi = idx[i].x;
                            //yi = idx[i].y;

                            id.x = idx[i].x;
                            id.y = idx[i].y;
                            //bTouches[i] = true;
                            //bButtonClick[i] = true;
                        }
                    }
                }


                {
                    if (Input.GetKey(KeyCode.A))
                    {
                        //xi -= xi > 0 ? 1 : 0;
                        id.x -= id.x > 0 ? 1 : 0;
                    }

                    if (Input.GetKey(KeyCode.D))
                    {
                        //xi += xi < 2 ? 1 : 0;
                        id.x += id.x < 2 ? 1 : 0;
                    }

                    if (Input.GetKey(KeyCode.S))
                    {
                        //yi -= yi > 0 ? 1 : 0;
                        id.y -= id.y > 0 ? 1 : 0;
                    }

                    if (Input.GetKey(KeyCode.W))
                    {
                        //yi += yi < 2 ? 1 : 0;
                        id.y += id.y < 2 ? 1 : 0;
                    }

                    bool2 b = new bool2(false, false);
                    b = (id != new int2(1, 1));

                    //if (xi != 1 || yi != 1)
                    if (b.x || b.y)
                    {
                        //img[xi, yi].color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
                        img[id.x, id.y].color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
                    }
                }
            }

            yield return null;

        }
    }

    public class RectWorld
    {
        public IEnumerator Schedule()
        {
            InitResource();
            while (true)
            {
                WriteToResourece();
                ExecuteJob();
                ReadFromResourece();

                yield return null;
            }
            ReleaseResourece();
        }

        public void Init(RectTransform[] rectTrs, bool[] bRects)
        {
            this.rectTrs = rectTrs;
            this.bRects = bRects;
            this.count = rectTrs.Length;

            rectInData = new RectIn[count];
            rectOutData = new RectOut[count];

            traa = new TransformAccessArray(rectTrs);
            rectIn = new NativeArray<RectIn>(count, Allocator.Persistent);
            rectOut = new NativeArray<RectOut>(count, Allocator.Persistent);

            job = new ActionJob();
        }

        int count;
        RectTransform[] rectTrs;
        bool[] bRects;
        public float3 posS;
        RectIn[] rectInData;
        public RectOut[] rectOutData;

        TransformAccessArray traa;
        NativeArray<RectIn> rectIn;
        NativeArray<RectOut> rectOut;

        ActionJob job;

        void InitResource()
        {
            job.rectIn = rectIn;
            job.rectOut = rectOut;
        }

        void WriteToResourece()
        {
            for (int i = 0; i < count; i++)
            {
                RectIn rIn;
                rIn.rect = rectTrs[i].rect;
                rIn.bRect = bRects[i];
                rectIn[i] = rIn;
            }

            job.posS = Input.mousePosition;
        }

        void ExecuteJob()
        {
            job.ScheduleReadOnly<ActionJob>(traa, count).Complete();

            //job.Schedule<ActionJob>(traa).Complete();
        }

        void ReadFromResourece()
        {
            for (int i = 0; i < count; i++)
            {
                rectOutData[i] = rectOut[i];
            }
        }

        public void ReleaseResourece()
        {
            if (traa.isCreated) traa.Dispose();
            if (rectIn.IsCreated) rectIn.Dispose();
            if (rectOut.IsCreated) rectOut.Dispose();
        }

        [BurstCompile]
        struct ActionJob : IJobParallelForTransform
        {
            [ReadOnly]
            public NativeArray<RectIn> rectIn;

            [ReadOnly]
            public float3 posS;


            [WriteOnly]
            public NativeArray<RectOut> rectOut;

            public void Execute(int i, TransformAccess ta)
            {
                float4x4 W = ta.localToWorldMatrix;

                RectOut rOut = new RectOut();
                Rect rIn = rectIn[i].rect;
                bool bRect = rectIn[i].bRect;

                float2 pos = rIn.position;
                float2 size = rIn.size;

                float4 min = math.mul(W, new float4(pos, 0.0f, 1.0f));
                float4 max = math.mul(W, new float4(pos + size, 0.0f, 1.0f));

                pos = min.xy;
                size = max.xy - min.xy;

                if (bRect)
                {
                    rIn.position = pos;
                    rIn.size = size;

                    rOut.rect = rIn;
                    rOut.bIn = rIn.Contains(posS);
                }
                else
                {
                    float2 center = 0.5f * (min.xy + max.xy);
                    float radius = size.x > size.y ? 0.5f * size.y : 0.5f * size.x;

                    rIn.position = center;
                    rIn.size = new float2(radius, radius);

                    rOut.rect = rIn;
                    rOut.bIn = math.distance(center, posS.xy) > radius ? false : true;
                }


                rectOut[i] = rOut;
            }
        }

    }

    public struct RectIn
    {
        public Rect rect;
        public bool bRect;
    }


    [System.Serializable]
    public struct RectOut
    {
        public Rect rect;
        public bool bIn;
    }
}