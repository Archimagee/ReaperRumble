using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using UnityEngine;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class SpawnPlayerCameraClientSystem : SystemBase
{
    private GameObject _cameraObject;



    protected override void OnCreate()
    {
        RequireForUpdate<CameraRequired>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entity playerCameraPrefabEntity = SystemAPI.GetSingleton<EntitySpawnerPrefabs>().PlayerCameraPrefabEntity;

        foreach ((RefRW<CameraRequired> cameraRequired, Entity entity) in SystemAPI.Query<RefRW<CameraRequired>>().WithEntityAccess().WithAll<GhostOwnerIsLocal>())
        {
            Entity newCameraEntity = ecb.Instantiate(playerCameraPrefabEntity);
            ecb.SetName(newCameraEntity, "Player Camera");
            ecb.AddComponent<PlayerCameraFollowTarget>(newCameraEntity, new PlayerCameraFollowTarget { Target = entity });
            ecb.RemoveComponent<CameraRequired>(entity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}