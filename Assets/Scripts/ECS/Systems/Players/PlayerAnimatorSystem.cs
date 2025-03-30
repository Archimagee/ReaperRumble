using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using static Unity.Entities.SystemAPI.ManagedAPI;



[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct PlayerAnimatorSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ClientPlayerInput>();
    }



    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRW<ClientPlayerInput> playerInput, UnityEngineComponent<Animator> animator) 
            in SystemAPI.Query<RefRW<ClientPlayerInput>, UnityEngineComponent<Animator>>().WithNone<GhostOwnerIsLocal>())
        {
            if (playerInput.ValueRO.ClientInput.x != 0f || playerInput.ValueRO.ClientInput.y != 0f) animator.Value.SetBool("IsRunning", true);

            if (playerInput.ValueRO.IsJumping == true)
            {
                animator.Value.SetTrigger("Jump");
            }

            if (playerInput.ValueRO.IsUsingAbility == true)
            {
                animator.Value.SetTrigger("Shoot");
            }

            if (playerInput.ValueRO.IsAttacking == true)
            {
                playerInput.ValueRW.IsAttacking = true;
                animator.Value.SetTrigger("Attack");
            }
        }
    }
}