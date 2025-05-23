using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using System.Threading.Tasks;
using System;
using UnityEngine.SceneManagement;



public class LoadMenu : MonoBehaviour
{
    [SerializeField] private GameObject _loadingTab;

    public static LoadMenu Instance;



    [SerializeField] private GameObject[] _gameObjectsToEnable;



    public void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);
    }



    private async void Start()
    {
        _loadingTab.SetActive(true);
        if (SceneManager.loadedSceneCount == 1) await SceneManager.LoadSceneAsync("CommonScene", LoadSceneMode.Additive);

        try
        {
            await UnityServices.InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }



        SetupEvents();
        await SignInAnonymouslyAsync();

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        MenuMusicManager.Instance.CurrentTrack = 0;
        MenuMusicManager.Instance.StartPlaying();
        _loadingTab.SetActive(false);
    }



    private void OnLoadMenuFinished()
    {
        foreach (GameObject gameObject in _gameObjectsToEnable)
        {
            gameObject.SetActive(true);
        }
    }



    void SetupEvents()
    {
        AuthenticationService.Instance.SignedIn += () => {
            // Shows how to get a playerID
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");

            // Shows how to get an access token
            Debug.Log($"Access Token: {AuthenticationService.Instance.AccessToken}");

        };

        AuthenticationService.Instance.SignInFailed += (err) => {
            Debug.LogError(err);
        };

        AuthenticationService.Instance.SignedOut += () => {
            Debug.Log("Player signed out.");
        };

        AuthenticationService.Instance.Expired += () =>
        {
            Debug.Log("Player session could not be refreshed and expired.");
        };
    }



    private async Task SignInAnonymouslyAsync()
    {
        try
        {
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign in anonymously succeeded!");

            // Shows how to get the playerID
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");

        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }
}
