using TMPro;
using UnityEngine;



public class JoinOrCreateLobby : MonoBehaviour
{
    [SerializeField] private TMP_InputField _lobbyCodeInputField;
    [SerializeField] private TMP_InputField _displayNameInputField;



    public string GetDisplayName()
    {
        if (_displayNameInputField.text != string.Empty) return _displayNameInputField.text;
        else return "Unnamed player";
    }



    public void CreateLobby()
    {
        LobbyManager.Instance.CreateLobby(GetDisplayName());
    }
    public void JoinLobbyByCode()
    {
        LobbyManager.Instance.JoinLobbyByCode(_lobbyCodeInputField.text, GetDisplayName());
    }
}