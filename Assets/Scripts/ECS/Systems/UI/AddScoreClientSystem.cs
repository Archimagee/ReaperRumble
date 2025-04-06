using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class AddScoreClientSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<AddScoreRequestRPC>();
        RequireForUpdate<ReceiveRpcCommandRequest>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRO<AddScoreRequestRPC> scoreRequest, RefRO<ReceiveRpcCommandRequest> rpc, Entity rpcEntity) in SystemAPI.Query<RefRO<AddScoreRequestRPC>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            ScoreboardManager.Instance.AddScore(scoreRequest.ValueRO.PlayerNumber, scoreRequest.ValueRO.Amount);

            ecb.DestroyEntity(rpcEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}