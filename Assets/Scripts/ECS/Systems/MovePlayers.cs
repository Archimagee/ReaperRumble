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

        foreach ((RefRW<ClientPlayerInput> playerInput, RefRW<IsPlayerGrounded> grounded, RefRW<PhysicsVelocity> playerVelocity, RefRW<LocalTransform> playerTransform, RefRO<Player> player) in SystemAPI.Query<RefRW<ClientPlayerInput>, RefRW<IsPlayerGrounded>, RefRW<PhysicsVelocity>, RefRW<LocalTransform>, RefRO<Player>>().WithAll<Simulate>().WithAll<GhostOwnerIsLocal>())
        {
            float3 playerPos = playerTransform.ValueRO.Position;
            if (float.IsNaN(playerPos.x) || float.IsNaN(playerPos.y) || float.IsNaN(playerPos.z)) playerTransform.ValueRW.Position = new float3 (0f, 10f, 0f);

            float2 input = playerInput.ValueRO.ClientInput;

            playerTransform.ValueRW.Rotation = playerInput.ValueRO.ClientPlayerRotation;


            float3 newVelocity = ((playerTransform.ValueRO.Forward() * input.y)
                        + (playerTransform.ValueRO.Right() * input.x)) * player.ValueRO.Speed;
            newVelocity += new float3(0f, playerVelocity.ValueRO.Linear.y, 0f);

            if (playerInput.ValueRO.IsJumping && grounded.ValueRW.IsGrounded)
            {
                newVelocity.y = player.ValueRO.JumpSpeed;
                playerInput.ValueRW.IsJumping = false;
                grounded.ValueRW.IsGrounded = false;
            }
            //else if (!grounded.ValueRO.IsGrounded)
            //{
            //    newVelocity.y -= 9.81f * SystemAPI.Time.DeltaTime;
            //}
            //else newVelocity.y = 0f;

            playerVelocity.ValueRW.Linear = newVelocity;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}