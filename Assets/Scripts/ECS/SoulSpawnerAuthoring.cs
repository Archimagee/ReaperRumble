using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;



public class SoulSpawnerAuthoring : MonoBehaviour
{
    [SerializeField] private SoulAuthoring _soulPrefab;
    [SerializeField] private Vector3 _spawnPosition;
    [SerializeField] private float _spawnPositionRandomisation;
    [SerializeField] private float _separationForce;
    [SerializeField] private float _speed;



    public class Baker : Baker<SoulSpawnerAuthoring>
    {
        public override void Bake(SoulSpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new SoulSpawner {
                SoulPrefabEntity = GetEntity(authoring._soulPrefab, TransformUsageFlags.Renderable),
            });
        }
    }
}



public struct SoulSpawner : IComponentData
{
    public Entity SoulPrefabEntity;
}