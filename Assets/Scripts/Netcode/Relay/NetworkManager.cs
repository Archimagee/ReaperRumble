using Samples.HelloNetcode;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;



public static class NetworkManager
{
    private static RelayServerData _relayServerData;
    private static RelayServerData _relayClientData;

    private static World _serverWorld;
    private static World _clientWorld;



    public static void OnGameCreatedFromLobby()
    {
        Debug.Log("Start Game");
        if (LobbyManager.Instance.IsHost) CreateRelay();
        else JoinRelay(LobbyManager.Instance.RelayJoinCode);
    }



    private static async void CreateRelay()
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

    private static async void JoinRelay(string joinCode)
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



    private static async void StartHost()
    {
        var oldConstructor = NetworkStreamReceiveSystem.DriverConstructor;
        NetworkStreamReceiveSystem.DriverConstructor = new RelayDriverConstructor(_relayServerData, _relayClientData);
        _serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        _clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        NetworkStreamReceiveSystem.DriverConstructor = oldConstructor;

        foreach (World world in World.All)
        {
            if (world.Flags == WorldFlags.Game)
            {
                world.Dispose();
                break;
            }
        }

        if (World.DefaultGameObjectInjectionWorld == null) World.DefaultGameObjectInjectionWorld = _serverWorld;

        await SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Additive);
        await SceneManager.UnloadSceneAsync("MainMenuScene");

        Entity networkStreamEntity = _serverWorld.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestListen>());
        _serverWorld.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestListen");
        _serverWorld.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestListen { Endpoint = NetworkEndpoint.AnyIpv4 });

        networkStreamEntity = _clientWorld.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
        _clientWorld.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestConnect");
        _clientWorld.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestConnect { Endpoint = _relayClientData.Endpoint });

        PlayerLobbyData playerLobbyData = LobbyManager.Instance.GetPlayerData();
        Entity playerDataEntity = _clientWorld.EntityManager.CreateEntity();
        _clientWorld.EntityManager.AddComponentData(playerDataEntity, new PlayerDataFromLobby() { PlayerNumber = playerLobbyData.PlayerNumber, PlayerAbility = playerLobbyData.PlayerAbility, PlayerColour = playerLobbyData.PlayerColour });

        Entity endGame = _serverWorld.EntityManager.CreateEntity();
        _serverWorld.EntityManager.AddComponentData(endGame, new EndGameTime() { TimeToEndGameAt = 300d });

        MenuMusicManager.Instance.StopPlaying();
        FightMusicManager.Instance.StartPlaying();

        Debug.Log("Host started");
    }

    private static async void StartClient()
    {
        var oldConstructor = NetworkStreamReceiveSystem.DriverConstructor;
        NetworkStreamReceiveSystem.DriverConstructor = new RelayDriverConstructor(new RelayServerData(), _relayClientData);
        _clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        NetworkStreamReceiveSystem.DriverConstructor = oldConstructor;

        foreach (World world in World.All)
        {
            if (world.Flags == WorldFlags.Game)
            {
                world.Dispose();
                break;
            }
        }

        if (World.DefaultGameObjectInjectionWorld == null) World.DefaultGameObjectInjectionWorld = _clientWorld;

        await SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Additive);        
        await SceneManager.UnloadSceneAsync("MainMenuScene");

        Entity networkStreamEntity = _clientWorld.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
        _clientWorld.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestConnect");
        _clientWorld.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestConnect { Endpoint = _relayClientData.Endpoint });

        PlayerLobbyData playerLobbyData = LobbyManager.Instance.GetPlayerData();
        Entity playerDataEntity = _clientWorld.EntityManager.CreateEntity();
        _clientWorld.EntityManager.AddComponentData(playerDataEntity, new PlayerDataFromLobby() { PlayerNumber = playerLobbyData.PlayerNumber, PlayerAbility = playerLobbyData.PlayerAbility, PlayerColour = playerLobbyData.PlayerColour });

        MenuMusicManager.Instance.StopPlaying();
        FightMusicManager.Instance.StartPlaying();

        Debug.Log("Client started");
    }

    public static async void EndClient()
    {
        await SceneManager.LoadSceneAsync("MainMenuScene", LoadSceneMode.Additive);

        if (_serverWorld != null) _serverWorld.Dispose();
        _clientWorld.Dispose();
        FightMusicManager.Instance.StopPlaying();
        await SceneManager.UnloadSceneAsync("GameScene");
    }
}