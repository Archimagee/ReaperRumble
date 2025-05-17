using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI.ManagedAPI;



[UpdateInGroup(typeof(GhostInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct HandleClientPlayerInput : ISystem
{
    private float3 _playerRotation;
    private float3 _cameraRotation;



    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkStreamInGame>();
        state.RequireForUpdate<PlayerInput>();
    }



    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRW<PlayerInput> playerInput, UnityEngineComponent<Animator> animator, RefRO<PlayerInputSettings> inputSettings, RefRO<PlayerSoulGroup> soulGroup, RefRO<Player> player, RefRO<LocalTransform> playerTransform) in SystemAPI.Query<RefRW<PlayerInput>, UnityEngineComponent<Animator>, RefRO<PlayerInputSettings>, RefRO<PlayerSoulGroup>, RefRO<Player>, RefRO<LocalTransform>>().WithAll<GhostOwnerIsLocal>())
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
            if (input.x != 0f || input.y != 0f) animator.Value.SetBool("IsRunning", true);
            else animator.Value.SetBool("IsRunning", false);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                playerInput.ValueRW.IsJumping = true;
                animator.Value.SetTrigger("Jump");
            }
            else playerInput.ValueRW.IsJumping = false;

            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                playerInput.ValueRW.IsUsingAbility = true;
                animator.Value.SetTrigger("Shoot");
            }
            else playerInput.ValueRW.IsUsingAbility = false;

            if (Input.GetKeyDown(KeyCode.Mouse0) && playerInput.ValueRO.LastAttackedAt <= SystemAPI.Time.ElapsedTime - player.ValueRO.AttackCooldownSeconds)
            {
                playerInput.ValueRW.IsAttacking = true;
                animator.Value.SetTrigger("Attack");
            }

            _cameraRotation.x = Mathf.Clamp(_cameraRotation.x - (Input.GetAxisRaw("Mouse Y") * inputSettings.ValueRO.LookSensitivity), -1.6f, 1.6f);
            _cameraRotation.y += Input.GetAxisRaw("Mouse X") * inputSettings.ValueRO.LookSensitivity;
            _playerRotation.y = _cameraRotation.y;
            playerInput.ValueRW.ClientPlayerRotationEuler = _playerRotation;
            playerInput.ValueRW.ClientCameraRotation = quaternion.EulerXYZ(_cameraRotation);
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
    public float3 Position;
}

public struct PlayerInput : IInputComponentData
{
    public float2 ClientInput;

    public bool IsJumping;

    public bool IsUsingAbility;
    public double LastUsedAbilityAt;

    public double LastAttackedAt;
    public bool IsAttacking;

    public float3 ClientPlayerRotationEuler;
    public quaternion ClientCameraRotation;
    public float3 ClientCameraRotationEuler;
}

public struct PlayerInputSettings : IInputComponentData
{
    public float LookSensitivity;
}