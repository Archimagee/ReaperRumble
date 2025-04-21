using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;



[BurstCompile]
[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct PoisonVialSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PoisonVialTag>();
    }



    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        NativeArray<Entity> vialEntities = SystemAPI.QueryBuilder().WithAll<PoisonVialTag>().Build().ToEntityArray(Allocator.Temp);
        NativeHashMap<Entity, float3> vialPositions = new(vialEntities.Length, Allocator.TempJob);
        foreach (Entity vialEntity in vialEntities) vialPositions.Add(vialEntity, SystemAPI.GetComponent<LocalTransform>(vialEntity).Position);
        vialEntities.Dispose();

        var job = new PoisonVialCollisionJob()
        {
            Ecb = ecb,
            VialPositions = vialPositions
        };

        state.Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
        state.Dependency.Complete();

        vialPositions.Dispose();



        foreach ((RefRO<PoisonVialImpact> impact, Entity vialEntity) in SystemAPI.Query<RefRO<PoisonVialImpact>>().WithEntityAccess())
        {
            Entity poisonField = ecb.Instantiate(SystemAPI.GetSingleton<AbilityPrefabs>().PoisonFieldPrefabEntity);
            ecb.SetComponent(poisonField, new LocalTransform() { Position = impact.ValueRO.Position, Scale = 2f, Rotation = quaternion.identity });
            ecb.DestroyEntity(vialEntity);
        }



        foreach ((RefRO<PoisonFieldData> poisonField, Entity poisonFieldEntity) in SystemAPI.Query<RefRO<PoisonFieldData>>().WithEntityAccess())
        {

        }



        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}



[BurstCompile]
struct PoisonVialCollisionJob : ICollisionEventsJob
{
    public EntityCommandBuffer Ecb;
    public NativeHashMap<Entity, float3> VialPositions;

    public void Execute(CollisionEvent collisionEvent)
    {
        if (VialPositions.ContainsKey(collisionEvent.EntityA))
        {
            Ecb.AddComponent(collisionEvent.EntityA, new PoisonVialImpact() { Position = VialPositions[collisionEvent.EntityA] });
        }
        else if (VialPositions.ContainsKey(collisionEvent.EntityB))
        {
            Ecb.AddComponent(collisionEvent.EntityB, new PoisonVialImpact() { Position = VialPositions[collisionEvent.EntityB] });
        }
    }
}

public partial struct PoisonVialImpact : IComponentData
{
    public float3 Position;
}