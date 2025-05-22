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
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
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
            Entity rpcEntity = ecb.CreateEntity();
            ecb.AddComponent(rpcEntity, new SpawnVFXRequest() { Effect = RRVFX.Explosion, Location = impact.ValueRO.Position, Rotation = quaternion.identity });
            ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);

            NativeList<DistanceHit> hits = new(Allocator.Temp);
            SystemAPI.GetSingleton<PhysicsWorldSingleton>().OverlapSphere(impact.ValueRO.Position, meteorData.ValueRO.ImpactRadius, ref hits, new CollisionFilter() { BelongsTo = ~0u, CollidesWith = 1u << 0 });

            foreach (DistanceHit hit in hits)
            {
                float3 currentKnockback = SystemAPI.GetComponent<Knockback>(hit.Entity).KnockbackValue;
                ecb.SetComponent(hit.Entity, new Knockback() { KnockbackValue = currentKnockback = math.normalizesafe(SystemAPI.GetComponent<LocalTransform>(hit.Entity).Position - impact.ValueRO.Position) * meteorData.ValueRO.KnockbackStrength });
                ecb.AddComponent(SystemAPI.GetComponent<PlayerSoulGroup>(hit.Entity).MySoulGroup, new OrphanSouls() { Amount = 3 });
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