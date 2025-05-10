using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Mathematics;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;



public class LobbyManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _loadingText;
    [SerializeField] private TextMeshProUGUI _lobbyCodeText;
    [SerializeField] private TextMeshProUGUI _lobbyNameText;
    [SerializeField] private TextMeshProUGUI _lobbyPlayercountText;
    [SerializeField] private GameObject _lobbyTab;

    [SerializeField] private TextMeshProUGUI[] _lobbyPlayerDisplayNames = new TextMeshProUGUI[4];



    public static LobbyManager Instance;
    public bool IsHost = false;
    public string RelayJoinCode;



    private CreateLobbyOptions _lobbyOptions = new CreateLobbyOptions();



    private Lobby _currentLobby;
    private Player _player;
    private bool _isGameStarting;
    [SerializeField] private PlayerLobbyData _thisPlayerData = new();



    public delegate void LobbyEvent();
    public LobbyEvent RaiseGameCreatedFromLobby;



    public void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);
    }



    public async void CreateLobby(string displayName)
    {
        _loadingText.enabled = true;
        try
        {
            _currentLobby = await LobbyService.Instance.CreateLobbyAsync("Test Lobby", 4, _lobbyOptions);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning(e);
        }
        IsHost = true;
        _thisPlayerData.PlayerNumber = 0;
        _loadingText.enabled = false;
        _lobbyTab.SetActive(true);

        HeartbeatLobby();
        Setup(displayName);
    }

    public async void JoinLobbyByCode(string code, string displayName)
    {
        _loadingText.enabled = true;
        try
        {
            _currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning(e);
        }

        IsHost = false;
        _thisPlayerData.PlayerNumber = _currentLobby.Players.Count - 1;

        _loadingText.enabled = false;
        _lobbyTab.SetActive(true);

        Setup(displayName);
    }

    public async void HeartbeatLobby()
    {
        while (!_isGameStarting)
        {
            if (_currentLobby == null) return;

            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogWarning(e);
            }

            await Task.Delay(15 * 1000);
        }
    }



    public async void SetRelayJoinCode(string joinCode)
    {
        RelayJoinCode = joinCode;

        UpdateLobbyOptions lobbyOptions = new();

        lobbyOptions.Data = new Dictionary<string, DataObject>()
        {
            { "RelayJoinCode", new DataObject(
                visibility: DataObject.VisibilityOptions.Member,
                value: joinCode) }
        };

        Debug.Log("Join code sent");
        var lobby = await LobbyService.Instance.UpdateLobbyAsync(_currentLobby.Id, lobbyOptions);
    }
    public async void SetPlayerAbility(PlayerAbility playerAbility)
    {
        UpdatePlayerOptions playerOptions = new();

        playerOptions.Data = new Dictionary<string, PlayerDataObject>()
        {
            { "Ability", new PlayerDataObject(
                visibility: PlayerDataObject.VisibilityOptions.Public,
                value: playerAbility.ToString()) }
        };

        string playerId = AuthenticationService.Instance.PlayerId;

        await LobbyService.Instance.UpdatePlayerAsync(_currentLobby.Id, playerId, playerOptions);
        _thisPlayerData.PlayerAbility = playerAbility;
    }
    public void SetPlayerAbility(string playerAbility)
    {
        SetPlayerAbility((PlayerAbility)Enum.Parse(typeof(PlayerAbility), playerAbility));
    }



    public void StartGame()
    {
        if (!IsHost) Debug.Log("Cannot start game because you are not the host");
        else
        {
            Debug.Log("Starting game");
            CreateGameFromLobby();
        }
    }

    public void CreateGameFromLobby()
    {
        Debug.Log("Game is starting");
        _loadingText.enabled = true;
        _lobbyTab.SetActive(false);
        _isGameStarting = true;
        RaiseGameCreatedFromLobby?.Invoke();
    }



    public PlayerLobbyData GetPlayerData()
    {
        return _thisPlayerData;
    }



    private async void Setup(string displayName)
    {
        UpdatePlayerOptions playerOptions = new();

        playerOptions.Data = new Dictionary<string, PlayerDataObject>()
        {
            { "Display Name", new PlayerDataObject(
                visibility: PlayerDataObject.VisibilityOptions.Public,
                value: displayName) }
        };

        string playerId = AuthenticationService.Instance.PlayerId;

        await LobbyService.Instance.UpdatePlayerAsync(_currentLobby.Id, playerId, playerOptions);
        _thisPlayerData.PlayerName = displayName;
        SetPlayerAbility(PlayerAbility.PoisonVial);

        _lobbyCodeText.text = _currentLobby.LobbyCode;
        _lobbyNameText.text = _currentLobby.Name;
        _lobbyPlayercountText.text = _currentLobby.Players.Count + " / " + _currentLobby.MaxPlayers;

        for (int i = 0; i < _currentLobby.Players.Count; i++)
        {
            _lobbyPlayerDisplayNames[i].text = _currentLobby.Players[i].Data["Display Name"].Value;
        }

        SubscribeToLobbyEvents();
    }

    private async void SubscribeToLobbyEvents()
    {
        var callbacks = new LobbyEventCallbacks();
        callbacks.PlayerJoined += OnPlayerJoined;
        callbacks.PlayerLeft += OnPlayerLeft;
        callbacks.PlayerDataAdded += OnPlayerDataAdded;
        callbacks.DataChanged += OnLobbyDataChanged;
        callbacks.DataAdded += OnLobbyDataAdded;
        try
        {
            await LobbyService.Instance.SubscribeToLobbyEventsAsync(_currentLobby.Id, callbacks);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogWarning(ex);
        }
    }



    private void OnPlayerJoined(List<LobbyPlayerJoined> players)
    {
        _lobbyPlayercountText.text = _currentLobby.Players.Count + " / " + _currentLobby.MaxPlayers;
    }
    private void OnPlayerLeft(List<int> players)
    {
        _lobbyPlayercountText.text = _currentLobby.Players.Count + " / " + _currentLobby.MaxPlayers;
        Debug.Log("need to make player names change when player leaves");
    }
    private void OnPlayerDataAdded(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> changes)
    {
        Debug.Log("Player data changed");
        foreach (KeyValuePair<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> player in changes)
        {
            foreach (KeyValuePair<string, ChangedOrRemovedLobbyValue<PlayerDataObject>> change in player.Value)
            {
                if (change.Key == "Display Name")
                {
                    for (int i = 0; i < _currentLobby.Players.Count; i++)
                    {
                        _lobbyPlayerDisplayNames[i].text = _currentLobby.Players[i].Data["Display Name"].Value;
                    }
                }
            }
        }
    }
    private void OnLobbyDataChanged(Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>> changedData)
    {
        Debug.Log("Lobby data changed");
        foreach (KeyValuePair<string, ChangedOrRemovedLobbyValue<DataObject>> dataChange in changedData)
        {
            if (dataChange.Key == "RelayJoinCode")
            {
                Debug.Log("Join code recieved");
                RelayJoinCode = dataChange.Value.Value.Value;
                CreateGameFromLobby();
            }
        }
    }
    private void OnLobbyDataAdded(Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>> addedData)
    {
        Debug.Log("Lobby data added");
        foreach (KeyValuePair<string, ChangedOrRemovedLobbyValue<DataObject>> dataChange in addedData)
        {
            if (dataChange.Key == "RelayJoinCode")
            {
                Debug.Log("Join code recieved");
                RelayJoinCode = dataChange.Value.Value.Value;
                CreateGameFromLobby();
            }
        }
    }
}



[System.Serializable]
public class PlayerLobbyData
{
    public PlayerAbility PlayerAbility;
    public float4 PlayerColour;
    public string PlayerName;
    public int PlayerNumber;
}