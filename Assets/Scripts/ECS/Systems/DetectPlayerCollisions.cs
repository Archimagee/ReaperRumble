using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Collections;
using UnityEngine;



[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class DetectPlayerCollisions : SystemBase
{
    //private EndFixedStepSimulationEntityCommandBufferSystem endECBSystem;

    protected override void OnCreate()
    {
        //endECBSystem = new EndFixedStepSimulationEntityCommandBufferSystem();
    }


    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        EntityQuery query = SystemAPI.QueryBuilder().WithAll<Player>().Build();

        NativeArray<Entity> players = query.ToEntityArray(Allocator.Temp);
        NativeHashMap<Entity, Entity> playerSoulGroups = new(players.Length, Allocator.Temp);

        foreach (Entity entity in players)
            playerSoulGroups.Add(entity, SystemAPI.GetComponent<PlayerSoulGroup>(entity).MySoulGroup);

        players.Dispose();



        query = SystemAPI.QueryBuilder().WithAll<Soul>().Build();

        NativeArray<Entity> soulEntities = query.ToEntityArray(Allocator.TempJob);



        var job = new PlayerSoulCollisionJob
        {
            //PlayerSoulGroups = playerSoulGroups,
            //SoulEntities = soulEntities,
            //Ecb = ecb
        };

        Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);

        soulEntities.Dispose();
        playerSoulGroups.Dispose();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}



[BurstCompile]
struct PlayerSoulCollisionJob : ITriggerEventsJob
{
    //public NativeHashMap<Entity, Entity> PlayerSoulGroups;
    //public NativeArray<Entity> SoulEntities;
    //public EntityCommandBuffer Ecb;

    public void Execute(TriggerEvent triggerEvent)
    {
        //Entity entityA = triggerEvent.EntityA;
        //Entity entityB = triggerEvent.EntityB;

        Debug.Log("Triggerevent");

        //if (PlayerSoulGroups.ContainsKey(entityA) && SoulEntities.Contains(entityB))
        //{
        //    Ecb.DestroyEntity(entityB);
        //}
        //else if (PlayerSoulGroups.ContainsKey(entityB) && SoulEntities.Contains(entityA))
        //{
        //    Ecb.DestroyEntity(entityA);
        //}
    }
}