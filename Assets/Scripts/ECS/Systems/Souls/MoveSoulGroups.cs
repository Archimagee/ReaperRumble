using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;



[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct MoveSoulGroups : ISystem
{
    EntityQuery _query;
    float _groupHeightOffset;



    public void OnCreate(ref SystemState state)
    {
        _query = state.EntityManager.CreateEntityQuery(typeof(SoulGroupTarget));
        state.RequireForUpdate(_query);
        _groupHeightOffset = 2f;
    }

    public void OnDestroy(ref SystemState state)
    {
        _query.Dispose();
    }



    public void OnUpdate(ref SystemState state)
    {
        NativeArray<Entity> queryResults = _query.ToEntityArray(Allocator.Temp);
        NativeHashMap<Entity, float3> soulGroupTargetPositions = new NativeHashMap<Entity, float3>(queryResults.Length, Allocator.TempJob);
        queryResults.Dispose();
        foreach ((RefRO<SoulGroupTarget> target, Entity soulGroupEntity) in SystemAPI.Query<RefRO<SoulGroupTarget>>().WithEntityAccess())
        {
            Entity groupTarget = target.ValueRO.MyTarget;

            if (groupTarget != Entity.Null)
                soulGroupTargetPositions.Add(soulGroupEntity, SystemAPI.GetComponent<LocalTransform>(groupTarget).Position + new float3 (0f, _groupHeightOffset, 0f));
            else soulGroupTargetPositions.Add(soulGroupEntity, SystemAPI.GetComponent<LocalTransform>(soulGroupEntity).Position);
        }

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        new MoveSoulGroupJob
        {
            Ecb = ecb.AsParallelWriter(),
            SoulGroupTargetPositions = soulGroupTargetPositions
        }.ScheduleParallel();
        state.CompleteDependency();
        ecb.Playback(state.EntityManager);

        ecb.Dispose();
        soulGroupTargetPositions.Dispose();
    }
}



[BurstCompile]
[WithAll(typeof(SoulGroupTarget))]
public partial struct MoveSoulGroupJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    [ReadOnly] public NativeHashMap<Entity, float3> SoulGroupTargetPositions;

    [BurstCompile]
    public void Execute([ChunkIndexInQuery] int index, in Entity entity, in LocalTransform localTransform)
    {
        if (SoulGroupTargetPositions.ContainsKey(entity))
        {
            float3 currentPosition = localTransform.Position;
            float3 targetPosition = SoulGroupTargetPositions[entity];

            currentPosition += math.normalizesafe(targetPosition - currentPosition) * math.max(math.distance(currentPosition, targetPosition) - 9f, 0f) * 0.05f;
            currentPosition.y += math.normalizesafe(targetPosition - currentPosition).y * math.distance(currentPosition, targetPosition) * 0.02f;

            Ecb.SetComponent<LocalTransform>(index, entity, new LocalTransform { Position = currentPosition, Scale = 1f, Rotation = quaternion.identity });
        }
    }
}