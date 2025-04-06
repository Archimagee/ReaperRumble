using UnityEngine;
using Unity.Entities;



public class SoulDepositAuthoring : MonoBehaviour
{
    public class Baker : Baker<SoulDepositAuthoring>
    {
        public override void Bake(SoulDepositAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<SoulDepositTag>(entity);
        }
    }
}



public partial struct SoulDepositTag : IComponentData { }