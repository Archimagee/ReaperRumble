using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class DestroySoulGroupsClientSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<DestroySoulGroupRequestRPC>();
    }




    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new(Allocator.Temp);



        foreach ((RefRO<DestroySoulGroupRequestRPC> destroyRequest, RefRO<ReceiveRpcCommandRequest> recieveRPC, Entity rpcEntity)
            in SystemAPI.Query<RefRO<DestroySoulGroupRequestRPC>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            BufferLookup<SoulBufferElement> lookup = GetBufferLookup<SoulBufferElement>(true);
            lookup.Update(this);

            foreach ((RefRO<GhostInstance> ghost, Entity entity) in SystemAPI.Query<RefRO<GhostInstance>>().WithEntityAccess())
            {
                if (ghost.ValueRO.ghostId == destroyRequest.ValueRO.GroupToDestroyID)
                {
                    lookup.TryGetBuffer(entity, out DynamicBuffer<SoulBufferElement> buffer);

                    foreach (SoulBufferElement bufferElement in buffer) ecb.DestroyEntity(bufferElement.Soul);
                }
            }

            ecb.DestroyEntity(rpcEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}