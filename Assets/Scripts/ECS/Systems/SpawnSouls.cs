using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class SpawnSouls : SystemBase
{
    private BufferLookup<LinkedEntityGroup> _playerLookup;



    protected override void OnCreate()
    {
        RequireForUpdate<SoulSpawner>();
        RequireForUpdate<SpawnSoulsRequestRPC>();
        _playerLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>(true);
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        _playerLookup.Update(this);



        foreach ((RefRO<ReceiveRpcCommandRequest> rpcCommandRequest, RefRO <SpawnSoulsRequestRPC> spawnRequest, Entity entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO <SpawnSoulsRequestRPC>>().WithEntityAccess())
        {
            //Entity soulGroup = EntityManager.Instantiate(_spawner.SoulGroupPrefabEntity);
            //EntityManager.SetComponentData(soulGroup, new LocalTransform { Position = _spawner.SpawnPosition });
            //if (objectToFollow != null) EntityManager.AddComponentObject(soulGroup, objectToFollow);

            float randomisation = 10f;

            _playerLookup.TryGetBuffer(rpcCommandRequest.ValueRO.SourceConnection, out DynamicBuffer<LinkedEntityGroup> buffer);
            Entity playerGroup = SystemAPI.GetComponent<SoulGroup>(buffer[1].Value).MySoulGroup;

            for (int i = 0; i < spawnRequest.ValueRO.Amount; i++)
            {
                float3 spawnPos = new float3(UnityEngine.Random.Range(-randomisation, randomisation), UnityEngine.Random.Range(-randomisation, randomisation), UnityEngine.Random.Range(-randomisation, randomisation));
                spawnPos.y += 4f;

                Entity soul = EntityManager.Instantiate(SystemAPI.GetSingleton<SoulSpawner>().SoulPrefabEntity);
                ecb.SetComponent(soul, new LocalTransform { Position = spawnPos, Scale = 1f, Rotation = quaternion.identity });
                ecb.SetComponent(soul, new Soul { Speed = 0.2f, SeparationForce = 0.03f, MyGroup = playerGroup });
                ecb.AppendToBuffer(playerGroup, new SoulBufferElement { Soul = soul });
            }
            ecb.DestroyEntity(entity);
        }



        CompleteDependency();
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}



//[BurstCompile]
//public partial struct SpawnSoulJob : IJobEntity
//{
//    public EntityCommandBuffer.ParallelWriter Ecb;
//    [ReadOnly] public float3 SpawnPosition;
//    [ReadOnly] public Entity Group;
//    [ReadOnly] public Entity SoulPrefab;

//    public void Execute([ChunkIndexInQuery] int index)
//    {
//        Entity soul = Ecb.Instantiate(index, SoulPrefab);

//        LocalTransform soulTransform = new LocalTransform { Position = SpawnPosition, Scale = 1f };
//        Ecb.SetComponent(index, soul, soulTransform);
//        Ecb.SetComponent(index, soul, new Soul { Speed = 0.2f, SeparationForce = 0.03f, MyGroup = Group });
//        Ecb.AppendToBuffer(index, Group, new SoulBufferElement { Soul = soul });
//    }
//}