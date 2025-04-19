using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class TornadoDisasterSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<TornadoDisasterData>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        foreach ((RefRO<DisasterData> disasterData, RefRW<TornadoDisasterData> tornadoData, RefRW<LocalTransform> localTransformTornado, Entity disasterEntity) in
            SystemAPI.Query<RefRO<DisasterData>, RefRW<TornadoDisasterData>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            double currentTime = SystemAPI.Time.ElapsedTime;

            if (tornadoData.ValueRO.StartTime == 0.0) tornadoData.ValueRW.StartTime = currentTime;



            foreach ((RefRW<Player> player, RefRO<LocalTransform> localTransformPlayer, Entity playerEntity) in SystemAPI.Query<RefRW<Player>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                float playerDistance = math.length((localTransformPlayer.ValueRO.Position.x + localTransformPlayer.ValueRO.Position.z) - (localTransformTornado.ValueRO.Position.x + localTransformTornado.ValueRO.Position.z));

                if (currentTime >= tornadoData.ValueRO.LastTickedAt + tornadoData.ValueRO.TickTimeSeconds)
                {
                    if (playerDistance <= tornadoData.ValueRO.TornadoInnerRange)
                    {
                        if (SystemAPI.GetBuffer<SoulBufferElement>(SystemAPI.GetComponent<PlayerSoulGroup>(playerEntity).MySoulGroup).Length > 0)
                        {
                            Entity rpcEntity = ecb.CreateEntity();
                            ecb.AddComponent(rpcEntity, new OrphanSoulsRequestRPC
                            {
                                GroupID = SystemAPI.GetComponent<GhostInstance>(SystemAPI.GetComponent<PlayerSoulGroup>(playerEntity).MySoulGroup).ghostId,
                                Amount = tornadoData.ValueRO.SoulsOrphaned,
                                Position = SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetComponent<PlayerSoulGroup>(playerEntity).MySoulGroup).Position
                            });
                            ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
                        }

                        tornadoData.ValueRW.LastTickedAt = SystemAPI.Time.ElapsedTime;
                    }
                }

                if (playerDistance <= tornadoData.ValueRO.TornadoOuterRange)
                {
                    Entity rpcEntity = ecb.CreateEntity();

                    ecb.AddComponent(rpcEntity, new ApplyKnockbackToPlayerRequestRPC()
                    {
                        KnockbackDirection = new float3(localTransformTornado.ValueRO.Position.x - localTransformPlayer.ValueRO.Position.x, 0f, localTransformTornado.ValueRO.Position.z - localTransformPlayer.ValueRO.Position.z),
                        Strength = math.lerp(tornadoData.ValueRO.KnockbackStrength, 0f, math.clamp(playerDistance - tornadoData.ValueRO.TornadoInnerRange, 0f, tornadoData.ValueRO.TornadoOuterRange) - tornadoData.ValueRO.TornadoInnerRange / (tornadoData.ValueRO.TornadoOuterRange - tornadoData.ValueRO.TornadoInnerRange)),
                        PlayerGhostID = SystemAPI.GetComponent<GhostInstance>(playerEntity).ghostId
                    });
                    ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
                }
            }

            if (SystemAPI.Time.ElapsedTime >= disasterData.ValueRO.TimeLastsForSeconds + tornadoData.ValueRO.StartTime) ecb.DestroyEntity(disasterEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}