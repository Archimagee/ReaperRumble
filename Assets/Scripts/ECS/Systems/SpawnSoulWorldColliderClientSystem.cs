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
        RequireForUpdate<Player>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entity colliderPrefabEntity = SystemAPI.GetSingleton<EntitySpawnerPrefabs>().PlayerColliderPrefabEntity;

        foreach ((RefRO<Player> colliderRequired, Entity player) in SystemAPI.Query<RefRO<Player>>().WithEntityAccess().WithNone<SoulWorldCollider>().WithAll<GhostOwnerIsLocal>())
        {
            //ecb.RemoveComponent<ColliderRequiredTag>(player);

            Entity newColliderEntity = ecb.Instantiate(colliderPrefabEntity);

            ecb.SetName(colliderPrefabEntity, "Player Collider");
            ecb.AddComponent(player, new SoulWorldCollider { ColliderEntity = newColliderEntity });
            ecb.AddComponent(newColliderEntity, new PlayerCollider { FollowTarget = player });

            this.Enabled = false;
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}