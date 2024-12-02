using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;



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
    public void Execute([ChunkIndexInQuery] int index, ref LocalTransform transform, in SoulComponent soul, ref SoulFacingComponent facingComponent, Entity entity)
    {
        float3 separationDistance = float3.zero;
        float3 distanceToMove = float3.zero;
        float3 currentPos = transform.Position;
        float speed = soul.Speed + (math.distance(currentPos, TargetPosition) / 45);

        float3 facing = facingComponent.FacingDirection;
        //float3 tick = math.cross(facing, math.cross(facing, math.normalizesafe(TargetPosition - currentPos)));
        //float3 newFacing = math.cos(7.2f) * facing + math.sin(7.2f) * tick;
        facing = math.normalizesafe(UnityEngine.Vector3.RotateTowards(facing, math.normalizesafe(TargetPosition - currentPos), math.PI / 25f, 1000f)); // quaternion.AxisAngle()??

        foreach (float3 otherPosition in OtherPositions)
        {
            float3 direction = math.normalizesafe(otherPosition - currentPos);
            float distance = math.distance(currentPos, otherPosition);
            float3 newDirectionTowards = math.normalizesafe(UnityEngine.Vector3.RotateTowards(facing, direction, math.PI / 1000f, 1000f));
            facing = math.normalizesafe(facing + ((facing - newDirectionTowards)));
            separationDistance += math.normalizesafe(-direction / distance) * (soul.SeparationForce / OtherPositions.Length);
        }

        Ecb.SetComponent<LocalTransform>(index, entity, new LocalTransform { Position = currentPos + separationDistance + (facing * speed), Scale = 1f });
        Ecb.SetComponent<SoulFacingComponent>(index, entity, new SoulFacingComponent { FacingDirection = facing });
    }
}