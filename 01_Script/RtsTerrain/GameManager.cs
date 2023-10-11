using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Unity.Mathematics;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

using Fusion;
using Fusion.Sockets;
using System;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("LobbyRoom")]
    public int2 gameTime;    
    public Canvas[] canvas;
    Canvas canvas_now;
    Canvas canvas_lobby;
    Canvas canvas_room;
    Canvas canvas_play;

    //public Button btJoinLobby;
    Button btCreateRoom;
    Button btPlay;
    public Button[] btDisConnect;

    Button btJoinRandomMatch;
    Button btSingleSimulation;

    public GameObject prefab_roomItem;    
    public GameObject prefab_playerItem;

    public Canvas canvas_shutdown;
    public Text text_shutdown;

    RectTransform content_lobby;
    RectTransform content_player;

    Dictionary<string, SessionProperty> roomProperty;
    Dictionary<string, SessionInfo> dic_Room;
    
    Dictionary<PlayerRef, PlayerItem> dic_pItem;

    byte[] data;
    PoolManager pool_RoomItem;
    PoolManager pool_PlayerItem;

    [HideInInspector]
    public NetworkRunner runner = null;

    LobbyManager lobbyMan;
    RoomManager roomMan;

    enum MatchState : int
    {       
        Lobby = 0,
        Room = 1,
        Play = 2,
    }

    MatchState state = 0;

    [Header("Play")]
    public LightManager _lgihtMan;
    public CSM_Action _csm_action;
    public CBM_Action _cbm_action;
    public TerrainManager _terrainMan;
   
    public DeferredCullManager _dfCullMan;
    public DeferredRenderManager _dfMan;
    public SkyBoxManager _skyboxMan;
    public DecalManager _decalMan;

    public UnitManager[] _unitMan;
    public ArrowManager _arrowMan;
    public TargetManager _targetMan;
    public SelectManager _selectMan;
    public TorusManager _torusMan;
    public HpbarManager _hpbarMan;
    public CullManager _cullMan;
    public UIManager _uiMan;
    public AudioManager _audioMan;

    public GameObject prefab_ArrowMan;
    public GameObject[] prefab_PlayerMan;
    public GameObject prefab_GameRunMan;
    

    public int[] _unitCounts;

    public int[] _playerNum;
    public float[] _viewRadius;
    public float[] _attackRadius;
    public Color[] _playerColor;
    public int[] _killedCount;
    bool[] _activeData;
    int[] _selectData;
    public float[] _maxHp;
    public float[] _hitHp;
    //public float[] _audioCull;

    public float4 _unitTessInfo;

    public static int cullTestMode { get; set; }

    public static LightManager lightMan
    {
        get; set;
    }

    public static CSM_Action csm_action
    {
        get; set;
    }

    public static CBM_Action cbm_action
    {
        get; set;
    }

    public static TerrainManager terrainMan
    {
        get; set;
    }

    public static SkyBoxManager skyboxMan
    {
        get; set;
    }    

    public static DeferredCullManager dfCullMan
    {
        get; set;
    }

    public static DeferredRenderManager dfMan
    {
        get; set;
    }

    public static DecalManager decalMan
    {
        get; set;
    }


    public static PlayerManager[] playerMan
    {
        get; set;
    }

    public static UnitManager[] unitMan
    {
        get; set;
    }

    public static GameRunManager gameRunMan 
    { 
        get; set; 
    }

    public static ArrowManager arrowMan
    {
        get; set;
    }

    public static TargetManager targetMan
    {
        get; set;
    }

    public static SelectManager selectMan
    {
        get; set;
    }

    public static TorusManager torusMan
    {
        get; set;
    }

    public static HpbarManager hpbarMan
    {
        get; set;
    }

    public static CullManager cullMan
    {
        get; set;
    }

    public static UIManager uiMan
    {
        get; set;
    }

    public static AudioManager audioMan
    {
        get; set;
    }


    public static int[] unitCounts
    {
        get; private set;
    }
    public static int unitCount
    {
        get; set;
    }

    public static bool[] activeData
    {
        get; set;
    }

    public static int[] selectData
    {
        get; private set;
    }

    public static int[] has_input_Data
    {
        get; private set;
    }

    public static int[] stateData
    {
        get; set;
    }

    public static float4[] terrainArea
    {
        get; private set;
    }

    public static float3[] targetPos
    {
        get; private set;
    }

    public static float3[] refTargetPos
    {
        get; private set;
    }

    public static float4[] minDist
    {
        get; private set;
    }


    public float4[] _minDist;

    public static int2[] baseCount
    {
        get; set;
    }

    public int2[] _baseCount;

    public static UnitActor[] unitActors
    {
        get; set;
    }

    public static Transform[] unitTrs
    {
        get; set;
    }

    public static NetworkTransform[] unitNtTrs
    {
        get; set;
    }

    public static int[] playerNum
    {
        get; set;
    }

    public static Color[] playerColor
    {
        get; private set;
    }

    public static int[] killedCount
    {
        get; set;
    }

    public static float[] viewRadius
    {
        get; set;
    }

    public static float[] attackRadius
    {
        get; set;
    }

    public static float[] viewRadiusDef
    {
        get; set;
    }

    public static float[] attackRadiusDef
    {
        get; set;
    }

    public static float[] maxHp
    {
        get; set;
    }

    public static float[] hp
    {
        get; private set;
    }

    public static float[] hitHp
    {
        get; set;
    }

    public static float[] refHp
    {
        get; set;
    }

    public static float4 unitTessInfo
    {
        get; set;
    }

    public static float[] audioCull
    {
        get; set;
    }


    public static ROBuffer<int> active_Buffer
    {
        get; set;
    }

    public static ROBuffer<int> select_Buffer
    {
        get; set;
    }

    public static ROBuffer<int> has_input_Buffer
    {
        get; set;
    }

    public static ROBuffer<int> state_Buffer
    {
        get; set;
    }

    public static RWBuffer<float4> terrainArea_Buffer
    {
        get; set;
    }

    public static RWBuffer<float3> targetPos_Buffer
    {
        get; set;
    }

    public static RWBuffer<float3> refTargetPos_Buffer
    {
        get; set;
    }

    public static RWBuffer<float4> minDist_Buffer
    {
        get; set;
    }

    public static COBuffer<Color> playerColor_Buffer
    {
        get; set;
    }

    public static RWBuffer<float> refHp_Buffer
    {
        get; set;
    }

    public static RWBuffer<float> audioCull_Buffer
    {
        get; set;
    }


    //
    public bool[] _bColDebug;
    public static bool[] bColDebug
    {
        get; set;
    }

   
    public static bool bUpdate
    {
        get; set;
    } = false;

    //public NetworkRunner runner = null;

    public static GameManager instance;

    void Awake()
    {
        AwakeRoom();        
    }

    void Start()
    {
        StartRoom();       
    }

    public static bool isNvStop { get; set; } = false;

    public static bool isPause { get; set; } = false;

    void Update()
    {
        //if(runner != null)
        //{
        //    if(runner.GameMode == GameMode.Single)
        //    {
        //        if(Input.GetKeyDown(KeyCode.X))
        //        {
        //            if(!isPause)
        //            {
        //                //runner.SinglePlayerPause(isPause);
        //                isNvStop = true;
        //              
        //                await Task.Delay(1000);
        //
        //                isPause = !isPause;
        //            }
        //            else
        //            {
        //                isNvStop = false;
        //
        //                await Task.Yield();
        //
        //                isPause = !isPause;                                                
        //            }                    
        //        }                
        //    }
        //}

        if(!bUpdate)
        {
            return;
        }

        if (unitCount > 0)
        {
            {
                for (int i = 0; i < unitCount; i++)
                {
                    active_Buffer.data[i] = activeData[i] ? 1 : 0;
                }
                active_Buffer.Write();
            }

            {
                select_Buffer.Write();
                state_Buffer.Write();
            }
        }


        {
            if(Input.GetKeyDown(KeyCode.F))
            {
                CamAction.useMouseForMove = !CamAction.useMouseForMove;
            }
        }

#if UNITY_EDITOR
        {
            if (Input.GetKeyDown(KeyCode.Home))
            {
                cullTestMode = (++cullTestMode) % 3;
            }
        }

#endif
    }

    private void AwakePlay()
    {      

        //{
        //    instance = this;            
        //}

        {
            unitCounts = _unitCounts;
            baseCount = new int2[unitCounts.Length];

            unitCount = 0;
            for (int i = 0; i < unitCounts.Length; i++)
            {
                baseCount[i].x = (i < 1 ? 0 : baseCount[i - 1].x + baseCount[i - 1].y);
                baseCount[i].y = unitCounts[i];

                unitCount += unitCounts[i];
            }

            _baseCount = baseCount;

            playerMan = new PlayerManager[2];

            lightMan = _lgihtMan;
            csm_action = _csm_action;
            cbm_action = _cbm_action;
            terrainMan = _terrainMan;

            decalMan = _decalMan;            
            skyboxMan = _skyboxMan;
            
            dfCullMan = _dfCullMan;
            dfMan = _dfMan;


            unitMan = _unitMan;
            //arrowMan = _arrowMan;
            selectMan = _selectMan;
            targetMan = _targetMan;
            torusMan = _torusMan;
            hpbarMan = _hpbarMan;
            cullMan = _cullMan;
            uiMan = _uiMan;
            audioMan = _audioMan;
        }

        if (unitCount > 0)
        {
            {
                unitActors = new UnitActor[unitCount];
                unitTrs = new Transform[unitCount];
                unitNtTrs = new NetworkTransform[unitCount];
            }

            {
                playerNum = _playerNum;

                viewRadiusDef = _viewRadius;
                attackRadiusDef = _attackRadius;

                viewRadius = new float[unitCount];
                attackRadius = new float[unitCount];
            }

            {
                active_Buffer = new ROBuffer<int>(unitCount);
                activeData = _activeData = new bool[unitCount];
                for (int i = 0; i < unitCount; i++)
                {
                    activeData[i] = true;
                    //activeData[i] = false;

                    //if(i % 2 == 0)
                    //{
                    //    activeData[i] = true;
                    //}
                    //else
                    //{
                    //    activeData[i] = false;
                    //}
                }
            }

            {
                state_Buffer = new ROBuffer<int>(unitCount);
                stateData = state_Buffer.data;

                for (int i = 0; i < unitCount; i++)
                {
                    stateData[i] = 0;
                    //stateData[i] = 4;

                    //if (i % 2 == 0)
                    //{
                    //    stateData[i] = 0;
                    //}
                    //else
                    //{
                    //    stateData[i] = 4;
                    //}
                }
            }

            {
                select_Buffer = new ROBuffer<int>(unitCount);
                _selectData = selectData = select_Buffer.data;
            }

            {
                has_input_Buffer = new ROBuffer<int>(unitCount);
                has_input_Data = has_input_Buffer.data;
            }

            {
                terrainArea_Buffer = new RWBuffer<float4>(unitCount);
                terrainArea = terrainArea_Buffer.data;
            }

            {
                targetPos_Buffer = new RWBuffer<float3>(unitCount);
                targetPos = targetPos_Buffer.data;
            }

            {
                refTargetPos_Buffer = new RWBuffer<float3>(unitCount);
                refTargetPos = refTargetPos_Buffer.data;
            }

            {
                minDist_Buffer = new RWBuffer<float4>(unitCount);
                minDist = minDist_Buffer.data;

                //Debug
                _minDist = minDist;
            }

            {
                maxHp = _maxHp;
                hitHp = _hitHp;
                hp = new float[unitCount];
            }

            //{
            //    killedCount = _killedCount;
            //}

            {
                playerColor = _playerColor;
                playerColor_Buffer = new COBuffer<Color>(_playerColor.Length);
                playerColor_Buffer.data = playerColor;
                playerColor_Buffer.Write();
            }

            {
                refHp_Buffer = new RWBuffer<float>(unitCount);
                refHp = refHp_Buffer.data;
            }

            {
                unitTessInfo = _unitTessInfo;
            }


            {
                bColDebug = _bColDebug;
            }
        }

        {
            {
                killedCount = _killedCount;
            }
        }


        //{
        //    StartCoroutine(UpdateRoutine());
        //}


        ////Init()
        //{
        //    csm_action.Init();
        //    terrainMan.Init();
        //
        //    for (int i = 0; i < unitMan.Length; i++)
        //    {
        //        unitMan[i].Init_Fusion(unitCounts[i]);
        //    }
        //}
        //

        //{
        //    uiMan.Init();
        //    uiMan.Begin();
        //}

        //{
        //    if(btRamdomMatch != null)
        //    {
        //        btRamdomMatch.onClick.AddListener(
        //            () =>
        //            {
        //                StartGame(GameMode.AutoHostOrClient);
        //
        //                btRamdomMatch.gameObject.SetActive(false);
        //                btSingleMode.gameObject.SetActive(false);
        //            });
        //    }
        //
        //    if (btSingleMode != null)
        //    {
        //        btSingleMode.onClick.AddListener(
        //            () =>
        //            {
        //                StartGame(GameMode.Single);
        //
        //                btRamdomMatch.gameObject.SetActive(false);
        //                btSingleMode.gameObject.SetActive(false);
        //            });
        //    }
        //
        //    if (btShutDown != null)
        //    {
        //        btShutDown.onClick.AddListener(
        //            () =>
        //            {
        //                ShutDown();
        //            });
        //    }
        //}

        {
            //if (runner == null)
            //{
            //    runner = gameObject.AddComponent<NetworkRunner>();
            //}
        }

    }   


    #region LoobyRoom

    void AwakeRoom()
    {
        {
            instance = this;            
        }

        {
            canvas_now = canvas[0];

            canvas_lobby = canvas[0];
            canvas_room = canvas[1];
            canvas_play = canvas[2];
        }

        {
            lobbyMan = canvas_lobby.GetComponent<LobbyManager>();
            roomMan = canvas_room.GetComponent<RoomManager>();
        }

        {
            content_lobby = lobbyMan.content_lobby;
            content_player = roomMan.content_player;
        }

        {
            btCreateRoom = lobbyMan.btCreateRoom;
            btSingleSimulation = lobbyMan.btSingleSimulation;
        }

        {
            btPlay = roomMan.btPlay;
        }


        //{
        //    if (btJoinLobby != null)
        //    {
        //        btJoinLobby.onClick.AddListener(
        //            () =>
        //            {
        //                JoinLobby();
        //            });
        //    }
        //}

        {
            if (btCreateRoom != null)
            {
                btCreateRoom.onClick.AddListener(
                    () =>
                    {
                        {
                            //var lobbyMan = canvas[1].GetComponent<LobbyManager>();
                            
                            string roomName = lobbyMan.roomName.text;
                            //roomProperty.Add("password_host", SessionProperty.Convert(lobbyMan.password.text));

                            SessionProperty sp;
                            if (roomProperty.TryGetValue("password_host", out sp))
                            {
                                sp = SessionProperty.Convert(lobbyMan.password.text);
                            }
                            else
                            {
                                roomProperty.Add("password_host", SessionProperty.Convert(lobbyMan.password.text));
                            }


                            JoinRoom(GameMode.Host, roomName);
                        }
                    });
            }
        }

        {
            if (btPlay != null)
            {
                btPlay.onClick.AddListener(
                    () =>
                    {
                        if (runner.IsServer)
                        {
                            data[2] = 1;
                            foreach (var p in runner.ActivePlayers)
                            {
                                if (runner.LocalPlayer != p)
                                {
                                    runner.SendReliableDataToPlayer(p, data);
                                }
                            }
                        }

                        Play();
                    });
            }
        }

        {
            if (btDisConnect != null)
            {
                for (int i = 0; i < btDisConnect.Length; i++)
                {
                    if (btDisConnect[i] != null)
                    {
                        Button bt = btDisConnect[i];
                        btDisConnect[i].onClick.AddListener(
                        () =>
                        {
                            bt.interactable = false;
                            ShutDown("ShutDown");
                        });
                    }
                }
            }
        }

        {
            if (btJoinRandomMatch != null)
            {
                btJoinRandomMatch.onClick.AddListener(
                    () =>
                    {
                        JoinRamdomRoom();
                    });
            }
        }

        {
            if (btSingleSimulation != null)
            {
                btSingleSimulation.onClick.AddListener(
                    () =>
                    {
                        JoinSingleRoom();
                    });
            }
        }


        {
            roomProperty = new Dictionary<string, SessionProperty>();
            dic_Room = new Dictionary<string, SessionInfo>();

            SetUp_SearchRoom();
        }

        {
            //dic_Player = new Dictionary<PlayerRef, PlayerItem0>();
            //StartCoroutine(CheckAllReady());
        }

        {
            dic_pItem = new Dictionary<PlayerRef, PlayerItem>();
            StartCoroutine(CheckAllReady());
        }


        if (btPlay != null)
        {
            btPlay.interactable = false;
        }

        {
            pool_RoomItem = new PoolManager(prefab_roomItem, 50);
            //pool_PlayerItem = new PoolManager(prefab_playerItem, 2);
        }
    }

    void StartRoom()
    {
        data = new byte[10];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = 0;
        }

        //btJoinLobby.onClick.Invoke();

        {
            JoinLobby();
        }
    }

    IEnumerator UnloadSceneAsync()
    {
        Scene scene = SceneManager.GetActiveScene();
        yield return SceneManager.UnloadSceneAsync(scene);
    }

    IEnumerator LoadSceneAsync(int idx)
    {
        yield return SceneManager.LoadSceneAsync(idx);
    }

    IEnumerator ReturnLobbySceneAsync()
    {
        //Scene scene = SceneManager.GetActiveScene();

        yield return SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());

        yield return SceneManager.LoadSceneAsync(0);

        yield return SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());

        yield return SceneManager.LoadSceneAsync(1);

    }

    void ReturnLobbyScene()
    {        
        SceneManager.UnloadScene(SceneManager.GetActiveScene());

        SceneManager.LoadScene(0);

        SceneManager.UnloadScene(SceneManager.GetActiveScene());

        SceneManager.LoadScene(1);

    }

    void UnloadScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.UnloadScene(scene);
    }

    void LoadScene(int idx)
    {
        SceneManager.LoadScene(idx);
    }


   

    IEnumerator CheckAllReady()
    {
        int count = 0;

        while (true)
        {
            if (dic_pItem != null)
            {
                count = 0;
                foreach (var kv in dic_pItem)
                {
                    if (kv.Value.ready.isOn)
                    {
                        count++;
                    }
                }

                if (runner != null)
                {
                    if (runner.IsServer)
                    {
                        if (runner.SessionInfo != null)
                        {
                            if (btPlay != null)
                            {
                                if (count == runner.SessionInfo.MaxPlayers)
                                {
                                    btPlay.interactable = true;
                                }
                                else
                                {
                                    btPlay.interactable = false;
                                }
                            }
                        }
                    }
                }
            }


            yield return null;
        }
    }

    void SetUp_SearchRoom()
    {
        //var lobbyMan = canvas[1].GetComponent<LobbyManager>();
        var search_content = lobbyMan.search_content;
        var search_input = lobbyMan.search_input;

        if (dic_Room != null)
        {
            search_input.onSubmit.AddListener(
                (name) =>
                {
                    DebugInfo.Log($"search_input.onSubmit() // name : {name}");

                    for (int i = 0; i < search_content.childCount; i++)
                    {
                        Transform child = search_content.GetChild(i);
                        child.gameObject.SetActive(false);
                        GameObject.Destroy(child.gameObject);
                    }

                    SessionInfo session;
                    if (dic_Room.TryGetValue(name, out session))
                    {
                        //if (session.Properties["isRandomHostOrClient"] == 1)
                        //{
                        //    return;
                        //}

                        DebugInfo.Log($"dic_Room.TryGetValue() : true // name : {name}");


                        GameObject item = GameObject.Instantiate(prefab_roomItem);
                        item.transform.parent = search_content.transform;
                        RoomItem sItem = item.GetComponent<RoomItem>();

                        {
                            RectTransform rtItem = item.transform as RectTransform;
                            float y0 = 0.0f;
                            float y1 = 1.0f;
                            rtItem.anchorMin = new Vector2(0.0f, y0);
                            rtItem.anchorMax = new Vector2(1.0f, y1);
                            rtItem.pivot = new Vector2(0.5f, 0.5f);
                            rtItem.offsetMin = new Vector2(+10.0f, +10.0f);
                            rtItem.offsetMax = new Vector2(-10.0f, -10.0f);
                        }

                        {
                            sItem.Name.text = session.Name;
                            sItem.PlayerCount.text = $"{session.PlayerCount} / {session.MaxPlayers}";
                            sItem.btJoin.interactable = session.PlayerCount < session.MaxPlayers ? true : false;
                            sItem.password.interactable = session.Properties["password_host"] == "" ? false : true;

                            sItem.btJoin.onClick.AddListener(
                                () =>
                                {
                                    //DebugInfo.Log($"password0 : {runner.SessionInfo.Properties["password"]}");
                                    //DebugInfo.Log($"password1 : {sItem.password.text}");
                                    //
                                    //if(runner.SessionInfo.Properties["password"].ToString() == sItem.password.text)

                                    SessionProperty sp;
                                    if (roomProperty.TryGetValue("password_client", out sp))
                                    {
                                        sp = SessionProperty.Convert(sItem.password.text);
                                    }
                                    else
                                    {
                                        roomProperty.Add("password_client", SessionProperty.Convert(sItem.password.text));
                                    }

                                    //roomProperty.Add("password_client", SessionProperty.Convert(sItem.password.text));

                                    {
                                        JoinRoom(GameMode.Client, session.Name);
                                    }
                                });
                        }
                    }
                });
        }
    }


    async void JoinLobby()
    {
        if (runner == null)
        {
            runner = gameObject.AddComponent<NetworkRunner>();
        }

        //DontDestroyOnLoad(gameObject);        

        //while(!runner.IsShutdown)
        //{
        //    await Task.Yield();            
        //}

        //await Task.Delay(2000);

        //{
        //    Fusion.Photon.Realtime.AuthenticationValues avalue = new Fusion.Photon.Realtime.AuthenticationValues();
        //    avalue.AuthType = Fusion.Photon.Realtime.CustomAuthenticationType.Steam;
        //}

        //{
        //    Fusion.Photon.Realtime.AppSettings appId;
        //    appId.
        //}

        while(!runner.IsShutdown)
        {
            await Task.Yield();
        }


        //canvas[0].gameObject.SetActive(false);
        var result = await runner.JoinSessionLobby(SessionLobby.Custom, "Lobby");         
        
       
        if(result.Ok)
        {
            canvas_lobby.gameObject.SetActive(true);
            canvas_now = canvas_lobby;

            state = MatchState.Lobby;

            DebugInfo.Log($"JoinSessionLobby()// region : {runner.LobbyInfo.Region}");
        }
        else
        {
            ShutDown("ShutDown");
        }
        
    }

    async void JoinRoom(GameMode mode, string sessionName)
    {
        DebugInfo.Log("CreateRoom()");

        canvas_lobby.gameObject.SetActive(false);

        if (mode == GameMode.Host)
        {
            foreach (var kv in dic_Room)
            {
                if (kv.Value.Name == sessionName)
                {
                    ShutDown("RoomName is repeated");
                    return;
                    //goto End;
                }
            }
        }

        //roomProperty.Add("isRandomHostOrClient", SessionProperty.Convert(0));
                
        runner.ProvideInput = true;
        var result = await runner.StartGame(new StartGameArgs()
        {

            //GameMode = GameMode.AutoHostOrClient,
            GameMode = mode,
            PlayerCount = 2,
            SessionName = sessionName,
            IsVisible = true,
            IsOpen = true,
            SessionProperties = roomProperty,
            Initialized = (runner) => { if (!runner.IsRunning) { ShutDown("ShutDown"); } }
        });        
        

        if(result.Ok)
        {
            if (runner.IsClient)
            {
                if (runner.SessionInfo.Properties["password_host"].ToString() != runner.SessionInfo.Properties["password_client"].ToString())
                {
                    ShutDown("password is wrong");
                    DebugInfo.Log("password is wrong");

                    return;
                }
            }

            if (runner.IsRunning)
            {
                canvas_room.gameObject.SetActive(true);
                canvas_now = canvas_room;
                
                roomMan.RoomName.text = runner.SessionInfo.Name;
                roomMan.Host_Client_Name.text = runner.IsServer ? "Host" : "Client";

                state = MatchState.Room;
            }
            else
            {
                ShutDown("ShutDown");

                //UnloadScene();
                //LoadScene(0);
            }
        }
        else
        {
            ShutDown(result.ShutdownReason.ToString());
            
            //UnloadScene();
            //LoadScene(0);
        }

       
        //else
        //{
        //    canvas[2].gameObject.SetActive(true);
        //    canvas_now = canvas[2];
        //}
                                

        //if(runner.State == 0)
        //{
        //    ShutDown("ShutDown");
        //}

        //else
        //{
        //    canvas[1].gameObject.SetActive(true);
        //}

        //state = MatchState.Room;

        //DebugInfo.Log($"password_host : {runner.SessionInfo.Properties["password_host"]}");
        //DebugInfo.Log($"password_client : {runner.SessionInfo.Properties["password_client"]}");

        //End:;
    }

  
    async void JoinRamdomRoom()
    {
        DebugInfo.Log("CreateRoom()");

        canvas_lobby.gameObject.SetActive(false);

        roomProperty.Add("isRandomHostOrClient", SessionProperty.Convert(1));

        runner.ProvideInput = true;
        var result = await runner.StartGame(new StartGameArgs()
        {

            GameMode = GameMode.AutoHostOrClient,
            PlayerCount = 2,
            //SessionName = sessionName,
            //IsVisible = false,
            //IsOpen = false,
            //SessionProperties = roomProperty
            //Initialized = ServerInit
        });

        if (result.Ok)
        {
            if (runner.IsRunning)
            {
                //var roomMan = canvas[2].GetComponent<RoomManager>();

                //RamdomReady();

                canvas_play.gameObject.SetActive(true);
                canvas_now = canvas_play;

                state = MatchState.Play;

                //if(runner.IsServer)
                {
                    Play();
                }
            }
            else
            {
                ShutDown("ShutDown");
            }
        }
        else
        {
            ShutDown(result.ShutdownReason.ToString());
        }

       

        //else
        //{
        //    canvas[1].gameObject.SetActive(true);
        //}

        //state = MatchState.Room;

        //DebugInfo.Log($"password_host : {runner.SessionInfo.Properties["password_host"]}");
        //DebugInfo.Log($"password_client : {runner.SessionInfo.Properties["password_client"]}");

        //End:;
    }

    async void JoinSingleRoom()
    {
        DebugInfo.Log("CreateRoom()");

        canvas_lobby.gameObject.SetActive(false);

        //roomProperty.Add("isRandomHostOrClient", SessionProperty.Convert(0));

        runner.ProvideInput = true;
        var result = await runner.StartGame(new StartGameArgs()
        {

            GameMode = GameMode.Single,
            PlayerCount = 2,
            //SessionName = sessionName,
            IsVisible = false,
            IsOpen = false,
            SessionProperties = roomProperty
            //Initialized = ServerInit
        });

        if (result.Ok)
        {
            if (runner.IsRunning)
            {
                canvas_play.gameObject.SetActive(true);
                canvas_now = canvas_play;

                state = MatchState.Play;

                Play();
            }
            else
            {
                ShutDown("ShutDown");
            }
        }
        else
        {
            ShutDown(result.ShutdownReason.ToString());
        }

        
        //else
        //{
        //    canvas[1].gameObject.SetActive(true);
        //}

        //state = MatchState.Room;

        //DebugInfo.Log($"password_host : {runner.SessionInfo.Properties["password_host"]}");
        //DebugInfo.Log($"password_client : {runner.SessionInfo.Properties["password_client"]}");

        //End:;
    }

    void Play()
    {
        canvas_room.gameObject.SetActive(false);

        canvas_play.gameObject.SetActive(true);
        canvas_now = canvas_play;

        state = MatchState.Play;

        {
            AwakePlay();
            ServerInit(runner);
        }
    }

    public async void ShutDown(string message)
    {
        if(gameRunMan != null)
        {
            //GameManager.gameRunMan.TestPause();
            GameManager.gameRunMan.RPC_TestPause();
        }
        
        
        //canvas[3].gameObject.SetActive(false);
        if (canvas_now != null)
        {
            canvas_now.gameObject.SetActive(false);
        }

        //foreach(var c in canvas)
        //{
        //    c.gameObject.SetActive(false);
        //}
        if (runner != null)
        {
            runner.IsVisible = false;
        }


        if(canvas_shutdown != null)
        {
            canvas_shutdown.gameObject.SetActive(true);
        }

        if(text_shutdown != null)
        {
            text_shutdown.text = message;
        }               

        await Task.Delay(2000);


        if (runner != null)
        {
            //runner.IsVisible = false;

            if (runner.IsRunning)
            {
                await runner.Shutdown();

                //{
                //    StartCoroutine(UnloadSceneAsync());
                //    StartCoroutine(LoadSceneAsync(0));
                //}
            }
            else
            {
                await Task.Yield();

                {
                    StartCoroutine(UnloadSceneAsync());
                    StartCoroutine(LoadSceneAsync(0));
                }

                {
                    //StartCoroutine(ReturnLobbySceneAsync());
                    //ReturnLobbyScene();
                }

                //if(state == MatchState.Room || state == MatchState.Play)
                //{
                //    ReturnLobbyScene();
                //}
                //else
                //{
                //    UnloadScene();
                //    LoadScene(0);
                //}
            }
        }
        //else
        //{
        //    await Task.Yield();
        //
        //    {
        //        StartCoroutine(UnloadSceneAsync());
        //        StartCoroutine(LoadSceneAsync(0));
        //    }
        //}

    }

  


    public void AddPlayerItem(PlayerItem item)
    {
        item.transform.parent = content_player.transform;
        dic_pItem.Add(item.Object.InputAuthority, item);

        int i = order_pItem;
        {
            RectTransform rtItem = item.transform as RectTransform;
            float y0 = 1.0f - 0.5f * (float)(i + 1);
            float y1 = 1.0f - 0.5f * (float)(i + 0);
            rtItem.anchorMin = new Vector2(0.0f, y0);
            rtItem.anchorMax = new Vector2(1.0f, y1);
            rtItem.pivot = new Vector2(0.5f, 0.5f);
            rtItem.offsetMin = new Vector2(+10.0f, +10.0f);
            rtItem.offsetMax = new Vector2(-10.0f, -10.0f);
            rtItem.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        }
        order_pItem++;
    }

    public void RemovePlayerItem(PlayerItem item)
    {
        item.transform.parent = null;
        dic_pItem.Remove(item.Object.InputAuthority);

        order_pItem--;
    }

    public void ResetReady()
    {
        foreach (var kv in dic_pItem)
        {
            kv.Value.ready.isOn = false;
        }
    }

    int order_pItem = 0;


    #endregion


    private void OnGUI()
    {
        //if (runner == null)
        //{
        //    if (GUI.Button(new Rect(300, 40, 200, 40), "Ramdom Match"))
        //    {
        //        StartGame(GameMode.AutoHostOrClient);
        //    }
        //
        //    if (GUI.Button(new Rect(300, 80, 200, 40), "Single Mode"))
        //    {
        //        StartGame(GameMode.Single);
        //    }
        //}
        //else
        //{
        //    if (GUI.Button(new Rect(300, 120, 200, 40), "ShutDown"))
        //    {
        //        ShutDown();
        //    }
        //}
    }

   

    private void OnApplicationPause(bool pause)
    {
        //DebugInfo.Log($"OnApplicationPause : {pause}");
    }

    private void OnApplicationFocus(bool focus)
    {
        //DebugInfo.Log($"OnApplicationFocus : {focus}");
    }



   

    NetworkObject[] unit0;
    NetworkObject[] unit1;

    public Text text_dTime;

  

    void ServerInit0(NetworkRunner runner)
    {
        DebugInfo.Log("ServerInit()");

        {
            csm_action.Init();
            terrainMan.Init();

            for (int i = 0; i < unitMan.Length; i++)
            {
                unitMan[i].unitIdx = i;
                unitMan[i].Init_Fusion(unitCounts[i]);
            }
        }

        if (runner.IsServer)
        {
            DebugInfo.Log("ServerInit() : runner.IsServer");

            for (int i = 0; i < prefab_PlayerMan.Length; i++)
            {
                runner.Spawn(prefab_PlayerMan[i]);
            }

            {
                runner.Spawn(prefab_GameRunMan);
            }

            {
                runner.Spawn(prefab_ArrowMan);
            }

            for (int i = 0; i < unitMan.Length; i++)
            {
                unitMan[i].Spawn_Fusion(runner);
            }          
        }

        StartCoroutine(UnitSpawn_Post());

        //{
        //    StartCoroutine(Update_dTime());
        //}
    }

    void ServerInit(NetworkRunner runner)
    {
        DebugInfo.Log("ServerInit()");

        {
            lightMan.Init();
            //csm_action.Init();
            //cbm_action.Init();
            terrainMan.Init();

            for (int i = 0; i < unitMan.Length; i++)
            {
                unitMan[i].unitIdx = i;
                unitMan[i].Init_Fusion(unitCounts[i]);
            }                     
            
            //dfCullMan.Init();
            //dfMan.Init();

            //bUpdate = true;
        }

        if (runner.IsServer)
        {
            DebugInfo.Log("ServerInit() : runner.IsServer");
        
            for (int i = 0; i < prefab_PlayerMan.Length; i++)
            {
                runner.Spawn(prefab_PlayerMan[i]);
            }
        
            {
                runner.Spawn(prefab_GameRunMan);
            }
        
            {
                runner.Spawn(prefab_ArrowMan);
            }
        
            for (int i = 0; i < unitMan.Length; i++)
            {
                unitMan[i].Spawn_Fusion(runner);
            }
        }
        
        StartCoroutine(UnitSpawn_Post());

        //{
        //    StartCoroutine(Update_dTime());
        //}
    }

    IEnumerator UnitSpawn_Post()
    {
        int count = 0;
        int maxCount = unitCount;
        while (count < maxCount)
        {
            if (runner != null)
            {
                var units = runner.GetAllBehaviours<UnitActor>().ToArray();                               

                if (units != null)
                {
                    count = 0;
                    foreach (var p in units)
                    {
                        count++;
                    }
                }
            }
            DebugInfo.Log("count < maxCount");

            yield return null;
        }

        count = 0;
        maxCount = 1;
        while (count < maxCount)
        {
            if (runner != null)
            {
                var runMans = runner.GetAllBehaviours<GameRunManager>().ToArray();

                if (runMans != null)
                {
                    count = 0;
                    foreach (var m in runMans)
                    {
                        GameManager.gameRunMan = m;

                        count++;
                        break;
                    }
                }
            }
            DebugInfo.Log("count < maxCount");

            yield return null;
        }

        count = 0;
        maxCount = 1;
        while (count < maxCount)
        {
            if (runner != null)
            {
                var arrowMans = runner.GetAllBehaviours<ArrowManager>().ToArray();

                if (arrowMans != null)
                {                    
                    count = 0;
                    foreach (var a in arrowMans)
                    {
                        GameManager.arrowMan = a;                                                

                        count++;
                        break;
                    }
                }
            }
            DebugInfo.Log("count < maxCount");

            yield return null;
        }

        count = 0;
        maxCount = 2;
        while (count < maxCount)
        {
            if (runner != null)
            {
                var playerMans = runner.GetAllBehaviours<PlayerManager>().ToArray();
        
                if (playerMans != null)
                {
                    count = 0;
                    foreach (var p in playerMans)
                    {
                        GameManager.playerMan[count] = p;
        
                        count++;                        
                    }
                }
            }
            DebugInfo.Log("count < maxCount");
        
            yield return null;
        }


        {
            DebugInfo.Log("count == maxCount");

            {
                FindUnit();
            }
                       
            for (int i = 0; i < unitMan.Length; i++)
            {
                unitMan[i].Spawn_Fusion_Post(runner, i);
            }

            {
                Init();
                Enable();
                Begin();
            }

            if(runner.GameMode == GameMode.Single)
            {
                StartCoroutine(AssignPlayer_Single());
            }
            else
            {
                StartCoroutine(AssignPlayer());
            }
                                   
            yield return null;                            
        }
    }

    IEnumerator AssignPlayer()
    {       
        int count = 0;
        PlayerRef[] players = new PlayerRef[2];

        {
            while (count < 2)
            //while (count < 1)
            {
                count = 0;
                foreach (var p in runner.ActivePlayers)
                {
                    players[count] = p;
                    count++;
                }

                if(count == 2)
                {
                    int a = 0;
                }

                yield return null;
            }
        }

        if (runner.IsServer)
        {
            DebugInfo.Log("AssignPlayer()");

            for (int i = 0; i < 4; i++)
            {
                var uMan = unitMan[i];
                int c = uMan.count;
                for (int j = 0; j < c; j++)
                {
                    uMan.units[j].GetComponent<NetworkObject>().AssignInputAuthority(players[0]);
                }

                //if(c > 0)
                //{
                //    uMan.player = uMan.units[0].GetComponent<NetworkObject>().InputAuthority;
                //}

            }

            for (int i = 4; i < 8; i++)
            {
                var uMan = unitMan[i];
                int c = uMan.count;
                for (int j = 0; j < c; j++)
                {
                    uMan.units[j].GetComponent<NetworkObject>().AssignInputAuthority(players[1]);
                }

                //if (c > 0)
                //{
                //    uMan.player = uMan.units[0].GetComponent<NetworkObject>().InputAuthority;
                //}

            }

            //for (int i = 0; i < 8; i++)
            //{
            //    var uMan = unitMan[i];
            //    int c = uMan.count;
            //    for (int j = 0; j < c; j++)
            //    {
            //        var actor = uMan.units[j].GetComponent<UnitActor>();
            //        if(!actor.HasInputAuthority)
            //        {
            //            actor.bodyCollider.enabled = false;
            //            actor.hitCollider.enabled = false;
            //        }                    
            //    }            
            //}

            for (int i = 0; i < 8; i++)
            {
                var uMan = unitMan[i];
                int c = uMan.count;

                if (c > 0)
                {
                    uMan.player = uMan.units[0].GetComponent<NetworkObject>().InputAuthority;
                }
            }

            for (int i = 0; i < playerMan.Length; i++)
            {
                playerMan[i].Object.AssignInputAuthority(players[i]);
                playerMan[i].player = playerMan[i].Object.InputAuthority;
            }

            {
                for (int i = 0; i < unitCount; i++)
                {
                    has_input_Data[i] = unitActors[i].HasInputAuthority ? 1 : 0;
                }

                if (unitCount > 0)
                {
                    has_input_Buffer.Write();
                }
            }

            for(int i = 0; i < playerMan.Length; i++)
            {
                playerMan[i].Set_Spawn_Button();
            }
        }

        if (runner.IsServer)
        {
            //for(int i = 0; i < playerMan.Length; i++)
            //{
            //    playerMan[i].Object.AssignInputAuthority(players[i]);
            //    playerMan[i].player = playerMan[i].Object.InputAuthority;
            //}            
        }
       


        if(runner.IsClient && !runner.IsServer)
        {
            int maxCount = unitCount;
            count = 0;
            while (count < maxCount)
            {
                if (runner != null)
                {
                    var units = runner.GetAllBehaviours<UnitActor>().ToArray();

                    if (units != null)
                    {
                        count = 0;
                        foreach (var unit in units)
                        {
                            if(unit.Object.InputAuthority != PlayerRef.None)
                            {
                                count++;
                            }                            
                        }
                    }
                }
               
                yield return null;
            }

            //for (int i = 0; i < 8; i++)
            //{
            //    var uMan = unitMan[i];                                
            //    uMan.player = uMan.units[0].GetComponent<NetworkObject>().InputAuthority;
            //}

            //for (int i = 0; i < 8; i++)
            //{
            //    var uMan = unitMan[i];
            //    int c = uMan.count;
            //    for (int j = 0; j < c; j++)
            //    {
            //        var actor = uMan.units[j].GetComponent<UnitActor>();
            //        if (!actor.HasInputAuthority)
            //        {
            //            actor.bodyCollider.enabled = false;
            //            actor.hitCollider.enabled = false;
            //        }                    
            //    }
            //}

            for (int i = 0; i < 8; i++)
            {
                var uMan = unitMan[i];
                int c = uMan.count;

                if (c > 0)
                {
                    uMan.player = uMan.units[0].GetComponent<NetworkObject>().InputAuthority;
                }
            }


            maxCount = 2;
            count = 0;
            while (count < maxCount)
            {
                if (runner != null)
                {
                    var playerMans = runner.GetAllBehaviours<PlayerManager>().ToArray();

                    if (playerMans != null)
                    {
                        count = 0;
                        foreach (var p in playerMans)
                        {
                            if (p.Object.InputAuthority != PlayerRef.None)
                            {
                                p.player = p.Object.InputAuthority;

                                count++;
                            }
                        }
                    }
                }

                yield return null;
            }

            {
                for(int i = 0; i < unitCount; i++)
                {
                    has_input_Data[i] = unitActors[i].HasInputAuthority ? 1 : 0;
                }

                if (unitCount > 0)
                {
                    has_input_Buffer.Write();
                }
            }

            for (int i = 0; i < playerMan.Length; i++)
            {
                playerMan[i].Set_Spawn_Button();
            }

            //byte[] data = new byte[1];
            //data[0] = 1;

            data[3] = 1;
                                        
            runner.SendReliableDataToServer(data);
        
            bUpdate = true;
            
        }
      
    }
   
    IEnumerator AssignPlayer_Single()
    {
        int count = 0;

        int pCount = 1;
        PlayerRef[] players = new PlayerRef[pCount];

        {
            //while (count < 2)
            while (count < 1)
            {
                count = 0;
                foreach (var p in runner.ActivePlayers)
                {
                    players[count] = p;
                    count++;
                }

                yield return null;
            }
        }

        if (runner.IsServer)
        {
            DebugInfo.Log("AssignPlayer()");

            for (int i = 0; i < 4; i++)
            {
                var uMan = unitMan[i];
                int c = uMan.count;
                for (int j = 0; j < c; j++)
                {
                    uMan.units[j].GetComponent<NetworkObject>().AssignInputAuthority(players[0]);
                }

                //if(c > 0)
                //{
                //    uMan.player = uMan.units[0].GetComponent<NetworkObject>().InputAuthority;
                //}

            }

            for (int i = 4; i < 8; i++)
            {
                var uMan = unitMan[i];
                int c = uMan.count;
                for (int j = 0; j < c; j++)
                {
                    uMan.units[j].GetComponent<NetworkObject>().AssignInputAuthority(players[0]);
                }

                //if (c > 0)
                //{
                //    uMan.player = uMan.units[0].GetComponent<NetworkObject>().InputAuthority;
                //}

            }

            for (int i = 0; i < 8; i++)
            {
                var uMan = unitMan[i];
                int c = uMan.count;

                if (c > 0)
                {
                    uMan.player = uMan.units[0].GetComponent<NetworkObject>().InputAuthority;
                }
            }

            for (int i = 0; i < pCount; i++)
            {
                playerMan[i].Object.AssignInputAuthority(players[0]);
                playerMan[i].player = playerMan[i].Object.InputAuthority;
            }

            {
                for (int i = 0; i < unitCount; i++)
                {
                    has_input_Data[i] = unitActors[i].HasInputAuthority ? 1 : 0;
                }

                if (unitCount > 0)
                {
                    has_input_Buffer.Write();
                }
            }

            for (int i = 0; i < pCount; i++)
            {
                if (runner.GameMode == GameMode.Single)
                {
                    playerMan[i].Set_Spawn_Button_Single();
                }
                else
                {
                    playerMan[i].Set_Spawn_Button();
                }
            }

            bUpdate = true;
        }
    }

   
    void FindUnit()
    {
        UnitActor[] units = runner.GetAllBehaviours<UnitActor>().ToArray();

        foreach(var unit in units)
        {
            if(unit is ArcherActor)
            {
                if(unit.pNum_nt == 0)
                {
                    int idx = 0;
                    unitMan[idx].AddUnit(unit.gameObject);
                }
                else
                {
                    int idx = 4;
                    unitMan[idx].AddUnit(unit.gameObject);
                }
            }
            else if (unit is ArcherCavActor)            
            {
                if (unit.pNum_nt == 0)
                {
                    int idx = 1;
                    unitMan[idx].AddUnit(unit.gameObject);
                }
                else
                {
                    int idx = 5;
                    unitMan[idx].AddUnit(unit.gameObject);
                }
            }
            else if (unit is KnightActor)
            {
                if (unit.pNum_nt == 0)
                {
                    int idx = 2;
                    unitMan[idx].AddUnit(unit.gameObject);
                }
                else
                {
                    int idx = 6;
                    unitMan[idx].AddUnit(unit.gameObject);
                }
            }
            else if (unit is SpearCavActor)
            {
                if (unit.pNum_nt == 0)
                {
                    int idx = 3;
                    unitMan[idx].AddUnit(unit.gameObject);
                }
                else
                {
                    int idx = 7;
                    unitMan[idx].AddUnit(unit.gameObject);
                }
            }
        }
    }


    void Init()
    {
        {
            skyboxMan.Init();
            decalMan.Init();

            dfCullMan.Init();
            dfMan.Init();
        }

        for(int i = 0; i < playerMan.Length; i++)
        {
            playerMan[i].Init();
        }

        gameRunMan.Init();
        arrowMan.Init();
        targetMan.Init();
        selectMan.Init();
        torusMan.Init();
        hpbarMan.Init();
        cullMan.Init();
        uiMan.Init();
        audioMan.Init();
    }

    void Enable()
    {
        //{
        //    RenderGOM.BeginFrameRender += BeginFrameRender;
        //}
        {
            skyboxMan.Enable();
            decalMan.Enable();
        }


        {
            csm_action.Enable();
            cbm_action.Enable();
            terrainMan.Enable();

            dfCullMan.Enable();
            dfMan.Enable();
        }

        if (unitCount > 0)
        {           
            for (int i = 0; i < unitMan.Length; i++)
            {
                unitMan[i].Enable();
            }

            gameRunMan.Enable();
            arrowMan.Enable();            
            torusMan.Enable();
            hpbarMan.Enable();
            cullMan.Enable();
        }
    }

    void Begin()
    {
        //Start
        if (unitCount > 0)
        {
            for (int i = 0; i < unitMan.Length; i++)
            {
                unitMan[i].Begin();
            }
            targetPos_Buffer.Write();

            cullMan.Begin();
            selectMan.Begin();

            //uiMan.Begin();
        }

        {
            uiMan.Begin();
        }
    }

    
    private void Disable()
    {
        //{
        //    RenderGOM.BeginFrameRender -= BeginFrameRender;
        //}

        {
            skyboxMan.Disable();
            decalMan.Disable();
        }

        {
            csm_action.Disable();
            cbm_action.Disable();
            terrainMan.Disable();

            dfCullMan.Disable();
            dfMan.Disable();
        }

        if (unitCount > 0)
        {          
            for (int i = 0; i < unitMan.Length; i++)
            {
                unitMan[i].Disable();
            }

            gameRunMan.Disable();
            arrowMan.Disable();
            torusMan.Disable();
            hpbarMan.Disable();
            cullMan.Disable();
        }
        

        //if(runner.IsServer)
        {           
            bUpdate = false;
            isPause = false;
            isNvStop = false;
        }
    }
      

    void FixedUpdate()    
    {       
        if (!bUpdate)
        {
            return;
        }
        
        //if (unitCount > 0)
        //{
        //    {
        //        for (int i = 0; i < unitCount; i++)
        //        {
        //            active_Buffer.data[i] = activeData[i] ? 1 : 0;
        //        }
        //        active_Buffer.Write();
        //    }
        //
        //    {
        //        select_Buffer.Write();
        //        state_Buffer.Write();
        //    }
        //}      
    }

    void LateUpdate()
    {
        //if (!bUpdate)
        //{
        //    return;
        //}
        //
        //if (unitCount > 0)
        //{
        //    {
        //        for (int i = 0; i < unitCount; i++)
        //        {
        //            active_Buffer.data[i] = activeData[i] ? 1 : 0;
        //        }
        //        active_Buffer.Write();
        //    }
        //
        //    {
        //        select_Buffer.Write();
        //        state_Buffer.Write();
        //    }
        //}
    }

    private void OnDestroyPlay()
    {
        if(unitCount > 0)
        {
            BufferBase<int>.Release(active_Buffer);
            BufferBase<int>.Release(select_Buffer);
            BufferBase<int>.Release(has_input_Buffer);
            BufferBase<float4>.Release(terrainArea_Buffer);
            BufferBase<float3>.Release(targetPos_Buffer);
            BufferBase<float3>.Release(refTargetPos_Buffer);
            BufferBase<float4>.Release(minDist_Buffer);

            BufferBase<Color>.Release(playerColor_Buffer);
            BufferBase<float>.Release(refHp_Buffer);

            BufferBase<int>.Release(state_Buffer);
        }       
    }



    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        DebugInfo.Log("OnSessionListUpdated()");


        foreach (var session in sessionList)
        {
            DebugInfo.Log($"session.Name : {session.Name} // {session.PlayerCount} / {session.MaxPlayers}");
        }

        {
            pool_RoomItem.ReleaseAll();
        }

        {
            dic_Room.Clear();

            int i = 0;
            foreach (var session in sessionList)
            {
                dic_Room.Add(session.Name, session);

                //if (session.Properties["isRandomHostOrClient"] == 1)
                //{
                //    continue;
                //}

                //if(!session.IsValid)
                //{
                //    continue;
                //}


                //GameObject item = GameObject.Instantiate(prefab_roomItem);
                GameObject item = pool_RoomItem.Get();
                item.transform.parent = content_lobby.transform;
                RoomItem sItem = item.GetComponent<RoomItem>();

                {
                    RectTransform rtItem = item.transform as RectTransform;
                    float y0 = 1.0f - 0.1f * (float)(i + 1);
                    float y1 = 1.0f - 0.1f * (float)(i + 0);
                    rtItem.anchorMin = new Vector2(0.0f, y0);
                    rtItem.anchorMax = new Vector2(1.0f, y1);
                    rtItem.pivot = new Vector2(0.5f, 0.5f);
                    rtItem.offsetMin = new Vector2(+10.0f, +10.0f);
                    rtItem.offsetMax = new Vector2(-10.0f, -10.0f);
                }

                {
                    sItem.Name.text = session.Name;
                    sItem.PlayerCount.text = $"{session.PlayerCount} / {session.MaxPlayers}";
                    sItem.btJoin.interactable = session.PlayerCount < session.MaxPlayers ? true : false;
                    sItem.password.interactable = session.Properties["password_host"] == "" ? false : true;

                    sItem.btJoin.onClick.AddListener(
                        () =>
                        {
                            //DebugInfo.Log($"password0 : {runner.SessionInfo.Properties["password"]}");
                            //DebugInfo.Log($"password1 : {sItem.password.text}");
                            //
                            //if(runner.SessionInfo.Properties["password"].ToString() == sItem.password.text)

                            SessionProperty sp;
                            if(roomProperty.TryGetValue("password_client", out sp))
                            {                                
                                sp = SessionProperty.Convert(sItem.password.text);                                
                            }
                            else
                            {
                                roomProperty.Add("password_client", SessionProperty.Convert(sItem.password.text));
                            }

                            //roomProperty.Add("password_client", SessionProperty.Convert(sItem.password.text));
                            //session.UpdateCustomProperties(roomProperty);

                            {
                                JoinRoom(GameMode.Client, session.Name);
                            }
                        });
                }

                i++;
            }
        }


    }    

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        DebugInfo.Log("OnPlayerJoined()");

        if (runner.IsServer)
        {
            runner.Spawn(prefab_playerItem, Vector3.zero, Quaternion.identity, player);
        }
                          
        {
            ResetReady();
        }        
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        DebugInfo.Log("OnPlayerLeft()");

        if (runner.IsServer)
        {            
            runner.Despawn(dic_pItem[player].Object);
        }

        {
            ResetReady();
        }

        if (state == MatchState.Play)
        {
            ShutDown("Ohter Player is Left");
        }
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
        if (runner.GameMode == GameMode.Host || runner.GameMode == GameMode.Client)
        {
            //foreach (var kv in dic_Player)
            //{
            //    if (data.Array[kv.Key.PlayerId] == 1)
            //    {
            //        kv.Value.ready.isOn = true;
            //    }
            //    else
            //    {
            //        kv.Value.ready.isOn = false;
            //    }
            //}

            if (runner.IsClient)
            {
                if (data.Array[2] == 1)
                {
                    Play();
                }
            }

        }


        if (runner.IsServer)
        {
            if (data.Array[3] == 1)
            {
                DebugInfo.Log($"OnReliableDataReceived() : PlayerRef {player}");

                bUpdate = true;
            }
        }

        //{
        //    sPlayerId = (int)data.Array[4];
        //}

    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        {
            var data = inputData;

            {
                if (data.button0 == 0 && Input.GetKeyDown(KeyCode.Q))
                {
                    data.button0 = 1;
                }

                if (data.button0 == 1 && Input.GetKeyUp(KeyCode.Q))
                {
                    data.button0 = 0;
                }
            }

            {
                if (Input.GetKey(KeyCode.E))
                {
                    data.button1 = 1;
                }
                else
                {
                    data.button1 = 0;
                }
            }


            {
                if (Input.GetKey(KeyCode.R))
                {
                    data.button2 = 1;
                }
                else
                {
                    data.button2 = 0;
                }
            }


            {
                //if (data.lmButton == 0 && Input.GetMouseButtonDown(0))
                //{
                //    data.lmButton = 1;
                //}
                //
                //if (data.lmButton == 1 && Input.GetMouseButtonUp(0))
                //{
                //    data.lmButton = 0;
                //}

                if (Input.GetMouseButton(0))
                {
                    data.lmButton = 1;
                }
                else
                {
                    data.lmButton = 0;
                }
            }


            {
                //if (data.rmButton == 0 && Input.GetMouseButtonDown(1))
                //{
                //    data.rmButton = 1;
                //}
                //
                //if (data.rmButton == 1 && Input.GetMouseButtonUp(1))
                //{
                //    data.rmButton = 0;
                //}


                KeyCode key_orbit = CamAction.key_orbit;
                KeyCode key_spin = CamAction.key_spin;


                if (Input.GetMouseButton(1) && !Input.GetKey(key_orbit) && !Input.GetKey(key_spin))
                {
                    data.rmButton = 1;
                }
                else
                {
                    data.rmButton = 0;
                }
            }

            {
                data.mousePos = Input.mousePosition;
            }

            input.Set(data);
        }
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        {
            ShutDown("DisconnectedFromServer");
        }
    }

    public async void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        DebugInfo.Log($"OnShutdown() : {shutdownReason}");


        ////if(runner.IsClient)
        ///

        if (bUpdate)
        {
            Disable();
            OnDestroyPlay();
        }

        if (this.isActiveAndEnabled)
        {
            //if(runner != null)
            //{
            //    runner.IsVisible = false;
            //}

            foreach (var c in canvas)
            {
                c.gameObject.SetActive(false);
            }

            canvas_shutdown.gameObject.SetActive(true);
            //text_shutdown.text = $"OnShutdown() : {shutdownReason}";
            text_shutdown.text = $"Disconnected by Host";

            await Task.Delay(2000);


            if (Application.isPlaying)
            {
                UnloadScene();
                LoadScene(0);

                //ReturnLobbyScene();
            }
            ////StartCoroutine(UnloadSceneMessage());
            //StartCoroutine(UnloadSceneAsync());
            //StartCoroutine(LoadSceneAsync(0));
        }
    }

   
       

    NetworkInputData inputData;

    
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
       
    }
   
    public void OnConnectedToServer(NetworkRunner runner)
    {
        
    }
   
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        //ShutDown("OnConnectFailed");
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        
    }
   
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        ShutDown("OnHostMigration");
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        
    }



    //Test

    async void RamdomReady()
    {

        await Task.Delay(2000);

        {
            PlayerRef p = runner.LocalPlayer;
            data[p.PlayerId] = true ? (byte)1 : (byte)0;

            foreach (var _p in runner.ActivePlayers)
            {
                if (p != _p)
                {
                    runner.SendReliableDataToPlayer(_p, data);
                }
            }
        }
    }

    async void ShutDown()
    {
        canvas_play.gameObject.SetActive(false);

        if (runner != null)
        {
            if (runner.IsRunning)
            {
                await runner.Shutdown();

                {
                    StartCoroutine(UnloadSceneAsync());
                    StartCoroutine(LoadSceneAsync(0));
                }
            }
            else
            {
                await Task.Yield();

                {
                    StartCoroutine(UnloadSceneAsync());
                    StartCoroutine(LoadSceneAsync(0));
                }
            }
        }
    }

    async void StartGame(GameMode mode)
    {
        DebugInfo.Log("StartGame()");

        if (runner == null)
        {
            runner = gameObject.AddComponent<NetworkRunner>();
        }

        runner.ProvideInput = true;

        await runner.JoinSessionLobby(SessionLobby.ClientServer);

        await runner.StartGame(new StartGameArgs()
        {
            //GameMode = GameMode.AutoHostOrClient,
            //GameMode = GameMode.Single,
            GameMode = mode,
            PlayerCount = 2,
            Initialized = ServerInit
        });

        DebugInfo.Log($"runner.IsServer : {runner.IsServer}");

        {
            //Application.runInBackground = true;

            DebugInfo.Log($"Application.runInBackground : {Application.runInBackground}");


        }
    }

    IEnumerator Update_dTime()
    {
        while (true)
        {
            if (text_dTime != null)
            {
                text_dTime.text = string.Format($"Runner.DeltaTime : {runner.DeltaTime}");
            }


            yield return new WaitForSeconds(1.0f);
        }
    }

    IEnumerator AssignPlayer_Single0()
    {
        int count = 0;
        PlayerRef[] players = new PlayerRef[2];

        {
            //while (count < 2)
            while (count < 1)
            {
                count = 0;
                foreach (var p in runner.ActivePlayers)
                {
                    players[count] = p;
                    count++;
                }

                yield return null;
            }
        }

        if (runner.IsServer)
        {
            DebugInfo.Log("AssignPlayer()");

            for (int i = 0; i < 4; i++)
            {
                var uMan = unitMan[i];
                int c = uMan.count;
                for (int j = 0; j < c; j++)
                {
                    uMan.units[j].GetComponent<NetworkObject>().AssignInputAuthority(players[0]);
                }

                //if(c > 0)
                //{
                //    uMan.player = uMan.units[0].GetComponent<NetworkObject>().InputAuthority;
                //}

            }

            for (int i = 4; i < 8; i++)
            {
                var uMan = unitMan[i];
                int c = uMan.count;
                for (int j = 0; j < c; j++)
                {
                    uMan.units[j].GetComponent<NetworkObject>().AssignInputAuthority(players[0]);
                }

                //if (c > 0)
                //{
                //    uMan.player = uMan.units[0].GetComponent<NetworkObject>().InputAuthority;
                //}

            }

            for (int i = 0; i < 8; i++)
            {
                var uMan = unitMan[i];
                int c = uMan.count;

                if (c > 0)
                {
                    uMan.player = uMan.units[0].GetComponent<NetworkObject>().InputAuthority;
                }
            }

            for (int i = 0; i < playerMan.Length; i++)
            {
                playerMan[i].Object.AssignInputAuthority(players[i]);
                playerMan[i].player = playerMan[i].Object.InputAuthority;
            }

            {
                for (int i = 0; i < unitCount; i++)
                {
                    has_input_Data[i] = unitActors[i].HasInputAuthority ? 1 : 0;
                }

                if (unitCount > 0)
                {
                    has_input_Buffer.Write();
                }
            }

            for (int i = 0; i < playerMan.Length; i++)
            {
                if (runner.GameMode == GameMode.Single)
                {
                    playerMan[i].Set_Spawn_Button_Single();
                }
                else
                {
                    playerMan[i].Set_Spawn_Button();
                }
            }

            bUpdate = true;
        }
    }


    private void Awake0()
    {
        {
            unitCounts = _unitCounts;
            baseCount = new int2[unitCounts.Length];

            unitCount = 0;
            for (int i = 0; i < unitCounts.Length; i++)
            {
                baseCount[i].x = (i < 1 ? 0 : baseCount[i - 1].x + baseCount[i - 1].y);
                baseCount[i].y = unitCounts[i];

                unitCount += unitCounts[i];
            }

            unitMan = _unitMan;
            arrowMan = _arrowMan;
            selectMan = _selectMan;
            targetMan = _targetMan;
            torusMan = _torusMan;
            hpbarMan = _hpbarMan;
            cullMan = _cullMan;
        }

        if (unitCount > 0)
        {
            {
                unitActors = new UnitActor[unitCount];
                unitTrs = new Transform[unitCount];
            }

            {
                playerNum = _playerNum;

                viewRadiusDef = _viewRadius;
                attackRadiusDef = _attackRadius;

                viewRadius = new float[unitCount];
                attackRadius = new float[unitCount];
            }

            {
                active_Buffer = new ROBuffer<int>(unitCount);
                activeData = _activeData = new bool[unitCount];
                for (int i = 0; i < unitCount; i++)
                {
                    activeData[i] = true;
                    //activeData[i] = false;

                    //if(i % 2 == 0)
                    //{
                    //    activeData[i] = true;
                    //}
                    //else
                    //{
                    //    activeData[i] = false;
                    //}
                }
            }

            {
                state_Buffer = new ROBuffer<int>(unitCount);
                stateData = state_Buffer.data;

                for (int i = 0; i < unitCount; i++)
                {
                    stateData[i] = 0;
                    //stateData[i] = 4;

                    //if (i % 2 == 0)
                    //{
                    //    stateData[i] = 0;
                    //}
                    //else
                    //{
                    //    stateData[i] = 4;
                    //}
                }
            }

            {
                select_Buffer = new ROBuffer<int>(unitCount);
                _selectData = selectData = select_Buffer.data;
            }

            {
                terrainArea_Buffer = new RWBuffer<float4>(unitCount);
                terrainArea = terrainArea_Buffer.data;
            }

            {
                targetPos_Buffer = new RWBuffer<float3>(unitCount);
                targetPos = targetPos_Buffer.data;
            }

            {
                refTargetPos_Buffer = new RWBuffer<float3>(unitCount);
                refTargetPos = refTargetPos_Buffer.data;
            }

            {
                minDist_Buffer = new RWBuffer<float4>(unitCount);
                minDist = minDist_Buffer.data;
            }

            {
                maxHp = _maxHp;
                hitHp = _hitHp;
                hp = new float[unitCount];
            }

            {
                playerColor = _playerColor;
                playerColor_Buffer = new COBuffer<Color>(_playerColor.Length);
                playerColor_Buffer.data = playerColor;
                playerColor_Buffer.Write();
            }

            {
                refHp_Buffer = new RWBuffer<float>(unitCount);
                refHp = refHp_Buffer.data;
            }

            {
                bColDebug = _bColDebug;
            }
        }



        //Init()
        {
            csm_action.Init();
            terrainMan.Init();          

            for (int i = 0; i < unitMan.Length; i++)
            {
                unitMan[i].unitIdx = i;
                unitMan[i].Init(unitCounts[i]);
            }


            arrowMan.Init();
            targetMan.Init();
            selectMan.Init();
            torusMan.Init();
            hpbarMan.Init();
            cullMan.Init();
        }

        //OnEnable
        {
            if (unitCount > 0)
            {
                for (int i = 0; i < unitMan.Length; i++)
                {
                    unitMan[i].Enable();
                }

                arrowMan.Enable();
            }
        }

        //Start
        if (unitCount > 0)
        {
            for (int i = 0; i < unitMan.Length; i++)
            {
                unitMan[i].Begin();
            }
            targetPos_Buffer.Write();

            cullMan.Begin();
            selectMan.Begin();
        }
    }

    IEnumerator CheckAllReady0()
    {
        int count = 0;

        while (true)
        {
            if (dic_Player != null)
            {
                count = 0;
                foreach (var kv in dic_Player)
                {
                    if (kv.Value.ready.isOn)
                    {
                        count++;
                    }
                }

                if (runner != null)
                {
                    if (runner.IsServer)
                    {
                        if (runner.SessionInfo != null)
                        {
                            if (btPlay != null)
                            {
                                if (count == runner.SessionInfo.MaxPlayers)
                                {
                                    btPlay.interactable = true;
                                }
                                else
                                {
                                    btPlay.interactable = false;
                                }
                            }
                        }
                    }
                }
            }


            yield return null;
        }
    }

    Dictionary<PlayerRef, PlayerItem0> dic_Player;

    public void OnPlayerJoined0(NetworkRunner runner, PlayerRef player)
    {
        DebugInfo.Log("OnPlayerJoined()");

        {
            //runner.GetAllBehaviours

            //runner.Spawn<>
            //runner.Despawn()
        }

        if (state != MatchState.Play)
        {
            {
                pool_PlayerItem.ReleaseAll();
            }

            {
                dic_Player.Clear();

                int i = 0;
                foreach (var p in runner.ActivePlayers)
                {
                    DebugInfo.Log($"PlayerId : {p.PlayerId}");

                    //GameObject item = GameObject.Instantiate(prefab_playerItem);
                    GameObject item = pool_PlayerItem.Get();
                    item.transform.parent = content_player.transform;
                    PlayerItem0 pItem = item.GetComponent<PlayerItem0>();

                    dic_Player.Add(p, pItem);

                    {
                        RectTransform rtItem = item.transform as RectTransform;
                        float y0 = 1.0f - 0.5f * (float)(i + 1);
                        float y1 = 1.0f - 0.5f * (float)(i + 0);
                        rtItem.anchorMin = new Vector2(0.0f, y0);
                        rtItem.anchorMax = new Vector2(1.0f, y1);
                        rtItem.pivot = new Vector2(0.5f, 0.5f);
                        rtItem.offsetMin = new Vector2(+10.0f, +10.0f);
                        rtItem.offsetMax = new Vector2(-10.0f, -10.0f);
                        rtItem.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    }

                    {
                        if (runner.IsServer)
                        {
                            pItem.Name.text = runner.LocalPlayer == p ? "Host" : "Client";
                        }
                        else
                        {
                            pItem.Name.text = runner.LocalPlayer == p ? "Client" : "Host";
                        }


                        pItem.btDisconnect.interactable = runner.IsServer ? true : false;
                        pItem.btDisconnect.onClick.AddListener(
                            () =>
                            {
                                if (runner.IsServer)
                                {
                                    if (runner.LocalPlayer == p)
                                    {
                                        ShutDown("ShutDown");
                                    }
                                    else
                                    {
                                        runner.Disconnect(p);
                                    }
                                }
                            });


                        data[p.PlayerId] = 0;
                        runner.SendReliableDataToPlayer(p, data);
                        pItem.ready.isOn = false;
                        //pItem.ready.isOn = data[p.PlayerId] == 1 ? true : false;
                        pItem.ready.interactable = runner.LocalPlayer == p ? true : false;
                        pItem.ready.onValueChanged.AddListener(
                            (value) =>
                            {
                                data[p.PlayerId] = value ? (byte)1 : (byte)0;

                                foreach (var _p in runner.ActivePlayers)
                                {
                                    if (p != _p)
                                    {
                                        runner.SendReliableDataToPlayer(_p, data);
                                    }
                                }

                            });
                    }

                    i++;
                }
            }

            //for(int i = 0; i < data.Length; i++)
            //{
            //    data[i] = 0;
            //}
        }
    }

    public void OnPlayerLeft0(NetworkRunner runner, PlayerRef player)
    {
        DebugInfo.Log("OnPlayerLeft()");

        if (state != MatchState.Play)
        {
            {
                pool_PlayerItem.ReleaseAll();
            }

            {
                dic_Player.Clear();

                int i = 0;
                foreach (var p in runner.ActivePlayers)
                {
                    DebugInfo.Log($"PlayerId : {p.PlayerId}");

                    //GameObject item = GameObject.Instantiate(prefab_playerItem);
                    GameObject item = pool_PlayerItem.Get();
                    item.transform.parent = content_player.transform;
                    PlayerItem0 pItem = item.GetComponent<PlayerItem0>();

                    dic_Player.Add(p, pItem);

                    {
                        RectTransform rtItem = item.transform as RectTransform;
                        float y0 = 1.0f - 0.5f * (float)(i + 1);
                        float y1 = 1.0f - 0.5f * (float)(i + 0);
                        rtItem.anchorMin = new Vector2(0.0f, y0);
                        rtItem.anchorMax = new Vector2(1.0f, y1);
                        rtItem.pivot = new Vector2(0.5f, 0.5f);
                        rtItem.offsetMin = new Vector2(+10.0f, +10.0f);
                        rtItem.offsetMax = new Vector2(-10.0f, -10.0f);
                        rtItem.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    }

                    {
                        if (runner.IsServer)
                        {
                            pItem.Name.text = runner.LocalPlayer == p ? "Host" : "Client";
                        }
                        else
                        {
                            pItem.Name.text = runner.LocalPlayer == p ? "Client" : "Host";
                        }

                        pItem.btDisconnect.interactable = runner.IsServer ? true : false;
                        pItem.btDisconnect.onClick.AddListener(
                            () =>
                            {
                                if (runner.IsServer)
                                {
                                    if (runner.LocalPlayer == p)
                                    {
                                        ShutDown("ShutDown");
                                    }
                                    else
                                    {
                                        runner.Disconnect(p);
                                    }
                                }
                            });

                        data[p.PlayerId] = 0;
                        runner.SendReliableDataToPlayer(p, data);
                        pItem.ready.isOn = false;
                        //pItem.ready.isOn = data[p.PlayerId] == 1 ? true : false;
                        pItem.ready.interactable = runner.LocalPlayer == p ? true : false;
                        pItem.ready.onValueChanged.AddListener(
                            (value) =>
                            {
                                data[p.PlayerId] = value ? (byte)1 : (byte)0;

                                foreach (var _p in runner.ActivePlayers)
                                {
                                    if (p != _p)
                                    {
                                        runner.SendReliableDataToPlayer(_p, data);
                                    }
                                }
                            });
                    }

                    i++;
                }
            }

            //for (int i = 0; i < data.Length; i++)
            //{
            //    data[i] = 0;
            //}
        }
        else
        {
            ShutDown("Ohter Player is Left");
        }
    }


    //[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_Set_bUnitUpdate(bool value)
    {
        bUpdate = value;
    }

    IEnumerator AssignPlayer1()
    {

        if (runner.IsServer)
        {
            int count = 0;

            PlayerRef[] players = new PlayerRef[2];

            while (count < 1)
            {
                count = 0;
                foreach (var p in runner.ActivePlayers)
                {
                    players[count] = p;
                    count++;
                }

                yield return null;
            }

            DebugInfo.Log("AssignPlayer()");

            for (int i = 0; i < 4; i++)
            {
                var uMan = unitMan[i];
                int c = uMan.count;
                for (int j = 0; j < c; j++)
                {
                    uMan.units[j].GetComponent<NetworkObject>().AssignInputAuthority(players[0]);
                }
            }
        }
    }


    void BeginFrameRender(ScriptableRenderContext context, CommandBuffer cmd, Camera[] cams)
    {
        if (unitCount > 0)
        {
            {
                state_Buffer.Write();
            }
        }
    }

    IEnumerator UpdateRoutine()
    {
        while (true)
        {

            if (unitCount > 0)
            {
                {
                    state_Buffer.Write();
                }
            }


            yield return null;

        }
    }

}


