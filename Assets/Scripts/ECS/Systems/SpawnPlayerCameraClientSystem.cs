using Unity.Collections;
using Unity.Entities;
using Unity.Burst;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class SpawnPlayerCameraClientSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<CameraRequired>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entity playerCameraPrefabEntity = SystemAPI.GetSingleton<EntitySpawnerPrefabs>().PlayerCameraPrefabEntity;



        foreach ((RefRW<CameraRequired> cameraRequired, Entity entity) in SystemAPI.Query<RefRW<CameraRequired>>().WithEntityAccess())
        {
            if (cameraRequired.ValueRW.Complete == false)
            {
                ecb.Instantiate(playerCameraPrefabEntity);
                ecb.AddComponent<PlayerCameraFollowTarget>(entity, new PlayerCameraFollowTarget { Target = entity });
                cameraRequired.ValueRW.Complete = true;
            }
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}