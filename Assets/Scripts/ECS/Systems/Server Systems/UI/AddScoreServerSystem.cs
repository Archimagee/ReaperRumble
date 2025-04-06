using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct AddScoreServerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AddScoreRequestRPC>();
        state.RequireForUpdate<ReceiveRpcCommandRequest>();
    }



    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRO<AddScoreRequestRPC> scoreRequest, RefRO<ReceiveRpcCommandRequest> rpc, Entity rpcEntity) in SystemAPI.Query<RefRO<AddScoreRequestRPC>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            Entity newRpcEntity = ecb.CreateEntity();
            ecb.AddComponent(newRpcEntity, new AddScoreRequestRPC()
            {
                PlayerNumber = scoreRequest.ValueRO.PlayerNumber,
                Amount = scoreRequest.ValueRO.Amount,
            });
            ecb.AddComponent<SendRpcCommandRequest>(newRpcEntity);



            ecb.DestroyEntity(rpcEntity);
        }



        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}