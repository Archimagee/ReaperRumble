using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;



[BurstCompile]
[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
partial struct MovePlayers : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkStreamInGame>();
    }



    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        Entity playerEntityPrefab = SystemAPI.GetSingleton<PlayerSpawner>().PlayerPrefabEntity;

        foreach ((RefRO<ClientPlayerInput> playerInput, RefRW<LocalTransform> playerTransform, RefRO<Player> player) in SystemAPI.Query<RefRO<ClientPlayerInput>, RefRW<LocalTransform>, RefRO<Player>>().WithAll<Simulate>())
        {
            float2 input = playerInput.ValueRO.ClientInput;

            float3 movement = math.normalizesafe(new float3(input.x, 0f, input.y));
            movement *= player.ValueRO.Speed * SystemAPI.Time.DeltaTime;
            playerTransform.ValueRW.Position += movement;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}