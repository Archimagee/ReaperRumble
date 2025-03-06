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



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRO<ReceiveRpcCommandRequest> recieveRpcCommandRequest, Entity recieveRpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<GoInGameRequestRPC>().WithEntityAccess())
        {
            Entity sourceConnection = recieveRpcCommandRequest.ValueRO.SourceConnection;
            int playerNumber = SystemAPI.GetComponent<NetworkId>(sourceConnection).Value;

            ecb.AddComponent<NetworkStreamInGame>(sourceConnection);

            Entity newPlayer = ecb.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().PlayerPrefabEntity);
            Entity newSoulGroup = ecb.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().SoulGroupPrefabEntity);
            ecb.SetName(newPlayer, "Player");
            ecb.SetName(newSoulGroup, "Soul Group");

            ecb.SetComponent(newPlayer, new PlayerSoulGroup { MySoulGroup = newSoulGroup });
            ecb.SetComponent(newSoulGroup, new SoulGroupTarget { MyTarget = newPlayer });

            ecb.AddComponent(newPlayer, new GhostOwner { NetworkId = playerNumber });
            ecb.AddComponent(newSoulGroup, new GhostOwner { NetworkId = playerNumber });
            
            ecb.AppendToBuffer(sourceConnection, new LinkedEntityGroup { Value = newPlayer });



            _connectedPlayers[playerNumber - 1] = newPlayer;
            ecb.SetComponent(newPlayer, new LocalTransform { Position = _spawnPositions[playerNumber - 1], Scale = 1f, Rotation = quaternion.identity });



            ecb.DestroyEntity(recieveRpcEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}