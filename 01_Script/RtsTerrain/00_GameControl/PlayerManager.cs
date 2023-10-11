using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Fusion;

public class PlayerManager : NetworkBehaviour
{
    public int pNum;

    public PlayerRef player { get; set; }

    public KeyCode[] key_spawn;

    public UnitManager[] unitMan;

    public GameObject Panel_UI;

    public Button[] bt_unit_spawn;
    

    UnitActor[] unitActors;

    public void Init()
    {
        unitMan = GameManager.unitMan;        
       
        {
            unitActors = GameManager.unitActors;
        }
    }


    public void Set_Spawn_Button()
    {
        if(Object.HasInputAuthority)
        {
            Panel_UI = GameObject.Find("ReSpawn_Unit");

            bt_unit_spawn = new Button[4];

            bt_unit_spawn[0] = Panel_UI.transform.Find("Unit00").GetComponent<Button>();
            bt_unit_spawn[1] = Panel_UI.transform.Find("Unit01").GetComponent<Button>();
            bt_unit_spawn[2] = Panel_UI.transform.Find("Unit02").GetComponent<Button>();
            bt_unit_spawn[3] = Panel_UI.transform.Find("Unit03").GetComponent<Button>();

            bt_unit_spawn[0].onClick.AddListener(
                () =>
                {
                    int i = 0;

                    if (player == unitMan[i].player)
                    {
                        RPC_ReSpawn(i);
                    }
                    else if (player == unitMan[i + 4].player)
                    {
                        RPC_ReSpawn(i + 4);
                    }
                });

            bt_unit_spawn[1].onClick.AddListener(
                () =>
                {
                    int i = 1;

                    if (player == unitMan[i].player)
                    {
                        RPC_ReSpawn(i);
                    }
                    else if (player == unitMan[i + 4].player)
                    {
                        RPC_ReSpawn(i + 4);
                    }
                });

            bt_unit_spawn[2].onClick.AddListener(
                () =>
                {
                    int i = 2;

                    if (player == unitMan[i].player)
                    {
                        RPC_ReSpawn(i);
                    }
                    else if (player == unitMan[i + 4].player)
                    {
                        RPC_ReSpawn(i + 4);
                    }
                });

            bt_unit_spawn[3].onClick.AddListener(
                () =>
                {
                    int i = 3;

                    if (player == unitMan[i].player)
                    {
                        RPC_ReSpawn(i);
                    }
                    else if (player == unitMan[i + 4].player)
                    {
                        RPC_ReSpawn(i + 4);
                    }
                });
        }

    }

