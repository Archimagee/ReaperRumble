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
        NativeArray<Entity> queryResults = _query.ToEntityArray(Allocator.Temp);
        NativeHashMap<Entity, float3> soulGroupTargetPositions = new NativeHashMap<Entity, float3>(queryResults.Length, Allocator.TempJob);
        foreach (Entity entity in queryResults)
        {
            soulGroupTargetPositions.Add(entity, state.EntityManager.GetComponentObject<Transform>(entity).position);
        }
        queryResults.Dispose();

        EntityManager entityManager = state.EntityManager;
        new MoveSoulGroupJob
        {
            Ecb = _ecbs.CreateCommandBuffer(World.DefaultGameObjectInjectionWorld.EntityManager.WorldUnmanaged).AsParallelWriter(),
            SoulGroupTargetPositions = soulGroupTargetPositions
        }.ScheduleParallel();
        state.CompleteDependency();

        soulGroupTargetPositions.Dispose();
    }
}



[BurstCompile]
[WithAll(typeof(SoulGroupTag))]
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

            currentPosition += math.normalizesafe(targetPosition - currentPosition) * math.max(math.distance(currentPosition, targetPosition) - 20f, 0f) * 0.05f;
            currentPosition.y += math.normalizesafe(targetPosition - currentPosition).y * math.distance(currentPosition, targetPosition) * 0.02f;

            Ecb.SetComponent<LocalTransform>(index, entity, new LocalTransform { Position = currentPosition });
        }
    }
}