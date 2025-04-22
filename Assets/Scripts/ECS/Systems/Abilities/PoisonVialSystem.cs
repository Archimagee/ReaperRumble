using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using System.Linq;
using Unity.NetCode;



[BurstCompile]
[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct PoisonVialSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        NativeArray<EntityQuery> queries = new NativeArray<EntityQuery>(2, Allocator.Temp);
        queries[0] = SystemAPI.QueryBuilder().WithAll<PoisonVialTag>().Build();
        queries[1] = SystemAPI.QueryBuilder().WithAll<PoisonFieldData>().Build();
        state.RequireAnyForUpdate(queries);
        queries.Dispose();
    }



    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        NativeArray<Entity> vialEntities = SystemAPI.QueryBuilder().WithAll<PoisonVialTag>().Build().ToEntityArray(Allocator.Temp);
        NativeHashMap<Entity, float3> vialPositions = new(vialEntities.Length, Allocator.TempJob);
        foreach (Entity vialEntity in vialEntities) vialPositions.Add(vialEntity, SystemAPI.GetComponent<LocalTransform>(vialEntity).Position);
        vialEntities.Dispose();

        var job = new PoisonVialCollisionJob()
        {
            Ecb = ecb,
            VialPositions = vialPositions
        };

        state.Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
        state.Dependency.Complete();

        vialPositions.Dispose();



        foreach ((RefRO<PoisonVialImpact> impact, Entity vialEntity) in SystemAPI.Query<RefRO<PoisonVialImpact>>().WithEntityAccess())
        {
            Entity poisonField = ecb.Instantiate(SystemAPI.GetSingleton<AbilityPrefabs>().PoisonFieldPrefabEntity);
            ecb.SetComponent(poisonField, new LocalTransform() { Position = impact.ValueRO.Position, Scale = 2f, Rotation = quaternion.identity });
            ecb.DestroyEntity(vialEntity);
        }



        double currentTime = SystemAPI.Time.ElapsedTime;

        foreach ((RefRW<PoisonFieldData> poisonField, RefRO<LocalTransform> poisonTransform, Entity poisonFieldEntity) in SystemAPI.Query<RefRW<PoisonFieldData>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            if (poisonField.ValueRO.StartTime == 0.0) poisonField.ValueRW.StartTime = currentTime;
            if (currentTime >= poisonField.ValueRO.StartTime + poisonField.ValueRO.DurationSeconds) ecb.DestroyEntity(poisonFieldEntity);

            if (currentTime >= poisonField.ValueRO.LastTickedAt + poisonField.ValueRO.TickTimeSeconds)
            {
                NativeList<DistanceHit> hits = new(Allocator.Temp);
                if (SystemAPI.GetSingleton<PhysicsWorldSingleton>().OverlapSphere(
                    poisonTransform.ValueRO.Position,
                    1.4f, ref hits, new CollisionFilter() { BelongsTo = ~0u, CollidesWith = 1u }))
                {
                    foreach (DistanceHit hit in hits)
                    {
                        Entity playerEntity = hit.Entity;
                        if (SystemAPI.GetComponent<LocalTransform>(playerEntity).Position.y - 1f - poisonTransform.ValueRO.Position.y <= 0.2f)
                        {
                            Entity sendRpcEntity = ecb.CreateEntity();
                            ecb.AddComponent(sendRpcEntity, new OrphanSoulsRequestRPC
                            {
                                GroupID = SystemAPI.GetComponent<GhostInstance>(SystemAPI.GetComponent<PlayerSoulGroup>(playerEntity).MySoulGroup).ghostId,
                                Amount = 1,
                                Position = poisonTransform.ValueRO.Position
                            });
                            ecb.AddComponent<ReceiveRpcCommandRequest>(sendRpcEntity);
                        }
                    }
                }

                poisonField.ValueRW.LastTickedAt = currentTime;
            }
        }



        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}



[BurstCompile]
struct PoisonVialCollisionJob : ICollisionEventsJob
{
    public EntityCommandBuffer Ecb;
    public NativeHashMap<Entity, float3> VialPositions;

    public void Execute(CollisionEvent collisionEvent)
    {
        if (VialPositions.ContainsKey(collisionEvent.EntityA))
        {
            Ecb.AddComponent(collisionEvent.EntityA, new PoisonVialImpact() { Position = VialPositions[collisionEvent.EntityA] });
        }
        else if (VialPositions.ContainsKey(collisionEvent.EntityB))
        {
            Ecb.AddComponent(collisionEvent.EntityB, new PoisonVialImpact() { Position = VialPositions[collisionEvent.EntityB] });
        }
    }
}

public partial struct PoisonVialImpact : IComponentData
{
    public float3 Position;
}