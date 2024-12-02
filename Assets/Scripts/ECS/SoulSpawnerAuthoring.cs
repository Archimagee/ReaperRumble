using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;



public class SoulSpawnerAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject _soulPrefab;
    [SerializeField] private GameObject _soulGroupPrefab;
    [SerializeField] private Vector3 _spawnPosition;
    [SerializeField] private float _spawnPositionRandomisation;



    public class Baker : Baker<SoulSpawnerAuthoring>
    {
        public override void Bake(SoulSpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new SoulSpawnerComponent {
                SoulPrefabEntity = GetEntity(authoring._soulPrefab, TransformUsageFlags.Renderable),
                SoulGroupPrefabEntity = GetEntity(authoring._soulGroupPrefab, TransformUsageFlags.Dynamic),
                SpawnPosition = authoring._spawnPosition,
                SpawnPositionRandomisation = authoring._spawnPositionRandomisation
            });
        }
    }
}



public struct SoulSpawnerComponent : IComponentData
{
    public Entity SoulPrefabEntity;
    public Entity SoulGroupPrefabEntity;
    public float3 SpawnPosition;
    public float SpawnPositionRandomisation;
}