using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class DepositSoulsServerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<DepositSoulsRequestRPC>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        foreach ((RefRO<ReceiveRpcCommandRequest> rpcCommandRequest, RefRO<DepositSoulsRequestRPC> depositRequest, Entity recieveRpcEntity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<DepositSoulsRequestRPC>>().WithEntityAccess())
        {
            Entity newRpcEntity = ecb.CreateEntity();
            ecb.AddComponent(newRpcEntity, new DepositSoulsRequestRPC() { PlayerNumber = depositRequest.ValueRO.PlayerNumber });
            ecb.AddComponent<SendRpcCommandRequest>(newRpcEntity);

            ecb.DestroyEntity(recieveRpcEntity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}



public partial struct DepositSoulsRequestRPC : IRpcCommand
{
    public int PlayerNumber;
}