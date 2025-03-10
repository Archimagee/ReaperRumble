using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Collections;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class SpawnSoulsOverTimeServerSystem : SystemBase
{
    private readonly double _minTimeBetweenSpawns = 7.0;
    private readonly double _maxTimeBetweenSpawns = 15.0;
    private double _nextSpawnAt = 5.0;

    private NativeQueue<Entity> _spawnQueue = new(Allocator.Persistent);

    private readonly AABB _spawnBounds = new()
    {
        Center = float3.zero,
        Extents = new float3(48f, 15f, 39f)
    };

    private Entity _soulGroupPrefab;
    Unity.Mathematics.Random _random = new();



    [BurstCompile]
    protected override void OnCreate()
    {
        _random.InitState(140783u);

        RequireForUpdate<EntitySpawnerPrefabs>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        if (_soulGroupPrefab == Entity.Null)
        {
            _soulGroupPrefab = SystemAPI.GetSingleton<EntitySpawnerPrefabs>().SoulGroupPrefabEntity;
        }



        if (_spawnQueue.Count > 0)
        {
            Entity newSoulGroup = _spawnQueue.Dequeue();

            float3 spawnPosition = GetSpawnPosition();
            spawnPosition.y += 1f;
            EntityManager.SetComponentData(newSoulGroup, new LocalTransform() { Position = spawnPosition });

            Debug.Log(newSoulGroup);
            Debug.Log(SystemAPI.GetComponent<GhostInstance>(newSoulGroup).ghostId);

            Entity rpcEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(rpcEntity, new SpawnSoulsRequestRPC() { Amount = 3, GroupID = SystemAPI.GetComponent<GhostInstance>(newSoulGroup).ghostId, Position = spawnPosition });
            EntityManager.AddComponent<SendRpcCommandRequest>(rpcEntity);
        }



        double currentTime = SystemAPI.Time.ElapsedTime;

        if (currentTime >= _nextSpawnAt)
        {
            _spawnQueue.Enqueue(EntityManager.Instantiate(_soulGroupPrefab));

            _nextSpawnAt += _random.NextDouble(_minTimeBetweenSpawns, _maxTimeBetweenSpawns);
        }
    }



    private float3 GetSpawnPosition()
    {
        float3 randomSpawnPos = _random.NextFloat3(_spawnBounds.Min, _spawnBounds.Max);
        randomSpawnPos.y = _spawnBounds.Extents.y;

        RaycastInput raycastInput = new RaycastInput()
        {
            Start = randomSpawnPos,
            End = randomSpawnPos + new float3(0f, -30f, 0f),
            Filter = new CollisionFilter { BelongsTo = ~0u, CollidesWith = 1u << 2 }
        };

        Unity.Physics.RaycastHit hit = new();
        bool hasHit = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CastRay(raycastInput, out hit);

        return hit.Position;
    }
}