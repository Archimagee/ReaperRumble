using Unity.Entities;
using Unity.Collections;
using Unity.Physics.Systems;
using Unity.NetCode;
using UnityEngine.VFX;
using UnityEngine;
using Unity.Mathematics;



[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
[UpdateAfter(typeof(DetectPlayerSoulCollisions))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class ChangeSoulGroupClientSystem : SystemBase
{
    NativeQueue<ChangeSoulGroupData> _changeQueue = new NativeQueue<ChangeSoulGroupData>(Allocator.Persistent);



    protected override void OnUpdate()
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
            SetSoulColor(soulEntityToMove, SystemAPI.GetComponent<SoulGroupTarget>(groupToMoveTo).MyTarget);

            SystemAPI.GetBuffer<SoulBufferElement>(groupToMoveFrom).Clear();
            if (soulsToKeepArray.Length == 0) DestroySoulGroup(groupToMoveFrom, ecb);
            else foreach (Entity soulToKeep in soulsToKeepArray) ecb.AppendToBuffer(groupToMoveFrom, new SoulBufferElement() { Soul = soulToKeep });



            if (SystemAPI.GetComponent<SoulGroupTarget>(groupToMoveTo).MyTarget != Entity.Null && SystemAPI.HasComponent<GhostOwnerIsLocal>(SystemAPI.GetComponent<SoulGroupTarget>(groupToMoveTo).MyTarget))
            {
                UIManager.Instance.SetSoulCount(SystemAPI.GetBuffer<SoulBufferElement>(groupToMoveTo).Length + 1);
            }
            if (SystemAPI.GetComponent<SoulGroupTarget>(groupToMoveFrom).MyTarget != Entity.Null && SystemAPI.HasComponent<GhostOwnerIsLocal>(SystemAPI.GetComponent<SoulGroupTarget>(groupToMoveTo).MyTarget))
            {
                UIManager.Instance.SetSoulCount(SystemAPI.GetBuffer<SoulBufferElement>(groupToMoveFrom).Length + 1);
            }



            soulsToKeepArray.Dispose();



            ecb.RemoveComponent<ChangeSoulGroup>(soulEntityToMove);
        }



        if (_changeQueue.Count > 0)
        {
            ChangeSoulGroupData changeData = _changeQueue.Dequeue();

            Entity groupToMoveTo = changeData.SoulGroupToMoveTo;
            Entity groupToMoveFrom = changeData.SoulGroupToMoveFrom;

            NativeArray<SoulBufferElement> soulElementArray = SystemAPI.GetBuffer<SoulBufferElement>(groupToMoveFrom).ToNativeArray(Allocator.Temp);
            NativeArray<Entity> soulsToMove = new(soulElementArray.Length, Allocator.Temp);



            for (int i = 0; i < changeData.AmountOfSoulsToMove; i++)
            {
                Entity soul = soulElementArray[i].Soul;
                soulsToMove[i] = soul;
                ecb.AppendToBuffer(groupToMoveTo, new SoulBufferElement { Soul = soul });
                SystemAPI.SetComponent(soul, new SoulGroupMember() { MyGroup = groupToMoveTo });
                SetSoulColor(soul, SystemAPI.GetComponent<SoulGroupTarget>(groupToMoveTo).MyTarget);
            }

            NativeList<Entity> soulsToKeep = new(Allocator.Temp);
            foreach (SoulBufferElement soulBufferElement in soulElementArray) if (!soulsToMove.Contains(soulBufferElement.Soul)) soulsToKeep.Add(soulBufferElement.Soul);
            soulElementArray.Dispose();


            if (soulsToKeep.Length == 0 && SystemAPI.GetComponent<SoulGroupTarget>(groupToMoveFrom).MyTarget == Entity.Null)
            {
                DestroySoulGroup(groupToMoveFrom, ecb);
            }
            else
            {
                SystemAPI.GetBuffer<SoulBufferElement>(groupToMoveFrom).Clear();
                foreach (Entity soulToKeep in soulsToKeep) ecb.AppendToBuffer(groupToMoveFrom, new SoulBufferElement() { Soul = soulToKeep });
            }



            if (SystemAPI.GetComponent<SoulGroupTarget>(groupToMoveTo).MyTarget != Entity.Null && SystemAPI.HasComponent<GhostOwnerIsLocal>(SystemAPI.GetComponent<SoulGroupTarget>(groupToMoveTo).MyTarget))
            {
                UIManager.Instance.SetSoulCount(SystemAPI.GetBuffer<SoulBufferElement>(groupToMoveTo).Length + 1);
            }
            if (SystemAPI.GetComponent<SoulGroupTarget>(groupToMoveFrom).MyTarget != Entity.Null && SystemAPI.HasComponent<GhostOwnerIsLocal>(SystemAPI.GetComponent<SoulGroupTarget>(groupToMoveTo).MyTarget))
            {
                UIManager.Instance.SetSoulCount(SystemAPI.GetBuffer<SoulBufferElement>(groupToMoveFrom).Length + 1);
            }


            soulsToKeep.Dispose();
            soulsToMove.Dispose();
        }



        foreach ((RefRO<ChangeSoulGroupRequestRPC> changeRequest, RefRO<ReceiveRpcCommandRequest> rpc, Entity rpcEntity) in SystemAPI.Query<RefRO<ChangeSoulGroupRequestRPC>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            Entity groupToMoveTo = Entity.Null;
            Entity groupToMoveFrom = Entity.Null;

            foreach ((RefRO<GhostInstance> ghost, Entity ghostEntity) in SystemAPI.Query<RefRO<GhostInstance>>().WithEntityAccess())
            {
                if (ghost.ValueRO.ghostId == changeRequest.ValueRO.GroupIDTo) groupToMoveTo = ghostEntity;
                else if (ghost.ValueRO.ghostId == changeRequest.ValueRO.GroupIDFrom) groupToMoveFrom = ghostEntity;
            }

            if (!SystemAPI.HasBuffer<SoulBufferElement>(groupToMoveFrom)) ecb.AddBuffer<SoulBufferElement>(groupToMoveFrom);
            if (!SystemAPI.HasBuffer<SoulBufferElement>(groupToMoveTo)) ecb.AddBuffer<SoulBufferElement>(groupToMoveTo);

            _changeQueue.Enqueue(new ChangeSoulGroupData() {
                SoulGroupToMoveFrom = groupToMoveFrom,
                SoulGroupToMoveTo = groupToMoveTo,
                AmountOfSoulsToMove = math.min(changeRequest.ValueRO.Amount, SystemAPI.GetBuffer<SoulBufferElement>(groupToMoveFrom).Length) });

            ecb.DestroyEntity(rpcEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }



    public void DestroySoulGroup(Entity soulGroup, EntityCommandBuffer ecb)
    {
        int ghostID = SystemAPI.GetComponent<GhostInstance>(soulGroup).ghostId;

        Entity rpcEntity = ecb.CreateEntity();
        ecb.AddComponent(rpcEntity, new DestroySoulGroupRequestRPC() { GroupToDestroyID = ghostID });
        ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
    }



    public void SetSoulColor(Entity soul, Entity owner)
    {
        VisualEffect vfx = EntityManager.GetComponentObject<VisualEffect>(soul);
        Vector4 color;

        if (owner == Entity.Null) color = new Vector4(0.32f, 0.2f, 0.7f, 1f);
        else
        {
            color = SystemAPI.GetComponent<PlayerData>(owner).MyColour;
        }
        vfx.SetVector4("SoulColor", color);
    }



    private struct ChangeSoulGroupData
    {
        public Entity SoulGroupToMoveFrom;
        public Entity SoulGroupToMoveTo;
        public int AmountOfSoulsToMove;
    }
}



public struct ChangeSoulGroupRequestRPC : IRpcCommand
{
    public int GroupIDFrom;
    public int GroupIDTo;
    public int Amount;
}