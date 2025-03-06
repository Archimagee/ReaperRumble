using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;



[BurstCompile]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial class DetectPlayerGrounded : SystemBase
{
    protected override void OnUpdate()
    {
        foreach ((RefRO<LocalTransform> localTransform, RefRO<PhysicsVelocity> velocity, RefRW<IsPlayerGrounded> grounded) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PhysicsVelocity>, RefRW<IsPlayerGrounded>>().WithAll<GhostOwnerIsLocal>())
        {
            NativeList<DistanceHit> hits = new(Allocator.Temp);
            bool hasHit = SystemAPI.GetSingleton<PhysicsWorldSingleton>().OverlapSphere(localTransform.ValueRO.Position + new float3(0f, -1f, 0f), 0.1f, ref hits, new CollisionFilter()
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << 2
            });

            if (hasHit && velocity.ValueRO.Linear.y <= 0f)
            {
                grounded.ValueRW.IsGrounded = true;
            }
            hits.Dispose();
        }
    }
}