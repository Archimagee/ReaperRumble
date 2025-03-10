using UnityEngine;
using Unity.Entities;



public class VFXAuthoring : MonoBehaviour
{
    [SerializeField] protected double _lifetimeSeconds;



    public class Baker : Baker<VFXAuthoring>
    {
        public override void Bake(VFXAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new VFXLifetime() { LifetimeSeconds = authoring._lifetimeSeconds});
        }
    }
}



public struct DestroyVFX : IComponentData
{
    public double DestroyAt;
}
public struct VFXLifetime : IComponentData
{
    public double LifetimeSeconds;
}