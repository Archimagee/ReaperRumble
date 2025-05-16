using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;



[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct MoveSoulGroups : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SoulGroupTag>();
    }



    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRO<SoulGroupTarget> target, RefRW<LocalTransform> localTransform, Entity soulGroupEntity) in SystemAPI.Query<RefRO<SoulGroupTarget>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            if (target.ValueRO.MyTarget == Entity.Null) continue;
            else
            {
                float3 currentPosition = localTransform.ValueRO.Position;
                float3 targetPosition = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.MyTarget).Position + new float3(0f, 2f, 0f);

                currentPosition += math.normalizesafe(targetPosition - currentPosition) * math.max(math.distance(currentPosition, targetPosition) - 9f, 0f) * 0.05f;
                currentPosition.y += math.normalizesafe(targetPosition - currentPosition).y * math.distance(currentPosition, targetPosition) * 0.02f;

                localTransform.ValueRW.Position = currentPosition;
            }
        }
    }
}