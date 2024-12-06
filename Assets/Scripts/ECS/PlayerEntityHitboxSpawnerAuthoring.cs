using UnityEngine;
using Unity.Entities;



public class PlayerEntityHitboxSpawnerAuthoring : MonoBehaviour
{
    [SerializeField] private PlayerEntityHitboxAuthoring _playerHitboxPrefab;



    public class Baker : Baker<PlayerEntityHitboxSpawnerAuthoring>
    {
        public override void Bake(PlayerEntityHitboxSpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PlayerEntityHitboxSpawnerComponent
            {
                PlayerHitboxPrefabEntity = GetEntity(authoring._playerHitboxPrefab, TransformUsageFlags.Dynamic),
            });
        }
    }
}



public struct PlayerEntityHitboxSpawnerComponent : IComponentData
{
    public Entity PlayerHitboxPrefabEntity;
}