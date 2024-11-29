using Unity.Entities;



[WithAll(typeof(SoulAspect))]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct SoulMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var job = new MoveForwardJob { };
        job.ScheduleParallel();
    }
}