using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Collections;
using Unity.Physics.Systems;
using Unity.NetCode;



[BurstCompile]
[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class DetectPlayerSoulCollisions : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<PlayerCollider>();
    }



    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);


        EntityQuery query = SystemAPI.QueryBuilder().WithAll<Player>().WithAll<GhostOwnerIsLocal>().Build();

        NativeArray<Entity> players = query.ToEntityArray(Allocator.Temp);

        NativeHashMap<Entity, Entity> playerSoulGroups = new(players.Length, Allocator.TempJob);
        foreach (Entity entity in players)
            playerSoulGroups.Add(SystemAPI.GetComponent<SoulWorldCollider>(entity).ColliderEntity, SystemAPI.GetComponent<PlayerSoulGroup>(entity).MySoulGroup);

        players.Dispose();



        query = SystemAPI.QueryBuilder().WithAll<Soul>().Build();
        NativeArray<Entity> soulEntities = query.ToEntityArray(Allocator.Temp);
        NativeHashMap<Entity, SoulGroupMember> souls = new(soulEntities.Length, Allocator.TempJob);
        foreach (Entity soulEntity in soulEntities)
        {
            if (SystemAPI.HasComponent<SoulGroupMember>(soulEntity)) souls.Add(soulEntity, SystemAPI.GetComponent<SoulGroupMember>(soulEntity));
        }



        var job = new PlayerSoulCollisionJob
        {
            PlayerSoulGroups = playerSoulGroups,
            SoulEntities = souls,
            Ecb = ecb
        };

        Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
        Dependency.Complete();

        playerSoulGroups.Dispose();
        soulEntities.Dispose();
        souls.Dispose();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}



[BurstCompile]
struct PlayerSoulCollisionJob : ITriggerEventsJob
{
    public NativeHashMap<Entity, Entity> PlayerSoulGroups;
    public NativeHashMap<Entity, SoulGroupMember> SoulEntities;
    public EntityCommandBuffer Ecb;

    public void Execute(TriggerEvent triggerEvent)
    {
        Entity entityA = triggerEvent.EntityA;
        Entity entityB = triggerEvent.EntityB;



        if (PlayerSoulGroups.ContainsKey(entityA) && SoulEntities.ContainsKey(entityB))
        {
            HandleSoulCollision(entityA, entityB);
        }
        else if (PlayerSoulGroups.ContainsKey(entityB) && SoulEntities.ContainsKey(entityA))
        {
            HandleSoulCollision(entityB, entityA);
        }
    }



    public void HandleSoulCollision(Entity player, Entity soul)
    {
        if (SoulEntities[soul].MyGroup != PlayerSoulGroups[player])
        {
            Ecb.AddComponent(soul, new ChangeSoulGroup { SoulGroupToMoveTo = PlayerSoulGroups[player] });
        }
    }
}