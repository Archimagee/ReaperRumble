using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;



public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private TextMeshProUGUI _soulCountText;
    [SerializeField] private TextMeshProUGUI _depositCooldownText;
    [SerializeField] private Image _depositCooldownFill;
    [SerializeField] private Color _depositAvailableColor;
    [SerializeField] private Color _depositUnavailableColor;

    [SerializeField] private GameObject _announcementGO;
    [SerializeField] private TextMeshProUGUI _announcementText;

    [SerializeField] private float _gameTimeSeconds;
    private float _gameTimeLeft;
    [SerializeField] private TextMeshProUGUI _timerText;

    [SerializeField] private GameObject scoreTab;
    [SerializeField] private TextMeshProUGUI[] scoreText = new TextMeshProUGUI[4];
    private Dictionary<int, int> playerScores = new Dictionary<int, int>(4);

    [SerializeField] private GameObject endGameTab;
    [SerializeField] private TextMeshProUGUI[] endGamePlayerNameText = new TextMeshProUGUI[4];
    [SerializeField] private TextMeshProUGUI[] endGameScoreText = new TextMeshProUGUI[4];

    [SerializeField] private GameObject menuTab;

    private float _lastCooldown;
    private float _currentDepositCooldown;
    private float _hideAnnouncementAt;


    public void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);
    }

    public void Start()
    {
        _gameTimeLeft = _gameTimeSeconds;
        _announcementGO.SetActive(false);
        scoreTab.SetActive(false);
        menuTab.SetActive(false);

        for (int i = 1; i <= scoreText.Length; i++) playerScores.Add(i, 0);
    }



    public void Update()
    {
        if (_currentDepositCooldown > 0)
        {
            _currentDepositCooldown -= Time.deltaTime * 1000;
            if (_currentDepositCooldown < 0)
            {
                _currentDepositCooldown = 0;
                _depositCooldownText.text = "";
                _depositCooldownFill.color = _depositAvailableColor;
            }
            else _depositCooldownText.text = ((int)TimeSpan.FromMilliseconds(_currentDepositCooldown).TotalSeconds).ToString() + "s";

            _depositCooldownFill.fillAmount = _currentDepositCooldown / _lastCooldown;
        }

        if (_announcementGO.activeInHierarchy && _hideAnnouncementAt <= Time.time) _announcementGO.SetActive(false);
        if (Input.GetKeyDown(KeyCode.Tab)) scoreTab.SetActive(true);
        else if (Input.GetKeyUp(KeyCode.Tab)) scoreTab.SetActive(false);

        if (Input.GetKeyDown(KeyCode.Escape) && menuTab.activeInHierarchy == false) OpenEscapeMenu();
        else if (Input.GetKeyDown(KeyCode.Escape) && menuTab.activeInHierarchy == true) CloseEscapeMenu();

        _gameTimeLeft -= Time.deltaTime;
        if (_gameTimeLeft <= 0f) SendAnnouncement("Game Over!", 100f);

        _timerText.text = "";
        if (TimeSpan.FromSeconds(_gameTimeLeft).Minutes < 10) _timerText.text += "0";
        _timerText.text += TimeSpan.FromSeconds(_gameTimeLeft).Minutes.ToString() + ":";
        if (TimeSpan.FromSeconds(_gameTimeLeft).Seconds < 10) _timerText.text += "0";
        _timerText.text += TimeSpan.FromSeconds(_gameTimeLeft).Seconds.ToString();
    }



    public void SetCamera(Camera camera)
    {
        GetComponent<Canvas>().worldCamera = camera;
    }

    public void AddScore(int playerNumber, int scoreToAdd)
    {
        if (playerNumber < 1 || playerNumber > 4) throw new Exception("Tried to add score to player number " + playerNumber);
        else
        {
            playerScores[playerNumber] += scoreToAdd;
            scoreText[playerNumber].text = playerScores[playerNumber].ToString();
        }
    }

    public NativeList<int> GetScores()
    {
        NativeList<int> scores = new NativeList<int>(4, Allocator.Temp);
        for (int i = 1; i <= scoreText.Length; i++) scores.Add(playerScores[i]);
        return scores;
    }

    public void SetSoulCount(int amount)
    {
        _soulCountText.text = amount.ToString();
    }

    public void SetDepositCooldown(int amount)
    {
        _currentDepositCooldown = amount;
        _lastCooldown = amount;
        _depositCooldownFill.color = _depositUnavailableColor;
    }

    public void SendAnnouncement(string text, float timeSeconds)
    {
        _hideAnnouncementAt = Time.time + timeSeconds;

        _announcementText.text = text;
        _announcementGO.SetActive(true);
    }



    public void OpenEscapeMenu()
    {
        menuTab.SetActive(true);
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }
    public void CloseEscapeMenu()
    {
        menuTab.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }



    public void EndGame(int player1Score, int player2Score, int player3Score, int player4Score)
    {
        endGamePlayerNameText[0].text = "Player 1";
        endGameScoreText[0].text = player1Score.ToString();

        endGamePlayerNameText[1].text = "Player 2";
        endGameScoreText[1].text = player2Score.ToString();

        endGamePlayerNameText[2].text = "Player 3";
        endGameScoreText[2].text = player3Score.ToString();

        endGamePlayerNameText[3].text = "Player 4";
        endGameScoreText[3].text = player4Score.ToString();

        endGameTab.SetActive(true);
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadSceneAsync("MainMenuScene", LoadSceneMode.Single);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
