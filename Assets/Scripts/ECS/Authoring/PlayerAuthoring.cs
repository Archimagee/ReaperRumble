using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Authoring;



public class PlayerAuthoring : MonoBehaviour
{
    [SerializeField] private float _playerSpeed;
    [SerializeField] private PhysicsCategoryTags _belongsTo;
    [SerializeField] private PhysicsCategoryTags _collidesWith;
    private CollisionFilter _collisionFilter => new()
    {
        BelongsTo = _belongsTo.Value,
        CollidesWith = _collidesWith.Value
    };



    public class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Player { Speed = authoring._playerSpeed, CollisionFilter = authoring._collisionFilter });
            AddComponent<PlayerSoulGroup>(entity);
            AddComponent<CameraRequired>(entity);
        }
    }
}



public struct Player : IComponentData
{
    public float Speed;
    public CollisionFilter CollisionFilter;
}

[GhostComponent]
public struct PlayerSoulGroup : IComponentData
{
    [GhostField] public Entity MySoulGroup;
}

public struct CameraRequired : IComponentData { }