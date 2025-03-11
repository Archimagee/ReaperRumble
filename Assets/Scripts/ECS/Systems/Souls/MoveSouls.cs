using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;
using Unity.Physics;



[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class MoveSouls : SystemBase
{
    private BufferLookup<SoulBufferElement> _lookup;



    protected override void OnCreate()
    {
        RequireForUpdate<Soul>();
        _lookup = GetBufferLookup<SoulBufferElement>(true);
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityQuery groupQuery = SystemAPI.QueryBuilder().WithAll<SoulGroupTag>().Build();
        NativeArray<Entity> groups = groupQuery.ToEntityArray(Allocator.TempJob);
        NativeHashMap<Entity, float3> groupPositions = new NativeHashMap<Entity, float3>(groups.Length, Allocator.TempJob);
        foreach (Entity group in groups)
        {
            groupPositions.Add(group, SystemAPI.GetComponentRO<LocalTransform>(group).ValueRO.Position);
        }

        EntityQuery soulQuery = SystemAPI.QueryBuilder().WithAll<Soul>().Build();
        NativeArray<Entity> souls = soulQuery.ToEntityArray(Allocator.TempJob);
        NativeHashMap<Entity, float3> soulPositions = new NativeHashMap<Entity, float3>(souls.Length, Allocator.TempJob);
        NativeHashMap<Entity, PhysicsVelocity> soulVelocities = new NativeHashMap<Entity, PhysicsVelocity>(souls.Length, Allocator.TempJob);

        foreach (Entity soul in souls)
        {
            soulPositions.Add(soul, SystemAPI.GetComponentRO<LocalTransform>(soul).ValueRO.Position);
            soulVelocities.Add(soul, SystemAPI.GetComponent<PhysicsVelocity>(soul));
        }



        //foreach (Entity soul in souls)
        //{
        //    LocalTransform transform = SystemAPI.GetComponent<LocalTransform>(soul);
        //    float3 currentPos = transform.Position;
        //    if (float.IsNaN(currentPos.x) || float.IsNaN(currentPos.y) || float.IsNaN(currentPos.z)) currentPos = float3.zero;
        //    transform.Rotation = quaternion.identity;
        //    transform.Position = currentPos;
        //}



        _lookup.Update(this);

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        new MoveSoulJob()
        {
            Ecb = ecb.AsParallelWriter(),
            SoulBufferLookup = _lookup,
            GroupPositions = groupPositions,
            SoulPositions = soulPositions,
            SoulVelocities = soulVelocities,
            DeltaTime = SystemAPI.Time.DeltaTime
        }.ScheduleParallel();

        this.CompleteDependency();
        ecb.Playback(EntityManager);
        groupPositions.Dispose();
        soulPositions.Dispose();
    }
}



[BurstCompile]
public partial struct MoveSoulJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    [ReadOnly] public NativeHashMap<Entity, float3> GroupPositions;
    [ReadOnly] public NativeHashMap<Entity, float3> SoulPositions;
    [ReadOnly] public BufferLookup<SoulBufferElement> SoulBufferLookup;
    [ReadOnly] public NativeHashMap<Entity, PhysicsVelocity> SoulVelocities;
    [ReadOnly] public float DeltaTime;



    [BurstCompile]
    public void Execute([ChunkIndexInQuery] int index, in LocalTransform transform, in Soul soul, in SoulGroupMember soulGroupMember, in SoulFacingDirection facingComponent, in Entity entity)
    {
        float3 separation = float3.zero;
        Entity group = soulGroupMember.MyGroup;
        float3 currentPos = transform.Position;
        float3 groupPosition = GroupPositions[group];



        float3 facing = facingComponent.FacingDirection;
        facing = Vector3.RotateTowards(facing, math.normalizesafe(groupPosition - currentPos), 0.06f, 1000f);



        SoulBufferLookup.TryGetBuffer(group, out DynamicBuffer<SoulBufferElement> buffer);
        buffer.ToNativeArray(Allocator.Temp);
        foreach (SoulBufferElement bufferElement in buffer)
        {
            Entity otherSoul = bufferElement.Soul;
            if (bufferElement.Soul != entity)
            {
                float3 otherSoulPosition = SoulPositions[bufferElement.Soul];
                float3 directionToOther = math.normalize(otherSoulPosition - currentPos);
                float distanceFromOther = math.distance(currentPos, otherSoulPosition);
                facing = Vector3.RotateTowards(facing, directionToOther, -((0.007f / buffer.Length) / (distanceFromOther / 3f)), 1000f);
                separation -= directionToOther * (soul.SeparationForce / distanceFromOther);
            }
        }


        float3 resultantForce = separation + (facing * soul.Speed * (1 + (math.distance(currentPos, groupPosition) / 45f)));


        LocalTransform newTransform = new LocalTransform { Position = currentPos + separation + (facing * soul.Speed * (1 + (math.distance(currentPos, groupPosition) / 45f))), Scale = 1f };
        Ecb.SetComponent<PhysicsVelocity>(index, entity, new PhysicsVelocity { Linear = resultantForce });
        Ecb.SetComponent<SoulFacingDirection>(index, entity, new SoulFacingDirection { FacingDirection = facing });
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