using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;
using Unity.Collections;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class EndGameServerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<EndGameTime>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        SystemAPI.TryGetSingleton(out EndGameTime endGame);
        if (SystemAPI.Time.ElapsedTime >= endGame.TimeToEndGameAt)
        {
            NativeList<int> scores = UIManager.Instance.GetScores();

            Entity rpcEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(rpcEntity, new EndGameRPC() {
                Player1Score = scores[0],
                Player2Score = scores[1],
                Player3Score = scores[2],
                Player4Score = scores[3]
            });
            EntityManager.AddComponent<SendRpcCommandRequest>(rpcEntity);

            EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<EndGameTime>());
        }
    }
}

public partial struct EndGameTime : IComponentData
{
    public double TimeToEndGameAt;
}

public partial struct EndGameRPC : IRpcCommand
{
    public int Player1Score;
    public int Player2Score;
    public int Player3Score;
    public int Player4Score;
}