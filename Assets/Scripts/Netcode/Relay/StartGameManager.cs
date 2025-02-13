using Samples.HelloNetcode;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;



public class StartGameManager : MonoBehaviour
{
    RelayServerData _relayServerData;
    RelayServerData _relayClientData;



    public void Start()
    {
        LobbyManager.Instance.RaiseGameCreatedFromLobby += OnGameCreatedFromLobby;
    }

    public void OnGameCreatedFromLobby()
    {
        Debug.Log("Start Game");
        if (LobbyManager.Instance.IsHost) CreateRelay();
        else JoinRelay(LobbyManager.Instance.RelayJoinCode);
    }



    private async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(5);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            _relayServerData = allocation.ToRelayServerData("dtls");

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            _relayClientData = joinAllocation.ToRelayServerData("dtls");

            LobbyManager.Instance.SetRelayJoinCode(joinCode);

            Debug.Log("Relay created");
            StartHost();
        }
        catch(RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            _relayClientData = joinAllocation.ToRelayServerData("dtls");

            Debug.Log("Relay joined");
            StartClient();
        }
        catch(RelayServiceException e)
        {
            Debug.Log(e);
        }
    }



    private void StartHost()
    {
        var oldConstructor = NetworkStreamReceiveSystem.DriverConstructor;
        NetworkStreamReceiveSystem.DriverConstructor = new RelayDriverConstructor(_relayServerData, _relayClientData);
        World serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        World clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        NetworkStreamReceiveSystem.DriverConstructor = oldConstructor;

        foreach (World world in World.All)
        {
            if (world.Flags == WorldFlags.Game)
            {
                world.Dispose();
                break;
            }
        }

        if (World.DefaultGameObjectInjectionWorld == null) World.DefaultGameObjectInjectionWorld = serverWorld;

        SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);

        Entity networkStreamEntity = serverWorld.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestListen>());
        serverWorld.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestListen");
        serverWorld.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestListen { Endpoint = NetworkEndpoint.AnyIpv4 });

        networkStreamEntity = clientWorld.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
        clientWorld.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestConnect");
        clientWorld.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestConnect { Endpoint = _relayClientData.Endpoint });

        Debug.Log("Host started");
    }

    private void StartClient()
    {
        var oldConstructor = NetworkStreamReceiveSystem.DriverConstructor;
        NetworkStreamReceiveSystem.DriverConstructor = new RelayDriverConstructor(new RelayServerData(), _relayClientData);
        World clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        NetworkStreamReceiveSystem.DriverConstructor = oldConstructor;

        foreach (World world in World.All)
        {
            if (world.Flags == WorldFlags.Game)
            {
                world.Dispose();
                break;
            }
        }

        if (World.DefaultGameObjectInjectionWorld == null) World.DefaultGameObjectInjectionWorld = clientWorld;

        SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);

        Entity networkStreamEntity = clientWorld.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
        clientWorld.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestConnect");
        clientWorld.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestConnect { Endpoint = _relayClientData.Endpoint });

        Debug.Log("Client started");
    }
}
