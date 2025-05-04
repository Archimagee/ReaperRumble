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
    private readonly double _firstChallengeDelaySeconds = 3f;
    private readonly double _challengeCooldownSeconds = 45f;
    private Random _random = new();



    protected override void OnCreate()
    {
        _random.InitState((uint)System.DateTime.Now.Millisecond * (uint)System.DateTime.Now.Second);
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
            ChallengeType newChallengeType = (ChallengeType)_random.NextInt(0, System.Enum.GetValues(typeof(ChallengeType)).Length);

            Entity rpc = EntityManager.CreateEntity();
            ecb.AddComponent(rpc, new StartChallengeRequestRPC() { ChallengeType = newChallengeType });
            ecb.AddComponent<SendRpcCommandRequest>(rpc);

            Entity challengePrefab = GetChallengePrefab(newChallengeType);

            Entity newChallengeEntity = ecb.Instantiate(challengePrefab);
            ecb.AddComponent(newChallengeEntity, new EventDestroyAt()
            {
                TimeToDestroyAt = SystemAPI.Time.ElapsedTime + SystemAPI.GetComponent<ChallengeData>(challengePrefab).TimeLastsForSeconds
            });
            ecb.SetName(newChallengeEntity, newChallengeType.ToString());
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }



    private Entity GetChallengePrefab(ChallengeType challengeType)
    {
        if (challengeType == ChallengeType.Parkour)
        {
            return SystemAPI.GetSingleton<EntitySpawnerPrefabs>().ParkourChallengePrefabEntity;
        }
        else return Entity.Null;
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