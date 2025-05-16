using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class SetupPlayerClientSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<PlayerSetupRequired>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRO<PlayerSetupRequired> playerSetup, RefRO<PlayerSoulGroup> soulGroup, Entity playerEntity) in SystemAPI.Query<RefRO<PlayerSetupRequired>, RefRO<PlayerSoulGroup>>().WithEntityAccess())
        {
            ecb.SetName(playerEntity, "Player " + playerSetup.ValueRO.PlayerNumber);

            ecb.AddComponent(playerEntity, new PlayerData()
            {
                MyAbility = playerSetup.ValueRO.PlayerAbility,
                MyColour = playerSetup.ValueRO.PlayerColor,
                IsNicknameSet = false
            });



            if (SystemAPI.IsComponentEnabled<GhostOwnerIsLocal>(playerEntity))
            {
                Entity newCameraEntity = ecb.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().PlayerCameraPrefabEntity);
                ecb.SetName(newCameraEntity, "Player camera");
                ecb.AddComponent(newCameraEntity, new PlayerCameraFollowTarget { Target = playerEntity });
            }



            ecb.RemoveComponent<PlayerSetupRequired>(playerEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}