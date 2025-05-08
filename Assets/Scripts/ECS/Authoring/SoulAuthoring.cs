using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;



public class SoulAuthoring : MonoBehaviour
{
    [SerializeField] private float _soulSpeed;
    [SerializeField] private float _soulSeparationForce;



    public class Baker : Baker<SoulAuthoring>
    {
        public override void Bake(SoulAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new Soul() { Speed = authoring._soulSpeed, SeparationForce = authoring._soulSeparationForce });
            AddComponent(entity, new SoulFacingDirection() { FacingDirection = new float3(0f, 0f, 1f) });
        }
    }
}



public struct Soul : IComponentData
{
    public float Speed;
    public float SeparationForce;
}
public struct SoulGroupMember : IComponentData
{
    public Entity MyGroup;
}

public struct SoulFacingDirection : IComponentData
{
    public float3 FacingDirection;
}

public struct ChangeSoulGroup : IComponentData
{
    public Entity SoulGroupToMoveTo;
}