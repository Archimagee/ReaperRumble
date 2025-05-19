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
public partial class DetectEruptionRockCollision : SystemBase
{
    Entity _rockImpactVFXPrefabEntity;



    protected override void OnCreate()
    {
        RequireForUpdate<EruptionRockData>();
        RequireForUpdate<VFXPrefabs>();
    }



    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        if (_rockImpactVFXPrefabEntity == Entity.Null) _rockImpactVFXPrefabEntity = SystemAPI.GetSingleton<VFXPrefabs>().MeteorImpactVFXPrefabEntity;



        NativeArray<Entity> rockEntities = SystemAPI.QueryBuilder().WithAll<EruptionRockData>().Build().ToEntityArray(Allocator.Temp);
        NativeHashMap<Entity, float3> rockPositions = new(rockEntities.Length, Allocator.TempJob);
        foreach (Entity meteor in rockEntities) rockPositions.Add(meteor, SystemAPI.GetComponent<LocalTransform>(meteor).Position);
        rockEntities.Dispose();

        var job = new EruptionRockCollisionJob()
        {
            Ecb = ecb,
            MeteorPositions = rockPositions
        };

        Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
        Dependency.Complete();

        rockPositions.Dispose();



        foreach ((RefRO<EruptionRockImpact> impact, RefRO<EruptionRockData> rockData, Entity rockEntity) in SystemAPI.Query<RefRO<EruptionRockImpact>, RefRO<EruptionRockData>>().WithEntityAccess())
        {
            Entity impactVFX = ecb.Instantiate(_rockImpactVFXPrefabEntity);
            ecb.SetComponent(impactVFX, new LocalTransform() { Position = impact.ValueRO.Position, Rotation = quaternion.identity, Scale = 4f });

            NativeList<DistanceHit> hits = new(Allocator.Temp);
            SystemAPI.GetSingleton<PhysicsWorldSingleton>().OverlapSphere(impact.ValueRO.Position, rockData.ValueRO.ImpactRadius, ref hits, new CollisionFilter() { BelongsTo = ~0u, CollidesWith = 1u << 0 });

            foreach (DistanceHit hit in hits)
            {
                ecb.SetComponent(hit.Entity, new Knockback() { KnockbackValue = SystemAPI.GetComponent<Knockback>(hit.Entity).KnockbackValue + rockData.ValueRO.KnockbackStrength });

                ecb.AddComponent(SystemAPI.GetComponent<PlayerSoulGroup>(hit.Entity).MySoulGroup, new OrphanSouls() { Amount = 3 });
            }

            ecb.DestroyEntity(rockEntity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}



[BurstCompile]
struct EruptionRockCollisionJob : ICollisionEventsJob
{
    public EntityCommandBuffer Ecb;
    public NativeHashMap<Entity, float3> MeteorPositions;

    public void Execute(CollisionEvent collisionEvent)
    {
        if (MeteorPositions.ContainsKey(collisionEvent.EntityA))
        {
            Ecb.AddComponent(collisionEvent.EntityA, new EruptionRockImpact() { Position = MeteorPositions[collisionEvent.EntityA] });
        }
        else if (MeteorPositions.ContainsKey(collisionEvent.EntityB))
        {
            Ecb.AddComponent(collisionEvent.EntityB, new EruptionRockImpact() { Position = MeteorPositions[collisionEvent.EntityB] });
        }
    }
}



public struct EruptionRockImpact : IComponentData
{
    public float3 Position;
}