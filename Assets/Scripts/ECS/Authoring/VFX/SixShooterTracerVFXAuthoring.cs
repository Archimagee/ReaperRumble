using UnityEngine;
using Unity.Entities;



public class SixShooterTracerAuthoring : VFXAuthoring
{
    [SerializeField] private LineRenderer _lineRenderer;



    public new class Baker : Baker<SixShooterTracerAuthoring>
    {
        public override void Bake(SixShooterTracerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new VFXLifetime() { LifetimeSeconds = authoring._lifetimeSeconds });
            AddComponentObject(entity, authoring._lineRenderer);
        }
    }
}