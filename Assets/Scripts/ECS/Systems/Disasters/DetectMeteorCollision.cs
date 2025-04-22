using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Collections;
using Unity.Physics.Systems;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;



[BurstCompile]
[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class DetectMeteorCollision : SystemBase
{
    Entity _meteorImpactVFXPrefabEntity;



    protected override void OnCreate()
    {
        RequireForUpdate<MeteorData>();
        RequireForUpdate<VFXPrefabs>();
    }



    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        if (_meteorImpactVFXPrefabEntity == Entity.Null) _meteorImpactVFXPrefabEntity = SystemAPI.GetSingleton<VFXPrefabs>().MeteorImpactVFXPrefabEntity;



        NativeArray<Entity> meteorEntities = SystemAPI.QueryBuilder().WithAll<MeteorData>().Build().ToEntityArray(Allocator.Temp);
        NativeHashMap<Entity, float3> meteorPositions = new(meteorEntities.Length, Allocator.TempJob);
        foreach (Entity meteor in meteorEntities) meteorPositions.Add(meteor, SystemAPI.GetComponent<LocalTransform>(meteor).Position);
        meteorEntities.Dispose();

        var job = new MeteorCollisionJob()
        {
            Ecb = ecb,
            MeteorPositions = meteorPositions
        };

        Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
        Dependency.Complete();

        meteorPositions.Dispose();



        foreach ((RefRO<MeteorImpact> impact, RefRO<MeteorData> meteorData, Entity meteorEntity) in SystemAPI.Query<RefRO<MeteorImpact>, RefRO<MeteorData>>().WithEntityAccess())
        {
            Entity impactVFX = ecb.Instantiate(_meteorImpactVFXPrefabEntity);
            ecb.SetComponent(impactVFX, new LocalTransform() { Position = impact.ValueRO.Position, Rotation = quaternion.identity, Scale = 4f });

            NativeList<DistanceHit> hits = new(Allocator.Temp);
            SystemAPI.GetSingleton<PhysicsWorldSingleton>().OverlapSphere(impact.ValueRO.Position, meteorData.ValueRO.ImpactRadius, ref hits, new CollisionFilter() { BelongsTo = ~0u, CollidesWith = 1u << 0 });

            foreach (DistanceHit hit in hits)
            {
                Entity rpcEntity = ecb.CreateEntity();
                ecb.AddComponent(rpcEntity, new ApplyKnockbackToPlayerRequestRPC()
                {
                    KnockbackDirection = math.normalizesafe(SystemAPI.GetComponent<LocalTransform>(hit.Entity).Position - impact.ValueRO.Position),
                    Strength = meteorData.ValueRO.KnockbackStrength,
                    PlayerGhostID = SystemAPI.GetComponent<GhostInstance>(hit.Entity).ghostId
                });
                ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);

                if (!SystemAPI.GetBuffer<SoulBufferElement>(SystemAPI.GetComponent<PlayerSoulGroup>(hit.Entity).MySoulGroup).IsEmpty)
                {
                    rpcEntity = ecb.CreateEntity();
                    ecb.AddComponent(rpcEntity, new OrphanSoulsRequestRPC
                    {
                        GroupID = SystemAPI.GetComponent<GhostInstance>(SystemAPI.GetComponent<PlayerSoulGroup>(hit.Entity).MySoulGroup).ghostId,
                        Amount = math.min(SystemAPI.GetBuffer<SoulBufferElement>(SystemAPI.GetComponent<PlayerSoulGroup>(hit.Entity).MySoulGroup).Length, 4),
                        Position = impact.ValueRO.Position
                    });
                    ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
                }
            }

            ecb.DestroyEntity(meteorEntity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}



[BurstCompile]
struct MeteorCollisionJob : ICollisionEventsJob
{
    public EntityCommandBuffer Ecb;
    public NativeHashMap<Entity, float3> MeteorPositions;

    public void Execute(CollisionEvent collisionEvent)
    {
        if (MeteorPositions.ContainsKey(collisionEvent.EntityA))
        {
            Ecb.AddComponent(collisionEvent.EntityA, new MeteorImpact() { Position = MeteorPositions[collisionEvent.EntityA] });
        }
        else if (MeteorPositions.ContainsKey(collisionEvent.EntityB))
        {
            Ecb.AddComponent(collisionEvent.EntityB, new MeteorImpact() { Position = MeteorPositions[collisionEvent.EntityB] });
        }
    }
}



public struct MeteorImpact : IComponentData
{
    public float3 Position;
}