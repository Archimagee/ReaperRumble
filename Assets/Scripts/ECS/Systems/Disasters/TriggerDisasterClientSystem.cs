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
        if (disasterType == DisasterType.LightningStorm)
        {
            return SystemAPI.GetSingleton<DisasterPrefabs>().LightningStormDisasterPrefabEntity;
        }
        else return Entity.Null;
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