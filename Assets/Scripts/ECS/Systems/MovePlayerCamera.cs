using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class MovePlayerCamera : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<PlayerCameraFollowTarget>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        foreach ((RefRO<PlayerCameraFollowTarget> cameraTarget, Entity cameraEntity) in SystemAPI.Query<RefRO<PlayerCameraFollowTarget>>().WithEntityAccess())
        {
            float3 targetPosition = SystemAPI.GetComponent<LocalTransform>(cameraTarget.ValueRO.Target).Position;
            ecb.SetComponent<LocalTransform>(cameraEntity, new LocalTransform { Position = targetPosition, Rotation = quaternion.identity, Scale = 1f });
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}