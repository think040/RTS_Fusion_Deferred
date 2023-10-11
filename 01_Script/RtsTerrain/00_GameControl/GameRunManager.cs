using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Fusion;
using System.Threading.Tasks;
using System.Diagnostics;
using Unity.Mathematics;

public class GameRunManager : NetworkBehaviour
{
    
    Text gameTimeInfo;
    
    RectTransform blueBar;
    RectTransform redBar;
    Text blueCount;
    Text redCount;

    PlayUIManager playUIMan;

    GameObject panel_Setting;
    GameObject panel_End;

    public override void Spawned()
    {
        base.Spawned();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
    }

    //public override void FixedUpdateNetwork()
    ////public override void Render()
    //{
    //    if(!GameManager.bUpdate)
    //    {
    //        return;
    //    }
    //    
    //    if (GameManager.isPause)
    //    {
    //        return;
    //    }
    //    
    //    
    //    //{
    //    //    SceneTranslation.instance.ShowFPS();
    //    //}
    //    for(int i = 0; i < GameManager.unitMan.Length; i++)
    //    {
    //        GameManager.unitMan[i].UpdateBone();
    //    }        
    //}

    public override void FixedUpdateNetwork()
    {
        if (!GameManager.bUpdate)
        {
            return;
        }        
    
        SceneTranslation.instance.ShowFPS_Net(Runner.DeltaTime);
                
    }


    public void Init()
    {
        playUIMan = GameObject.Find("Canvas_Play").GetComponent<PlayUIManager>();

        if(playUIMan != null)
        {
            gameTimeInfo = playUIMan.gameTimeInfo;
            blueBar = playUIMan.blueBar;
            redBar = playUIMan.redBar;
            blueCount = playUIMan.blueCount;
            redCount = playUIMan.redCount;

            panel_Setting = playUIMan.panel_Setting;
            panel_End = playUIMan.panel_End;
        }

        if(HasStateAuthority)
        {
            int2 gameTime = GameManager.instance.gameTime;

            GameTimer.inMin = gameTime.x;
            GameTimer.inSec = gameTime.y;
            

            GameTimer.Reset();
            GameTimer.Restart();
        }
      
        if (HasStateAuthority)
        {
            TestPauseResume();
            TestTimeOut();
        }
    }

    public void Enable()
    {
        isDestory = false;
    }

    public void Disable()
    {
        isPause = false;
        isDestory = true;
    }

    public void Begin()
    {

    }

    //public override void FixedUpdateNetwork()
    public void Update()
    {
        if (!GameManager.bUpdate)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            RPC_SetIsPauseKeyDown(true ? 1 : 0);
        }

