using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using UnityEngine;



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

        foreach ((RefRO<SoulGroupTarget> target, Entity entity) in SystemAPI.Query<RefRO<SoulGroupTarget>>().WithNone<SoulGroupInitialise>().WithEntityAccess())
        {
            if (target.ValueRO.MyTarget == Entity.Null && (!SystemAPI.HasBuffer<SoulBufferElement>(entity) || SystemAPI.GetBuffer<SoulBufferElement>(entity).IsEmpty)) ecb.DestroyEntity(entity);
        }

        foreach ((RefRO<DestroySoulGroup> destroy, Entity groupToDestroy) in SystemAPI.Query<RefRO<DestroySoulGroup>>().WithNone<SoulGroupInitialise>().WithEntityAccess())
        {
            if (SystemAPI.Time.ElapsedTime >= destroy.ValueRO.TimeToDestroyAt)
            {
                foreach (SoulBufferElement soul in SystemAPI.GetBuffer<SoulBufferElement>(groupToDestroy)) ecb.DestroyEntity(soul.Soul);
                ecb.DestroyEntity(groupToDestroy);
            }
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}



public partial struct DestroySoulGroup : IComponentData
{
    public double TimeToDestroyAt;
}