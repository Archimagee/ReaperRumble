using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Collections;
using Unity.NetCode;
using Unity.Physics.Systems;



[BurstCompile]
[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class DetectSoulDeposit : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<SoulDepositTag>();
        RequireForUpdate<PlayerData>();
    }



    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        foreach ((RefRO<DepositSoulCooldown> cooldown, Entity entity) in SystemAPI.Query<RefRO<DepositSoulCooldown>>().WithEntityAccess())
        {
            if (cooldown.ValueRO.CanDepositAt <= SystemAPI.Time.ElapsedTime) ecb.RemoveComponent<DepositSoulCooldown>(entity);
        }



        EntityQuery query = SystemAPI.QueryBuilder().WithAll<PlayerSoulGroup>().WithNone<DepositSoulCooldown>().WithAll<GhostOwnerIsLocal>().Build();
        NativeArray<Entity> players = query.ToEntityArray(Allocator.Temp);

        NativeHashMap<Entity, int> playerNumbers = new(players.Length, Allocator.TempJob);

        foreach (Entity player in players)
        {
            playerNumbers.Add(player, SystemAPI.GetComponent<PlayerData>(player).PlayerNumber);
        }



        var job = new DetectSoulDepositTriggerJob
        {
            SoulDepositCollider = SystemAPI.GetSingletonEntity<SoulDepositTag>(),
            PlayerNumbers = playerNumbers,
            Ecb = ecb
        };

        Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
        Dependency.Complete();

        players.Dispose();
        playerNumbers.Dispose();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}



[BurstCompile]
struct DetectSoulDepositTriggerJob : ITriggerEventsJob
{
    public NativeHashMap<Entity, int> PlayerNumbers;
    public Entity SoulDepositCollider;
    public EntityCommandBuffer Ecb;

    public void Execute(TriggerEvent collisionEvent)
    {
        Entity entityA = collisionEvent.EntityA;
        Entity entityB = collisionEvent.EntityB;



        if (PlayerNumbers.ContainsKey(entityA) && entityB == SoulDepositCollider)
        {
            HandleDepositTrigger(entityA);
        }
        else if (PlayerNumbers.ContainsKey(entityB) && entityA == SoulDepositCollider)
        {
            HandleDepositTrigger(entityB);
        }
    }



    public void HandleDepositTrigger(Entity player)
    {
        Entity rpcEntity = Ecb.CreateEntity();
        Ecb.AddComponent(rpcEntity, new DepositSoulsRequestRPC() { PlayerNumber = PlayerNumbers[player] });
        Ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
    }
}



public partial struct DepositSoulCooldown : IComponentData
{
    public double CanDepositAt;
}