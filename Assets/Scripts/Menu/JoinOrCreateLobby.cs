using TMPro;
using UnityEngine;



public class JoinOrCreateLobby : MonoBehaviour
{
    [SerializeField] private TMP_InputField _lobbyCodeInputField;
    [SerializeField] private TMP_InputField _displayNameInputField;



    public void CreateLobby()
    {
        LobbyManager.Instance.CreateLobby(_displayNameInputField.text);
    }
    public void JoinLobbyByCode()
    {
        LobbyManager.Instance.JoinLobbyByCode(_lobbyCodeInputField.text, _displayNameInputField.text);
    }
}