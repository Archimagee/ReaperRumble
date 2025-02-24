using UnityEngine;
using Unity.Entities;
using Unity.NetCode;



public class SoulGroupAuthoring : MonoBehaviour
{
    public class Baker : Baker<SoulGroupAuthoring>
    {
        public override void Bake(SoulGroupAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SoulGroupTarget { });
            AddComponent(entity, new SoulGroupTag { });
        }
    }
}



public struct SoulBufferElement : IBufferElementData
{
    public Entity Soul;

    public SoulBufferElement(Entity soul)
    {
        Soul = soul;
    }
}

[GhostComponent]
public struct SoulGroupTarget : IComponentData
{
    [GhostField] public Entity MyTarget;
}

public struct SoulGroupTag : IComponentData { }