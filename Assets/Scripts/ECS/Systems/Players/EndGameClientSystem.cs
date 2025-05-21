using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;
using Unity.Collections;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class EndGameClientSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<EndGameRPC>();
        RequireForUpdate<ReceiveRpcCommandRequest>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new(Allocator.Temp);

        foreach ((RefRO<EndGameRPC> endGame, Entity rpcEntity) in SystemAPI.Query<RefRO<EndGameRPC>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
        {
            UIManager.Instance.EndGame(endGame.ValueRO.Player1Score, endGame.ValueRO.Player2Score, endGame.ValueRO.Player3Score, endGame.ValueRO.Player4Score);
            ecb.DestroyEntity(rpcEntity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}