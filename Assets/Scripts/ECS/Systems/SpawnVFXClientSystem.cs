using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;
using Unity.Transforms;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class SpawnVFXClientSystem : SystemBase
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
            Entity newVFX = ecb.Instantiate(GetVFXPrefab(vfxRequest.ValueRO.Effect));
            ecb.SetComponent(newVFX, new LocalTransform() { Position = vfxRequest.ValueRO.Location, Rotation = vfxRequest.ValueRO.Rotation, Scale = 1f });
            ecb.DestroyEntity(recieveRpcEntity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    private Entity GetVFXPrefab(VisualEffect effect)
    {
        if (effect == VisualEffect.ScytheSlash) return SystemAPI.GetSingleton<VFXPrefabs>().ScytheSlashVFXPrefabEntity;
        else return Entity.Null;
    }
}

public enum VisualEffect
{
    ScytheSlash
}