using UnityEngine;

public class MenuMusicManager : MonoBehaviour
{
    public static MenuMusicManager Instance;
    public void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);
    }

    private FMOD.Studio.EventInstance _music;

    private int _currentTrack;
    public int CurrentTrack
    {
        get { return _currentTrack; }
        set
        {
            if (value < 0 || value > 1) Debug.LogWarning("Tried to set invalid menu music");
            else
            {
                _currentTrack = value;
                _music.setParameterByName("CurrentMenu", value);
            }
        }
    }

    public void SetMusic(int value)
    {
        CurrentTrack = value;
    }

    public void StopPlaying()
    {
        _music.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
    }

    public void StartPlaying()
    {
        _music.start();
    }



    private void Start()
    {
        _music = FMODUnity.RuntimeManager.CreateInstance("event:/MenuMusic");
    }
}
