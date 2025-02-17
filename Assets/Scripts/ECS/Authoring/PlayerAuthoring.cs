using UnityEngine;
using Unity.Entities;
using Unity.NetCode;



public class PlayerAuthoring : MonoBehaviour
{
    [SerializeField] private float _playerSpeed;
    [SerializeField] private float _playerJumpSpeed;
    //[SerializeField] private PhysicsCategoryTags _belongsTo;
    //[SerializeField] private PhysicsCategoryTags _collidesWith;
    //private CollisionFilter _collisionFilter => new()
    //{
    //    BelongsTo = _belongsTo.Value,
    //    CollidesWith = _collidesWith.Value
    //};



    public class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Player { Speed = authoring._playerSpeed, JumpSpeed = authoring._playerJumpSpeed });
            AddComponent<PlayerSoulGroup>(entity);
            AddComponent<CameraRequired>(entity);
            AddComponent<FreezeRotationTag>(entity);
            AddComponent(entity, new IsPlayerGrounded { IsGrounded = true });
        }
    }
}



public struct Player : IComponentData
{
    public float Speed;
    public float JumpSpeed;
}

[GhostComponent]
public struct PlayerSoulGroup : IComponentData
{
    [GhostField] public Entity MySoulGroup;
}

public struct IsPlayerGrounded : IComponentData
{
    public bool IsGrounded;
}

public struct CameraRequired : IComponentData { }
public struct FreezeRotationTag : IComponentData { }