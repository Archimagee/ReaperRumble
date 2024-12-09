using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Burst;



[BurstCompile]
public partial class SpawnSoulSystem : SystemBase
{
    private SoulSpawnerComponent _spawner;
    private EntityManager _entityManager;
    private NativeQueue<int> _spawnQueue = new NativeQueue<int>(Allocator.Persistent);



    protected override void OnCreate()
    {
        RequireForUpdate<SoulSpawnerComponent>();
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        SoulSpawnManager.OnSpawnSouls += OnSoulsSpawned;
    }

    protected override void OnStartRunning()
    {
        _spawner = SystemAPI.GetSingleton<SoulSpawnerComponent>();
    }

    protected override void OnDestroy()
    {
        SoulSpawnManager.OnSpawnSouls -= OnSoulsSpawned;
        _spawnQueue.Dispose();
    }



    protected override void OnUpdate() { }



    [BurstCompile]
    private void OnSoulsSpawned(int amount, Transform objectToFollow)
    {
        Entity spawnerEntity = SystemAPI.GetSingletonEntity<SoulSpawnerComponent>();

        float randomisation = _spawner.SpawnPositionRandomisation;

        Entity soulGroup = EntityManager.Instantiate(_spawner.SoulGroupPrefabEntity);
        EntityManager.SetComponentData(soulGroup, new LocalTransform { Position = _spawner.SpawnPosition });
        if (objectToFollow != null) EntityManager.AddComponentObject(soulGroup, objectToFollow);

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        EntityCommandBuffer.ParallelWriter ecbpw = ecb.AsParallelWriter();

        for (int i = 0; i < amount; i++)
        {
            float3 randPos = new float3(UnityEngine.Random.Range(-randomisation, randomisation), UnityEngine.Random.Range(-randomisation, randomisation), UnityEngine.Random.Range(-randomisation, randomisation));
            new SpawnSoulJob
            {
                Ecb = ecbpw,
                RandPos = randPos,
                Group = soulGroup
            }.ScheduleParallel();
        }
        this.CompleteDependency();
        ecb.Playback(_entityManager);
        ecb.Dispose();
    }
}



[BurstCompile]
[WithAll(typeof(SoulSpawnerComponent))]
public partial struct SpawnSoulJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    [ReadOnly] public float3 RandPos;
    [ReadOnly] public Entity Group;

    public void Execute([ChunkIndexInQuery] int index, ref SoulSpawnerComponent spawner)
    {
        Entity soul = Ecb.Instantiate(index, spawner.SoulPrefabEntity);

        LocalTransform soulTransform = new LocalTransform { Position = RandPos + spawner.SpawnPosition, Scale = 1f };
        Ecb.SetComponent(index, soul, soulTransform);
        Ecb.SetComponent(index, soul, new SoulComponent { Speed = spawner.Speed, SeparationForce = spawner.SeparationForce, MyGroup = Group });
        Ecb.AppendToBuffer(index, Group, new SoulBufferElement { Soul = soul });
    }
}