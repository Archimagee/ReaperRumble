using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using Unity.Physics.Systems;
using Unity.NetCode;
using Unity.Physics;
using Unity.Mathematics;



[BurstCompile]
[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
[UpdateAfter(typeof(DetectPlayerSoulCollisions))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct OrphanSoulsClientSystem : ISystem
{
    Entity _groupToOrphanFrom;



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
            }
            Debug.Log("Orphaning " + orphanRequest.ValueRO.Amount + " souls from " + _groupToOrphanFrom);

            int amountToMove = orphanRequest.ValueRO.Amount;
            if (!SystemAPI.HasBuffer<SoulBufferElement>(_groupToOrphanFrom))
            {
                Debug.Log("No Buffer");
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



            //Entity newSoulGroup = group;
            //ecb.RemoveComponent<SoulGroupTarget>(newSoulGroup);
            //ecb.SetComponent(newSoulGroup, new LocalTransform() { Position = SystemAPI.GetComponent<LocalTransform>(_groupToOrphanFrom).Position + orphanRequest.ValueRO.Velocity, Rotation = quaternion.identity, Scale = 1f });
            foreach (Entity soulEntity in soulsToMove)
            {
                ecb.RemoveComponent<SoulGroupMember>(soulEntity);
                ecb.AddComponent<OrphanedTag>(soulEntity);
                ecb.SetComponent(soulEntity, new PhysicsVelocity() { Linear = float3.zero });
            }



            SystemAPI.GetBuffer<SoulBufferElement>(_groupToOrphanFrom).Clear();
            if (soulsToMove.Length == 0 && !SystemAPI.HasComponent<SoulGroupTarget>(_groupToOrphanFrom)) ecb.DestroyEntity(_groupToOrphanFrom);
            foreach (Entity soul in soulsRemaining) ecb.AppendToBuffer<SoulBufferElement>(_groupToOrphanFrom, new SoulBufferElement() { Soul = soul });



            soulsRemaining.Dispose();
            soulsToMove.Dispose();
            ecb.DestroyEntity(recieveRpcEntity);
        }



        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}



public struct OrphanedTag : IComponentData { }