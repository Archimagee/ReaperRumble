using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class OrphanSoulsServerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<OrphanSoulsRequestRPC>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);


        foreach ((RefRO<ReceiveRpcCommandRequest> rpcCommandRequest, RefRO<OrphanSoulsRequestRPC> orphanRequest, Entity recieveRpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<OrphanSoulsRequestRPC>>().WithEntityAccess())
        {
            Entity sendRpcEntity = EntityManager.CreateEntity();
            ecb.AddComponent(sendRpcEntity, new OrphanSoulsRequestRPC { GroupID = orphanRequest.ValueRO.GroupID, Amount = orphanRequest.ValueRO.Amount, Velocity = orphanRequest.ValueRO.Velocity });
            ecb.AddComponent<SendRpcCommandRequest>(sendRpcEntity);
            ecb.DestroyEntity(recieveRpcEntity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}