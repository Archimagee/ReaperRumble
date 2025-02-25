using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ChangeSoulGroupServerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        

        foreach ((RefRO<ChangeSoulGroupRequestRPC> changeRequest, RefRO<ReceiveRpcCommandRequest> rpc, Entity rpcEntity) in SystemAPI.Query<RefRO<ChangeSoulGroupRequestRPC>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            Entity source = rpc.ValueRO.SourceConnection;

            foreach ((RefRO<NetworkStreamConnection> otherSource, Entity entity) in SystemAPI.Query<RefRO<NetworkStreamConnection>>().WithEntityAccess())
            {
                Entity newRpcEntity = ecb.CreateEntity();
                ecb.AddComponent(newRpcEntity, new ChangeSoulGroupRequestRPC
                {
                    GroupIDFrom = changeRequest.ValueRO.GroupIDFrom,
                    GroupIDTo = changeRequest.ValueRO.GroupIDTo,
                    Amount = 1
                });
                ecb.AddComponent(rpcEntity, new SendRpcCommandRequest() { TargetConnection = entity });
            }



            ecb.DestroyEntity(rpcEntity);
        }



        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}