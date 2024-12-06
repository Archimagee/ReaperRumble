using UnityEngine;
using Unity.Entities;



public class PlayerEntityHitboxAuthoring : MonoBehaviour
{
    public class Baker : Baker<PlayerEntityHitboxAuthoring>
    {
        public override void Bake(PlayerEntityHitboxAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<PlayerEntityHitboxComponent>(entity);
        }
    }
}



public struct PlayerEntityHitboxComponent : IComponentData
{
    public Entity MyGroup;
}