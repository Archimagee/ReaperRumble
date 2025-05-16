using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class PlayAnnouncementSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireAnyForUpdate(SystemAPI.QueryBuilder().WithAny<PlayAnnouncementAt>().WithAny<PlayAnnouncementAtRPC>().Build());
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

        foreach ((RefRO<PlayAnnouncementAtRPC> announcement, Entity rpcEntity) in SystemAPI.Query<RefRO<PlayAnnouncementAtRPC>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
        {
            if (currentTime >= announcement.ValueRO.TimeToPlayAt)
            {
                UIManager.Instance.SendAnnouncement(announcement.ValueRO.AnnouncementToPlay.ToString(), 5f);

                ecb.DestroyEntity(rpcEntity);
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

public partial struct PlayAnnouncementAtRPC : IRpcCommand
{
    public FixedString64Bytes AnnouncementToPlay;
    public double TimeToPlayAt;
}