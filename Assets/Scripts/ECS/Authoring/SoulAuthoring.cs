using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;



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
            AddComponent(entity, new SoulGroupMember() { MyGroup = Entity.Null });
            AddComponent<SoulGroupWasChanged>(entity);
            AddComponent<SoulColorChanged>(entity);
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
    [GhostField] public Entity MyGroup;
}

public struct SoulFacingDirection : IComponentData
{
    public float3 FacingDirection;
}

public struct SoulGroupWasChanged : IComponentData
{
    [GhostField] public double WasChangedAt;
}