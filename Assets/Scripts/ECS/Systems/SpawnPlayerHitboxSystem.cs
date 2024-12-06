using Unity.Entities;
using UnityEngine;
using Unity.Burst;
using System.Collections.Generic;



[BurstCompile]
public partial class SpawnEntityPlayerHitboxSystem : SystemBase
{
    private PlayerEntityHitboxSpawnerComponent _hitboxSpawner;
    private SoulSpawnerComponent _soulSpawner;
    private Queue<Transform> spawnQueue = new();



    protected override void OnCreate()
    {
        PlayerEntityHitboxSpawnManager.OnSpawnPlayerEntityHitbox += OnSpawnPlayerEntityHitbox;
    }

    protected override void OnStartRunning()
    {
        _hitboxSpawner = SystemAPI.GetSingleton<PlayerEntityHitboxSpawnerComponent>();
        _soulSpawner = SystemAPI.GetSingleton<SoulSpawnerComponent>();
    }

    protected override void OnDestroy()
    {
        PlayerEntityHitboxSpawnManager.OnSpawnPlayerEntityHitbox -= OnSpawnPlayerEntityHitbox;
    }



    protected override void OnUpdate()
    {
        if (spawnQueue.Count > 0)
        {
            Entity playerEntityHitbox = EntityManager.Instantiate(_hitboxSpawner.PlayerHitboxPrefabEntity);
            Transform transformToFollow = spawnQueue.Dequeue();
            EntityManager.AddComponentObject(playerEntityHitbox, transformToFollow);
            Entity playerSoulGroup = EntityManager.Instantiate(_soulSpawner.SoulGroupPrefabEntity);
            EntityManager.SetComponentData(playerEntityHitbox, new PlayerEntityHitboxComponent { MyGroup = playerSoulGroup });
            EntityManager.AddComponentObject(playerSoulGroup, transformToFollow);
        }
    }



    private void OnSpawnPlayerEntityHitbox(Transform objectToFollow)
    {
        spawnQueue.Enqueue(objectToFollow);
    }
}