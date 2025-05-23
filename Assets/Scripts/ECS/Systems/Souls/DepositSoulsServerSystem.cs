using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Physics.Systems;
using Unity.Physics;
using Unity.NetCode;
using UnityEngine;



[BurstCompile]
[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class DepositSoulsServerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<SoulDepositTag>();
        RequireForUpdate<Player>();
    }



    [BurstCompile]
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



        var job = new PlayerDepositTriggerJob
        {
            PlayerSoulGroups = playerSoulGroups,
            DepositHitbox = SystemAPI.GetSingletonEntity<SoulDepositTag>(),
            Ecb = ecb
        };

        Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
        Dependency.Complete();

        ecb.Playback(EntityManager);
        ecb.Dispose();
        ecb = new EntityCommandBuffer(Allocator.Temp);
        playerSoulGroups.Dispose();



        foreach ((RefRO<DepositCooldown> cooldown, Entity soulGroup) in SystemAPI.Query<RefRO<DepositCooldown>>().WithAll<SoulGroupTarget>().WithEntityAccess())
        {
            if (SystemAPI.HasComponent<DepositSouls>(soulGroup)) ecb.RemoveComponent<DepositSouls>(soulGroup);

            if (SystemAPI.Time.ElapsedTime >= cooldown.ValueRO.CanDepositAt) ecb.RemoveComponent<DepositCooldown>(soulGroup);
        }



            foreach ((RefRO<SoulGroupTarget> player, Entity soulGroup) in SystemAPI.Query<RefRO<SoulGroupTarget>>().WithAll<DepositSouls>().WithNone<DepositCooldown>().WithEntityAccess())
        {
            DynamicBuffer<SoulBufferElement> souls = SystemAPI.GetBuffer<SoulBufferElement>(soulGroup);
            int amount = souls.Length;

            if (amount > 0)
            {
                foreach (SoulBufferElement soul in souls) ecb.DestroyEntity(soul.Soul);
                souls.Clear();

                Entity rpcEntity = ecb.CreateEntity();
                ecb.AddComponent(rpcEntity, new PlayAnnouncementAtRPC()
                {
                    AnnouncementToPlay = "Player " + SystemAPI.GetComponent<PlayerData>(player.ValueRO.MyTarget).PlayerNumber + " scored " + amount + " souls!",
                    TimeToPlayAt = SystemAPI.Time.ElapsedTime
                });
                ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);

                rpcEntity = ecb.CreateEntity();
                ecb.AddComponent(rpcEntity, new AddPlayerScore()
                {
                    PlayerNumber = SystemAPI.GetComponent<PlayerData>(player.ValueRO.MyTarget).PlayerNumber,
                    ScoreToAdd = amount
                });
                ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);

                ecb.AddComponent(soulGroup, new DepositCooldown() { CanDepositAt = SystemAPI.Time.ElapsedTime + 60d });
            }
            ecb.RemoveComponent<DepositSouls>(soulGroup);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}



[BurstCompile]
struct PlayerDepositTriggerJob : ITriggerEventsJob
{
    public NativeHashMap<Entity, Entity> PlayerSoulGroups;
    public Entity DepositHitbox;
    public EntityCommandBuffer Ecb;

    public void Execute(TriggerEvent triggerEvent)
    {
        Entity entityA = triggerEvent.EntityA;
        Entity entityB = triggerEvent.EntityB;


        if (PlayerSoulGroups.ContainsKey(entityA) && entityB == DepositHitbox)
        {
            HandleDeposit(entityA);
        }
        else if (PlayerSoulGroups.ContainsKey(entityB) && entityA == DepositHitbox)
        {
            HandleDeposit(entityB);
        }
    }



    public void HandleDeposit(Entity player)
    {
        Ecb.AddComponent<DepositSouls>(PlayerSoulGroups[player]);
    }
}



public partial struct DepositSouls : IComponentData { }

public partial struct AddPlayerScore : IRpcCommand
{
    public int PlayerNumber;
    public int ScoreToAdd;
}

public partial struct DepositCooldown : IComponentData
{
    public double CanDepositAt;
}