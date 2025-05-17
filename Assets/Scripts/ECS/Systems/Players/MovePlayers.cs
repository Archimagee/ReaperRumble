using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;



[BurstCompile]
[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
public partial struct MovePlayers : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach ((RefRW<PlayerInput> playerInput, RefRW<IsPlayerGrounded> grounded, RefRW<PhysicsVelocity> playerVelocity, RefRW<Knockback> knockback, RefRW<LocalTransform> playerTransform, RefRO<Player> player)
            in SystemAPI.Query<RefRW<PlayerInput>, RefRW<IsPlayerGrounded>, RefRW<PhysicsVelocity>, RefRW<Knockback>, RefRW<LocalTransform>, RefRO<Player>>().WithAll<Simulate>().WithAll<GhostOwnerIsLocal>())
        {
            playerTransform.ValueRW.Rotation = quaternion.EulerXYZ(playerInput.ValueRO.ClientPlayerRotationEuler);



            float3 newVelocity = float3.zero;
            newVelocity += playerInput.ValueRO.ClientInput.y * playerTransform.ValueRO.Forward() * player.ValueRO.Speed;
            newVelocity += playerInput.ValueRO.ClientInput.x * playerTransform.ValueRO.Right() * player.ValueRO.Speed;
            newVelocity += new float3(0f, playerVelocity.ValueRO.Linear.y, 0f);



            if (playerInput.ValueRO.IsJumping && grounded.ValueRW.IsGrounded)
            {
                //if (playerVelocity.ValueRO.Linear.y <= 0f - player.ValueRO.JumpSpeed * 2) noKnockbackVelocity.y = player.ValueRO.JumpSpeed / 3;
                //else if (playerVelocity.ValueRO.Linear.y <= (player.ValueRO.JumpSpeed / 3) * 2) noKnockbackVelocity.y = player.ValueRO.JumpSpeed;
                //else noKnockbackVelocity.y += player.ValueRO.JumpSpeed / 3;

                newVelocity.y = player.ValueRO.JumpSpeed;

                playerInput.ValueRW.IsJumping = false;
                grounded.ValueRW.IsGrounded = false;
            }



            if (knockback.ValueRO.KnockbackValue.y > 0f) knockback.ValueRW.KnockbackValue.y /= 3f;

            newVelocity += knockback.ValueRO.KnockbackValue;

            knockback.ValueRW.KnockbackValue.y = 0f;
            knockback.ValueRW.KnockbackValue = math.lerp(knockback.ValueRO.KnockbackValue, float3.zero, 0.07f);



            playerVelocity.ValueRW.Linear = newVelocity;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}