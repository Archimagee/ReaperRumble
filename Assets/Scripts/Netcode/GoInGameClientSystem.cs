using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct GoInGameClientSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkId>();
    }



    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRO<NetworkId> networkID, Entity entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<NetworkStreamInGame>().WithEntityAccess())
        {
            ecb.AddComponent<NetworkStreamInGame>(entity);

            Entity rpcEntity = ecb.CreateEntity();
            ecb.AddComponent<GoInGameRequestRPC>(rpcEntity);
            ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
        }



        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}



public struct GoInGameRequestRPC : IRpcCommand { }