using Unity.Entities;
using Unity.NetCode;



[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct SetPlayerNicknameSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach ((PresentationGameObjectCleanup cleanup, RefRW<PlayerData> playerData) in SystemAPI.Query<PresentationGameObjectCleanup, RefRW<PlayerData>>().WithNone<GhostOwnerIsLocal>())
        {
            if (!playerData.ValueRO.IsNicknameSet)
            {
                cleanup.Instance.GetComponent<SetText>().Set("Player " + playerData.ValueRO.PlayerNumber.ToString());
                playerData.ValueRW.IsNicknameSet = true;
            }
        }
    }
}