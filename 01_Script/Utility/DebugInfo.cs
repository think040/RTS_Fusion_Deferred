using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Debug = UnityEngine.Debug;

public class DebugInfo : MonoBehaviour
{
    public GameObject debugInfoPanel;
    public GameObject debugInfo;
    static Text debugInfoText;
    public Toggle tgShow;
    public Button btClear;

    private void Awake()
    {
        debugInfoText = debugInfo.GetComponent<Text>();


        tgShow.onValueChanged.AddListener(
            (value) =>
            {
                debugInfoPanel.SetActive(value);
            });

        btClear.onClick.AddListener(
            () =>
            {
                debugInfoText.text = "";
            });

        {
            fpsTimer = new Stopwatch();
            fpsTimer.Start();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
       
    }

    void Update()
    {
        //ShowFPS();
    }

    public static void Log(string text, bool useEditor = false)
    {
        if (debugInfoText != null)
        {
            debugInfoText.text += string.Format("[{0}] {1}\n", DateTime.Now.ToString(), text);

            if(useEditor)
            {
                Debug.Log(text);
            }           
        }
    }

    //public Text fpsInfo;
    Text fpsInfo;

    static Stopwatch fpsTimer;
    static int fpsCounter;

    public void ShowFPS()
    {        
        fpsCounter++;
        if (fpsTimer.ElapsedMilliseconds > 1000)
        {
            //float fps = (float)fpsCounter;
            float fps = (float)1000.0 * fpsCounter / fpsTimer.ElapsedMilliseconds;
            float timePerFrame = (float)fpsTimer.ElapsedMilliseconds / fpsCounter;
            //fpsInfo.text = string.Format("(FPS : {0:F2}), (msPF : {1:F2} (ms))", fps, timePerFrame);
            fpsInfo.text = string.Format("{0, 3:D0}", (int)fps);

            fpsTimer.Stop();
            fpsTimer.Reset();
            fpsTimer.Start();
            fpsCounter = 0;
        }
    }
}
