using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class MovePlayerCollider : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<PlayerCollider>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        foreach ((RefRO<PlayerCollider> playerCollider, RefRW<LocalTransform> colliderTransform) in SystemAPI.Query<RefRO<PlayerCollider>, RefRW<LocalTransform>>().WithAll<GhostOwnerIsLocal>())
        {
            float3 targetPos = SystemAPI.GetComponent<LocalTransform>(playerCollider.ValueRO.FollowTarget).Position;
            colliderTransform.ValueRW.Position = targetPos;
        }
    }
}