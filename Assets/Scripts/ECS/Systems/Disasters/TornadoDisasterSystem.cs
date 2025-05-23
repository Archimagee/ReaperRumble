using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class TornadoDisasterSystem : SystemBase
{
    Unity.Mathematics.Random random;



    protected override void OnCreate()
    {
        RequireForUpdate<TornadoDisasterData>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        foreach ((RefRO<DisasterData> disasterData, RefRW<TornadoDisasterData> tornadoData, RefRO <EventSeed> seed, RefRW <LocalTransform> localTransformTornado, Entity disasterEntity) in
            SystemAPI.Query<RefRO<DisasterData>, RefRW<TornadoDisasterData>, RefRO<EventSeed>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            double currentTime = SystemAPI.Time.ElapsedTime;

            if (tornadoData.ValueRO.StartTime == 0.0)
            {
                tornadoData.ValueRW.StartTime = currentTime;
                random = new Unity.Mathematics.Random();
                random.InitState(seed.ValueRO.Seed);

                localTransformTornado.ValueRW.Position = new float3(-500f, -500f, -500f);
            }
            if (tornadoData.ValueRO.SpawnTime == 0.0 && SystemAPI.Time.ElapsedTime >= tornadoData.ValueRO.StartTime + tornadoData.ValueRO.SpawnDelaySeconds)
            {
                tornadoData.ValueRW.SpawnTime = currentTime;
                localTransformTornado.ValueRW.Position = GetNewTarget(tornadoData.ValueRO.MovementBounds);
                tornadoData.ValueRW.CurrentDirection = math.normalizesafe(tornadoData.ValueRO.CurrentTarget - localTransformTornado.ValueRO.Position);
            }



            if (tornadoData.ValueRO.SpawnTime != 0.0) HandleMovement(tornadoData, localTransformTornado, currentTime);



            foreach ((RefRW<Player> player, RefRO<LocalTransform> localTransformPlayer, Entity playerEntity) in SystemAPI.Query<RefRW<Player>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                float playerDistance = math.distance(new float3(localTransformPlayer.ValueRO.Position.x, 0f, localTransformPlayer.ValueRO.Position.z), new float3(localTransformTornado.ValueRO.Position.x, 0f, localTransformTornado.ValueRO.Position.z));

                if (currentTime >= tornadoData.ValueRO.LastTickedAt + tornadoData.ValueRO.TickTimeSeconds)
                {
                    if (playerDistance <= tornadoData.ValueRO.TornadoInnerRange)
                    {
                        Entity rpcEntity = ecb.CreateEntity();
                        ecb.AddComponent(rpcEntity, new OrphanSoulsRequestRPC
                        {
                            GroupID = SystemAPI.GetComponent<GhostInstance>(SystemAPI.GetComponent<PlayerSoulGroup>(playerEntity).MySoulGroup).ghostId,
                            Amount = 1
                        });
                        ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);

                        tornadoData.ValueRW.LastTickedAt = SystemAPI.Time.ElapsedTime;
                    }
                }

                if (playerDistance <= tornadoData.ValueRO.TornadoOuterRange)
                {
                    Entity rpcEntity = ecb.CreateEntity();

                    ecb.AddComponent(rpcEntity, new ApplyKnockbackToPlayerRequestRPC()
                    {
                        KnockbackDirection = new float3(localTransformTornado.ValueRO.Position + new float3(0f, 8f, 0f) - localTransformPlayer.ValueRO.Position),
                        Strength = 0.1f * math.lerp(tornadoData.ValueRO.KnockbackStrength, 0f, math.clamp(playerDistance - tornadoData.ValueRO.TornadoInnerRange, 0f, tornadoData.ValueRO.TornadoOuterRange) - tornadoData.ValueRO.TornadoInnerRange / (tornadoData.ValueRO.TornadoOuterRange - tornadoData.ValueRO.TornadoInnerRange)),
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



    private float3 GetNewTarget(AABB bounds)
    {
        float3 target = random.NextFloat3(bounds.Min, bounds.Max);
        target.y = 0f;
        return target;
    }

    private void HandleMovement(RefRW<TornadoDisasterData> tornado, RefRW<LocalTransform> transform, double currentTime)
    {
        if (currentTime >= tornado.ValueRO.ChangeTargetAt)
        {
            tornado.ValueRW.CurrentTarget = GetNewTarget(tornado.ValueRO.MovementBounds);
            tornado.ValueRW.ChangeTargetAt = currentTime + random.NextDouble(tornado.ValueRO.MinTargetChangeTimeSeconds, tornado.ValueRO.MaxTargetChangeTimeSeconds);
        }

        tornado.ValueRW.CurrentDirection = math.normalize((float3)Vector3.RotateTowards(
            tornado.ValueRO.CurrentDirection,
            tornado.ValueRO.CurrentTarget - transform.ValueRO.Position,
            tornado.ValueRO.RotationAmountRadians, 10f));

        transform.ValueRW.Position += tornado.ValueRO.CurrentDirection * tornado.ValueRO.TornadoMoveSpeed;
    }
}