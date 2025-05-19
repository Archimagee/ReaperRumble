using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;
using Unity.Physics;
using Unity.NetCode;



[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
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
        NativeArray<Entity> groups = SystemAPI.QueryBuilder().WithAll<SoulGroupTag>().WithAll<Simulate>().Build().ToEntityArray(Allocator.TempJob);
        NativeHashMap<Entity, float3> groupPositions = new NativeHashMap<Entity, float3>(groups.Length, Allocator.TempJob);
        foreach (Entity group in groups)
        {
            groupPositions.Add(group, SystemAPI.GetComponentRO<LocalTransform>(group).ValueRO.Position);
        }

        NativeArray<Entity> souls = SystemAPI.QueryBuilder().WithAll<Soul>().WithAll<Simulate>().Build().ToEntityArray(Allocator.TempJob);
        NativeHashMap<Entity, float3> soulPositions = new NativeHashMap<Entity, float3>(souls.Length, Allocator.TempJob);

        foreach (Entity soul in souls)
        {
            soulPositions.Add(soul, SystemAPI.GetComponentRO<LocalTransform>(soul).ValueRO.Position);
        }



        _lookup.Update(this);

        new MoveSoulJob()
        {
            SoulBufferLookup = _lookup,
            GroupPositions = groupPositions,
            SoulPositions = soulPositions
        }.Schedule();

        this.CompleteDependency();

        groups.Dispose();
        souls.Dispose();
        groupPositions.Dispose();
        soulPositions.Dispose();
    }
}



[BurstCompile]
public partial struct MoveSoulJob : IJobEntity
{
    [ReadOnly] public NativeHashMap<Entity, float3> GroupPositions;
    [ReadOnly] public NativeHashMap<Entity, float3> SoulPositions;
    [ReadOnly] public BufferLookup<SoulBufferElement> SoulBufferLookup;



    [BurstCompile]
    public void Execute([ChunkIndexInQuery] int index, in LocalTransform transform, in Soul soul, in SoulGroupMember soulGroupMember, ref PhysicsVelocity velocity, ref SoulFacingDirection facingComponent, in Entity entity)
    {
        float3 separation = float3.zero;
        Entity group = soulGroupMember.MyGroup;
        float3 groupPosition = GroupPositions[group];



        facingComponent.FacingDirection = Vector3.RotateTowards(facingComponent.FacingDirection, math.normalizesafe(groupPosition - transform.Position), 0.085f, 1000f);



        SoulBufferLookup.TryGetBuffer(group, out DynamicBuffer<SoulBufferElement> buffer);
        buffer.ToNativeArray(Allocator.Temp);
        foreach (SoulBufferElement bufferElement in buffer)
        {
            Entity otherSoul = bufferElement.Soul;
            if (bufferElement.Soul != entity)
            {
                float3 otherSoulPosition = SoulPositions[bufferElement.Soul];
                float3 directionToOther = math.normalize(otherSoulPosition - transform.Position);
                float distanceFromOther = math.distance(transform.Position, otherSoulPosition);
                facingComponent.FacingDirection = Vector3.RotateTowards(facingComponent.FacingDirection, directionToOther, -((0.007f / buffer.Length) / (distanceFromOther / 3f)), 1000f);
                separation -= directionToOther * (soul.SeparationForce / distanceFromOther);
            }
        }

        velocity.Linear = separation + (facingComponent.FacingDirection * soul.Speed * (1 + (math.distance(transform.Position, groupPosition) / 45f)));
    }
}