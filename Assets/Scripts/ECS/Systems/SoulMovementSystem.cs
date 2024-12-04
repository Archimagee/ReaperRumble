using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;



[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class SoulMovementSystem : SystemBase
{
    private BufferLookup<SoulBufferElement> _lookup;
    private EndInitializationEntityCommandBufferSystem.Singleton _ecbs;



    protected override void OnCreate()
    {
        RequireForUpdate<SoulComponent>();
        _ecbs = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
        _lookup = GetBufferLookup<SoulBufferElement>(true);
    }



    protected override void OnUpdate()
    {
        foreach ((RefRW<SoulComponent> soul, RefRW<SoulFacingComponent> facingComponent, Entity entity) in SystemAPI.Query<RefRW<SoulComponent>, RefRW<SoulFacingComponent>>().WithEntityAccess())
        {
            _lookup.Update(this);
            _lookup.TryGetBuffer(soul.ValueRO.MyGroup, out DynamicBuffer<SoulBufferElement> buffer);

            NativeArray<float3> otherPositions = new NativeArray<float3>(buffer.Length, Allocator.TempJob);
            for (int i = 0; i < buffer.Length; i++)
            {
                otherPositions[i] = SystemAPI.GetComponent<LocalTransform>(buffer[i].Soul).Position;
            }

            new MoveSoulJob()
            {
                Ecb = _ecbs.CreateCommandBuffer(World.DefaultGameObjectInjectionWorld.EntityManager.WorldUnmanaged).AsParallelWriter(),
                TargetPosition = SystemAPI.GetComponentRO<LocalTransform>(soul.ValueRO.MyGroup).ValueRO.Position,
                OtherPositions = otherPositions
            }.ScheduleParallel();
            this.CompleteDependency();
        }
    }
}



[BurstCompile]
[WithAll(typeof(SoulComponent))]
public partial struct MoveSoulJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    public float3 TargetPosition;
    [DeallocateOnJobCompletion] public NativeArray<float3> OtherPositions;
    public void Execute([ChunkIndexInQuery] int index, in LocalTransform transform, in SoulComponent soul, in SoulFacingComponent facingComponent, in Entity entity)
    {
        float3 separation = float3.zero;
        float3 currentPos = transform.Position;

        float3 facing = facingComponent.FacingDirection;

        facing = Vector3.RotateTowards(facing, math.normalizesafe(TargetPosition - currentPos), 0.06f, 1000f);

        int others = OtherPositions.Length;
        float separationForce = soul.SeparationForce;
        foreach (float3 otherPosition in OtherPositions)
        {
            if (!otherPosition.Equals(currentPos))
            {
                float3 directionToOther = math.normalize(otherPosition - currentPos);
                float distanceFromOther = math.distance(currentPos, otherPosition);
                facing = Vector3.RotateTowards(facing, directionToOther, -((0.007f / others) / (distanceFromOther / 3f)), 1000f);
                separation -= directionToOther * (separationForce / distanceFromOther);
            }
        }
        Ecb.SetComponent<LocalTransform>(index, entity, new LocalTransform { Position = currentPos + separation + (facing * soul.Speed * (1 + (math.distance(currentPos, TargetPosition) / 45f))), Scale = 1f });
        Ecb.SetComponent<SoulFacingComponent>(index, entity, new SoulFacingComponent { FacingDirection = facing });
    }
}



//[BurstCompile]
//[WithAll(typeof(SoulComponent))]
//public partial struct MoveSoulJobQuat : IJobEntity
//{
//    public EntityCommandBuffer.ParallelWriter Ecb;
//    public float3 TargetPosition;
//    [DeallocateOnJobCompletion] public NativeArray<float3> OtherPositions;
//    public void Execute([ChunkIndexInQuery] int index, in LocalTransform transform, in SoulComponent soul, in Entity entity)
//    {
//        float3 separationDistance = float3.zero;
//        float3 distanceToMove = float3.zero;
//        float3 currentPos = transform.Position;
//        float speed = soul.Speed + (math.distance(currentPos, TargetPosition) / 45f);

//        quaternion currentRotation = transform.Rotation;
//        quaternion directionTowards = quaternion.Euler(TargetPosition - currentPos);
//        currentRotation = Quaternion.RotateTowards(currentRotation, directionTowards, 12.25f);

//        foreach (float3 otherPosition in OtherPositions)
//        {
//            directionTowards = quaternion.Euler(otherPosition - currentPos);
//            float distance = math.distance(currentPos, otherPosition);
//            quaternion newFacing = Quaternion.RotateTowards(currentRotation, directionTowards, -0.13f);
//            separationDistance += math.normalizesafe(-math.normalizesafe(otherPosition - currentPos) / distance) * (soul.SeparationForce / OtherPositions.Length);
//        }

//        float3 u = new float3(currentRotation.value.x, currentRotation.value.y, currentRotation.value.z);
//        float s = currentRotation.value.w;
//        float3 forward = new float3(0f, 0f, 1f);
//        forward = math.normalizesafe(
//            2.0f * math.dot(u, forward) * u
//           + (s * s - math.dot(u, u)) * forward
//           + 2.0f * s * math.cross(u, forward));

//        Ecb.SetComponent<LocalTransform>(index, entity, new LocalTransform { Position = currentPos + separationDistance + forward * speed, Rotation = currentRotation, Scale = 1f });
//    }
//}