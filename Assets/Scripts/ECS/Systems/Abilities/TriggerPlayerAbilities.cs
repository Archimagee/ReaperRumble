using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using UnityEngine;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;



[BurstCompile]
[UpdateInGroup(typeof(GhostInputSystemGroup))]
[UpdateAfter(typeof(HandleClientPlayerInput))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class TriggerPlayerAbilities : SystemBase
{
    [BurstCompile]
    protected override void OnCreate()
    {
        RequireForUpdate<PlayerInput>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        foreach ((RefRO<PlayerInput> playerInput, RefRO<LocalTransform> playerTransform, RefRO<PhysicsVelocity> playerVelocity, RefRO<PlayerData> playerClass)
            in SystemAPI.Query<RefRO<PlayerInput>, RefRO<LocalTransform>, RefRO<PhysicsVelocity>, RefRO<PlayerData>>().WithAll<GhostOwnerIsLocal>())
        {
            if (!playerInput.ValueRO.IsUsingAbility) break;

            if (playerClass.ValueRO.MyAbility == PlayerAbility.SixShooter) UseSixShooter(playerTransform.ValueRO.Position, math.mul(playerInput.ValueRO.ClientCameraRotation, new float3(0f, 0f, 1f)), ecb);
            else if (playerClass.ValueRO.MyAbility == PlayerAbility.PoisonVial) UsePoisonVial(playerTransform.ValueRO.Position, math.mul(playerInput.ValueRO.ClientCameraRotation, new float3(0f, 0f, 1f)), playerVelocity.ValueRO.Linear, ecb);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }



    #region SixShooter

    private readonly float _sixShooterRange = 100f;
    private readonly float _sixShooterKnockbackStrength = 25f;
    private readonly int _sixShooterSoulsOrphaned = 3;

    [BurstCompile]
    public void UseSixShooter(float3 playerPosition, float3 playerFacingDirection, EntityCommandBuffer ecb)
    {
        RaycastInput raycastInput = new RaycastInput()
        {
            Start = playerPosition + playerFacingDirection,
            End = playerPosition + playerFacingDirection * _sixShooterRange,
            Filter = new CollisionFilter { BelongsTo = ~0u, CollidesWith = 1u << 0 }
        };

        Unity.Physics.RaycastHit hit = new();
        bool hasHit = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CastRay(raycastInput, out hit);

        Entity gunshotVFX = ecb.Instantiate(SystemAPI.GetSingleton<VFXPrefabs>().SixShooterTracerVFXPrefabEntity);
        ecb.SetComponent(gunshotVFX, new LocalTransform() { Position = playerPosition, Scale = 1f, Rotation =  quaternion.identity });
        


        if (hasHit)
        {
            Entity hitPlayer = hit.Entity;

            float3 knockbackDirection = playerFacingDirection;
            Entity rpcEntity = ecb.CreateEntity();
            ecb.AddComponent(rpcEntity, new ApplyKnockbackToPlayerRequestRPC()
            {
                PlayerGhostID = SystemAPI.GetComponent<GhostInstance>(hitPlayer).ghostId,
                KnockbackDirection = knockbackDirection,
                Strength = _sixShooterKnockbackStrength
            });
            ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);

            if (!SystemAPI.GetBuffer<SoulBufferElement>(SystemAPI.GetComponent<PlayerSoulGroup>(hitPlayer).MySoulGroup).IsEmpty)
            {
                rpcEntity = ecb.CreateEntity();
                ecb.AddComponent(rpcEntity, new OrphanSoulsRequestRPC()
                {
                    Amount = _sixShooterSoulsOrphaned,
                    GroupID = SystemAPI.GetComponent<GhostInstance>(SystemAPI.GetComponent<PlayerSoulGroup>(hitPlayer).MySoulGroup).ghostId,
                    Position = SystemAPI.GetComponent<LocalTransform>(hitPlayer).Position
                });
                ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
            }
        }
    }

    #endregion

    #region PoisonVial

    [BurstCompile]
    public void UsePoisonVial(float3 playerPosition, float3 playerFacingDirection, float3 playerVelocity, EntityCommandBuffer ecb)
    {
        Entity rpcEntity = ecb.CreateEntity();
        ecb.AddComponent(rpcEntity, new SpawnGhostProjectileCommandRequest {
            Ability = PlayerAbility.PoisonVial,
            VelocityLinear = (playerFacingDirection * 14) + playerVelocity,
            VelocityAngular = new float3(0f, 0f, 0f),
            Position = playerPosition,
            Scale = 0.08f
        });
        ecb.AddComponent(rpcEntity, new SendRpcCommandRequest());
    }

    #endregion
}



public enum PlayerAbility
{
    SixShooter,
    PoisonVial
}

public partial struct PlayerData : IComponentData
{
    public int PlayerNumber;
    public PlayerAbility MyAbility;
    public Color MyColour;
    public bool IsNicknameSet;
}