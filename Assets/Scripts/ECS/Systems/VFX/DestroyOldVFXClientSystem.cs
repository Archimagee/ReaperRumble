using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;
using UnityEngine;
using Unity.Transforms;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class DestroyOldVFXClientSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<VFXLifetime>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);


        foreach ((RefRO<VFXLifetime> lifetime, Entity vfxEntity) in SystemAPI.Query<RefRO<VFXLifetime>>().WithEntityAccess().WithNone<DestroyVFX>())
        {
            ecb.AddComponent(vfxEntity, new DestroyVFX() { DestroyAt = SystemAPI.Time.ElapsedTime + lifetime.ValueRO.LifetimeSeconds });
        }

        foreach ((RefRO<DestroyVFX> lifetime, Entity vfxEntity) in SystemAPI.Query<RefRO<DestroyVFX>>().WithEntityAccess())
        {
            if (SystemAPI.Time.ElapsedTime >= lifetime.ValueRO.DestroyAt) ecb.DestroyEntity(vfxEntity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}