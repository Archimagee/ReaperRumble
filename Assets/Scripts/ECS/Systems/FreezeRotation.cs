using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Physics;



[BurstCompile]
public partial struct FreezeRotation : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        foreach ((RefRO<FreezeRotationTag> freezeRotation, RefRW<PhysicsMass> physicsMass, Entity entity) in SystemAPI.Query<RefRO<FreezeRotationTag>, RefRW<PhysicsMass>>().WithEntityAccess())
        {
            physicsMass.ValueRW.InverseInertia = new float3(0f, physicsMass.ValueRO.InverseInertia.y, 0f);
            ecb.RemoveComponent<FreezeRotationTag>(entity);
        }



        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}