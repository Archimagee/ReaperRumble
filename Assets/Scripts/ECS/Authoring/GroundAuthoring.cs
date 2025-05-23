using UnityEngine;
using Unity.Entities;



public class GroundAuthoring : MonoBehaviour
{
    public class Baker : Baker<GroundAuthoring>
    {
        public override void Bake(GroundAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<GroundTag>(entity);
        }
    }
}



public struct GroundTag : IComponentData { }