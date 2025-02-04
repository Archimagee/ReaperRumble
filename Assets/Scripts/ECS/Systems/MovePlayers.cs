using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;



[BurstCompile]
//[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial class MovePlayers : SystemBase
{
    private float3 _playerRotation = float3.zero;



    protected override void OnCreate()
    {
        RequireForUpdate<NetworkStreamInGame>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach ((RefRW<ClientPlayerInput> playerInput, RefRO<ClientPlayerInputSettings> inputSettings, RefRW <LocalTransform> playerTransform, RefRO<Player> player) in SystemAPI.Query<RefRW<ClientPlayerInput>, RefRO<ClientPlayerInputSettings>, RefRW<LocalTransform>, RefRO<Player>>().WithAll<Simulate>())
        {
            float2 input = playerInput.ValueRO.ClientInput;
            playerTransform.ValueRW.Position += ((playerTransform.ValueRO.Forward() * input.y)
                        + (playerTransform.ValueRO.Right() * input.x)) * player.ValueRO.Speed * SystemAPI.Time.DeltaTime;

            playerTransform.ValueRW.Rotation = playerInput.ValueRO.ClientPlayerRotation;
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}