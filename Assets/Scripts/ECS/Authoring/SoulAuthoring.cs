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
            AddComponent<Soul>(entity);
            AddComponent(entity, new SoulFacingDirection { FacingDirection = new float3(0f, 0f, 1f) });
        }
    }
}



public struct Soul : IComponentData
{
    public Entity MyGroup;
    public float Speed;
    public float SeparationForce;
}

public struct SoulFacingDirection : IComponentData
{
    public float3 FacingDirection;
}

public struct ChangeSoulGroup : IComponentData
{
    public Entity SoulToMove;
    public Entity SoulGroupToMoveTo;
}