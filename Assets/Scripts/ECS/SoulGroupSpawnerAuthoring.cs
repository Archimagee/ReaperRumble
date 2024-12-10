using UnityEngine;
using Unity.Entities;



public class SoulGroupSpawnerAuthoring : MonoBehaviour
{
    [SerializeField] private SoulGroupAuthoring _soulGroupPrefab;



    public class Baker : Baker<SoulGroupSpawnerAuthoring>
    {
        public override void Bake(SoulGroupSpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new SoulGroupSpawner
            {
                SoulGroupPrefab = GetEntity(authoring._soulGroupPrefab, TransformUsageFlags.Dynamic),
            });
        }
    }
}



public struct SoulGroupSpawner : IComponentData
{
    public Entity SoulGroupPrefab;
}