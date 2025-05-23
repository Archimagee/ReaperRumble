using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Collections;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct PlayerAttackSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerInput>();
    }



    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Allocator.Temp);



        foreach ((RefRW<PlayerInput> playerInput, RefRO<LocalTransform> localTransform, RefRO<GhostInstance> ghost, Entity playerEntity) 
            in SystemAPI.Query<RefRW<PlayerInput>, RefRO<LocalTransform>, RefRO<GhostInstance>>().WithAll<GhostOwnerIsLocal>().WithEntityAccess())
        {
            if (playerInput.ValueRO.IsAttacking)
            {
                float3 playerForward = localTransform.ValueRO.Forward();

                float3 point1 = localTransform.ValueRO.Position + playerForward;
                point1 += localTransform.ValueRO.Right();

                float3 point2 = localTransform.ValueRO.Position + playerForward;
                point2 -= localTransform.ValueRO.Right();

                NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);

                SystemAPI.GetSingleton<PhysicsWorldSingleton>().OverlapCapsule(point1, point2, 2f, ref hits, new CollisionFilter()
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1
                });



                Entity rpcEntity = ecb.CreateEntity();
                ecb.AddComponent(rpcEntity, new SpawnVFXRequest
                {
                    Effect = RRVFX.ScytheSlash,
                    Location = localTransform.ValueRO.Position,
                    Rotation = localTransform.ValueRO.Rotation
                });
                ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);

                ecb.AddComponent(rpcEntity, new ApplyKnockbackToPlayerRequestRPC
                {
                    PlayerGhostID = ghost.ValueRO.ghostId,
                    KnockbackDirection = math.mul(playerInput.ValueRO.ClientCameraRotation, new float3(0f, 0f, 1f)),
                    Strength = 15f
                });
                ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);



                foreach (DistanceHit hit in hits)
                {
                    Entity hitEntity = hit.Entity;

                    if (hitEntity != playerEntity)
                    {
                        rpcEntity = ecb.CreateEntity();
                        ecb.AddComponent(rpcEntity, new OrphanSoulsRequestRPC
                        {
                            GroupID = SystemAPI.GetComponent<GhostInstance>(SystemAPI.GetComponent<PlayerSoulGroup>(hit.Entity).MySoulGroup).ghostId,
                            Amount = 3
                        });
                        ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);

                        rpcEntity = ecb.CreateEntity();
                        float3 knockback = math.normalizesafe(SystemAPI.GetComponent<LocalTransform>(hitEntity).Position - localTransform.ValueRO.Position);
                        ecb.AddComponent(rpcEntity, new ApplyKnockbackToPlayerRequestRPC
                        {
                            PlayerGhostID = SystemAPI.GetComponent<GhostInstance>(hitEntity).ghostId,
                            KnockbackDirection = knockback,
                            Strength = 22f
                        });
                        ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);

                        Entity hitVFX = ecb.Instantiate(SystemAPI.GetSingleton<VFXPrefabs>().HitVFXPrefabEntity);
                        ecb.SetComponent(hitVFX, new LocalTransform() { Position = SystemAPI.GetComponent<LocalTransform>(hitEntity).Position, Rotation = quaternion.identity, Scale = 1f });
                    }
                }

                hits.Dispose();



                playerInput.ValueRW.LastAttackedAt = SystemAPI.Time.ElapsedTime;
                playerInput.ValueRW.IsAttacking = false;
            }
        }



        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}



public struct OrphanSoulsRequestRPC : IRpcCommand
{
    public int GroupID;
    public int Amount;
    public float3 Velocity;
    public float3 Position;
}

public struct ApplyKnockbackToPlayerRequestRPC : IRpcCommand
{
    public int PlayerGhostID;
    public float3 KnockbackDirection;
    public float Strength;
}