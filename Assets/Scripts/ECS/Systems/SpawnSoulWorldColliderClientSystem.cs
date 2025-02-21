using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class SpawnSoulWorldColliderClientSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<SoulWorldColliderRequired>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entity colliderPrefabEntity = SystemAPI.GetSingleton<EntitySpawnerPrefabs>().PlayerColliderPrefabEntity;

        foreach ((RefRO<SoulWorldColliderRequired> colliderRequired, Entity player) in SystemAPI.Query<RefRO<SoulWorldColliderRequired>>().WithEntityAccess().WithAll<GhostOwnerIsLocal>())
        {
            ecb.RemoveComponent<SoulWorldColliderRequired>(player);

            Entity newColliderEntity = ecb.Instantiate(colliderPrefabEntity);

            ecb.SetName(colliderPrefabEntity, "Player Collider");
            ecb.AddComponent(player, new SoulWorldCollider { ColliderEntity = newColliderEntity });
            ecb.AddComponent(newColliderEntity, new PlayerCollider { FollowTarget = player });
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}