using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct GoInGameServerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkId>();
    }



    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRO<ReceiveRpcCommandRequest> recieveRpcCommandRequest, Entity recieveRpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<GoInGameRequestRPC>().WithEntityAccess())
        {
            Debug.Log("Going in game");
            Entity sourceConnection = recieveRpcCommandRequest.ValueRO.SourceConnection;
            ecb.AddComponent<NetworkStreamInGame>(sourceConnection);

            Entity newPlayer = ecb.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().PlayerPrefabEntity);
            Entity newSoulGroup = ecb.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().SoulGroupPrefabEntity);
            ecb.SetName(newPlayer, "Player");
            ecb.SetName(newSoulGroup, "Soul Group");

            ecb.SetComponent<PlayerSoulGroup>(newPlayer, new PlayerSoulGroup { MySoulGroup = newSoulGroup });
            ecb.SetComponent<SoulGroupTarget>(newSoulGroup, new SoulGroupTarget { MyTarget = newPlayer });

            ecb.AddComponent(newPlayer, new GhostOwner { NetworkId = SystemAPI.GetComponent<NetworkId>(sourceConnection).Value });
            ecb.AddComponent(newSoulGroup, new GhostOwner { NetworkId = SystemAPI.GetComponent<NetworkId>(sourceConnection).Value });

            ecb.SetComponent<LocalTransform>(newPlayer, new LocalTransform { Position = new float3(UnityEngine.Random.Range(-10f, 10f), 6f, 0f), Scale = 1f, Rotation = quaternion.identity });
            
            ecb.AppendToBuffer(sourceConnection, new LinkedEntityGroup { Value = newPlayer });

            ecb.DestroyEntity(recieveRpcEntity);
        }



        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}