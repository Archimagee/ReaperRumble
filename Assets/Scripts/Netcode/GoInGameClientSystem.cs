using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct GoInGameClientSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkId>();
        state.RequireForUpdate<PlayerDataFromLobby>();
    }



    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRO<NetworkId> networkID, Entity entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<NetworkStreamInGame>().WithEntityAccess())
        {
            ecb.AddComponent<NetworkStreamInGame>(entity);

            Entity rpcEntity = ecb.CreateEntity();
            ecb.AddComponent(rpcEntity, new GoInGameRequestRPC() { PlayerAbility = SystemAPI.GetSingleton<PlayerDataFromLobby>().PlayerAbility });
            ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);

            ecb.DestroyEntity(SystemAPI.GetSingletonEntity<PlayerDataFromLobby>());

            Entity announcementEntity = ecb.CreateEntity();
            ecb.AddComponent(announcementEntity, new PlayAnnouncementAt() { AnnouncementToPlay = (FixedString32Bytes)"It's time to RUMBLE!", TimeToPlayAt = 7.0d });
        }



        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}



public struct GoInGameRequestRPC : IRpcCommand
{
    public PlayerAbility PlayerAbility;
}

public partial struct PlayerDataFromLobby : IComponentData
{
    public int PlayerNumber;
    public PlayerAbility PlayerAbility;
    public float4 PlayerColour;
}