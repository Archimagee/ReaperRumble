using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class GoInGameServerSystem : SystemBase
{
    private NativeArray<Entity> _connectedPlayers = new(4, Allocator.Persistent);
    private NativeList<float3> _spawnPositions = new(4, Allocator.Persistent) {
        new float3(-40f, 5.5f, -28f),
        new float3(40f, 5.5f, 28f),
        new float3(-40f, 5.5f, 28f),
        new float3(40f, 5.5f, -28f) };



    protected override void OnCreate()
    {
        RequireForUpdate<NetworkId>();
    }



    //[BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRO<ReceiveRpcCommandRequest> recieveRpcCommandRequest, Entity recieveRpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<GoInGameRequestRPC>().WithEntityAccess())
        {
            Entity sourceConnection = recieveRpcCommandRequest.ValueRO.SourceConnection;
            int playerNumber = SystemAPI.GetComponent<NetworkId>(sourceConnection).Value;

            ecb.AddComponent<NetworkStreamInGame>(sourceConnection);

            Entity newPlayerEntity = ecb.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().PlayerPrefabEntity);
            Entity newSoulGroupEntity = ecb.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().SoulGroupPrefabEntity);
            ecb.SetName(newPlayerEntity, "Player " + playerNumber);
            ecb.SetName(newSoulGroupEntity, "Player " + playerNumber + "'s soul group");

            ecb.AddComponent(newPlayerEntity, new PlayerSetupRequired()
            {
                PlayerNumber = playerNumber,
                PlayerAbility = PlayerAbility.SixShooter
            });

            ecb.AddComponent(newPlayerEntity, new PlayerSoulGroup { MySoulGroup = newSoulGroupEntity });
            ecb.AddComponent(newSoulGroupEntity, new SoulGroupTarget { MyTarget = newPlayerEntity });

            ecb.AddComponent(newPlayerEntity, new GhostOwner { NetworkId = playerNumber });
            ecb.AddComponent(newSoulGroupEntity, new GhostOwner { NetworkId = playerNumber });
            
            ecb.AppendToBuffer(sourceConnection, new LinkedEntityGroup { Value = newPlayerEntity });



            _connectedPlayers[playerNumber - 1] = newPlayerEntity;
            ecb.SetComponent(newPlayerEntity, new LocalTransform { Position = _spawnPositions[playerNumber - 1], Scale = 1f, Rotation = quaternion.identity });



            ecb.DestroyEntity(recieveRpcEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

//[GhostComponent]
public struct PlayerSetupRequired : IComponentData
{
    [GhostField] public int PlayerNumber;
    [GhostField] public PlayerAbility PlayerAbility;
}