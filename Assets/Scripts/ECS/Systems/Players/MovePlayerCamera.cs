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
        foreach ((RefRO<PlayerCameraFollowTarget> cameraTarget, RefRW<LocalTransform> cameraTransform) in SystemAPI.Query<RefRO<PlayerCameraFollowTarget>, RefRW<LocalTransform>>())
        {
            cameraTransform.ValueRW.Rotation = SystemAPI.GetComponent<ClientPlayerInput>(cameraTarget.ValueRO.Target).ClientCameraRotation;

            float3 newPosition = SystemAPI.GetComponent<LocalTransform>(cameraTarget.ValueRO.Target).Position;

            if (float.IsNaN(newPosition.x) || float.IsNaN(newPosition.y) || float.IsNaN(newPosition.z)) cameraTransform.ValueRW.Position = float3.zero;
            else cameraTransform.ValueRW.Position = newPosition;
        }
    }
}