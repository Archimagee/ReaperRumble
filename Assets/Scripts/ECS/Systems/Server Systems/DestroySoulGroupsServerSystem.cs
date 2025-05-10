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
        RequireAnyForUpdate(SystemAPI.QueryBuilder().WithAny<DestroySoulGroupRequestRPC, DestroySoulGroup>().Build());
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new(Allocator.Temp);
        double currentTime = SystemAPI.Time.ElapsedTime;



        foreach ((RefRW<DestroySoulGroup> destroy, RefRO<GhostInstance> ghost, Entity entity) in SystemAPI.Query<RefRW<DestroySoulGroup>, RefRO<GhostInstance>>().WithEntityAccess())
        {
            if (currentTime >= destroy.ValueRO.TimeToDestroyAt + 1d && destroy.ValueRO.SoulsDestroyed)
            {
                ecb.DestroyEntity(entity);
            }
            else if (currentTime >= destroy.ValueRO.TimeToDestroyAt && !destroy.ValueRO.SoulsDestroyed)
            {
                Entity newRPC = ecb.CreateEntity();
                ecb.AddComponent(newRPC, new DestroySoulGroupRequestRPC() { GroupToDestroyID = ghost.ValueRO.ghostId });
                ecb.AddComponent<SendRpcCommandRequest>(newRPC);

                destroy.ValueRW.SoulsDestroyed = true;
            }
        }



        foreach ((RefRO<DestroySoulGroupRequestRPC> destroyRequest, RefRO<ReceiveRpcCommandRequest> recieveRPC, Entity rpcEntity)
            in SystemAPI.Query<RefRO<DestroySoulGroupRequestRPC>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            foreach ((RefRO<GhostInstance> ghostInstance, RefRO<SoulGroupTag> soulGroup, Entity soulGroupEntity)
                in SystemAPI.Query<RefRO<GhostInstance>, RefRO<SoulGroupTag>>().WithEntityAccess())
            {
                if (ghostInstance.ValueRO.ghostId == destroyRequest.ValueRO.GroupToDestroyID)
                {
                    if (SystemAPI.GetComponent<SoulGroupTarget>(soulGroupEntity).MyTarget == Entity.Null)
                    {
                        ecb.AddComponent(soulGroupEntity, new DestroySoulGroup() { TimeToDestroyAt = currentTime, SoulsDestroyed = false });
                    }
                }
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

public partial struct DestroySoulGroup : IComponentData
{
    public double TimeToDestroyAt;
    public bool SoulsDestroyed;
}