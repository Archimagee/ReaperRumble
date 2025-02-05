using TMPro;
using UnityEngine;



public class GetLobbyCode : MonoBehaviour
{
    [SerializeField] private TMP_InputField _lobbyCodeInputField;



    public void JoinLobbyByCode()
    {
        LobbyManager.Instance.JoinLobbyByCode(_lobbyCodeInputField.text);
    }
}