using Unity.Collections;
using Unity.Entities;
using Unity.Burst;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class DestroyChallengesClientSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<ChallengeDestroyAt>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        double currentTime = SystemAPI.Time.ElapsedTime;



        foreach ((RefRO<ChallengeDestroyAt> challenge, Entity challengeToDestroy) in SystemAPI.Query<RefRO<ChallengeDestroyAt>>().WithEntityAccess())
        {
            if (currentTime >= challenge.ValueRO.TimeToDestroyAt) ecb.DestroyEntity(challengeToDestroy);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}