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
            AddComponent(entity, new SoulComponent { });
            AddComponent(entity, new SoulFacingComponent { FacingDirection = new float3(0f, 0f, 1f) });
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