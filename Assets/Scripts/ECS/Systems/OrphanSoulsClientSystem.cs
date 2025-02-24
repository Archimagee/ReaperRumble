using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using Unity.Physics.Systems;
using Unity.NetCode;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct OrphanSoulsClientSystem : ISystem
{
    Entity _groupToOrphanFrom;
    Entity _newGroup;



    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<OrphanSoulsRequestRPC>();
    }



    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRO<ReceiveRpcCommandRequest> rpcCommandRequest, RefRO<OrphanSoulsRequestRPC> orphanRequest, Entity recieveRpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<OrphanSoulsRequestRPC>>().WithEntityAccess())
        {
            foreach ((RefRO<GhostInstance> ghost, Entity ghostEntity) in SystemAPI.Query<RefRO<GhostInstance>>().WithEntityAccess())
            {
                if (ghost.ValueRO.ghostId == orphanRequest.ValueRO.GroupID) _groupToOrphanFrom = ghostEntity;
                if (ghost.ValueRO.ghostId == orphanRequest.ValueRO.NewGroupID) _newGroup = ghostEntity;
            }

            int amountToMove = orphanRequest.ValueRO.Amount;
            if (!SystemAPI.HasBuffer<SoulBufferElement>(_groupToOrphanFrom))
            {
                Debug.LogWarning("No Buffer");
                break;
            }
            NativeArray<SoulBufferElement> soulElements = SystemAPI.GetBuffer<SoulBufferElement>(_groupToOrphanFrom).ToNativeArray(Allocator.Temp);
            if (amountToMove > soulElements.Length) amountToMove = soulElements.Length;
            if (amountToMove <= 0)
            {
                soulElements.Dispose();
                break;
            }

            NativeList<Entity> soulsInGroup = new(Allocator.Temp);
            foreach (SoulBufferElement soulElement in soulElements) soulsInGroup.Add(soulElement.Soul);
            soulElements.Dispose();



            NativeList<Entity> soulsToMove = new(Allocator.Temp);
            NativeList<Entity> soulsRemaining = new(Allocator.Temp);
            foreach (Entity soul in soulsInGroup)
            {
                if (soulsToMove.Length < amountToMove) soulsToMove.Add(soul);
                else soulsRemaining.Add(soul);
            }



            if (!SystemAPI.HasBuffer<SoulBufferElement>(_newGroup)) ecb.AddBuffer<SoulBufferElement>(_newGroup);
            SystemAPI.SetComponent(_newGroup, new LocalTransform() { Position = SystemAPI.GetComponent<LocalTransform>(_groupToOrphanFrom).Position })
            foreach (Entity soulEntity in soulsToMove)
            {
                ecb.SetComponent(soulEntity, new SoulGroupMember() { MyGroup = _newGroup });
                ecb.AddComponent<OrphanedTag>(soulEntity);
                ecb.SetComponent(soulEntity, new PhysicsVelocity() { Linear = float3.zero });
                ecb.AppendToBuffer(_newGroup, new SoulBufferElement() { Soul = soulEntity });
            }



            SystemAPI.GetBuffer<SoulBufferElement>(_groupToOrphanFrom).Clear();
            if (soulsToMove.Length == 0 && !SystemAPI.HasComponent<SoulGroupTarget>(_groupToOrphanFrom)) ecb.DestroyEntity(_groupToOrphanFrom);
            foreach (Entity soul in soulsRemaining) ecb.AppendToBuffer(_groupToOrphanFrom, new SoulBufferElement() { Soul = soul });



            soulsRemaining.Dispose();
            soulsToMove.Dispose();
            ecb.DestroyEntity(recieveRpcEntity);
        }



        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}



public struct OrphanedTag : IComponentData { }