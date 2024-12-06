using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Physics.Systems;
using Unity.Transforms;
using Unity.VisualScripting;



[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))]
[UpdateAfter(typeof(SoulMovementSystem))]
[UpdateAfter(typeof(PlayerEntityHitboxMovementSystem))]
public partial struct PlayerSoulCollisionSystem : ISystem
{
    private EndSimulationEntityCommandBufferSystem.Singleton _ecbs;



    public void OnStartRunning(ref SystemState state)
    {
        _ecbs = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
    }



    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityQuery playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerEntityHitboxComponent>().Build();
        NativeArray<Entity> playerArray = playerQuery.ToEntityArray(Allocator.TempJob);
        NativeArray<Entity> playerGroups = new NativeArray<Entity>(playerArray.Length, Allocator.TempJob);
        for (int i = 0; i < playerArray.Length; i++)
        {
            playerGroups[i] = state.EntityManager.GetComponentData<PlayerEntityHitboxComponent>(playerArray[i]).MyGroup;
        }



        EntityQuery soulQuery = SystemAPI.QueryBuilder().WithAll<SoulComponent>().Build();
        NativeArray<Entity> soulArray = soulQuery.ToEntityArray(Allocator.TempJob);
        NativeArray<Entity> soulGroups = new NativeArray<Entity>(soulArray.Length, Allocator.TempJob);
        NativeArray<LocalTransform> soulTransforms = new NativeArray<LocalTransform>(soulArray.Length, Allocator.TempJob);
        NativeArray<DynamicBuffer<SoulBufferElement>> soulGroupBuffers = new NativeArray<DynamicBuffer<SoulBufferElement>>(soulArray.Length, Allocator.TempJob);
        for (int i = 0; i < soulArray.Length; i++)
        {
            soulGroups[i] = state.EntityManager.GetComponentData<SoulComponent>(soulArray[i]).MyGroup;
            soulGroupBuffers[i] = state.EntityManager.GetBuffer<SoulBufferElement>(soulGroups[i]);
        }



        //EntityCommandBuffer ecb = _ecbs.CreateCommandBuffer(state.EntityManager.WorldUnmanaged);
        //foreach (TriggerEvent triggerEvent in SystemAPI.GetSingleton<SimulationSingleton>().AsSimulation().TriggerEvents)
        //{
        //    Entity entityA = triggerEvent.EntityA;
        //    Entity entityB = triggerEvent.EntityB;
        //    bool found = false;
        //    Entity soul = entityA;
        //    Entity player = entityA;

        //    if (soulArray.Contains(entityA) && playerArray.Contains(entityB))
        //    {
        //        soul = entityA;
        //        player = entityB;
        //        found = true;
        //    }
        //    else if (soulArray.Contains(entityB) && playerArray.Contains(entityA))
        //    {
        //        player = entityA;
        //        soul = entityB;
        //        found = true;
        //    }



        //    if (found)
        //    {
        //        int soulIndex = soulArray.IndexOf(soul);
        //        ecb.AppendToBuffer<SoulBufferElement>(playerGroups[playerArray.IndexOf(player)], new SoulBufferElement { Soul = soul });

        //        DynamicBuffer<SoulBufferElement> buffer = soulGroupBuffers[soulIndex];
        //        NativeArray<SoulBufferElement> newBufferElements = buffer.AsNativeArray();
        //        buffer.Clear();
        //        for (int i = 0; i < newBufferElements.Length; i++)
        //        {
        //            if (newBufferElements[i].Soul != soul) ecb.AppendToBuffer(soulGroups[soulIndex], new SoulBufferElement { Soul = newBufferElements[i].Soul });
        //        }
        //    }
        //}



        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        PlayerSoulCollisionTriggerJob triggerJob = new PlayerSoulCollisionTriggerJob
        {
            //Ecb = _ecbs.CreateCommandBuffer(state.EntityManager.World),
            Ecb = ecb,
            Players = playerArray,
            PlayerGroups = playerGroups,
            Souls = soulArray,
            SoulGroups = soulGroups,
            SoulTransforms = soulTransforms,
            SoulGroupBuffers = soulGroupBuffers
        };
        state.Dependency = triggerJob.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        state.CompleteDependency();



        playerGroups.Dispose();
        playerArray.Dispose();
        soulGroups.Dispose();
        soulArray.Dispose();
        soulTransforms.Dispose();
        soulGroupBuffers.Dispose();
    }
}



[BurstCompile]
public partial struct PlayerSoulCollisionTriggerJob : ITriggerEventsJob
{
    [DeallocateOnJobCompletion, ReadOnly] public NativeArray<Entity> Players;
    [DeallocateOnJobCompletion, ReadOnly] public NativeArray<Entity> PlayerGroups;
    [DeallocateOnJobCompletion, ReadOnly] public NativeArray<Entity> Souls;
    [DeallocateOnJobCompletion, ReadOnly] public NativeArray<Entity> SoulGroups;
    [DeallocateOnJobCompletion, ReadOnly] public NativeArray<LocalTransform> SoulTransforms;
    [DeallocateOnJobCompletion, ReadOnly] public NativeArray<DynamicBuffer<SoulBufferElement>> SoulGroupBuffers;
    [ReadOnly] public EntityCommandBuffer Ecb;

    [DeallocateOnJobCompletion] private NativeArray<SoulBufferElement> _newBufferElements;

    [BurstCompile]
    public void Execute(TriggerEvent triggerEvent)
    {
        Debug.Log("Test");
        Entity entityA = triggerEvent.EntityA;
        Entity entityB = triggerEvent.EntityB;
        bool found = false;
        Entity soul = entityA;
        Entity player = entityA;

        if (Souls.Contains(entityA) && Players.Contains(entityB))
        {
            soul = entityA;
            player = entityB;
            found = true;
        }
        else if (Souls.Contains(entityB) && Players.Contains(entityA))
        {
            player = entityA;
            soul = entityB;
            found = true;
        }



        if (found)
        {
            int soulIndex = Souls.IndexOf(soul);
            int playerIndex = Players.IndexOf(player);

            if (PlayerGroups[playerIndex] != SoulGroups[soulIndex])
            {
                Ecb.AppendToBuffer<SoulBufferElement>(PlayerGroups[playerIndex], new SoulBufferElement { Soul = soul });

                DynamicBuffer<SoulBufferElement> buffer = SoulGroupBuffers[soulIndex];
                _newBufferElements = buffer.AsNativeArray();
                buffer.Clear();
                for (int i = 0; i < _newBufferElements.Length; i++)
                {
                    if (_newBufferElements[i].Soul != soul) Ecb.AppendToBuffer(SoulGroups[soulIndex], new SoulBufferElement { Soul = _newBufferElements[i].Soul });
                }
            }
        }
    }
}