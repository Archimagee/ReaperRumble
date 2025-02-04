using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class SpawnSoulsClientSystem : SystemBase
{
    Entity _playerGroup;



    protected override void OnCreate()
    {
        RequireForUpdate<SpawnSoulsRequestRPC>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        foreach ((RefRO<ReceiveRpcCommandRequest> rpcCommandRequest, RefRO <SpawnSoulsRequestRPC> spawnRequest, Entity recieveRpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO <SpawnSoulsRequestRPC>>().WithEntityAccess())
        {
            foreach ((RefRO<GhostInstance> ghost, Entity ghostEntity) in SystemAPI.Query<RefRO<GhostInstance>>().WithEntityAccess())
            {
                if (ghost.ValueRO.ghostId == spawnRequest.ValueRO.GroupID) _playerGroup = ghostEntity;
            }
            if (!EntityManager.HasBuffer<SoulBufferElement>(_playerGroup)) ecb.AddBuffer<SoulBufferElement>(_playerGroup);


            float randomisation = 10f;

            for (int i = 0; i < spawnRequest.ValueRO.Amount; i++)
            {
                float3 spawnPos = new float3(UnityEngine.Random.Range(-randomisation, randomisation), UnityEngine.Random.Range(-randomisation, randomisation), UnityEngine.Random.Range(-randomisation, randomisation));
                spawnPos.y += 4f;

                Entity soul = EntityManager.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().SoulPrefabEntity);
                ecb.SetComponent(soul, new LocalTransform { Position = spawnPos, Scale = 1f, Rotation = quaternion.identity });
                ecb.SetComponent(soul, new Soul { Speed = 0.2f, SeparationForce = 0.03f, MyGroup = _playerGroup });

                ecb.AppendToBuffer(_playerGroup, new SoulBufferElement { Soul = soul });
            }

            ecb.DestroyEntity(recieveRpcEntity);
        }



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