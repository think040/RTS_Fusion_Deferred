using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using Fusion;

public class PlayerItem : NetworkBehaviour
{
    public Text Name;
    public Toggle ready;
    public Button btDisconnect;

    PlayerRef player;   
   

    public override void Spawned()
    {
        Init();
        Add();
    }


    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Remove();
    }

   
    public void Init()   
    {
        player = Object.InputAuthority;

        {
            SetName();
        }            


        if (Runner.IsServer)
        {
            btDisconnect.interactable = true;

            btDisconnect.onClick.AddListener(
            () =>
            {
                if (Runner.LocalPlayer == player)
                {
                    GameManager.instance.ShutDown("ShutDown");
                }
                else
                {
                    Runner.Disconnect(player);
                }
            });

        }
        else
        {
            btDisconnect.interactable = false;
        }

        if (HasInputAuthority)
        {
            ready.interactable = true;
            ready.onValueChanged.AddListener(
            (value) =>
            {
                RPC_OnReady(value);
            });
        }
        else
        {
            ready.interactable = false;
        }
    }

    void SetName()
    {
        int MaxPlayers = Runner.SessionInfo.MaxPlayers;

        if(player == MaxPlayers - 1)
        {
            Name.text = "Host";
        }
        else
        {
            Name.text = "Client";
        }

     

        //if (Runner.IsServer)
        //{
        //    Name.text = Runner.LocalPlayer == player ? "Host" : "Client";
        //}
        //else
        //{
        //    Name.text = Runner.LocalPlayer == player ? "Client" : "Host";
        //}        
    }     

    public void Add()
    {
        GameManager.instance.AddPlayerItem(this);
    }

    public void Remove()
    {
        GameManager.instance.RemovePlayerItem(this);
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.All,
        Channel = RpcChannel.Reliable, HostMode = RpcHostMode.SourceIsHostPlayer,
        InvokeLocal = false, InvokeResim = false, TickAligned = true)]
    void RPC_OnReady(bool value)
    {
        ready.isOn = value;
    }


    
    

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}