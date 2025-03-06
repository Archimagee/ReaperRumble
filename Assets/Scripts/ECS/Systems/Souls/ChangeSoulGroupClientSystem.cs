using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
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



        foreach ((RefRO<ChangeSoulGroup> changeRequest, RefRW<SoulGroupMember> soul, Entity soulEntityToMove) in SystemAPI.Query<RefRO<ChangeSoulGroup>, RefRW<SoulGroupMember>>().WithEntityAccess())
        {
            Entity groupToMoveTo = changeRequest.ValueRO.SoulGroupToMoveTo;
            Entity groupToMoveFrom = soul.ValueRO.MyGroup;



            NativeArray<SoulBufferElement> soulElementArray = SystemAPI.GetBuffer<SoulBufferElement>(groupToMoveFrom).ToNativeArray(Allocator.Temp);
            NativeList<Entity> soulsToKeepArray = new(Allocator.Temp);
            foreach (SoulBufferElement soulBufferElement in soulElementArray) if (soulBufferElement.Soul != soulEntityToMove) soulsToKeepArray.Add(soulBufferElement.Soul);
            soulElementArray.Dispose();



            ecb.AppendToBuffer(groupToMoveTo, new SoulBufferElement { Soul = soulEntityToMove });

            soul.ValueRW.MyGroup = groupToMoveTo;

            SystemAPI.GetBuffer<SoulBufferElement>(groupToMoveFrom).Clear();
            foreach (Entity soulToKeep in soulsToKeepArray) ecb.AppendToBuffer(groupToMoveFrom, new SoulBufferElement() { Soul = soulToKeep });

            soulsToKeepArray.Dispose();



            ecb.RemoveComponent<ChangeSoulGroup>(soulEntityToMove);
        }

        // change this to add to a queue so it doesnt try to change the same soul when multiple rpcs recieved
        foreach ((RefRO<ChangeSoulGroupRequestRPC> changeRequest, RefRO<ReceiveRpcCommandRequest> rpc, Entity rpcEntity) in SystemAPI.Query<RefRO<ChangeSoulGroupRequestRPC>, RefRO<ReceiveRpcCommandRequest>> ().WithEntityAccess())
        {
            Entity groupToMoveTo = Entity.Null;
            Entity groupToMoveFrom = Entity.Null;

            foreach ((RefRO<GhostInstance> ghost, Entity ghostEntity) in SystemAPI.Query<RefRO<GhostInstance>>().WithEntityAccess())
            {
                if (ghost.ValueRO.ghostId == changeRequest.ValueRO.GroupIDTo) groupToMoveTo = ghostEntity;
                else if (ghost.ValueRO.ghostId == changeRequest.ValueRO.GroupIDFrom) groupToMoveFrom = ghostEntity;
            }



            NativeArray<SoulBufferElement> soulElementArray = SystemAPI.GetBuffer<SoulBufferElement>(groupToMoveFrom).ToNativeArray(Allocator.Temp);
            Entity soulEntityToMove = soulElementArray[0].Soul;
            NativeList<Entity> soulsToKeepArray = new(Allocator.Temp);
            foreach (SoulBufferElement soulBufferElement in soulElementArray) if (soulBufferElement.Soul != soulEntityToMove) soulsToKeepArray.Add(soulBufferElement.Soul);
            soulElementArray.Dispose();



            ecb.AppendToBuffer(groupToMoveTo, new SoulBufferElement { Soul = soulEntityToMove });

            SystemAPI.SetComponent(soulEntityToMove, new SoulGroupMember() { MyGroup = groupToMoveTo });

            SystemAPI.GetBuffer<SoulBufferElement>(groupToMoveFrom).Clear();
            foreach (Entity soulToKeep in soulsToKeepArray) ecb.AppendToBuffer(groupToMoveFrom, new SoulBufferElement() { Soul = soulToKeep });

            soulsToKeepArray.Dispose();



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