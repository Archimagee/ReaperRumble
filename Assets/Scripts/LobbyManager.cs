using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;



public class LobbyManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _loadingText;
    [SerializeField] private TextMeshProUGUI _lobbyCodeText;
    [SerializeField] private TextMeshProUGUI _lobbyNameText;
    [SerializeField] private TextMeshProUGUI _lobbyPlayercountText;



    public static LobbyManager Instance;



    CreateLobbyOptions _lobbyOptions = new CreateLobbyOptions();



    private Lobby _currentLobby;



    public void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);
    }



    public async void CreateLobby()
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

        UpdateText();
    }

    public async void JoinLobbyByCode(string code)
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

        UpdateText();
    }



    private void UpdateText()
    {
        _lobbyCodeText.text = _currentLobby.LobbyCode;
        _lobbyNameText.text = _currentLobby.Name;
        _lobbyPlayercountText.text = _currentLobby.Players.Count + " / " + _currentLobby.MaxPlayers;
    }

    private async void SubscribeToLobbyEvents()
    {
        var callbacks = new LobbyEventCallbacks();
        callbacks.PlayerJoined += OnPlayerJoined;
        callbacks.PlayerLeft += OnPlayerLeft;
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
}
