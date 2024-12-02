using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;



public partial class SpawnSoulSystem : SystemBase
{
    private SoulSpawnerComponent _spawner;
    private EntityManager _entityManager;
    private NativeQueue<int> _spawnQueue = new NativeQueue<int>(Allocator.Persistent);
    private BufferLookup<SoulBufferElement> _lookup;



    protected override void OnCreate()
    {
        RequireForUpdate<SoulSpawnerComponent>();
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        SoulSpawnManager.OnSpawnSouls += OnSoulsSpawned;
        _lookup = GetBufferLookup<SoulBufferElement>(false);
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



    protected override void OnUpdate()
    {
        Entity spawnerEntity = SystemAPI.GetSingletonEntity<SoulSpawnerComponent>();

        if (_spawnQueue.Count > 0f)
        {
            float randomisation = _spawner.SpawnPositionRandomisation;
            int amount = _spawnQueue.Dequeue();

            Entity soulGroup = EntityManager.Instantiate(_spawner.SoulGroupPrefabEntity);
            EntityManager.SetComponentData(soulGroup, new LocalTransform { Position = _spawner.SpawnPosition });
            _lookup.Update(this);
            _lookup.TryGetBuffer(soulGroup, out DynamicBuffer<SoulBufferElement> buffer);

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            EntityCommandBuffer.ParallelWriter ecbpw = ecb.AsParallelWriter();

            for (int i = 0; i < amount; i++)
            {
                float3 randPos = new float3(UnityEngine.Random.Range(-randomisation, randomisation), UnityEngine.Random.Range(-randomisation, randomisation), UnityEngine.Random.Range(-randomisation, randomisation));
                new SpawnSoulJob
                {
                    Ecb = ecbpw,
                    RandPos = randPos,
                    Group = soulGroup,
                }.ScheduleParallel();
            }
            this.CompleteDependency();
            ecb.Playback(_entityManager);
            ecb.Dispose();
        }
    }



    private void OnSoulsSpawned(int amount)
    {
        _spawnQueue.Enqueue(amount);
    }
}



[WithAll(typeof(SoulSpawnerComponent))]
public partial struct SpawnSoulJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    public float3 RandPos;
    public Entity Group;

    public void Execute([ChunkIndexInQuery] int index, ref SoulSpawnerComponent spawner)
    {
        Entity soul = Ecb.Instantiate(index, spawner.SoulPrefabEntity);

        Ecb.SetComponent(index, soul, new LocalTransform { Position = RandPos + spawner.SpawnPosition, Scale = 1f });
        Ecb.SetComponent(index, soul, new SoulComponent { Speed = 0.12f, SeparationForce = 0.1f, MyGroup = Group });
        Ecb.AppendToBuffer(index, Group, new SoulBufferElement { Soul = soul });
    }
}