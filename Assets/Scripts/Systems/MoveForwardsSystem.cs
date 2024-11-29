using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;



[BurstCompile]
[WithAll(typeof(MoveForwardsTag))]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct MoveForwardsSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MoveSpeedComponent>();
    }



    public void OnUpdate(ref SystemState state)
    {
        var job = new MoveForwardJob { };
        job.ScheduleParallel();
    }
}



[BurstCompile]
public partial struct MoveForwardJob : IJobEntity
{
    public void Execute(ref MoveSpeedComponent moveComponent, ref LocalTransform transform)
    {
        transform.Position.x += moveComponent.Speed;
    }
}



public struct MoveForwardsTag : IComponentData { }