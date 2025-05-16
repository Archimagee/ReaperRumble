using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class LavaFloodDisasterSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<LavaFloodDisasterData>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        foreach ((RefRO<DisasterData> disasterData, RefRW<LavaFloodDisasterData> lavaData, RefRW<LocalTransform> localTransformFlood, Entity disasterEntity) in 
            SystemAPI.Query<RefRO<DisasterData>, RefRW<LavaFloodDisasterData>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            double currentTime = SystemAPI.Time.ElapsedTime;

            if (lavaData.ValueRO.StartTime == 0.0) lavaData.ValueRW.StartTime = currentTime;

            if (currentTime >= lavaData.ValueRO.StartTime + lavaData.ValueRO.FloodDelaySeconds)
            {
                if (currentTime >= lavaData.ValueRO.StartTime + lavaData.ValueRO.FloodDelaySeconds + lavaData.ValueRO.FloodRiseTimeSeconds)
                {
                    localTransformFlood.ValueRW.Position.y = lavaData.ValueRO.LavaEndHeight;
                }
                else
                {
                    localTransformFlood.ValueRW.Position.y = math.lerp(lavaData.ValueRO.LavaStartHeight, lavaData.ValueRO.LavaEndHeight, (float)(currentTime - lavaData.ValueRO.StartTime - lavaData.ValueRO.FloodDelaySeconds) / lavaData.ValueRO.FloodRiseTimeSeconds);
                }
            }



            foreach ((RefRW<Player> player, RefRO<LocalTransform> localTransformPlayer, Entity playerEntity) in SystemAPI.Query<RefRW<Player>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (localTransformPlayer.ValueRO.Position.y - 1f <= localTransformFlood.ValueRO.Position.y)
                {
                    if (currentTime >= lavaData.ValueRO.LastTickedAt + lavaData.ValueRO.LavaTickTimeSeconds)
                    {
                        Debug.Log("Hit");
                        Entity rpcEntity = ecb.CreateEntity();
                        ecb.AddComponent(rpcEntity, new ApplyKnockbackToPlayerRequestRPC()
                        {
                            KnockbackDirection = new float3(0f, 1f, 0f),
                            Strength = lavaData.ValueRO.LavaKnockbackStrength,
                            PlayerGhostID = SystemAPI.GetComponent<GhostInstance>(playerEntity).ghostId
                        });
                        ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);

                        rpcEntity = ecb.CreateEntity();
                        ecb.AddComponent(rpcEntity, new OrphanSoulsRequestRPC
                        {
                            GroupID = SystemAPI.GetComponent<GhostInstance>(SystemAPI.GetComponent<PlayerSoulGroup>(playerEntity).MySoulGroup).ghostId,
                            Amount = 1
                        });
                        ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);

                        lavaData.ValueRW.LastTickedAt = SystemAPI.Time.ElapsedTime;
                    }

                    player.ValueRW.Speed = 3.5f;
                }
                else player.ValueRW.Speed = 7f;
            }

            if (SystemAPI.Time.ElapsedTime >= disasterData.ValueRO.TimeLastsForSeconds + lavaData.ValueRO.StartTime) ecb.DestroyEntity(disasterEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}