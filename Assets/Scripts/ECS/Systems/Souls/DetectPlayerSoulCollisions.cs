using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Collections;
using Unity.Physics.Systems;
using Unity.NetCode;



[BurstCompile]
[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class DetectPlayerSoulCollisions : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<Soul>();
        RequireForUpdate<Player>();
    }



    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new(Allocator.TempJob);



        NativeArray<Entity> players = SystemAPI.QueryBuilder().WithAll<Player>().WithAll<PlayerSoulGroup>().Build().ToEntityArray(Allocator.Temp);

        NativeHashMap<Entity, Entity> playerSoulGroups = new(players.Length, Allocator.TempJob);
        foreach (Entity playerEntity in players)
        {
            playerSoulGroups.Add(playerEntity, SystemAPI.GetComponent<PlayerSoulGroup>(playerEntity).MySoulGroup);
        }

        players.Dispose();



        NativeArray<Entity> soulEntities = SystemAPI.QueryBuilder().WithAll<Soul>().Build().ToEntityArray(Allocator.Temp);
        NativeHashMap<Entity, Entity> soulSoulGroups = new(soulEntities.Length, Allocator.TempJob);
        foreach (Entity soulEntity in soulEntities)
        {
            soulSoulGroups.Add(soulEntity, SystemAPI.GetComponent<SoulGroupMember>(soulEntity).MyGroup);
        }

        soulEntities.Dispose();



        var job = new PlayerSoulCollisionJob
        {
            PlayerSoulGroups = playerSoulGroups,
            SoulSoulGroups = soulSoulGroups,
            Ecb = ecb
        };

        Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
        Dependency.Complete();

        playerSoulGroups.Dispose();
        soulSoulGroups.Dispose();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}



[BurstCompile]
struct PlayerSoulCollisionJob : ITriggerEventsJob
{
    public NativeHashMap<Entity, Entity> PlayerSoulGroups;
    public NativeHashMap<Entity, Entity> SoulSoulGroups;
    public EntityCommandBuffer Ecb;

    public void Execute(TriggerEvent triggerEvent)
    {
        Entity entityA = triggerEvent.EntityA;
        Entity entityB = triggerEvent.EntityB;



        if (PlayerSoulGroups.ContainsKey(entityA) && SoulSoulGroups.ContainsKey(entityB))
        {
            HandleSoulCollision(entityA, entityB);
        }
        else if (PlayerSoulGroups.ContainsKey(entityB) && SoulSoulGroups.ContainsKey(entityA))
        {
            HandleSoulCollision(entityB, entityA);
        }
    }



    public void HandleSoulCollision(Entity player, Entity soul)
    {
        if (SoulSoulGroups[soul] != PlayerSoulGroups[player])
        {
            Ecb.AddComponent(soul, new ChangeSoulGroup { SoulGroupToMoveTo = PlayerSoulGroups[player] });
        }
    }
}