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
public partial struct ChangeSoulGroupClientSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRO<ChangeSoulGroup> changeRequest, RefRW<SoulGroupMember> soul, Entity soulEntity) in SystemAPI.Query<RefRO<ChangeSoulGroup>, RefRW<SoulGroupMember>>().WithEntityAccess())
        {
            Entity groupToMoveTo = changeRequest.ValueRO.SoulGroupToMoveTo;
            Entity groupToMoveFrom = soul.ValueRO.MyGroup;

            //Debug.Log("Changing " + soulEntity + " to " + groupToMoveTo);



            if (!SystemAPI.HasBuffer<SoulBufferElement>(groupToMoveTo)) ecb.AddBuffer<SoulBufferElement>(groupToMoveTo);
            ecb.AppendToBuffer(groupToMoveTo, new SoulBufferElement { Soul = soulEntity });
            soul.ValueRW.MyGroup = groupToMoveTo;


            NativeArray<SoulBufferElement> soulElementArray = SystemAPI.GetBuffer<SoulBufferElement>(groupToMoveFrom).ToNativeArray(Allocator.Temp);
            NativeList<Entity> soulArray = new(Allocator.Temp);
            foreach (SoulBufferElement soulBufferElement in soulElementArray) soulArray.Add(soulBufferElement.Soul);
            soulElementArray.Dispose();

            SystemAPI.GetBuffer<SoulBufferElement>(groupToMoveFrom).Clear();
            foreach (Entity soulElement in soulArray) ecb.AppendToBuffer<SoulBufferElement>(groupToMoveFrom, new SoulBufferElement() { Soul = soulElement });


            soulArray.Dispose();



            ecb.RemoveComponent<ChangeSoulGroup>(soulEntity);



            Entity rpcEntity = ecb.CreateEntity();
            ecb.AddComponent(rpcEntity, new ChangeSoulGroupRequestRPC
            {
                GroupIDFrom = SystemAPI.GetComponent<GhostInstance>(groupToMoveFrom).ghostId,
                GroupIDTo = SystemAPI.GetComponent<GhostInstance>(groupToMoveTo).ghostId,
                Amount = 1
            });
            ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
        }

        // change this to add to a queue so it doesnt try to change the same soul when multiple rpcs recieved
        foreach ((RefRO<ChangeSoulGroupRequestRPC> changeRequest, RefRO<ReceiveRpcCommandRequest> rpc, Entity rpcEntity) in SystemAPI.Query<RefRO<ChangeSoulGroupRequestRPC>, RefRO<ReceiveRpcCommandRequest>> ().WithEntityAccess())
        {
            Entity groupToMoveTo = rpcEntity;
            Entity groupToMoveFrom = rpcEntity;

            foreach ((RefRO<GhostInstance> ghost, Entity ghostEntity) in SystemAPI.Query<RefRO<GhostInstance>>().WithEntityAccess())
            {
                int id = ghost.ValueRO.ghostId;
                if (id == changeRequest.ValueRO.GroupIDTo) groupToMoveTo = ghostEntity;
                else if (id == changeRequest.ValueRO.GroupIDFrom) groupToMoveFrom = ghostEntity;
            }



            NativeArray<SoulBufferElement> soulElementArray = SystemAPI.GetBuffer<SoulBufferElement>(groupToMoveFrom).ToNativeArray(Allocator.Temp);
            NativeList<Entity> soulArray = new(Allocator.Temp);
            foreach (SoulBufferElement soulBufferElement in soulElementArray) soulArray.Add(soulBufferElement.Soul);
            soulElementArray.Dispose();

            Entity soulEntity = soulArray[0];
            Debug.Log("Changing " + soulEntity + " to " + groupToMoveTo);
            SoulGroupMember soul = SystemAPI.GetComponent<SoulGroupMember>(soulEntity);



            if (!SystemAPI.HasBuffer<SoulBufferElement>(groupToMoveTo)) ecb.AddBuffer<SoulBufferElement>(groupToMoveTo);

            ecb.AppendToBuffer(groupToMoveTo, new SoulBufferElement { Soul = soulEntity });
            soul.MyGroup = groupToMoveTo;

            SystemAPI.GetBuffer<SoulBufferElement>(groupToMoveFrom).Clear();
            foreach (Entity soulElement in soulArray) ecb.AppendToBuffer(groupToMoveFrom, new SoulBufferElement() { Soul = soulElement });

            soulArray.Dispose();



            ecb.DestroyEntity(rpcEntity);
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