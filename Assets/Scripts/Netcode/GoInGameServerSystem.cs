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
        state.RequireForUpdate<PlayerSpawner>();
        state.RequireForUpdate<SoulGroupSpawner>();
    }



    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach ((RefRO<ReceiveRpcCommandRequest> recieveRpcCommandRequest, Entity entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<GoInGameRequestRPC>().WithEntityAccess())
        {
            Entity sourceConnection = recieveRpcCommandRequest.ValueRO.SourceConnection;
            ecb.AddComponent<NetworkStreamInGame>(sourceConnection);

            Entity newPlayer = ecb.Instantiate(SystemAPI.GetSingleton<PlayerSpawner>().PlayerPrefabEntity);
            Entity newSoulGroup = ecb.Instantiate(SystemAPI.GetSingleton<SoulGroupSpawner>().SoulGroupPrefab);
            ecb.SetComponent<SoulGroupTarget>(newSoulGroup, new SoulGroupTarget { MyTarget = newPlayer });
            ecb.AddComponent(newPlayer, new GhostOwner { NetworkId = SystemAPI.GetComponent<NetworkId>(sourceConnection).Value });
            ecb.AddComponent(newSoulGroup, new GhostOwner { NetworkId = SystemAPI.GetComponent<NetworkId>(sourceConnection).Value });
            ecb.SetComponent<LocalTransform>(newPlayer, new LocalTransform { Position = new float3(UnityEngine.Random.Range(-10f, 10f), 2f, 0f), Scale = 1f, Rotation = quaternion.identity });
            ecb.SetComponent<SoulGroup>(newPlayer, new SoulGroup { MySoulGroup = newSoulGroup });
            ecb.AddBuffer<SoulBufferElement>(newSoulGroup);
            ecb.AppendToBuffer(sourceConnection, new LinkedEntityGroup { Value = newPlayer });

            ecb.DestroyEntity(entity);
        }



        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}