        ShowGameTimer();
        ShowKillInfo();
    }

    float eTime;
    int sec;
    int min;

    int inSec = 30;
    int inMin = 3;
    int toSec = 0;
    public int cdSec = 0;
    public int cdMin = 0;

    void ShowGameTimer()
    {        

        if(HasStateAuthority)
        {
            GameTimer.Run();
            eTime = GameTimer.eTime;
            sec = GameTimer.sec;
            min = GameTimer.min;

            toSec = GameTimer.toSec;
            cdMin = GameTimer.cdMin;
            cdSec = GameTimer.cdSec;

            if (GameTimer.toSec == 0)
            {
                GameTimer.Stop();
            }

            RPC_SendTimeInfo(toSec, cdMin, cdSec);
        }
        
        //gameTimeInfo.text = string.Format("RemainTime : {0}(m) : {1}(s)", GameTimer.cdMin, GameTimer.cdSec);
        gameTimeInfo.text = string.Format("{0, 2:D2} : {1, 2:D2}", GameTimer.cdMin, GameTimer.cdSec);
    }
   
    public void ShowKillInfo()
    {
        if(HasStateAuthority)
        {
            RPC_SendKillInfo(GameManager.killedCount[0], GameManager.killedCount[1]);
        }

        float blueKill = (float)GameManager.killedCount[1];
        float redKill = (float)GameManager.killedCount[0];
        float sum = blueKill + redKill;

        if (sum > 0)
        {
            float blueRatio = blueKill / sum;
            float redRatio = redKill / sum;

            blueBar.anchorMin = new Vector2(0.0f, 0.0f);
            blueBar.anchorMax = new Vector2(blueRatio, 1.0f);

            redBar.anchorMin = new Vector2(1.0f - redRatio, 0.0f);
            redBar.anchorMax = new Vector2(1.0f, 1.0f);
        }

        blueCount.text = string.Format("{0, 2:D2}", GameManager.killedCount[1]);
        redCount.text = string.Format("{0, 2:D2}", GameManager.killedCount[0]);

        //killInfo.text = string.Format("(player0_Kill : {0}), (player1_Kill : {1})", UnitData.killNum[1], UnitData.killNum[0]);
        //killInfo.text = string.Format("(Blue: {0, 2:D2}), (Red : {1, 2:D2})", GameManager.killedCount[1], GameManager.killedCount[0]);

    }

   
    public static bool isNvStop { get; set; } = false;

    public static bool isPause { get; set; } = false;
     
    bool isPauseKeyDown { get; set; } = false;
    bool isDestory { get; set; } = false;

    async void TestPauseResume()
    {
        while (!isPauseKeyDown)
        {
            await Task.Yield();

            if (isDestory)
            {
                return;
            }
        }

        isPauseKeyDown = false;
        
        //if (Input.GetKeyDown(KeyCode.X))
        {
            if (!isPause)
            {
                if (GameTimer.toSec > 5)
                {
                    //isNvStop = true;
                    GameManager.isNvStop = true;

                    await Task.Delay(1000);

                    GameTimer.Stop();
                    RPC_SetPause(!isPause ? 1 : 0);                    
                    RPC_Active_SettingPanel(true ? 1 : 0);
                    RPC_Audio_Mute(true ? 1 : 0);

                    TestPauseResume();                    
                }
            }
            else
            {
                {
                    //isNvStop = false;
                    GameManager.isNvStop = false;

                    await Task.Yield();

                    GameTimer.Start();
                    RPC_SetPause(!isPause ? 1 : 0);                    
                    RPC_Active_SettingPanel(false ? 1 : 0);
                    RPC_Audio_Mute(false ? 1 : 0);

                    TestPauseResume();
                }
            }
        }
    }
    

    async void  TestTimeOut()
    {
        while (!GameTimer.isEnd)
        {
            await Task.Yield();

            if (isDestory)
            {
                return;
            }
        }

        if (!isPause)
        {
            GameManager.isNvStop = true;

            await Task.Delay(1000);

            //if (GameManager.bUpdate)
            //if(!isPause)
            {
                GameTimer.Stop();
                RPC_SetPause(true ? 1 : 0);                
                RPC_Active_EndPanel(true ? 1 : 0);
                RPC_Audio_Mute(true ? 1 : 0);
                RPC_Audio_BGM_Stop();
            }
        }

    }   

    async void TestPause()
    {
        {            
            GameManager.isNvStop = true;

            await Task.Delay(1000);

            if(isDestory)
            {
                return;
            }

            GameTimer.Stop();
            RPC_SetPause(true ? 1 : 0);
            RPC_Audio_Mute(true ? 1 : 0);
            RPC_Audio_BGM_Stop();
        }
    }

    

    [Rpc(RpcSources.StateAuthority, RpcTargets.All,
    Channel = RpcChannel.Unreliable, HostMode = RpcHostMode.SourceIsHostPlayer,
    InvokeLocal = true, InvokeResim = false, TickAligned = true)]
    void RPC_SendTimeInfo(int toSec, int cdMin, int cdSec)
    {
        GameTimer.toSec = toSec;
        GameTimer.cdMin = cdMin;
        GameTimer.cdSec = cdSec;
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All,
    Channel = RpcChannel.Unreliable, HostMode = RpcHostMode.SourceIsHostPlayer,
    InvokeLocal = true, InvokeResim = false, TickAligned = true)]
    void RPC_SendKillInfo(int k0, int k1)
    {
        GameManager.killedCount[0] = k0;
        GameManager.killedCount[1] = k1;
    }


    [Rpc(RpcSources.All, RpcTargets.All,
       Channel = RpcChannel.Reliable, HostMode = RpcHostMode.SourceIsHostPlayer,
       InvokeLocal = true, InvokeResim = false, TickAligned = true)]
    void RPC_SetPause(int value)
    {
        isPause = value == 1 ? true : false;
        GameManager.isPause = isPause;

        DebugInfo.Log($"GameRunManager : Pause : {isPause}");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority,
      Channel = RpcChannel.Reliable, HostMode = RpcHostMode.SourceIsHostPlayer,
      InvokeLocal = true, InvokeResim = false, TickAligned = true)]
    public void RPC_TestPause()
    {
        TestPause();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority,
     Channel = RpcChannel.Reliable, HostMode = RpcHostMode.SourceIsHostPlayer,
     InvokeLocal = true, InvokeResim = false, TickAligned = true)]
    void RPC_SetIsPauseKeyDown(int value)
    {
        isPauseKeyDown = value == 1 ? true : false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All,
    Channel = RpcChannel.Reliable, HostMode = RpcHostMode.SourceIsHostPlayer,
    InvokeLocal = true, InvokeResim = false, TickAligned = true)]
    void RPC_Active_SettingPanel(int value)
    {
        panel_Setting.SetActive(value == 1 ? true : false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All,
    Channel = RpcChannel.Reliable, HostMode = RpcHostMode.SourceIsHostPlayer,
    InvokeLocal = true, InvokeResim = false, TickAligned = true)]
    void RPC_Active_EndPanel(int value)
    {
        panel_End.SetActive(value == 1 ? true : false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All,
    Channel = RpcChannel.Reliable, HostMode = RpcHostMode.SourceIsHostPlayer,
    InvokeLocal = true, InvokeResim = false, TickAligned = true)]
    void RPC_Audio_Mute(int value)
    {
        AudioManager.MuteAll(value == 1 ? true : false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All,
    Channel = RpcChannel.Reliable, HostMode = RpcHostMode.SourceIsHostPlayer,
    InvokeLocal = true, InvokeResim = false, TickAligned = true)]
    void RPC_Audio_BGM_Stop()
    {
        AudioManager.instance.BGM_Stop();
    }

    //Test
    async void TestPauseResume0()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (!isPause)
            {
                //isNvStop = true;
                GameManager.isNvStop = true;

                await Task.Delay(1000);

                GameTimer.Stop();
                RPC_SetPause(!isPause ? 1 : 0);
            }
            else
            {
                //isNvStop = false;
                GameManager.isNvStop = false;

                await Task.Yield();

                GameTimer.Start();
                RPC_SetPause(!isPause ? 1 : 0);
            }
        }
    }

    async void TestPauseResume1()
    {
        while (!Input.GetKeyDown(KeyCode.X))
        {
            await Task.Yield();
        }


        //if (Input.GetKeyDown(KeyCode.X))
        {
            if (!isPause)
            {
                if (GameTimer.toSec > 5)
                {
                    //isNvStop = true;
                    GameManager.isNvStop = true;

                    await Task.Delay(1000);

                    GameTimer.Stop();
                    RPC_SetPause(!isPause ? 1 : 0);

                    TestPauseResume1();
                }
            }
            else
            {
                {
                    //isNvStop = false;
                    GameManager.isNvStop = false;

                    await Task.Yield();

                    GameTimer.Start();
                    RPC_SetPause(!isPause ? 1 : 0);

                    TestPauseResume1();
                }
            }
        }
    }

    async void TestPauseResume2()
    {
        while (!Input.GetKeyDown(KeyCode.X))
        {
            await Task.Yield();
        }



        //if (Input.GetKeyDown(KeyCode.X))
        {
            if (!isPause)
            {
                if (GameTimer.toSec > 5)
                {
                    //isNvStop = true;
                    //GameManager.isNvStop = true;
                    RPC_SetNvStop(true ? 1 : 0);

                    await Task.Delay(1000);

                    //GameTimer.Stop();
                    RPC_TimeStop();
                    RPC_SetPause(!isPause ? 1 : 0);

                    TestPauseResume2();
                }
            }
            else
            {
                {
                    //isNvStop = false;
                    //GameManager.isNvStop = false;
                    RPC_SetNvStop(false ? 1 : 0);

                    await Task.Yield();

                    //GameTimer.Start();
                    RPC_TimeStart();
                    RPC_SetPause(!isPause ? 1 : 0);

                    TestPauseResume2();
                }

            }

            //TestPauseResume();
        }
    }

    public async void TestPause0()
    {
        {
            RPC_SetNvStop(true ? 1 : 0);

            await Task.Delay(1000);

            RPC_TimeStop();
            RPC_SetPause(true ? 1 : 0);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority,
      Channel = RpcChannel.Reliable, HostMode = RpcHostMode.SourceIsHostPlayer,
      InvokeLocal = true, InvokeResim = false, TickAligned = true)]
    void RPC_SetNvStop(int value)
    {
        isNvStop = value == 1 ? true : false;
        GameManager.isNvStop = isNvStop;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority,
     Channel = RpcChannel.Reliable, HostMode = RpcHostMode.SourceIsHostPlayer,
     InvokeLocal = true, InvokeResim = false, TickAligned = true)]
    void RPC_TimeStop()
    {
        GameTimer.Stop();
    }


    [Rpc(RpcSources.All, RpcTargets.StateAuthority,
     Channel = RpcChannel.Reliable, HostMode = RpcHostMode.SourceIsHostPlayer,
     InvokeLocal = true, InvokeResim = false, TickAligned = true)]
    void RPC_TimeStart()
    {
        GameTimer.Start();
    }

}


class GameTimer
{
    public static Stopwatch timer;

    public static float eTime;
    public static int sec;
    public static int min;

    public static int inSec = 15;
    public static int inMin = 0;

    public static int toSec = 0;
    public static int cdSec = 0;
    public static int cdMin = 0;

    static GameTimer()
    {
        timer = new Stopwatch();
    }

    public static void Run()
    {
        eTime = (float)(timer.ElapsedMilliseconds) / 1000.0f;
        sec = (int)eTime;
        min = (int)sec / 60;

        toSec = inMin * 60 + inSec - sec;
        cdMin = toSec / 60;
        cdSec = toSec % 60;
    }

    public static void SetTime(int min, int sec)
    {
        inMin = min;
        inSec = sec;
    }

    public static void Reset()
    {
        timer.Reset();
        Run();
    }

    public static void Restart()
    {
        timer.Restart();
        Run();
    }

    public static void Stop()
    {
        timer.Stop();
        Run();
    }

    public static void Start()
    {
        timer.Start();
        Run();
    }

    public static bool isEnd
    {
        get
        {
            if (toSec <= 0)
            {
                return true;
            }

            return false;
        }
    }

    public static bool isRunning
    {
        get
        {
            return timer.IsRunning;
        }
    }
}
