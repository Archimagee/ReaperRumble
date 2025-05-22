using Unity.Entities;
using Unity.Burst;
using Unity.Physics;
using Unity.NetCode;



[BurstCompile]
[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class MoveMeteors : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<MeteorData>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        foreach ((RefRO<MeteorData> meteorData, RefRW<PhysicsVelocity> physicsVelocity)
            in SystemAPI.Query<RefRO<MeteorData>, RefRW<PhysicsVelocity>>().WithAll<Simulate>())
        {
            physicsVelocity.ValueRW.Linear = meteorData.ValueRO.MovementDirection * meteorData.ValueRO.MovementSpeed;
        }
    }
}