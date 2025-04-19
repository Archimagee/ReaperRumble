using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.VisualScripting;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class PlayerKnockbackServerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<ApplyKnockbackToPlayerRequestRPC>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new(Allocator.TempJob);



        foreach ((RefRO<ReceiveRpcCommandRequest> rpcCommandRequest, RefRO<ApplyKnockbackToPlayerRequestRPC> knockbackRequest, Entity recieveRpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ApplyKnockbackToPlayerRequestRPC>>().WithEntityAccess())
        {
            foreach ((RefRO<GhostInstance> ghostInstance, RefRW<Knockback> knockback) in SystemAPI.Query<RefRO<GhostInstance>, RefRW<Knockback>>())
            {
                float3 knockbackDirection = knockbackRequest.ValueRO.KnockbackDirection;

                if (ghostInstance.ValueRO.ghostId == knockbackRequest.ValueRO.PlayerGhostID)
                {
                    knockback.ValueRW.KnockbackDirection = math.normalize(knockbackDirection);
                    knockback.ValueRW.Strength = knockbackRequest.ValueRO.Strength;
                    knockback.ValueRW.Decay = 0.4f;
                }
            }

            ecb.DestroyEntity(recieveRpcEntity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

public struct Knockback : IComponentData
{
    public float3 KnockbackDirection;
    public float Strength;
    public float Decay;
}