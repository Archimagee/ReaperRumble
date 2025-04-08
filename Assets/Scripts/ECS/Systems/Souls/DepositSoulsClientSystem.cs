using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class DepositSoulsClientSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<DepositSoulsRequestRPC>();
        RequireForUpdate<ReceiveRpcCommandRequest>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        NativeArray<Entity> players = SystemAPI.QueryBuilder().WithAll<PlayerData>().Build().ToEntityArray(Allocator.Temp);



        foreach ((RefRO<ReceiveRpcCommandRequest> rpcCommandRequest, RefRO<DepositSoulsRequestRPC> depositRequest, Entity recieveRpcEntity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<DepositSoulsRequestRPC>>().WithEntityAccess())
        {
            foreach (Entity player in players)
                if (SystemAPI.GetComponent<PlayerData>(player).PlayerNumber == depositRequest.ValueRO.PlayerNumber)
                {
                    DepositSouls(player, depositRequest.ValueRO.PlayerNumber, ecb);
                }

            ecb.DestroyEntity(recieveRpcEntity);
        }

        players.Dispose();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }



    [BurstCompile]
    private void DepositSouls(Entity player, int PlayerNumber, EntityCommandBuffer ecb)
    {
        DynamicBuffer<SoulBufferElement> soulGroupBuffer = SystemAPI.GetBuffer<SoulBufferElement>(SystemAPI.GetComponent<PlayerSoulGroup>(player).MySoulGroup);
        int amountRemoved = 0;

        foreach (SoulBufferElement soulBufferElement in soulGroupBuffer)
        {
            ecb.DestroyEntity(soulBufferElement.Soul);
            amountRemoved++;
        }

        soulGroupBuffer.Clear();

        if (SystemAPI.HasComponent<GhostOwnerIsLocal>(player))
        {
            if (amountRemoved > 0)
            {
                Entity newRpcEntity = ecb.CreateEntity();
                ecb.AddComponent(newRpcEntity, new AddScoreRequestRPC() { PlayerNumber = PlayerNumber, Amount = amountRemoved });
                ecb.AddComponent<SendRpcCommandRequest>(newRpcEntity);

                ecb.AddComponent(player, new DepositSoulCooldown() { CanDepositAt = SystemAPI.Time.ElapsedTime + 60000 });

                UIManager.Instance.SetSoulCount(0);
                UIManager.Instance.SetDepositCooldown(60000);
            }
        }
    }
}



public partial struct AddScoreRequestRPC : IRpcCommand
{
    public int PlayerNumber;
    public int Amount;
}