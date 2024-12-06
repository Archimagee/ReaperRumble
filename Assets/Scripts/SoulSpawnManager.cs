using UnityEngine;



public class SoulSpawnManager : MonoBehaviour
{
    public static SoulSpawnManager Instance;

    public delegate void SoulsSpawnedCallback(int amount, Transform objectToFollow);
    public static SoulsSpawnedCallback OnSpawnSouls;

    [SerializeField] private Transform _playerTransform;



    public void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }



    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) RaiseSoulsSpawned(50, _playerTransform);
    }



    public void RaiseSoulsSpawned(int amount, Transform objectToFollow)
    {
        OnSpawnSouls?.Invoke(amount, objectToFollow);
    }
}