using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Collections;
using Unity.Physics.Systems;
using UnityEngine;



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



        NativeArray<Entity> meteorEntities = SystemAPI.QueryBuilder().WithAll<MeteorData>().Build().ToEntityArray(Allocator.TempJob);

        var job = new MeteorCollisionJob()
        {
            Ecb = ecb,
            MeteorEntities = meteorEntities,
            MeteorImpactVFXPrefab = _meteorImpactVFXPrefabEntity
        };

        Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
        Dependency.Complete();

        meteorEntities.Dispose();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}



[BurstCompile]
struct MeteorCollisionJob : ICollisionEventsJob
{
    public EntityCommandBuffer Ecb;
    public NativeArray<Entity> MeteorEntities;
    public Entity MeteorImpactVFXPrefab;

    public void Execute(CollisionEvent collisionEvent)
    {
        if (MeteorEntities.Contains(collisionEvent.EntityA))
        {
            HandleMeteorCollision(collisionEvent.EntityA);
        }
        else if (MeteorEntities.Contains(collisionEvent.EntityB))
        {
            HandleMeteorCollision(collisionEvent.EntityB);
        }


    }



    public void HandleMeteorCollision(Entity meteor)
    {
        Ecb.DestroyEntity(meteor);
    }
}