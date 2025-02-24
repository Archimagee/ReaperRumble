using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using Unity.Physics.Systems;
using Unity.NetCode;



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



        foreach ((RefRO<ChangeSoulGroup> changeRequest, RefRW<SoulGroupMember> soul, Entity soulEntity) in SystemAPI.Query<RefRO<ChangeSoulGroup>, RefRW<SoulGroupMember>>().WithEntityAccess())
        {
            Entity groupToMoveTo = changeRequest.ValueRO.SoulGroupToMoveTo;
            Entity groupToMoveFrom = soul.ValueRO.MyGroup;

            Debug.Log("Changing " + soulEntity + " to " + groupToMoveTo);



            if (!SystemAPI.HasBuffer<SoulBufferElement>(groupToMoveTo)) ecb.AddBuffer<SoulBufferElement>(groupToMoveTo);
            ecb.AppendToBuffer<SoulBufferElement>(groupToMoveTo, new SoulBufferElement { Soul = soulEntity });
            soul.ValueRW.MyGroup = groupToMoveTo;


            NativeArray<SoulBufferElement> soulElementArray = SystemAPI.GetBuffer<SoulBufferElement>(groupToMoveFrom).ToNativeArray(Allocator.Temp);
            NativeList<Entity> soulArray = new(Allocator.Temp);
            foreach (SoulBufferElement soulBufferElement in soulElementArray) soulArray.Add(soulBufferElement.Soul);
            soulElementArray.Dispose();

            SystemAPI.GetBuffer<SoulBufferElement>(groupToMoveFrom).Clear();
            foreach (Entity soulElement in soulArray) ecb.AppendToBuffer<SoulBufferElement>(groupToMoveFrom, new SoulBufferElement() { Soul = soulElement });


            soulArray.Dispose();



            ecb.RemoveComponent<ChangeSoulGroup>(soulEntity);



            //Entity rpcEntity = ecb.CreateEntity();
            //ecb.AddComponent(rpcEntity, new ChangeSoulGroupRequestRPC
            //{
            //    GroupIDFrom = SystemAPI.GetComponent<GhostInstance>(groupToMoveFrom).ghostId,
            //    GroupIDTo = SystemAPI.GetComponent<GhostInstance>(groupToMoveTo).ghostId,
            //    Amount = 1
            //});
            //ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
        }



        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}



public struct ChangeSoulGroupRequestRPC : IRpcCommand
{
    public int GroupIDFrom;
    public int GroupIDTo;
    public int Amount;
}