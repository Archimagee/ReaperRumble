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
    Entity _groupToSpawnTo;



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
                if (ghost.ValueRO.ghostId == spawnRequest.ValueRO.GroupID) _groupToSpawnTo = ghostEntity;
            }
            if (!EntityManager.HasBuffer<SoulBufferElement>(_groupToSpawnTo)) ecb.AddBuffer<SoulBufferElement>(_groupToSpawnTo);



            float randomisation = 0.5f;

            for (int i = 0; i < spawnRequest.ValueRO.Amount; i++)
            {
                float3 spawnPos = new float3(UnityEngine.Random.Range(-randomisation, randomisation), UnityEngine.Random.Range(-randomisation, randomisation), UnityEngine.Random.Range(-randomisation, randomisation));
                spawnPos += spawnRequest.ValueRO.Position;

                Entity soul = EntityManager.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().SoulPrefabEntity);
                ecb.SetName(soul, "Soul");
                ecb.SetComponent(soul, new LocalTransform { Position = spawnPos, Scale = 1f, Rotation = quaternion.identity });
                ecb.SetComponent(soul, new Soul { Speed = 7.5f, SeparationForce = 1.125f });
                ecb.AddComponent(soul, new SoulGroupMember { MyGroup = _groupToSpawnTo });

                ecb.AppendToBuffer(_groupToSpawnTo, new SoulBufferElement { Soul = soul });
            }

            ecb.DestroyEntity(recieveRpcEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}