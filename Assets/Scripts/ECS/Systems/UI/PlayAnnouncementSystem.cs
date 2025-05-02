using Unity.Collections;
using Unity.Entities;
using Unity.Burst;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class PlayAnnouncementSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<PlayAnnouncementAt>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new(Allocator.TempJob);



        double currentTime = SystemAPI.Time.ElapsedTime;

        foreach ((RefRO<PlayAnnouncementAt> announcement, Entity entity) in SystemAPI.Query<RefRO<PlayAnnouncementAt>>().WithEntityAccess())
        {
            if (currentTime >= announcement.ValueRO.TimeToPlayAt)
            {
                UIManager.Instance.SendAnnouncement(announcement.ValueRO.AnnouncementToPlay.ToString(), 5f);

                ecb.DestroyEntity(entity);
            }
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

public partial struct PlayAnnouncementAt : IComponentData
{
    public FixedString64Bytes AnnouncementToPlay;
    public double TimeToPlayAt;
}