using UnityEngine;
using Unity.Entities;



public class SoulAuthoring : MonoBehaviour
{
    [SerializeField] private float _speed;



    public class Baker : Baker<SoulAuthoring>
    {
        public override void Bake(SoulAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SoulTag { });
            AddComponent(entity, new MoveForwardsTag { });
            AddComponent(entity, new MoveSpeedComponent { Speed = authoring._speed });
            AddComponent(entity, new SoulBoidComponent { });
        }
    }
}



public struct SoulTag : IComponentData { }