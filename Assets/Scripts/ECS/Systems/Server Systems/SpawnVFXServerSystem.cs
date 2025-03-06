using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;
using Unity.Mathematics;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class SpawnVFXServerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<SpawnVFXRequest>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);


        foreach ((RefRO<ReceiveRpcCommandRequest> rpcCommandRequest, RefRO<SpawnVFXRequest> vfxRequest, Entity recieveRpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<SpawnVFXRequest>>().WithEntityAccess())
        {
            Entity sendRpcEntity = EntityManager.CreateEntity();
            ecb.AddComponent(sendRpcEntity, new SpawnVFXRequest { Effect = vfxRequest.ValueRO.Effect, Location = vfxRequest.ValueRO.Location, Rotation = vfxRequest.ValueRO.Rotation });
            ecb.AddComponent<SendRpcCommandRequest>(sendRpcEntity);
            ecb.DestroyEntity(recieveRpcEntity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

public struct SpawnVFXRequest : IRpcCommand
{
    public VisualEffect Effect;
    public float3 Location;
    public quaternion Rotation;
}