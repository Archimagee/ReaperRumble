//using Unity.Burst;
//using Unity.Entities;
//using Unity.Physics;
//using Unity.Collections;
//using Unity.Jobs;
//using UnityEngine;



//[BurstCompile]
//[RequireMatchingQueriesForUpdate]
//[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
//public partial struct DetectPlayerSoulCollision : ISystem
//{
//    [BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {
//        EntityQuery playerQuery = SystemAPI.QueryBuilder().WithAll<Player>().Build();
//        NativeArray<Entity> playerArray = playerQuery.ToEntityArray(Allocator.TempJob);
//        NativeHashMap<Entity, Entity> playerGroups = new NativeHashMap<Entity, Entity>(playerArray.Length, Allocator.TempJob);
//        foreach (Entity player in playerArray)
//        {
//            playerGroups.Add(player, state.EntityManager.GetComponentData<SoulGroup>(player).MySoulGroup);
//        }
//        playerArray.Dispose();



//        EntityQuery soulQuery = SystemAPI.QueryBuilder().WithAll<Soul>().Build();
//        NativeArray<Entity> soulArray = soulQuery.ToEntityArray(Allocator.TempJob);
//        NativeHashMap<Entity, DynamicBuffer<SoulBufferElement>> soulGroupBuffers = new NativeHashMap<Entity, DynamicBuffer<SoulBufferElement>>(soulArray.Length, Allocator.TempJob);
//        NativeHashMap<Entity, Entity> soulGroups = new NativeHashMap<Entity, Entity>(soulArray.Length, Allocator.TempJob);
//        foreach (Entity soul in soulArray)
//        {
//            Entity group = state.EntityManager.GetComponentData<Soul>(soul).MyGroup;
//            soulGroups.Add(soul, group);
//            if (!soulGroupBuffers.ContainsKey(group))
//            {
//                soulGroupBuffers.Add(group, state.EntityManager.GetBuffer<SoulBufferElement>(group));
//            }
//        }
//        soulArray.Dispose();



//        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
//        PlayerSoulCollisionTriggerJob triggerJob = new PlayerSoulCollisionTriggerJob
//        {
//            Ecb = ecb,
//            PlayerGroups = playerGroups,
//            SoulGroups = soulGroups,
//            SoulGroupBuffers = soulGroupBuffers
//        };
//        state.Dependency = triggerJob.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);

//        ecb.Playback(state.EntityManager);
//        ecb.Dispose();
//        state.CompleteDependency();



//        playerGroups.Dispose();
//        soulGroupBuffers.Dispose();
//        soulGroups.Dispose();
//    }
//}



//[BurstCompile]
//public partial struct PlayerSoulCollisionTriggerJob : ITriggerEventsJob
//{
//    [ReadOnly] public NativeHashMap<Entity, Entity> PlayerGroups;
//    [ReadOnly] public NativeHashMap<Entity, Entity> SoulGroups;
//    [ReadOnly] public NativeHashMap<Entity, DynamicBuffer<SoulBufferElement>> SoulGroupBuffers;
//    [ReadOnly] public EntityCommandBuffer Ecb;

//    [BurstCompile]
//    public void Execute(TriggerEvent triggerEvent)
//    {
//        NativeArray<SoulBufferElement> newBufferElements;

//        Debug.Log("Test");
//        Entity entityA = triggerEvent.EntityA;
//        Entity entityB = triggerEvent.EntityB;
//        bool found = false;
//        Entity soul = entityA;
//        Entity player = entityA;

//        if (SoulGroups.ContainsKey(entityA) && PlayerGroups.ContainsKey(entityB))
//        {
//            soul = entityA;
//            player = entityB;
//            found = true;
//        }
//        else if (SoulGroups.ContainsKey(entityB) && PlayerGroups.ContainsKey(entityA))
//        {
//            player = entityA;
//            soul = entityB;
//            found = true;
//        }



//        if (found)
//        {
//            Entity playerGroup = PlayerGroups[player];
//            Entity soulGroup = SoulGroups[soul];

//            if (playerGroup != soulGroup)
//            {
//                Ecb.AppendToBuffer<SoulBufferElement>(playerGroup, new SoulBufferElement { Soul = soul });

//                DynamicBuffer<SoulBufferElement> buffer = SoulGroupBuffers[soulGroup];
//                newBufferElements = buffer.AsNativeArray();
//                buffer.Clear();
//                for (int i = 0; i < newBufferElements.Length; i++)
//                {
//                    if (newBufferElements[i].Soul != soul) Ecb.AppendToBuffer(soulGroup, new SoulBufferElement { Soul = newBufferElements[i].Soul });
//                }
//            }
//        }
//    }
//}