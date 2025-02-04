using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class MovePlayerCamera : SystemBase
{
    private float3 _cameraRotation = float3.zero;



    protected override void OnCreate()
    {
        RequireForUpdate<PlayerCameraFollowTarget>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        foreach ((RefRO<PlayerCameraFollowTarget> cameraTarget, RefRW<LocalTransform> cameraTransform) in SystemAPI.Query<RefRO<PlayerCameraFollowTarget>, RefRW<LocalTransform>>())
        {
            quaternion cameraRotation = SystemAPI.GetComponent<ClientPlayerInput>(cameraTarget.ValueRO.Target).ClientCameraRotation;
            float3 playerPos = SystemAPI.GetComponent<LocalTransform>(cameraTarget.ValueRO.Target).Position;
            cameraTransform.ValueRW.Rotation = cameraRotation;
            cameraTransform.ValueRW.Position = playerPos;
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}