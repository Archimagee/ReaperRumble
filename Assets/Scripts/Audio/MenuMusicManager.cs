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

    private float _currentTrack;
    public float CurrentTrack
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

    public void SetMusic(float value)
    {
        CurrentTrack = value;
    }

    public void StopPlaying()
    {
        _music.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
    }



    private void Start()
    {
        _music = FMODUnity.RuntimeManager.CreateInstance("event:/MenuMusic");
        _music.start();
    }
}
