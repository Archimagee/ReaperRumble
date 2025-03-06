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
        state.RequireForUpdate<ClientPlayerInput>();
    }



    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Allocator.Temp);



        foreach ((RefRW<ClientPlayerInput> playerInput, RefRO<LocalTransform> localTransform, Entity playerEntity) in SystemAPI.Query<RefRW<ClientPlayerInput>, RefRO<LocalTransform>>().WithAll<GhostOwnerIsLocal>().WithEntityAccess())
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
                    Effect = VisualEffect.ScytheSlash,
                    Location = localTransform.ValueRO.Position,
                    Rotation = localTransform.ValueRO.Rotation
                });
                ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);



                foreach (DistanceHit hit in hits)
                {
                    Entity hitEntity = hit.Entity;

                    if (hitEntity != playerEntity)
                    {
                        Entity groupToOrphanFrom = SystemAPI.GetComponent<PlayerSoulGroup>(hitEntity).MySoulGroup;
                        rpcEntity = ecb.CreateEntity();
                        ecb.AddComponent(rpcEntity, new OrphanSoulsRequestRPC
                        {
                            GroupID = SystemAPI.GetComponent<GhostInstance>(groupToOrphanFrom).ghostId,
                            Amount = 3,
                            Position = SystemAPI.GetComponent<LocalTransform>(groupToOrphanFrom).Position
                        });
                        ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);

                        rpcEntity = ecb.CreateEntity();
                        float3 knockback = math.normalizesafe(SystemAPI.GetComponent<LocalTransform>(hitEntity).Position - localTransform.ValueRO.Position);
                        //knockback.y += 0.2f;
                        ecb.AddComponent(rpcEntity, new ApplyKnockbackToPlayerRequestRPC
                        {
                            PlayerGhostID = SystemAPI.GetComponent<GhostInstance>(hitEntity).ghostId,
                            KnockbackDirection = knockback,
                            Strength = 9f
                        });
                        ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
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
    public int NewGroupID;
}

public struct ApplyKnockbackToPlayerRequestRPC : IRpcCommand
{
    public int PlayerGhostID;
    public float3 KnockbackDirection;
    public float Strength;
}