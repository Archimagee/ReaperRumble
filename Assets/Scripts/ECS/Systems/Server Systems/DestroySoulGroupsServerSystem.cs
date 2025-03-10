using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class DestroySoulGroupsServerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<DestroySoulGroupRequestRPC>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        double currentTime = SystemAPI.Time.ElapsedTime;



        foreach ((RefRO<DestroySoulGroupRequestRPC> destroyRequest, RefRO<ReceiveRpcCommandRequest> recieveRPC, Entity rpcEntity)
            in SystemAPI.Query<RefRO<DestroySoulGroupRequestRPC>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            foreach ((RefRO<GhostInstance> ghostInstance, RefRO<SoulGroupTag> soulGroup, Entity soulGroupEntity)
                in SystemAPI.Query<RefRO<GhostInstance>, RefRO<SoulGroupTag>>().WithEntityAccess())
            {
                if (ghostInstance.ValueRO.ghostId == destroyRequest.ValueRO.GroupToDestroyID) ecb.DestroyEntity(soulGroupEntity);
            }
            ecb.DestroyEntity(rpcEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}



public partial struct DestroySoulGroupRequestRPC : IRpcCommand
{
    public int GroupToDestroyID;
}