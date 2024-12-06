using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;



[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class PlayerEntityHitboxMovementSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<PlayerEntityHitboxComponent>();
    }



    protected override void OnUpdate()
    {
        foreach ((RefRO<PlayerEntityHitboxComponent> hitbox, Entity entity) in SystemAPI.Query<RefRO<PlayerEntityHitboxComponent>>().WithEntityAccess())
        {
            float3 targetPosition = EntityManager.GetComponentObject<Transform>(entity).position;
            EntityManager.SetComponentData<LocalTransform>(entity, new LocalTransform { Position = targetPosition });
        }
    }
}