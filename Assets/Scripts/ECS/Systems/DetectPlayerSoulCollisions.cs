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
        NativeHashMap<Entity, int> playerSoulGroupGhostIDs = new(players.Length, Allocator.TempJob);
        foreach (Entity playerEntity in players)
        {
            playerSoulGroups.Add(SystemAPI.GetComponent<SoulWorldCollider>(playerEntity).ColliderEntity, SystemAPI.GetComponent<PlayerSoulGroup>(playerEntity).MySoulGroup);
            playerSoulGroupGhostIDs.Add(SystemAPI.GetComponent<SoulWorldCollider>(playerEntity).ColliderEntity, SystemAPI.GetComponent<GhostInstance>(SystemAPI.GetComponent<PlayerSoulGroup>(playerEntity).MySoulGroup).ghostId);
        }

        players.Dispose();



        query = SystemAPI.QueryBuilder().WithAll<Soul>().Build();
        NativeArray<Entity> soulEntities = query.ToEntityArray(Allocator.Temp);
        NativeHashMap<Entity, SoulGroupMember> souls = new(soulEntities.Length, Allocator.TempJob);
        NativeHashMap<Entity, int> soulSoulGroupGhostIDs = new(soulEntities.Length, Allocator.TempJob);
        foreach (Entity soulEntity in soulEntities)
        {
            if (SystemAPI.HasComponent<SoulGroupMember>(soulEntity))
            {
                souls.Add(soulEntity, SystemAPI.GetComponent<SoulGroupMember>(soulEntity));
                soulSoulGroupGhostIDs.Add(soulEntity, SystemAPI.GetComponent<GhostInstance>(SystemAPI.GetComponent<SoulGroupMember>(soulEntity).MyGroup).ghostId);
            }
        }



        var job = new PlayerSoulCollisionJob
        {
            PlayerSoulGroups = playerSoulGroups,
            PlayerSoulGroupGhostIDs = playerSoulGroupGhostIDs,
            SoulEntities = souls,
            SoulSoulGroupGhostIDs = soulSoulGroupGhostIDs,
            Ecb = ecb
        };

        Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
        Dependency.Complete();

        playerSoulGroups.Dispose();
        playerSoulGroupGhostIDs.Dispose();
        soulEntities.Dispose();
        soulSoulGroupGhostIDs.Dispose();
        souls.Dispose();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}



[BurstCompile]
struct PlayerSoulCollisionJob : ICollisionEventsJob
{
    public NativeHashMap<Entity, Entity> PlayerSoulGroups;
    public NativeHashMap<Entity, int> PlayerSoulGroupGhostIDs;
    public NativeHashMap<Entity, SoulGroupMember> SoulEntities;
    public NativeHashMap<Entity, int> SoulSoulGroupGhostIDs;
    public EntityCommandBuffer Ecb;

    public void Execute(CollisionEvent collisionEvent)
    {
        Entity entityA = collisionEvent.EntityA;
        Entity entityB = collisionEvent.EntityB;



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

            Entity rpcEntity = Ecb.CreateEntity();
            Ecb.AddComponent(rpcEntity, new ChangeSoulGroupRequestRPC() { Amount = 1, GroupIDFrom = SoulSoulGroupGhostIDs[soul], GroupIDTo = PlayerSoulGroupGhostIDs[player] });
            Ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
        }
    }
}