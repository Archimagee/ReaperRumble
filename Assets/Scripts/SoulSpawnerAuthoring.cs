using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;



public class SoulSpawnerAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject _soulPrefab;
    [SerializeField] private int _spawnAmount;
    [SerializeField] private Vector3 _spawnPosition;
    [SerializeField] private float _spawnPositionRandomisation;



    public class Baker : Baker<SoulSpawnerAuthoring>
    {
        public override void Bake(SoulSpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new SoulSpawner {
                SoulPrefabEntity = GetEntity(authoring._soulPrefab, TransformUsageFlags.Dynamic),
                SpawnAmount = authoring._spawnAmount,
                SpawnPosition = authoring._spawnPosition,
                SpawnPositionRandomisation = authoring._spawnPositionRandomisation
            });
        }
    }
}



public struct SoulSpawner : IComponentData
{
    public Entity SoulPrefabEntity;
    public int SpawnAmount;
    public float3 SpawnPosition;
    public float SpawnPositionRandomisation;
}