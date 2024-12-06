using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;



[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct SoulGroupMovementSystem : ISystem
{
    private EndInitializationEntityCommandBufferSystem.Singleton _ecbs;
    EntityQuery _query;



    public void OnCreate(ref SystemState state)
    {
        _ecbs = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
        _query = state.EntityManager.CreateEntityQuery(typeof(SoulGroupTag), typeof(Transform));
        state.RequireForUpdate(_query);
    }

    public void OnDestroy(ref SystemState state)
    {
        _query.Dispose();
    }



    public void OnUpdate(ref SystemState state)
    {
        EntityManager entityManager = state.EntityManager;
        NativeArray<Entity> queryResults = _query.ToEntityArray(Allocator.Temp);

        foreach (Entity entity in queryResults)
        {
            float3 targetPosition = entityManager.GetComponentObject<Transform>(entity).position;
            targetPosition.y += 3f;
            LocalTransform transform = entityManager.GetComponentData<LocalTransform>(entity);

            new MoveSoulGroupJob
            {
                Ecb = _ecbs.CreateCommandBuffer(World.DefaultGameObjectInjectionWorld.EntityManager.WorldUnmanaged).AsParallelWriter(),
                TargetPosition = targetPosition,
                CurrentPosition = transform.Position
            }.ScheduleParallel();
        }

        queryResults.Dispose();
    }
}



[BurstCompile]
[WithAll(typeof(SoulGroupTag))]
public partial struct MoveSoulGroupJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    public float3 TargetPosition;
    public float3 CurrentPosition;

    [BurstCompile]
    public void Execute([ChunkIndexInQuery] int index, in Entity entity)
    {
        CurrentPosition += math.normalizesafe(TargetPosition - CurrentPosition) * math.max(math.distance(CurrentPosition, TargetPosition) - 15f, 0f) * 0.05f;
        CurrentPosition.y += math.normalizesafe(TargetPosition - CurrentPosition).y * math.distance(CurrentPosition, TargetPosition) * 0.02f;

        Ecb.SetComponent<LocalTransform>(index, entity, new LocalTransform { Position = CurrentPosition });
    }
}