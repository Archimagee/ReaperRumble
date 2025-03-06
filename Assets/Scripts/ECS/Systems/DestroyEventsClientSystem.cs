using Unity.Collections;
using Unity.Entities;
using Unity.Burst;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class DestroyEventsClientSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<EventDestroyAt>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        double currentTime = SystemAPI.Time.ElapsedTime;



        foreach ((RefRO<EventDestroyAt> eventTime, Entity eventToDestroy) in SystemAPI.Query<RefRO<EventDestroyAt>>().WithEntityAccess())
        {
            if (currentTime >= eventTime.ValueRO.TimeToDestroyAt) ecb.DestroyEntity(eventToDestroy);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}