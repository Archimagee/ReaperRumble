using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using System;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateAfter(typeof(OrphanSoulsServerSystem))]
public partial class ChangeSoulGroupServerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<ChangeSoulGroup>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach ((RefRO<ChangeSoulGroup> change, RefRW<SoulGroupMember> soulGroup, RefRW<SoulGroupWasChanged> changed, Entity soulEntity) in SystemAPI.Query<RefRO<ChangeSoulGroup>, RefRW<SoulGroupMember>, RefRW<SoulGroupWasChanged>>().WithEntityAccess())
        {
            Entity groupToMoveTo = change.ValueRO.SoulGroupToMoveTo;
            Entity groupToMoveFrom = soulGroup.ValueRO.MyGroup;
            if (!SystemAPI.HasBuffer<SoulBufferElement>(groupToMoveFrom)) throw new Exception("Soul " + soulEntity + " wants to move from group " + groupToMoveFrom + " to group " + groupToMoveTo + " but it is not currently in a group with a buffer");
            if (!SystemAPI.HasBuffer<SoulBufferElement>(groupToMoveTo)) throw new Exception("Soul " + soulEntity + " wants to move from group " + groupToMoveFrom + " to group " + groupToMoveTo + " but that group does not have a buffer");



            DynamicBuffer<SoulBufferElement> buffer = SystemAPI.GetBuffer<SoulBufferElement>(groupToMoveFrom);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer.ElementAt(i).Soul == soulEntity)
                {
                    buffer.RemoveAt(i);
                    break;
                }
            }

            ecb.AppendToBuffer(groupToMoveTo, new SoulBufferElement { Soul = soulEntity });
            soulGroup.ValueRW.MyGroup = groupToMoveTo;



            changed.ValueRW.WasChangedAt = SystemAPI.Time.ElapsedTime;
            ecb.RemoveComponent<ChangeSoulGroup>(soulEntity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

public struct ChangeSoulGroup : IComponentData
{
    public Entity SoulGroupToMoveTo;
}