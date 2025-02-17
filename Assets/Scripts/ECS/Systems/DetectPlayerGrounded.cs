using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;



[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class DetectPlayerGrounded : SystemBase
{
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        EntityQuery query = SystemAPI.QueryBuilder().WithAll<Player>().Build();

        NativeArray<Entity> players = query.ToEntityArray(Allocator.Temp);

        NativeHashMap<Entity, float> playerGroundDistances = new(players.Length, Allocator.TempJob);
        foreach (Entity entity in players)
        {
            float3 playerPos = SystemAPI.GetComponent<LocalTransform>(entity).Position;

            RaycastInput raycastInput = new RaycastInput()
            {
                Start = playerPos + new float3(0f, 1f, 0f),
                End = playerPos + new float3(0f, -5f, 0f),
                Filter = new CollisionFilter { BelongsTo = ~0u, CollidesWith = 1, GroupIndex = 0 }
            };

            RaycastHit hit = new RaycastHit();
            bool hasHit = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CastRay(raycastInput, out hit);

            float distance = 100f;
            if (hasHit)
            {
                distance = (hit.Fraction * 6f) - 2f;
            }

            playerGroundDistances.Add(entity, distance);
        }

        players.Dispose();



        var job = new PlayerCheckGroundedJob
        {
            PlayerGroundDistances = playerGroundDistances,
            Ecb = ecb
        };

        Dependency = job.Schedule(Dependency);
        Dependency.Complete();

        playerGroundDistances.Dispose();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}



[BurstCompile]
partial struct PlayerCheckGroundedJob : IJobEntity
{
    public NativeHashMap<Entity, float> PlayerGroundDistances;
    public EntityCommandBuffer Ecb;

    public void Execute(in Entity entity, in IsPlayerGrounded player)
    {
        float distance = PlayerGroundDistances[entity];
        if (distance != 100f && distance <= 0.2f) Ecb.SetComponent<IsPlayerGrounded>(entity, new IsPlayerGrounded { IsGrounded = true });
    }
}