public class PoolManager
{
    public GameObject prefab;
    public int baseCount;
    public List<GameObject> pool;

    public PoolManager(GameObject prefab, int baseCount)
    {
        this.prefab = prefab;
        this.baseCount = baseCount;

        pool = new List<GameObject>();
    }

    public int Count
    {
        get
        {
            return pool.Count;
        }
    }

    public GameObject Get()
    {
        GameObject unit;

        for (int i = 0; i < pool.Count; i++)
        {
            unit = pool[i];
            if (unit.activeSelf == false)
            {
                unit.SetActive(true);
                return unit;
            }
        }

        unit = GameObject.Instantiate(prefab);
        unit.SetActive(true);
        pool.Add(unit);

        return unit;
    }

    public void Release(GameObject unit)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] == unit)
            {
                if (pool.Count > baseCount)
                {
                    unit.SetActive(false);
                    pool.Remove(unit);
                    //GameObject.DestroyImmediate(unit);
                    GameObject.Destroy(unit);
                }
                else
                {
                    unit.SetActive(false);
                }

                break;
            }
        }

    }

    public void ReleaseAll()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            var unit = pool[i];
            unit.SetActive(false);
            unit.transform.parent = null;
        }

        if (baseCount < pool.Count)
        {
            for (int i = baseCount; i < pool.Count;)
            {
                var unit = pool[i];
                pool.Remove(unit);
                GameObject.Destroy(unit);
            }
        }
    }


    //Test   

    public void Release0(GameObject unit)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] == unit)
            {
                if (pool.Count > baseCount)
                {
                    unit.SetActive(false);
                    pool.Remove(unit);
                    //GameObject.DestroyImmediate(unit);
                    GameObject.Destroy(unit);
                }
                else
                {
                    unit.SetActive(false);
                }
            }
        }
    }

}