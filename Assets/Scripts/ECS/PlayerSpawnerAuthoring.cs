using UnityEngine;
using Unity.Entities;



public class PlayerSpawnerAuthoring : MonoBehaviour
{
    [SerializeField] private PlayerAuthoring _playerPrefab;



    public class Baker : Baker<PlayerSpawnerAuthoring>
    {
        public override void Bake(PlayerSpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PlayerSpawner
            {
                PlayerPrefabEntity = GetEntity(authoring._playerPrefab, TransformUsageFlags.Dynamic),
            });
        }
    }
}



public struct PlayerSpawner : IComponentData
{
    public Entity PlayerPrefabEntity;
}