using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Physics;



[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class MoveMeteors : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<MeteorData>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new(Allocator.Temp);



        foreach ((RefRO<MeteorData> meteorData, RefRW<PhysicsVelocity> physicsVelocity)
            in SystemAPI.Query<RefRO<MeteorData>, RefRW<PhysicsVelocity>>())
        {
            physicsVelocity.ValueRW.Linear = meteorData.ValueRO.MovementDirection * meteorData.ValueRO.MovementSpeed;
        }



        ecb.Playback(EntityManager);
    }
}