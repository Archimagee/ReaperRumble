using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private TextMeshProUGUI _soulCountText;
    [SerializeField] private TextMeshProUGUI _depositCooldownText;
    [SerializeField] private Image _depositCooldownFill;
    [SerializeField] private Image _depositCooldownBG;
    [SerializeField] private Color _depositAvailableColor;
    [SerializeField] private Color _depositUnavailableColor;

    [SerializeField] private GameObject _announcementGO;
    [SerializeField] private TextMeshProUGUI _announcementText;

    [SerializeField] private float _gameTimeSeconds;
    private float _gameTimeLeft;
    [SerializeField] private TextMeshProUGUI _timerText;

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
                _depositCooldownBG.color = _depositAvailableColor;
            }
            else _depositCooldownText.text = ((int)TimeSpan.FromMilliseconds(_currentDepositCooldown).TotalSeconds).ToString() + "s";

            _depositCooldownFill.fillAmount = _currentDepositCooldown / _lastCooldown;
        }

        if (_announcementGO.activeInHierarchy && _hideAnnouncementAt <= Time.time) _announcementGO.SetActive(false);

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

    public void AddScore(int playerNumber, int newScore)
    {
        Debug.Log("Player " + playerNumber + " scored " + newScore + " souls!");
    }

    public void SetSoulCount(int amount)
    {
        _soulCountText.text = amount.ToString();
    }

    public void SetDepositCooldown(int amount)
    {
        _currentDepositCooldown = amount;
        _lastCooldown = amount;
        _depositCooldownBG.color = _depositUnavailableColor;
    }

    public void SendAnnouncement(string text, float timeSeconds)
    {
        _hideAnnouncementAt = Time.time + timeSeconds;

        _announcementText.text = text;
        _announcementGO.SetActive(true);
    }
}
