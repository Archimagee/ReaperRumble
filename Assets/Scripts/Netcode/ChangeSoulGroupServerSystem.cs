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

        

        // change this to add to a queue so it doesnt try to change the same soul when multiple rpcs recieved
        foreach ((RefRO<ChangeSoulGroupRequestRPC> changeRequest, RefRO<ReceiveRpcCommandRequest> rpc, Entity rpcEntity) in SystemAPI.Query<RefRO<ChangeSoulGroupRequestRPC>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {


            ecb.DestroyEntity(rpcEntity);
        }



        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}