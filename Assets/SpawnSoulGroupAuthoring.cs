using UnityEngine;
using Unity.Entities;
using Unity.NetCode;



public class SpawnSoulGroupAuthoring : MonoBehaviour
{
    [SerializeField] private int _initialSoulAmount;

    public class Baker : Baker<SpawnSoulGroupAuthoring>
    {
        public override void Bake(SpawnSoulGroupAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SoulGroupInitialise { SoulAmount = authoring._initialSoulAmount });
        }
    }
}

public struct SoulGroupInitialise : IComponentData
{
    [GhostField] public int SoulAmount;
}