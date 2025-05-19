using Unity.Entities;
using Unity.Burst;
using Unity.Physics;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;



[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class MoveEruptionRocks : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<EruptionRockData>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        new MoveEruptionRocksJob().ScheduleParallel();
        this.CompleteDependency();
    }
}



[BurstCompile]
public partial struct MoveEruptionRocksJob : IJobEntity
{
    [BurstCompile]
    public void Execute(in EruptionRockData rockData, ref LocalTransform transform, ref PhysicsVelocity physicsVelocity)
    {
        quaternion directionToImpactPoint = quaternion.LookRotation(math.normalize(rockData.ImpactPoint - transform.Position), new float3(0f, 1f, 0f));

        float angle = math.angle(transform.Rotation, directionToImpactPoint);



        if (angle == 0f) transform.Rotation = directionToImpactPoint;
        else transform.Rotation = math.slerp(transform.Rotation, directionToImpactPoint, math.min(1f, rockData.RotationSpeedRadians / angle));



        physicsVelocity.Linear = transform.Forward() * rockData.MovementSpeed;
    }
}