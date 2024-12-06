using UnityEngine;



public class PlayerEntityHitboxSpawnManager : MonoBehaviour
{
    public static PlayerEntityHitboxSpawnManager Instance;

    public delegate void PlayerEntityHitboxSpawnedCallback(Transform objectToFollow);
    public static PlayerEntityHitboxSpawnedCallback OnSpawnPlayerEntityHitbox;

    [SerializeField] private Transform _playerTransform;



    public void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }



    public void Start()
    {
        if (_playerTransform != null) RaisePlayerEntityHitboxSpawned(_playerTransform);
    }



    public void RaisePlayerEntityHitboxSpawned(Transform objectToFollow)
    {
        OnSpawnPlayerEntityHitbox?.Invoke(objectToFollow);
    }
}