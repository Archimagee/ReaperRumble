using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class TriggerDisasterServerSystem : SystemBase
{
    private double _lastDisasterAt = 0f;
    private readonly double _firstDisasterDelaySeconds = 5f;
    private readonly double _disasterCooldownSeconds = 60f;
    private Random _random = new();



    protected override void OnCreate()
    {
        _random.InitState(153478);
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
    MeteorShower
}

public struct StartDisasterRequestRPC : IRpcCommand
{
    public DisasterType DisasterType;
    public uint Seed;
}