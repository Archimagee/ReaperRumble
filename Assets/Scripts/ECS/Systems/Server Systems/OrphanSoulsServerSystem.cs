using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial class OrphanSoulsServerSystem : SystemBase
{
    NativeQueue<int> GroupQueue;
    NativeQueue<int> AmountQueue;
    NativeQueue<Entity> NewGroupQueue;
    protected override void OnCreate()
    {
        RequireForUpdate<OrphanSoulsRequestRPC>();
        GroupQueue = new NativeQueue<int>(Allocator.Persistent);
        AmountQueue = new NativeQueue<int>(Allocator.Persistent);
        NewGroupQueue = new NativeQueue<Entity>(Allocator.Persistent);
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        if (GroupQueue.Count > 0)
        {
            Unity.Mathematics.Random rand = new();
            rand.InitState(123455u);
            float3 randVelocity = new float3(rand.NextFloat(-1f, 1), 5f, rand.NextFloat(-1f, 1f));

            Entity sendRpcEntity = EntityManager.CreateEntity();
            ecb.AddComponent(sendRpcEntity, new OrphanSoulsRequestRPC
            {
                GroupID = GroupQueue.Dequeue(),
                Amount = AmountQueue.Dequeue(),
                NewGroupID = SystemAPI.GetComponent<GhostInstance>(NewGroupQueue.Dequeue()).ghostId,
                Velocity = randVelocity
            });
            ecb.AddComponent<SendRpcCommandRequest>(sendRpcEntity);
        }


        foreach ((RefRO<ReceiveRpcCommandRequest> rpcCommandRequest, RefRO<OrphanSoulsRequestRPC> orphanRequest, Entity recieveRpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<OrphanSoulsRequestRPC>>().WithEntityAccess())
        {
            Entity newGroup = EntityManager.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().SoulGroupPrefabEntity);
            ecb.SetComponent(newGroup, new LocalTransform() { Scale = 1f, Rotation = quaternion.identity, Position = orphanRequest.ValueRO.Position });
            NewGroupQueue.Enqueue(newGroup);
            GroupQueue.Enqueue(orphanRequest.ValueRO.GroupID);
            AmountQueue.Enqueue(orphanRequest.ValueRO.Amount);
            ecb.DestroyEntity(recieveRpcEntity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}