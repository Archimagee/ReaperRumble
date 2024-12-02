using UnityEngine;
using Unity.Entities;



public class SoulGroupAuthoring : MonoBehaviour
{
    public class Baker : Baker<SoulGroupAuthoring>
    {
        public override void Bake(SoulGroupAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SoulGroupTag { });
            AddBuffer<SoulBufferElement>(entity);
        }
    }
}



public struct SoulGroupTag : IComponentData { }



public struct SoulBufferElement : IBufferElementData
{
    public Entity Soul;

    public SoulBufferElement(Entity soul)
    {
        Soul = soul;
    }
}