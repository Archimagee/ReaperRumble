using UnityEngine;
using Unity.Entities;



public class PoisonVialAuthoring : MonoBehaviour
{
    public class Baker : Baker<PoisonVialAuthoring>
    {
        public override void Bake(PoisonVialAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PoisonVialTag());
        }
    }
}



public partial struct PoisonVialTag : IComponentData { }