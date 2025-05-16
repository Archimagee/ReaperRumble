using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Transforms;



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
            foreach ((RefRO<GhostInstance> ghostInstance, RefRW<Knockback> knockback, RefRW<LocalTransform> localTransform) in SystemAPI.Query<RefRO<GhostInstance>, RefRW<Knockback>, RefRW<LocalTransform>>())
            {
                if (ghostInstance.ValueRO.ghostId == knockbackRequest.ValueRO.PlayerGhostID)
                {
                    //float3 currentKnockback = knockback.ValueRO.KnockbackDirection * knockback.ValueRO.Strength;
                    //float3 newKnockback = currentKnockback + math.normalize(knockbackRequest.ValueRO.KnockbackDirection) * knockbackRequest.ValueRO.Strength;

                    //float3 knockbackDirection = math.normalize(newKnockback);
                    //float knockbackStrength = newKnockback.x / knockbackDirection.x;

                    knockback.ValueRW.KnockbackValue += knockbackRequest.ValueRO.KnockbackDirection * knockbackRequest.ValueRO.Strength;
                    //knockback.ValueRW.Strength = knockbackStrength;
                    //knockback.ValueRW.Decay = 0.4f;

                    break;
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
    public float3 KnockbackValue;
    //public float Strength;
    //public float Decay;
}