using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;



[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
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

        NativeHashMap<Entity, Entity> playerSoulGroups = new(players.Length, Allocator.TempJob);
        foreach (Entity entity in players)
            playerSoulGroups.Add(entity, SystemAPI.GetComponent<PlayerSoulGroup>(entity).MySoulGroup);

        NativeHashMap<Entity, float3> playerPositions = new(players.Length, Allocator.TempJob);
        foreach (Entity entity in players)
            playerPositions.Add(entity, SystemAPI.GetComponent<LocalTransform>(entity).Position);

        NativeHashMap<Entity, float> playerGroundDistances = new(players.Length, Allocator.TempJob);
        foreach (Entity entity in players)
        {
            float3 playerPos = SystemAPI.GetComponent<LocalTransform>(entity).Position;

            RaycastInput raycastInput = new RaycastInput()
            {
                Start = playerPos + new float3(0f, 1f, 0f),
                End = playerPos + new float3(0f, -5f, 0f),
                Filter = new CollisionFilter { BelongsTo = ~0u, CollidesWith = 1, GroupIndex = 0 }
            };

            Unity.Physics.RaycastHit hit = new Unity.Physics.RaycastHit();
            bool hasHit = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CastRay(raycastInput, out hit);

            float distance = -10f;
            if (hasHit)
            {
                distance = (hit.Fraction * 6f) - 2f;
            }

            playerGroundDistances.Add(entity, distance);
        }

        players.Dispose();



        query = SystemAPI.QueryBuilder().WithAll<GroundTag>().Build();
        NativeArray<Entity> groundEntities = query.ToEntityArray(Allocator.TempJob);



        query = SystemAPI.QueryBuilder().WithAll<Soul>().Build();
        NativeArray<Entity> soulEntities = query.ToEntityArray(Allocator.TempJob);



        var job = new PlayerSoulCollisionJob
        {
            //PlayerSoulGroups = playerSoulGroups,
            PlayerPositions = playerPositions,
            PlayerGroundDistances = playerGroundDistances,
            //SoulEntities = soulEntities,
            GroundEntities = groundEntities,
            Ecb = ecb
        };

        //Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
        //Dependency.Complete();

        playerSoulGroups.Dispose();
        playerPositions.Dispose();
        playerGroundDistances.Dispose();
        groundEntities.Dispose();
        soulEntities.Dispose();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}



[BurstCompile]
struct PlayerSoulCollisionJob : ITriggerEventsJob
{
    //public NativeHashMap<Entity, Entity> PlayerSoulGroups;
    public NativeHashMap<Entity, float3> PlayerPositions;
    public NativeHashMap<Entity, float> PlayerGroundDistances;
    //public NativeArray<Entity> SoulEntities;
    public NativeArray<Entity> GroundEntities;
    public EntityCommandBuffer Ecb;

    public void Execute(TriggerEvent triggerEvent)
    {
        Entity entityA = triggerEvent.EntityA;
        Entity entityB = triggerEvent.EntityB;


        if (PlayerPositions.ContainsKey(entityA) && GroundEntities.Contains(entityB))
        {
            HandleGroundCollision(entityA);
        }
        else if (PlayerPositions.ContainsKey(entityB) && GroundEntities.Contains(entityA))
        {
            HandleGroundCollision(entityB);
        }
    }

    public void HandleGroundCollision(Entity player)
    {
        Ecb.SetComponent<IsPlayerGrounded>(player, new IsPlayerGrounded { IsGrounded = true });

        Debug.Log(PlayerGroundDistances[player]);
        if (PlayerGroundDistances[player] != -10f)
        {
            LocalTransform newTransform = new LocalTransform() { Position = PlayerPositions[player], Scale = 1f, Rotation = quaternion.identity };
            newTransform.Position.y -= PlayerGroundDistances[player] + 1.1f;
            Ecb.SetComponent<LocalTransform>(player, newTransform);
        }
    }
}