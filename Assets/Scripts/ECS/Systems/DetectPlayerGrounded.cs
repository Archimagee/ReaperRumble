using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.NetCode;
using UnityEditor;



[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
//[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class DetectPlayerGrounded : SystemBase
{
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        foreach ((RefRO<LocalTransform> localTransform, Entity entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithEntityAccess().WithAll<GhostOwnerIsLocal>())
        {
            //RaycastInput raycastInput = new RaycastInput()
            //{
            //    Start = localTransform.ValueRO.Position + new float3(0f, -0.9f, 0f),
            //    End = localTransform.ValueRO.Position + new float3(0f, -1.1f, 0f),
            //    Filter = new CollisionFilter { BelongsTo = ~0u, CollidesWith = 3 }
            //};

            //Unity.Physics.RaycastHit hit = new Unity.Physics.RaycastHit();
            //bool hasHit = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CastRay(raycastInput, out hit);

            NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
            bool hasHit = SystemAPI.GetSingleton<PhysicsWorldSingleton>().OverlapSphere(localTransform.ValueRO.Position + new float3(0f, -1f, 0f), 0.1f, ref hits, new CollisionFilter()
            {
                BelongsTo = ~0u,
                CollidesWith = 3
            });
            Debug.Log(hasHit);

            if (hasHit)
            {
                ecb.SetComponent<IsPlayerGrounded>(entity, new IsPlayerGrounded { IsGrounded = true });
            }
            hits.Dispose();
        }



        //EntityQuery query = SystemAPI.QueryBuilder().WithAll<GroundTag>().Build();
        //NativeArray<Entity> groundEntities = query.ToEntityArray(Allocator.TempJob);

        //foreach ((RefRO<IsPlayerGrounded> playerGrounded, Entity playerEntity) in SystemAPI.Query<RefRO<IsPlayerGrounded>>().WithEntityAccess().WithAll<GhostOwnerIsLocal>())
        //{
        //    var job = new PlayerGroundCollisionJob
        //    {
        //        GroundEntities = groundEntities,
        //        Player = playerEntity,
        //        Ecb = ecb
        //    };

        //    Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
        //    Dependency.Complete();
        //}

        //groundEntities.Dispose();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}



[BurstCompile]
struct PlayerGroundCollisionJob : ICollisionEventsJob
{
    public NativeArray<Entity> GroundEntities;
    public Entity Player;
    public EntityCommandBuffer Ecb;

    public void Execute(CollisionEvent collisionEvent)
    {
        Entity entityA = collisionEvent.EntityA;
        Entity entityB = collisionEvent.EntityB;



        if (GroundEntities.Contains(entityA) && Player == entityB)
        {
            HandleGroundCollision(entityB);
        }
        else if (GroundEntities.Contains(entityB) && Player == entityA)
        {
            HandleGroundCollision(entityA);
        }
    }



    public void HandleGroundCollision(Entity player)
    {
        Ecb.SetComponent(player, new IsPlayerGrounded() { IsGrounded = true });
    }
}