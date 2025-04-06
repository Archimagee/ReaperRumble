using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using UnityEngine;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;



[BurstCompile]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial class TriggerPlayerAbilities : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<ClientPlayerInput>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        foreach ((RefRO<ClientPlayerInput> playerInput, RefRO<LocalTransform> playerTransform, RefRO<PlayerData> playerClass)
            in SystemAPI.Query<RefRO<ClientPlayerInput>, RefRO<LocalTransform>, RefRO<PlayerData>>().WithAll<GhostOwnerIsLocal>())
        {
            if (!playerInput.ValueRO.IsUsingAbility) break;

            if (playerClass.ValueRO.MyAbility == PlayerAbility.SixShooter) UseSixShooter(playerTransform.ValueRO.Position, math.mul(playerInput.ValueRO.ClientCameraRotation, new float3(0f, 0f, 1f)), ecb);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }


    #region SixShooter

    private readonly float _sixShooterRange = 100f;
    private readonly float _sixShooterKnockbackStrength = 25f;
    private readonly float _sixShooterKnockbackDecay = 0.8f;
    private readonly int _sixShooterSoulsOrphaned = 3;

    public void UseSixShooter(float3 playerPosition, float3 playerFacingDirection, EntityCommandBuffer ecb)
    {
        //Entity tracer = EntityManager.Instantiate(SystemAPI.GetSingleton<VFXPrefabs>().SixShooterTracerVFXPrefabEntity);
        //LineRenderer lineRenderer = EntityManager.GetComponentObject<LineRenderer>(tracer);

        //Vector3[] positions = new Vector3[2];
        //positions[0] = playerPosition + playerFacingDirection * 2f;
        //positions[1] = playerPosition + playerFacingDirection * _sixShooterRange;
        //lineRenderer.SetPositions(positions);



        RaycastInput raycastInput = new RaycastInput()
        {
            Start = playerPosition + playerFacingDirection,
            End = playerPosition + playerFacingDirection * _sixShooterRange,
            Filter = new CollisionFilter { BelongsTo = ~0u, CollidesWith = 1u << 0 }
        };

        Unity.Physics.RaycastHit hit = new();
        bool hasHit = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CastRay(raycastInput, out hit);

        Debug.Log(raycastInput.Start);
        Debug.Log(raycastInput.End);
        if (hasHit)
        {
            Entity hitPlayer = hit.Entity;
            Debug.Log("Hit player " + hitPlayer);

            float3 knockbackDirection = playerFacingDirection;
            knockbackDirection.y = math.clamp(knockbackDirection.y, 0f, 0.03f / _sixShooterKnockbackStrength);
            ecb.SetComponent(hitPlayer, new Knockback() { KnockbackDirection = playerFacingDirection, Decay = _sixShooterKnockbackDecay, Strength = _sixShooterKnockbackStrength });

            Entity rpcEntity = ecb.CreateEntity();
            ecb.AddComponent(rpcEntity, new OrphanSoulsRequestRPC() {
                Amount = _sixShooterSoulsOrphaned,
                GroupID = SystemAPI.GetComponent<GhostInstance>(SystemAPI.GetComponent<PlayerSoulGroup>(hitPlayer).MySoulGroup).ghostId,
                Position = SystemAPI.GetComponent<LocalTransform>(hitPlayer).Position });
            ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
        }
    }

    #endregion
}



public enum PlayerAbility
{
    SixShooter
}

public partial struct PlayerData : IComponentData
{
    public int PlayerNumber;
    public PlayerAbility MyAbility;
    public Color MyColour;
}