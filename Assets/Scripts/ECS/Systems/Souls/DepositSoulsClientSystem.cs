using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;
using UnityEngine;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class DepositSoulsClientSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<DepositSoulsRequestRPC>();
        RequireForUpdate<ReceiveRpcCommandRequest>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        NativeArray<Entity> players = SystemAPI.QueryBuilder().WithAll<PlayerData>().Build().ToEntityArray(Allocator.Temp);



        foreach ((RefRO<ReceiveRpcCommandRequest> rpcCommandRequest, RefRO<DepositSoulsRequestRPC> depositRequest, Entity recieveRpcEntity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<DepositSoulsRequestRPC>>().WithEntityAccess())
        {
            foreach (Entity player in players)
                if (SystemAPI.GetComponent<PlayerData>(player).PlayerNumber == depositRequest.ValueRO.PlayerNumber)
                {
                    DepositSouls(player, ecb);
                }

            ecb.DestroyEntity(recieveRpcEntity);
        }

        players.Dispose();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }



    [BurstCompile]
    private void DepositSouls(Entity player, EntityCommandBuffer ecb)
    {
        DynamicBuffer<SoulBufferElement> soulGroupBuffer = SystemAPI.GetBuffer<SoulBufferElement>(SystemAPI.GetComponent<PlayerSoulGroup>(player).MySoulGroup);

        foreach (SoulBufferElement soulBufferElement in soulGroupBuffer)
        {
            ecb.DestroyEntity(soulBufferElement.Soul);
        }

        soulGroupBuffer.Clear();
        ecb.AddComponent(player, new DepositSoulCooldown() { CanDepositAt = SystemAPI.Time.ElapsedTime + 30000 });

        Debug.Log("Deposit!");
    }
}