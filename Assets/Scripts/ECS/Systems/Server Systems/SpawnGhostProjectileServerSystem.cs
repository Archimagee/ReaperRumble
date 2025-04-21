using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class SpawnGhostProjectileServerSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<SpawnGhostProjectileCommandRequest>();
        RequireForUpdate<ReceiveRpcCommandRequest>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        foreach ((RefRO<ReceiveRpcCommandRequest> recieveRPC, RefRO<SpawnGhostProjectileCommandRequest> spawnRequest, Entity rpcEntity) in
            SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<SpawnGhostProjectileCommandRequest>>().WithEntityAccess())
        {
            Entity newProjectile = ecb.Instantiate(GetProjectilePrefab(spawnRequest.ValueRO.Ability));
            ecb.SetComponent(newProjectile, new PhysicsVelocity { Linear = spawnRequest.ValueRO.VelocityLinear, Angular = spawnRequest.ValueRO.VelocityAngular });
            ecb.SetComponent(newProjectile, new LocalTransform { Position = spawnRequest.ValueRO.Position, Scale = spawnRequest.ValueRO.Scale, Rotation = quaternion.identity });

            ecb.DestroyEntity(rpcEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    private Entity GetProjectilePrefab(PlayerAbility ability)
    {
        if (ability == PlayerAbility.PoisonVial) return SystemAPI.GetSingleton<AbilityPrefabs>().PoisonVialPrefabEntity;
        else return Entity.Null;
    }
}

public struct SpawnGhostProjectileCommandRequest : IRpcCommand
{
    public PlayerAbility Ability;
    public float3 VelocityLinear;
    public float3 VelocityAngular;
    public float3 Position;
    public float Scale;
}