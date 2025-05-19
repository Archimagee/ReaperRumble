using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class TriggerDisasterClientSystem : SystemBase
{
    private Entity GetDisasterPrefab(DisasterType disasterType)
    {
        if (disasterType == DisasterType.LightningStorm) return SystemAPI.GetSingleton<DisasterPrefabs>().LightningStormDisasterPrefabEntity;
        else if (disasterType == DisasterType.MeteorShower) return SystemAPI.GetSingleton<DisasterPrefabs>().MeteorShowerDisasterPrefabEntity;
        else if (disasterType == DisasterType.LavaFlood) return SystemAPI.GetSingleton<DisasterPrefabs>().LavaFloodDisasterPrefabEntity;
        else if (disasterType == DisasterType.Tornado) return SystemAPI.GetSingleton<DisasterPrefabs>().TornadoDisasterPrefabEntity;
        else throw new System.Exception("Disaster of type " + disasterType + " does not have an associated prefab");
    }

    private string GetDisasterAnnouncement(DisasterType disasterType)
    {
        if (disasterType == DisasterType.LightningStorm) return "A Storm Brews...";
        else if (disasterType == DisasterType.MeteorShower) return "Incoming Meteor Shower OF DOOOOM!";
        else if (disasterType == DisasterType.LavaFlood) return "The Floor is LAVA!";
        else if (disasterType == DisasterType.Tornado) return "Beware the Tornado of Terror!";
        else if (disasterType == DisasterType.Eruption) return "Mt. Sillinamus Stirs...";
        else throw new System.Exception("Disaster of type " + disasterType + " does not have an associated announcement message");
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRO<ReceiveRpcCommandRequest> rpcCommandRequest, RefRO<StartDisasterRequestRPC> disaster, Entity recieveRpcEntity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<StartDisasterRequestRPC>>().WithEntityAccess())
        {
            Entity disasterPrefab = GetDisasterPrefab(disaster.ValueRO.DisasterType);
           
            Entity newDisaster = ecb.Instantiate(disasterPrefab);
            double timeDisasterLastsFor = SystemAPI.Time.ElapsedTime + SystemAPI.GetComponent<DisasterData>(disasterPrefab).TimeLastsForSeconds;
            ecb.AddComponent(newDisaster, new EventDestroyAt() { TimeToDestroyAt = timeDisasterLastsFor });
            ecb.AddComponent(newDisaster, new EventSeed() { Seed = disaster.ValueRO.Seed });
            ecb.SetName(newDisaster, disaster.ValueRO.DisasterType.ToString());



            Entity announcement = ecb.CreateEntity();
            ecb.AddComponent(announcement, new PlayAnnouncementAt() { AnnouncementToPlay = GetDisasterAnnouncement(disaster.ValueRO.DisasterType), TimeToPlayAt = SystemAPI.Time.ElapsedTime + 1.5d });

            Entity musicChange = ecb.CreateEntity();
            ecb.AddComponent(musicChange, new PlayFightMusicAt() { TimeToPlayAt = SystemAPI.Time.ElapsedTime + timeDisasterLastsFor });
            FightMusicManager.Instance.SetMusicFromDisasterType(disaster.ValueRO.DisasterType);



            ecb.DestroyEntity(recieveRpcEntity);
        }

        foreach ((RefRO<PlayFightMusicAt> playFightMusic, Entity entity) in SystemAPI.Query<RefRO<PlayFightMusicAt>>().WithEntityAccess())
        {
            if (SystemAPI.Time.ElapsedTime >= playFightMusic.ValueRO.TimeToPlayAt)
            {
                FightMusicManager.Instance.PlayFightMusic();
                ecb.DestroyEntity(entity);
            }
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

public struct EventSeed : IComponentData
{
    public uint Seed;
}
public struct PlayFightMusicAt : IComponentData
{
    public double TimeToPlayAt;
}