    public void Set_Spawn_Button_Single()
    {
        if (Object.HasInputAuthority)
        {
            Panel_UI = GameObject.Find("ReSpawn_Unit");

            bt_unit_spawn = new Button[8];

            bt_unit_spawn[0] = Panel_UI.transform.Find("Unit00").GetComponent<Button>();
            bt_unit_spawn[1] = Panel_UI.transform.Find("Unit01").GetComponent<Button>();
            bt_unit_spawn[2] = Panel_UI.transform.Find("Unit02").GetComponent<Button>();
            bt_unit_spawn[3] = Panel_UI.transform.Find("Unit03").GetComponent<Button>();

            bt_unit_spawn[4] = Panel_UI.transform.Find("Unit10").GetComponent<Button>();
            bt_unit_spawn[5] = Panel_UI.transform.Find("Unit11").GetComponent<Button>();
            bt_unit_spawn[6] = Panel_UI.transform.Find("Unit12").GetComponent<Button>();
            bt_unit_spawn[7] = Panel_UI.transform.Find("Unit13").GetComponent<Button>();

            bt_unit_spawn[0].onClick.AddListener(
                () =>
                {
                    int i = 0;

                    RPC_ReSpawn(i);
                });

            bt_unit_spawn[1].onClick.AddListener(
                () =>
                {
                    int i = 1;

                    RPC_ReSpawn(i);
                });

            bt_unit_spawn[2].onClick.AddListener(
                () =>
                {
                    int i = 2;

                    RPC_ReSpawn(i);
                });

            bt_unit_spawn[3].onClick.AddListener(
                () =>
                {
                    int i = 3;

                    RPC_ReSpawn(i);
                });

            bt_unit_spawn[4].onClick.AddListener(
                () =>
                {
                    int i = 4;

                    RPC_ReSpawn(i);
                });

            bt_unit_spawn[5].onClick.AddListener(
                () =>
                {
                    int i = 5;

                    RPC_ReSpawn(i);
                });

            bt_unit_spawn[6].onClick.AddListener(
                () =>
                {
                    int i = 6;

                    RPC_ReSpawn(i);
                });

            bt_unit_spawn[7].onClick.AddListener(
                () =>
                {
                    int i = 7;

                    RPC_ReSpawn(i);
                });
        }

    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_ReSpawn(int idx)
    {
        StartCoroutine(unitMan[idx].SpawnTerrainRoutine());
    }

    public NetworkInputData inputData;

    public override void FixedUpdateNetwork()
    {
        if (Object.HasInputAuthority)
        {
            if (GetInput(out inputData))
            {
               
            }
        }       
    }
  

    void Update()
    {
       
        if (Object.HasInputAuthority)
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                //DebugInfo.Log($"PlayerMan{Object.InputAuthority} : FixedUpdateNetwork()");
                DebugInfo.Log($"PlayerMan{player} : FixedUpdateNetwork()");
            }

            for(int i = 0; i < 4; i++)
            {
                if (Input.GetKeyDown(key_spawn[i]))
                {
                    if (player == unitMan[i].player)
                    {
                        //DebugInfo.Log($"{unitMan[i].player}");
                        RPC_ReSpawn(i);
                    }
                    else if (player == unitMan[i + 4].player)
                    {
                        //DebugInfo.Log($"{unitMan[i + 4].player}");
                        RPC_ReSpawn(i + 4);
                    }
                }
            }            
        }
    }





    // Start is called before the first frame update
    void Start()
    {
        
    }


    //Test


    [Rpc(RpcSources.InputAuthority, RpcTargets.All,
        Channel = RpcChannel.Reliable, HostMode = RpcHostMode.SourceIsHostPlayer,
        InvokeLocal = true, InvokeResim = true, TickAligned = true)]
    public void RPC_SetSelectAll_Fusion(bool value, PlayerRef player)
    {
        int count = GameManager.unitActors.Length;
        for (int i = 0; i < count; i++)
        {
            var unit = GameManager.unitActors[i];
            //if (unit.HasInputAuthority)
            if (unit.Object.InputAuthority == player)
            {
                unit.isSelected = value;
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All,
        Channel = RpcChannel.Reliable, HostMode = RpcHostMode.SourceIsHostPlayer,
        InvokeLocal = true, InvokeResim = true, TickAligned = true)]
    public void RPC_SetSelectAll_Fusion_One(NetworkId id, PlayerRef player)
    {
        UnitActor hActor = Runner.FindObject(id).GetBehaviour<UnitActor>();

        int count = GameManager.unitActors.Length;
        for (int i = 0; i < count; i++)
        {
            var unit = GameManager.unitActors[i];
            //if (unit.HasInputAuthority)
            if (unit.Object.InputAuthority == player)
            {
                if (unit == hActor)
                {
                    unit.isSelected = true;
                }
                else
                {
                    unit.isSelected = false;
                }
            }
        }
    }

}


public struct NetworkInputData : INetworkInput
{
    public const byte MOUSEBUTTON1 = 0x01;
    public const byte MOUSEBUTTON2 = 0x02;

    public NetworkButtons buttons;       
    public Vector3 direction;

    //
    public int button0;     //Q
    public int button1;     //E
    public int button2;     //R
    public int lmButton;    //Left Mouse Button;
    public int rmButton;    //Right Mouse Button;
    public Vector3 mousePos; //Mouse Position;
}