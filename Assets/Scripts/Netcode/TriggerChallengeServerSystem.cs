using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class TriggerChallengeServerSystem : SystemBase
{
    private double _lastChallengeAt = 0f;
    private readonly double _firstChallengeDelaySeconds = 10f;
    private readonly double _challengeCooldownSeconds = 45f;
    private Random _random = new();



    protected override void OnCreate()
    {
        _random.InitState(56789);
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        double currentTime = SystemAPI.Time.ElapsedTime;

        if ((currentTime >= _firstChallengeDelaySeconds && _lastChallengeAt < _firstChallengeDelaySeconds)
            || currentTime >= _lastChallengeAt + _challengeCooldownSeconds)
        {
            _lastChallengeAt = currentTime;
            ChallengeType newChallenge = (ChallengeType)_random.NextInt(0, System.Enum.GetValues(typeof(ChallengeType)).Length);

            Entity rpc = EntityManager.CreateEntity();
            ecb.AddComponent(rpc, new StartChallengeRequestRPC() { ChallengeType = newChallenge });
            ecb.AddComponent<SendRpcCommandRequest>(rpc);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}



public enum ChallengeType
{
    Parkour = 0
}

public struct StartChallengeRequestRPC : IRpcCommand
{
    public ChallengeType ChallengeType;
}