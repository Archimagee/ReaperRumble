using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.Collections;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class SpawnSoulsOverTimeServerSystem : SystemBase
{
    private double _nextSpawnAt = 1.0;

    private readonly AABB _spawnBounds = new()
    {
        Center = float3.zero,
        Extents = new float3(48f, 30f, 39f)
    };

    Unity.Mathematics.Random _random = new();



    [BurstCompile]
    protected override void OnCreate()
    {
        _random.InitState((uint)System.DateTime.Now.Millisecond * (uint)System.DateTime.Now.Second);

        RequireForUpdate<EntitySpawnerPrefabs>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        double currentTime = SystemAPI.Time.ElapsedTime;

        if (currentTime >= _nextSpawnAt && SystemAPI.QueryBuilder().WithAll<SoulGroupTag>().Build().ToEntityArray(Allocator.Temp).Length <= 20)
        {
            float3 spawnPosition = GetSpawnPosition() + new float3(0f, 1.5f, 0f);

            Entity newSoulGroup = EntityManager.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().SoulGroupPrefabEntity);
            EntityManager.SetName(newSoulGroup, "Random Spawn Soul Group");
            EntityManager.SetComponentData(newSoulGroup, new LocalTransform() { Position = spawnPosition, Rotation = quaternion.identity, Scale = 1f });
            EntityManager.AddBuffer<SoulBufferElement>(newSoulGroup);

            for (int i = 0; i < _random.NextInt(2, 5); i++)
            {
                Entity newSoul = EntityManager.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().SoulPrefabEntity);
                EntityManager.SetName(newSoul, "Random Spawn Soul");
                EntityManager.SetComponentData(newSoul, new LocalTransform()
                {
                    Position = spawnPosition + _random.NextFloat3(new float3(-1f, -1f, -1f), new float3(1f, 1f, 1f)),
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
                EntityManager.SetComponentData<SoulGroupMember>(newSoul, new SoulGroupMember() { MyGroup = newSoulGroup });
                EntityManager.GetBuffer<SoulBufferElement>(newSoulGroup).Add(new SoulBufferElement() { Soul = newSoul });
            }

            _nextSpawnAt += _random.NextDouble(0.1d, 0.1d);
        }
    }



    private float3 GetSpawnPosition()
    {
        float3 randomSpawnPos = _random.NextFloat3(_spawnBounds.Min, _spawnBounds.Max);
        randomSpawnPos.y = _spawnBounds.Extents.y;

        RaycastInput raycastInput = new RaycastInput()
        {
            Start = randomSpawnPos,
            End = randomSpawnPos + new float3(0f, -50f, 0f),
            Filter = new CollisionFilter { BelongsTo = ~0u, CollidesWith = 1u << 2 }
        };

        Unity.Physics.RaycastHit hit = new();
        bool hasHit = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CastRay(raycastInput, out hit);

        return hit.Position;
    }
}