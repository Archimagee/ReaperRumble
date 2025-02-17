using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using Unity.Physics.Systems;



[BurstCompile]
[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
[UpdateAfter(typeof(DetectPlayerSoulCollisions))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ChangeSoulGroupSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ChangeSoulGroup>();
    }



    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRO<ChangeSoulGroup> changeRequest, RefRW<Soul> soul, Entity entity) in SystemAPI.Query<RefRO<ChangeSoulGroup>, RefRW<Soul>>().WithEntityAccess())
        {
            Entity soulToMove = changeRequest.ValueRO.SoulToMove;
            Entity groupToMoveTo = changeRequest.ValueRO.SoulGroupToMoveTo;
            Debug.Log("Changing " + soulToMove + " to " + groupToMoveTo);

            if (!SystemAPI.HasBuffer<SoulBufferElement>(groupToMoveTo)) ecb.AddBuffer<SoulBufferElement>(groupToMoveTo);
            ecb.AppendToBuffer<SoulBufferElement>(groupToMoveTo, new SoulBufferElement { Soul = soulToMove });
            soul.ValueRW.MyGroup = groupToMoveTo;

            ecb.RemoveComponent<ChangeSoulGroup>(entity);
        }



        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }


}