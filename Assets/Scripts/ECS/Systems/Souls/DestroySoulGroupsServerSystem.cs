using Unity.Collections;
using Unity.Entities;
using Unity.Burst;



[BurstCompile]
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[UpdateAfter(typeof(ChangeSoulGroupServerSystem))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class DestroySoulGroupsServerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<SoulGroupTarget>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new(Allocator.Temp);

        foreach ((RefRO<SoulGroupTarget> target, Entity entity) in SystemAPI.Query<RefRO<SoulGroupTarget>>().WithEntityAccess())
        {
            if (target.ValueRO.MyTarget == Entity.Null && (!SystemAPI.HasBuffer<SoulBufferElement>(entity) || SystemAPI.GetBuffer<SoulBufferElement>(entity).IsEmpty)) ecb.DestroyEntity(entity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}