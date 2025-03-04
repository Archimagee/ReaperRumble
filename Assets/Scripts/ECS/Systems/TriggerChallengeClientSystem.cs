using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using UnityEngine;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class TriggerChallengeClientSystem : SystemBase
{
    private Entity GetChallengePrefab(ChallengeType challengeType)
    {
        if (challengeType == ChallengeType.Parkour)
        {
            return SystemAPI.GetSingleton<EntitySpawnerPrefabs>().ParkourChallengePrefabEntity;
        }
        else return Entity.Null;
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        foreach ((RefRO<ReceiveRpcCommandRequest> rpcCommandRequest, RefRO<StartChallengeRequestRPC> challenge, Entity recieveRpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<StartChallengeRequestRPC>>().WithEntityAccess())
        {
            Entity challengePrefab = GetChallengePrefab(challenge.ValueRO.ChallengeType);

            Debug.Log("Spawning " + challenge.ValueRO.ChallengeType);
           
            Entity newChallenge = ecb.Instantiate(challengePrefab);
            ecb.AddComponent(newChallenge, new EventDestroyAt() {
                TimeToDestroyAt = SystemAPI.Time.ElapsedTime + SystemAPI.GetComponent<ChallengeData>(challengePrefab).TimeLastsForSeconds });


            ecb.DestroyEntity(recieveRpcEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}