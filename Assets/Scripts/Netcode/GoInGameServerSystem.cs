using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class GoInGameServerSystem : SystemBase
{
    private NativeArray<Entity> _connectedPlayers = new(4, Allocator.Persistent);
    private NativeList<float3> _spawnPositions = new(4, Allocator.Persistent) {
        new float3(-37.75f, 8f, -29.5f),
        new float3(37.75f, 8f, 29.5f),
        new float3(37.75f, 8f, -29.5f),
        new float3(-37.75f, 8f, 29.5f) };

    private NativeList<Color> _playerColors = new(4, Allocator.Persistent) {
        new Color(1f, 0f, 0f, 1f),
        new Color(1f, 1f, 0f, 1f),
        new Color(0f, 1f, 1f, 1f),
        new Color(1f, 0f, 1f, 1f) };



    protected override void OnCreate()
    {
        RequireForUpdate<NetworkId>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRO<ReceiveRpcCommandRequest> recieveRpcCommandRequest, RefRO<GoInGameRequestRPC> goInGameRequestRPC, Entity recieveRpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<GoInGameRequestRPC>>().WithEntityAccess())
        {
            Entity sourceConnection = recieveRpcCommandRequest.ValueRO.SourceConnection;
            int playerNumber = SystemAPI.GetComponent<NetworkId>(sourceConnection).Value;

            ecb.AddComponent<NetworkStreamInGame>(sourceConnection);

            Entity newPlayerEntity = ecb.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().PlayerPrefabEntity);
            Entity newSoulGroupEntity = ecb.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().SoulGroupPrefabEntity);
            ecb.SetName(newPlayerEntity, "Player " + playerNumber);
            ecb.SetName(newSoulGroupEntity, "Player " + playerNumber + "'s soul group");

            ecb.AddBuffer<SoulBufferElement>(newSoulGroupEntity);

            ecb.AddComponent(newPlayerEntity, new PlayerSoulGroup { MySoulGroup = newSoulGroupEntity });
            ecb.SetComponent(newSoulGroupEntity, new SoulGroupTarget { MyTarget = newPlayerEntity });

            ecb.AddComponent(newPlayerEntity, new GhostOwner { NetworkId = playerNumber });
            ecb.AddComponent(newSoulGroupEntity, new GhostOwner { NetworkId = playerNumber });

            ecb.AppendToBuffer(sourceConnection, new LinkedEntityGroup { Value = newPlayerEntity });



            _connectedPlayers[playerNumber - 1] = newPlayerEntity;
            ecb.SetComponent(newPlayerEntity, new LocalTransform { Position = _spawnPositions[playerNumber - 1], Scale = 1f, Rotation = quaternion.identity });
            ecb.AddComponent(newPlayerEntity, new PlayerSetupRequired()
            {
                PlayerNickname = goInGameRequestRPC.ValueRO.PlayerNickname,
                PlayerNumber = playerNumber,
                PlayerAbility = goInGameRequestRPC.ValueRO.PlayerAbility,
                PlayerColor = _playerColors[playerNumber - 1]
            });
            ecb.AddComponent(newPlayerEntity, new PlayerData()
            {
                PlayerNickname = goInGameRequestRPC.ValueRO.PlayerNickname,
                PlayerNumber = playerNumber,
                MyAbility = goInGameRequestRPC.ValueRO.PlayerAbility,
                MyColour = _playerColors[playerNumber - 1]
            });



            Entity rpcEntity = ecb.CreateEntity();

            ecb.DestroyEntity(recieveRpcEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

public struct PlayerSetupRequired : IComponentData
{
    [GhostField] public int PlayerNumber;
    [GhostField] public PlayerAbility PlayerAbility;
    [GhostField] public Color PlayerColor;
    [GhostField] public FixedString64Bytes PlayerNickname;
}