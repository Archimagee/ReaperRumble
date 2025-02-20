using Unity.Entities;
using Unity.Physics.Systems;



[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class SoulPhysicsSystemGroup : CustomPhysicsSystemGroup
{
    public SoulPhysicsSystemGroup() : base(1, false) { }
}