using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;
using Unity.Mathematics;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class TriggerDisasterServerSystem : SystemBase
{
    private double _lastDisasterAt = 0d;
    private readonly double _firstDisasterDelaySeconds = 15d;
    private readonly double _disasterCooldownSeconds = 90d;
    private Random _random = new();



    protected override void OnCreate()
    {
        _random.InitState((uint)(System.DateTime.Now.Millisecond * System.DateTime.Now.Second * System.DateTime.Now.Minute * 1000));
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new(Allocator.TempJob);



        double currentTime = SystemAPI.Time.ElapsedTime;

        if ((currentTime >= _firstDisasterDelaySeconds && _lastDisasterAt < _firstDisasterDelaySeconds)
            || currentTime >= _lastDisasterAt + _disasterCooldownSeconds)
        {
            _lastDisasterAt = currentTime;
            DisasterType newDisaster = (DisasterType)_random.NextInt(0, System.Enum.GetValues(typeof(DisasterType)).Length);

            Entity rpc = EntityManager.CreateEntity();
            ecb.AddComponent(rpc, new StartDisasterRequestRPC() { DisasterType = newDisaster, Seed = _random.NextUInt() });
            ecb.AddComponent<SendRpcCommandRequest>(rpc);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}



public enum DisasterType
{
    LightningStorm,
    MeteorShower,
    LavaFlood,
    Tornado
}

public struct StartDisasterRequestRPC : IRpcCommand
{
    public DisasterType DisasterType;
    public uint Seed;
}