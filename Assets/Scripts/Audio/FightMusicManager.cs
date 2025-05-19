using UnityEngine;

public class FightMusicManager : MonoBehaviour
{
    public static FightMusicManager Instance;
    public void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);
    }



    private MusicNames _currentTrack;
    public MusicNames CurrentTrack
    { 
        get { return _currentTrack; }
        set
        {
            _currentTrack = value;
            _music.setParameterByName("CurrentDisaster", (float)_currentTrack);
            _music.setParameterByName("IsTransitionFinishedPlaying", 1f);
        }
    }

    private FMOD.Studio.EventInstance _music;



    private void Start()
    {
        _music = FMODUnity.RuntimeManager.CreateInstance("event:/FightMusic");
        _music.start();
    }



    public void SetMusicFromDisasterType(DisasterType disaster)
    {
        if      (disaster == DisasterType.LavaFlood) CurrentTrack = MusicNames.LavaFlood;
        else if (disaster == DisasterType.LightningStorm) CurrentTrack = MusicNames.LightningStorm;
        else if (disaster == DisasterType.MeteorShower) CurrentTrack = MusicNames.MeteorShower;
        else if (disaster == DisasterType.Tornado) CurrentTrack = MusicNames.Tornado;
        else if (disaster == DisasterType.Eruption) CurrentTrack = MusicNames.Eruption;
    }
    public void PlayFightMusic()
    {
        CurrentTrack = MusicNames.None;
    }
    public void PlayFightMusicNoTransition()
    {
        _currentTrack = MusicNames.None;
        _music.setParameterByName("CurrentDisaster", (float)MusicNames.None);
    }
}

public enum MusicNames
{
    None,
    Eruption,
    LavaFlood,
    LightningStorm,
    MeteorShower,
    Tornado
}