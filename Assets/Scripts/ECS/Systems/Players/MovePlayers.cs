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

            newVelocity += new float3(0f, playerVelocity.ValueRO.Linear.y, 0f);



            if (playerInput.ValueRO.IsJumping && grounded.ValueRW.IsGrounded)
            {
                newVelocity.y = player.ValueRO.JumpSpeed;
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