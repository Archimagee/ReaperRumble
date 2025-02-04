using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class SpawnSoulsServerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<SpawnSoulsRequestRPC>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);


        foreach ((RefRO<ReceiveRpcCommandRequest> rpcCommandRequest, RefRO<SpawnSoulsRequestRPC> spawnRequest, Entity recieveRpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<SpawnSoulsRequestRPC>>().WithEntityAccess())
        {
            Entity sendRpcEntity = EntityManager.CreateEntity();
            ecb.AddComponent(sendRpcEntity, new SpawnSoulsRequestRPC { GroupID = spawnRequest.ValueRO.GroupID, Amount = spawnRequest.ValueRO.Amount } );
            ecb.AddComponent<SendRpcCommandRequest>(sendRpcEntity);
            ecb.DestroyEntity(recieveRpcEntity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}