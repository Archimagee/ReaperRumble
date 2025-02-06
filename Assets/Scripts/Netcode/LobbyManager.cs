using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
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

    [SerializeField] private TextMeshProUGUI[] _lobbyPlayerDisplayNames = new TextMeshProUGUI[4];



    public static LobbyManager Instance;



    CreateLobbyOptions _lobbyOptions = new CreateLobbyOptions();



    private Lobby _currentLobby;
    private Player _player;



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
            Debug.Log(e);
        }
        _loadingText.enabled = false;

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
            Debug.Log(e);
        }
        _loadingText.enabled = false;

        Setup(displayName);
    }

    public async void HeartbeatLobby()
    {
        while (true)
        {
            if (_currentLobby == null) return;

            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }

            await Task.Delay(15 * 1000);
        }
    }



    private async void Setup(string displayName)
    {
        UpdatePlayerOptions playerOptions = new();

        playerOptions.Data = new Dictionary<string, PlayerDataObject>() {
            { "Display Name", new PlayerDataObject(
                visibility: PlayerDataObject.VisibilityOptions.Public,
                value: displayName) }
        };

        string playerId = AuthenticationService.Instance.PlayerId;

        var lobby = await LobbyService.Instance.UpdatePlayerAsync(_currentLobby.Id, playerId, playerOptions);


        SubscribeToLobbyEvents();
        UpdateText();
    }

    private void UpdateText()
    {
        _lobbyCodeText.text = _currentLobby.LobbyCode;
        _lobbyNameText.text = _currentLobby.Name;
        _lobbyPlayercountText.text = _currentLobby.Players.Count + " / " + _currentLobby.MaxPlayers;

        for (int i = 0; i < _currentLobby.Players.Count; i++)
        {
            _lobbyPlayerDisplayNames[i].text = _currentLobby.Players[i].Data["Display Name"].Value;
        }
    }

    private async void SubscribeToLobbyEvents()
    {
        var callbacks = new LobbyEventCallbacks();
        callbacks.PlayerJoined += OnPlayerJoined;
        callbacks.PlayerLeft += OnPlayerLeft;
        callbacks.PlayerDataChanged += OnPlayerDataChanged;
        try
        {
            await LobbyService.Instance.SubscribeToLobbyEventsAsync(_currentLobby.Id, callbacks);
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
        }
    }



    private void OnPlayerJoined(List<LobbyPlayerJoined> players)
    {
        UpdateText();
    }
    private void OnPlayerLeft(List<int> players)
    {
        UpdateText();
    }
    private void OnPlayerDataChanged(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> changes)
    {
        UpdateText();
    }
}
