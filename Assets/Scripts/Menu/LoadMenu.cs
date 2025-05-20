using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using System.Threading.Tasks;
using System;



public class LoadMenu : MonoBehaviour
{
    [SerializeField] private GameObject _loadingTab;

    public static LoadMenu Instance;

    public delegate void LoadMenuDelegate();
    public LoadMenuDelegate RaiseLoadMenuFinished;



    [SerializeField] private GameObject[] _gameObjectsToEnable;



    public void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);
    }



    private async void Start()
    {
        RaiseLoadMenuFinished += OnLoadMenuFinished;

        _loadingTab.SetActive(true);

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

        _loadingTab.SetActive(false);
        RaiseLoadMenuFinished?.Invoke();
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }



    public void OnDestroy()
    {
        RaiseLoadMenuFinished -= OnLoadMenuFinished;
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
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
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
