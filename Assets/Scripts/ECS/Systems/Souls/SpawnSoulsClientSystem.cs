using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.NetCode;
using UnityEngine;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class SpawnSoulsClientSystem : SystemBase
{
    private NativeQueue<SoulSpawnData> _soulsToSpawn = new(Allocator.Persistent);
    private readonly float _spawnPosRandomisation = 0.5f;



    protected override void OnCreate()
    {
        //RequireForUpdate<SpawnSoulsRequestRPC>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        if (_soulsToSpawn.Count > 0)
        {
            SoulSpawnData soulSpawnData = _soulsToSpawn.Dequeue();
            for (int i = 0; i < soulSpawnData.AmountOfSoulsToSpawn; i++)
            {
                float3 spawnPos = SystemAPI.GetComponent<LocalTransform>(soulSpawnData.SoulGroupToSpawnTo).Position;
                spawnPos += new float3(UnityEngine.Random.Range(-_spawnPosRandomisation, _spawnPosRandomisation), UnityEngine.Random.Range(-_spawnPosRandomisation, _spawnPosRandomisation), UnityEngine.Random.Range(-_spawnPosRandomisation, _spawnPosRandomisation));

                Entity soul = EntityManager.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().SoulPrefabEntity);
                ecb.SetName(soul, "Soul");
                ecb.SetComponent(soul, new LocalTransform { Position = spawnPos, Scale = 1f, Rotation = quaternion.identity });
                ecb.SetComponent(soul, new Soul { Speed = 9f, SeparationForce = 1.125f });
                ecb.AddComponent(soul, new SoulGroupMember { MyGroup = soulSpawnData.SoulGroupToSpawnTo });

                ecb.AppendToBuffer(soulSpawnData.SoulGroupToSpawnTo, new SoulBufferElement { Soul = soul });
            }
        }



        foreach ((RefRO<ReceiveRpcCommandRequest> rpcCommandRequest, RefRO <SpawnSoulsRequestRPC> spawnRequest, Entity recieveRpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO <SpawnSoulsRequestRPC>>().WithEntityAccess())
        {
            foreach ((RefRO<GhostInstance> ghost, Entity ghostEntity) in SystemAPI.Query<RefRO<GhostInstance>>().WithEntityAccess())
            {
                if (ghost.ValueRO.ghostId == spawnRequest.ValueRO.GroupID)
                {
                    Entity groupToSpawnTo = ghostEntity;
                    if (!EntityManager.HasBuffer<SoulBufferElement>(groupToSpawnTo)) ecb.AddBuffer<SoulBufferElement>(groupToSpawnTo);

                    _soulsToSpawn.Enqueue(new SoulSpawnData { SoulGroupToSpawnTo = groupToSpawnTo, AmountOfSoulsToSpawn = spawnRequest.ValueRO.Amount });

                    ecb.DestroyEntity(recieveRpcEntity);
                }
            }
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }



    private struct SoulSpawnData
    {
        public Entity SoulGroupToSpawnTo;
        public int AmountOfSoulsToSpawn;
    }
}