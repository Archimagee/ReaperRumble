using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;



public partial struct SpawnSoulSystem : ISystem
{
    private SoulSpawner _soulSpawner;
    private EndInitializationEntityCommandBufferSystem.Singleton _ecbs;



    private EntityCommandBuffer.ParallelWriter GetCommandBuffer(ref SystemState state)
    {
        return _ecbs.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
    }



    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SoulSpawner>();
        state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        _ecbs = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
    }



    public void OnUpdate(ref SystemState state)
    {
        _soulSpawner = SystemAPI.GetSingleton<SoulSpawner>();

        float randomisation = _soulSpawner.SpawnPositionRandomisation;
        SoulBoidComponent boid = new SoulBoidComponent();

        for (int i = 0; i < _soulSpawner.SpawnAmount; i++)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            float3 randpos = new float3(UnityEngine.Random.Range(-randomisation, randomisation), UnityEngine.Random.Range(-randomisation, randomisation), UnityEngine.Random.Range(-randomisation, randomisation));
            new SpawnSoulJob { Ecb = GetCommandBuffer(ref state), Randpos = randpos, Boid = boid }.Schedule();
        }

        state.Enabled = false;
    }
}



public partial struct SpawnSoulJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    public float3 Randpos;
    public SoulBoidComponent Boid;

    public void Execute([ChunkIndexInQuery] int index, ref SoulSpawner soulSpawner)
    {
        Entity soul = Ecb.Instantiate(index, soulSpawner.SoulPrefabEntity);
        //Boid.Souls.Add(soul);

        float3 pos = Randpos;

        pos += soulSpawner.SpawnPosition;
        Ecb.SetComponent(index, soul, new LocalTransform { Position = pos, Scale = 1f });
    }
}