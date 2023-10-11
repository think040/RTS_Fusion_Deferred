using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTranslation : MonoBehaviour
{
    public int targetFPS = 60;
    public int vSyncCount = 1;

    public Button[] btToScene;
    List<IdxInfo> idx;

    public static SceneTranslation instance;
   
    void Awake()
    {
        QualitySettings.vSyncCount = vSyncCount;
        Application.targetFrameRate = targetFPS;
        //Application.runInBackground = true;                 

        fpsTimer = new Stopwatch();
        fpsTimer.Start();

        fpsTimer_net = new Stopwatch();
        fpsTimer_net.Start();

        fpsTimer_fixed = new Stopwatch();
        fpsTimer_fixed.Start();

        {
            instance = this;
        }

       
    }

    // Start is called before the first frame update
    void Start()
    {
        {
            idx = new List<IdxInfo>();

            if (btToScene != null)
            {
                for (int i = 0; i < btToScene.Length; i++)
                {
                    if (btToScene[i] != null)
                    {
                        IdxInfo info = new IdxInfo();
                        info.idx = i;
                        idx.Add(info);

                        //btToScene[i].onClick.AddListener(
                        //    () =>
                        //    {
                        //        int idx = info.idx;
                        //        SceneManager.LoadScene(idx);
                        //    });

                        btToScene[i].onClick.AddListener(
                           () =>
                           {
                               StartCoroutine(UnloadScene());
                               StartCoroutine(LoadScene(info.idx));
                           });

                    }
                }
            }
        }
    }

    IEnumerator LoadScene(int idx)
    {
        yield return SceneManager.LoadSceneAsync(idx);
    }


    IEnumerator UnloadScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        yield return SceneManager.UnloadSceneAsync(scene);
    }


    class IdxInfo
    {
        public int idx;
    }


    // Update is called once per frame
    void Update()
    {
        ShowFPS();
    }

    void FixedUpdate()
    {
        ShowFPS_Fixed();
    }

    static Stopwatch fpsTimer;
    static int fpsCounter;
    public Text fpsInfo;
    public static float fps;

    public void ShowFPS()
    {
        if (fpsInfo == null)
        {
            return;
        }


        fpsCounter++;
        if (fpsTimer.ElapsedMilliseconds > 1000)
        {
            //float fps = (float)fpsCounter;
            fps = (float)1000.0 * fpsCounter / fpsTimer.ElapsedMilliseconds;
            float timePerFrame = (float)fpsTimer.ElapsedMilliseconds / fpsCounter;
            fpsInfo.text = string.Format("(FPS : {0:F2}), (msPF : {1:F2} (ms)), (TickRate : {2:F2})", fps, timePerFrame, 1.0f / Time.deltaTime);

            fpsTimer.Stop();
            fpsTimer.Reset();
            fpsTimer.Start();
            fpsCounter = 0;
        }
       
    }

    static Stopwatch fpsTimer_fixed;
    static int fpsCounter_fixed;
    public Text fpsInfo_fixed;
    public static float fps_fixed;

    public void ShowFPS_Fixed()
    {
        if(fpsInfo_fixed == null)
        {
            return;
        }


        fpsCounter_fixed++;
        float ms = (float)fpsTimer_fixed.ElapsedMilliseconds;
        if (ms > 1000)
        {
            //float fps = (float)fpsCounter;
            fps_fixed = (float)1000.0 * fpsCounter_fixed / ms;
            float timePerFrame = ms / (float)fpsCounter_fixed;
            fpsInfo_fixed.text = string.Format("(FPS_Fixed : {0:F2}), (msPF : {1:F2} (ms)), (TickRate : {2:F2})", fps_fixed, timePerFrame, 1.0f / Time.fixedDeltaTime);

            fpsTimer_fixed.Stop();
            fpsTimer_fixed.Reset();
            fpsTimer_fixed.Start();
            fpsCounter_fixed = 0;
        }

    }

    static Stopwatch fpsTimer_net;
    static int fpsCounter_net;
    public Text fpsInfo_net;
    public static float fps_net;    

    public void ShowFPS_Net(float dt_tick)
    {
        if (fpsInfo_net == null)
        {
            return;
        }

        fpsCounter_net++;
        float ms = (float)fpsTimer_net.ElapsedMilliseconds;
        if (ms > 1000)
        {
            //float fps = (float)fpsCounter;
            fps_net = (float)1000.0 * fpsCounter_net / ms;
            float timePerFrame = ms / (float)fpsCounter_net;
            fpsInfo_net.text = string.Format("(FPS_Net : {0:F2}), (msPF_Net : {1:F2} (ms)), (TickRate : {2:F2})", fps_net, timePerFrame, 1.0f / dt_tick);

            fpsTimer_net.Stop();
            fpsTimer_net.Reset();
            fpsTimer_net.Start();
            fpsCounter_net = 0;
        }
    }
}