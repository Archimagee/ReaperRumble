using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;



public class SoulAuthoring : MonoBehaviour
{
    public class Baker : Baker<SoulAuthoring>
    {
        public override void Bake(SoulAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new SoulComponent { Speed = 0.07f, SeparationForce = 0.2f});
            AddComponent(entity, new SoulFacingComponent { FacingDirection = new float3(1f, 0f, 0f) });
        }
    }
}



public struct SoulComponent : IComponentData
{
    public Entity MyGroup;
    public float Speed;
    public float SeparationForce;
}

public struct SoulFacingComponent : IComponentData
{
    public float3 FacingDirection;
}