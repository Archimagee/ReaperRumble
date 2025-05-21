using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class AddScoreClientSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<AddPlayerScore>();
        RequireForUpdate<ReceiveRpcCommandRequest>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new(Allocator.Temp);



        foreach ((RefRO<AddPlayerScore> addScore, Entity rpcEntity) in SystemAPI.Query<RefRO<AddPlayerScore>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
        {
            UIManager.Instance.AddScore(addScore.ValueRO.PlayerNumber, addScore.ValueRO.ScoreToAdd);

            ecb.DestroyEntity(rpcEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}