using Unity.Entities;
using UnityEngine;



public class SoulSpawnManager : MonoBehaviour
{
    public static SoulSpawnManager Instance;
    private static EntityManager _entityManager;
    private static Entity _soulSpawner;

    public delegate void SoulsSpawnedCallback(int amount);
    public static SoulsSpawnedCallback OnSpawnSouls;



    public void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }



    public void Start()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        ComponentType[] components = new ComponentType[1];
        components[0] = typeof(SoulSpawnerComponent);
        _soulSpawner = _entityManager.CreateEntityQuery(components).GetSingletonEntity();
    }



    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) RaiseSoulsSpawned(20);
        }



    public void RaiseSoulsSpawned(int amount)
    {
        OnSpawnSouls?.Invoke(amount);
    }
}