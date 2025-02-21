using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;



[BurstCompile]
[UpdateInGroup(typeof(GhostInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct HandleClientPlayerInput : ISystem
{
    private float3 _playerRotation;
    private float3 _cameraRotation;



    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkId>();
        state.RequireForUpdate<NetworkStreamInGame>();
        state.RequireForUpdate<ClientPlayerInput>();
    }



    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRW<ClientPlayerInput> playerInput, RefRO<ClientPlayerInputSettings> inputSettings, RefRO<PlayerSoulGroup> soulGroup, RefRO<Player> player) in SystemAPI.Query<RefRW<ClientPlayerInput>, RefRO<ClientPlayerInputSettings>, RefRO<PlayerSoulGroup>, RefRO<Player>>().WithAll<GhostOwnerIsLocal>())
        {
            float2 input = new float2();
            if (Input.GetKey(KeyCode.W))
            {
                input.y += 1f;
            }
            if (Input.GetKey(KeyCode.S))
            {
                input.y += -1f;
            }
            if (Input.GetKey(KeyCode.D))
            {
                input.x += 1f;
            }
            if (Input.GetKey(KeyCode.A))
            {
                input.x += -1f;
            }
            math.normalizesafe(input);
            playerInput.ValueRW.ClientInput = input;

            if (Input.GetKeyDown(KeyCode.Return))
            {
                Entity rpcEntity = ecb.CreateEntity();
                ecb.AddComponent(rpcEntity, new SpawnSoulsRequestRPC { GroupID = state.EntityManager.GetComponentData<GhostInstance>(soulGroup.ValueRO.MySoulGroup).ghostId, Amount = 5 });
                ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
            }

            if (Input.GetKeyDown(KeyCode.Space)) playerInput.ValueRW.IsJumping = true;
            if (Input.GetKeyDown(KeyCode.Mouse0) && playerInput.ValueRO.LastAttackedAt <= SystemAPI.Time.ElapsedTime - player.ValueRO.AttackCooldownSeconds) playerInput.ValueRW.IsAttacking = true;

            _cameraRotation.x = Mathf.Clamp(_cameraRotation.x - (Input.GetAxisRaw("Mouse Y") * inputSettings.ValueRO.LookSensitivity), -90f, 90f);
            _cameraRotation.y += Input.GetAxisRaw("Mouse X") * inputSettings.ValueRO.LookSensitivity;
            _playerRotation.y = _cameraRotation.y;
            playerInput.ValueRW.ClientPlayerRotation = Quaternion.Euler(_playerRotation);
            playerInput.ValueRW.ClientPlayerRotationEuler = _playerRotation;
            playerInput.ValueRW.ClientCameraRotation = Quaternion.Euler(_cameraRotation);
            playerInput.ValueRW.ClientCameraRotationEuler = _cameraRotation;
        }



        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}



public struct SpawnSoulsRequestRPC : IRpcCommand
{
    public int GroupID;
    public int Amount;
}