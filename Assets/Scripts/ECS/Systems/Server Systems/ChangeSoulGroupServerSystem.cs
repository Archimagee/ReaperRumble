using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ChangeSoulGroupServerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ChangeSoulGroupRequestRPC>();
        state.RequireForUpdate<ReceiveRpcCommandRequest>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        

        foreach ((RefRO<ChangeSoulGroupRequestRPC> changeRequest, RefRO<ReceiveRpcCommandRequest> rpc, Entity rpcEntity) in SystemAPI.Query<RefRO<ChangeSoulGroupRequestRPC>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            Entity source = rpc.ValueRO.SourceConnection;

            foreach ((RefRO<NetworkId> networkId, Entity otherSource) in SystemAPI.Query<RefRO<NetworkId>>().WithEntityAccess())
            {
                if (otherSource != source)
                {
                    Entity newRpcEntity = ecb.CreateEntity();
                    ecb.AddComponent(newRpcEntity, new ChangeSoulGroupRequestRPC()
                    {
                        GroupIDFrom = changeRequest.ValueRO.GroupIDFrom,
                        GroupIDTo = changeRequest.ValueRO.GroupIDTo,
                        Amount = changeRequest.ValueRO.Amount
                    });
                    ecb.AddComponent(newRpcEntity, new SendRpcCommandRequest() { TargetConnection = otherSource });
                }
            }



            ecb.DestroyEntity(rpcEntity);
        }



        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}