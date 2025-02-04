using UnityEngine;
using Unity.Entities;



public class EntitySpawnerAuthoring : MonoBehaviour
{
    [SerializeField] private PlayerAuthoring _playerPrefab;
    [SerializeField] private PlayerCameraAuthoring _playerCameraPrefab;
    [SerializeField] private SoulGroupAuthoring _soulGroupPrefab;
    [SerializeField] private SoulAuthoring _soulPrefab;



    public class Baker : Baker<EntitySpawnerAuthoring>
    {
        public override void Bake(EntitySpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new EntitySpawnerPrefabs
            {
                PlayerPrefabEntity = GetEntity(authoring._playerPrefab, TransformUsageFlags.Renderable),
                PlayerCameraPrefabEntity = GetEntity(authoring._playerCameraPrefab, TransformUsageFlags.Renderable),
                SoulGroupPrefabEntity = GetEntity(authoring._soulGroupPrefab, TransformUsageFlags.Dynamic),
                SoulPrefabEntity = GetEntity(authoring._soulPrefab, TransformUsageFlags.Renderable)
            });
        }
    }
}



public struct EntitySpawnerPrefabs : IComponentData
{
    public Entity PlayerPrefabEntity;
    public Entity PlayerCameraPrefabEntity;
    public Entity SoulGroupPrefabEntity;
    public Entity SoulPrefabEntity;
}