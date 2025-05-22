using Unity.Burst;
using Unity.Entities;
using Unity.Collections;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class SetPlayerNicknameSystem : SystemBase
{
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new(Allocator.Temp);



        foreach ((RefRO<PlayerData> playerData, PresentationGameObjectCleanup cleanup, Entity playerEntity)
            in SystemAPI.Query<RefRO<PlayerData>, PresentationGameObjectCleanup> ().WithEntityAccess().WithNone<PlayerNicknameSet>())
        {
            if (cleanup.Instance.GetComponent<SetText>() != null) cleanup.Instance.GetComponent<SetText>().Set(playerData.ValueRO.PlayerNickname.ToString());

            UIManager.Instance.SetPlayerNickname(playerData.ValueRO.PlayerNumber, playerData.ValueRO.PlayerNickname);

            ecb.AddComponent<PlayerNicknameSet>(playerEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

public partial struct PlayerNicknameSet : IComponentData { }