using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;



[BurstCompile]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct MovePlayers : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach ((RefRW<ClientPlayerInput> playerInput, RefRW<IsPlayerGrounded> grounded, RefRW<PhysicsVelocity> playerVelocity, RefRW<Knockback> knockback, RefRW<LocalTransform> playerTransform, RefRO<Player> player)
            in SystemAPI.Query<RefRW<ClientPlayerInput>, RefRW<IsPlayerGrounded>, RefRW<PhysicsVelocity>, RefRW<Knockback>, RefRW<LocalTransform>, RefRO<Player>>().WithAll<Simulate>().WithAll<GhostOwnerIsLocal>())
        {
            float2 input = playerInput.ValueRO.ClientInput;



            playerTransform.ValueRW.Rotation = quaternion.EulerXYZ(playerInput.ValueRO.ClientPlayerRotationEuler);



            float3 newVelocity = ((playerTransform.ValueRO.Forward() * input.y)
                        + (playerTransform.ValueRO.Right() * input.x))
                        * player.ValueRO.Speed;

            newVelocity += new float3(0f, playerVelocity.ValueRO.Linear.y - (knockback.ValueRO.KnockbackDirection.y * knockback.ValueRO.Strength), 0f);



            if (playerInput.ValueRO.IsJumping && grounded.ValueRW.IsGrounded)
            {
                if (playerVelocity.ValueRO.Linear.y <= 0f - player.ValueRO.JumpSpeed * 2) newVelocity.y = player.ValueRO.JumpSpeed / 3;
                else if (playerVelocity.ValueRO.Linear.y <= (player.ValueRO.JumpSpeed / 3) * 2) newVelocity.y = player.ValueRO.JumpSpeed;
                else newVelocity.y += player.ValueRO.JumpSpeed / 3;
                playerInput.ValueRW.IsJumping = false;
                grounded.ValueRW.IsGrounded = false;
            }



            if (knockback.ValueRO.Strength != 0f)
            {
                float newKnockbackStrength = math.clamp(knockback.ValueRO.Strength - knockback.ValueRO.Decay, 0f, knockback.ValueRO.Strength);
                knockback.ValueRW.Strength = newKnockbackStrength;
                newVelocity += knockback.ValueRO.KnockbackDirection * newKnockbackStrength;
            }



            playerVelocity.ValueRW.Linear = newVelocity;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}