using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;
using UnityEngine;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class SetupPlayerClientSystem : SystemBase
{
    private Entity _cameraPrefabEntity = Entity.Null;



    protected override void OnCreate()
    {
        RequireForUpdate<PlayerSetupRequired>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        if (_cameraPrefabEntity == Entity.Null) _cameraPrefabEntity = SystemAPI.GetSingleton<EntitySpawnerPrefabs>().PlayerCameraPrefabEntity;

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRO<PlayerSetupRequired> playerSetup, RefRO<PlayerSoulGroup> soulGroup, Entity playerEntity) in SystemAPI.Query<RefRO<PlayerSetupRequired>, RefRO<PlayerSoulGroup>>().WithEntityAccess().WithAll<GhostOwnerIsLocal>())
        {
            ecb.SetName(playerEntity, "Player " + playerSetup.ValueRO.PlayerNumber);

            Entity newCameraEntity = ecb.Instantiate(_cameraPrefabEntity);
            ecb.SetName(newCameraEntity, "Player camera");
            ecb.AddComponent(newCameraEntity, new PlayerCameraFollowTarget { Target = playerEntity });

            ecb.AddComponent(playerEntity, new PlayerData()
            {
                MyAbility = playerSetup.ValueRO.PlayerAbility,
                MyColour = playerSetup.ValueRO.PlayerColor,
                IsNicknameSet = false
            });

            Entity playerSoulGroup = soulGroup.ValueRO.MySoulGroup;
            ecb.SetName(playerSoulGroup, "Player " + playerSetup.ValueRO.PlayerNumber + "'s soul group");
            if (!SystemAPI.HasBuffer<SoulBufferElement>(playerSoulGroup)) ecb.AddBuffer<SoulBufferElement>(playerSoulGroup);



            ecb.RemoveComponent<PlayerSetupRequired>(playerEntity);
        }

        foreach ((RefRO<PlayerSetupRequired> playerSetup, RefRO<PlayerSoulGroup> soulGroup, Entity playerEntity) in SystemAPI.Query<RefRO<PlayerSetupRequired>, RefRO<PlayerSoulGroup>>().WithEntityAccess().WithNone<GhostOwnerIsLocal>())
        {
            ecb.SetName(playerEntity, "Player " + playerSetup.ValueRO.PlayerNumber);

            ecb.AddComponent(playerEntity, new PlayerData()
            {
                PlayerNumber = playerSetup.ValueRO.PlayerNumber,
                MyAbility = playerSetup.ValueRO.PlayerAbility,
                MyColour = playerSetup.ValueRO.PlayerColor
            });

            Entity playerSoulGroup = soulGroup.ValueRO.MySoulGroup;
            ecb.SetName(playerSoulGroup, "Player " + playerSetup.ValueRO.PlayerNumber + "'s soul group");

            if (!SystemAPI.HasBuffer<SoulBufferElement>(playerSoulGroup)) ecb.AddBuffer<SoulBufferElement>(playerSoulGroup);



            ecb.RemoveComponent<PlayerSetupRequired>(playerEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}