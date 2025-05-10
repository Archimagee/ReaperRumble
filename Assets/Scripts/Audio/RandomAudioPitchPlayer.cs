using UnityEngine;

public class RandomAudioPitchPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    public void Awake()
    {
        _audioSource.pitch = Random.Range(0.8f, 1.2f);
        _audioSource.Play();
    }
}
