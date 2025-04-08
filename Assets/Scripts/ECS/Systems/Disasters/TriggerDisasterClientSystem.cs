using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using UnityEngine;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class TriggerDisasterClientSystem : SystemBase
{
    private Entity GetDisasterPrefab(DisasterType disasterType)
    {
        if (disasterType == DisasterType.LightningStorm) return SystemAPI.GetSingleton<DisasterPrefabs>().LightningStormDisasterPrefabEntity;
        else if (disasterType == DisasterType.MeteorShower) return SystemAPI.GetSingleton<DisasterPrefabs>().MeteorShowerDisasterPrefabEntity;
        else return Entity.Null;
    }

    private string GetDisasterAnnouncement(DisasterType disasterType)
    {
        if (disasterType == DisasterType.LightningStorm) return "Lightning storm incoming!";
        else if (disasterType == DisasterType.MeteorShower) return "Meteor shower incoming!";
        else return "";
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        foreach ((RefRO<ReceiveRpcCommandRequest> rpcCommandRequest, RefRO<StartDisasterRequestRPC> disaster, Entity recieveRpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<StartDisasterRequestRPC>>().WithEntityAccess())
        {
            Entity disasterPrefab = GetDisasterPrefab(disaster.ValueRO.DisasterType);

            Debug.Log("Spawning " + disaster.ValueRO.DisasterType);
           
            Entity newDisaster = ecb.Instantiate(disasterPrefab);
            ecb.AddComponent(newDisaster, new EventDestroyAt() {
                TimeToDestroyAt = SystemAPI.Time.ElapsedTime + SystemAPI.GetComponent<DisasterData>(disasterPrefab).TimeLastsForSeconds });
            ecb.AddComponent(newDisaster, new EventSeed() { Seed = disaster.ValueRO.Seed });
            ecb.SetName(newDisaster, disaster.ValueRO.DisasterType.ToString());

            UIManager.Instance.SendAnnouncement(GetDisasterAnnouncement(disaster.ValueRO.DisasterType), 3f);



            ecb.DestroyEntity(recieveRpcEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

public struct EventSeed : IComponentData
{
    public uint Seed;
}