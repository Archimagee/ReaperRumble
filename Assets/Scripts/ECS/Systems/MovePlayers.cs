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

        foreach ((RefRO<ClientPlayerInput> playerInput, RefRW<PhysicsVelocity> playerVelocity, RefRW <LocalTransform> playerTransform, RefRO<Player> player) in SystemAPI.Query<RefRO<ClientPlayerInput>, RefRW<PhysicsVelocity>, RefRW<LocalTransform>, RefRO<Player>>().WithAll<Simulate>())
        {
            float3 playerPos = playerTransform.ValueRO.Position;
            if (float.IsNaN(playerPos.x) || float.IsNaN(playerPos.y) || float.IsNaN(playerPos.z)) playerTransform.ValueRW.Position = float3.zero;

            float2 input = playerInput.ValueRO.ClientInput;

            playerTransform.ValueRW.Rotation = playerInput.ValueRO.ClientPlayerRotation;


            float3 newVelocity = ((playerTransform.ValueRO.Forward() * input.y)
                        + (playerTransform.ValueRO.Right() * input.x)) * player.ValueRO.Speed;
            newVelocity += new float3(0f, playerVelocity.ValueRO.Linear.y, 0f);

            playerVelocity.ValueRW.Linear = newVelocity;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}