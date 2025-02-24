using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;
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
                Debug.Log("Attack");

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

                foreach (DistanceHit hit in hits)
                {
                    Entity hitEntity = hit.Entity;

                    if (hitEntity != playerEntity)
                    {
                        Entity rpcEntity = ecb.CreateEntity();
                        ecb.AddComponent(rpcEntity, new OrphanSoulsRequestRPC
                        {
                            GroupID = SystemAPI.GetComponent<GhostInstance>(SystemAPI.GetComponent<PlayerSoulGroup>(hitEntity).MySoulGroup).ghostId,
                            Amount = 3
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
    public int NewGroupID;
}