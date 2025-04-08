using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class TriggerChallengeClientSystem : SystemBase
{
    private string GetChallengeAnnouncement(ChallengeType challengeType)
    {
        if (challengeType == ChallengeType.Parkour) return "Parkour course challenge active!";
        else return "";
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        foreach ((RefRO<ReceiveRpcCommandRequest> rpcCommandRequest, RefRO<StartChallengeRequestRPC> challenge, Entity recieveRpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<StartChallengeRequestRPC>>().WithEntityAccess())
        {
            UIManager.Instance.SendAnnouncement(GetChallengeAnnouncement(challenge.ValueRO.ChallengeType), 3f);



            ecb.DestroyEntity(recieveRpcEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}