using UnityEngine;
using Unity.Entities;
using Unity.NetCode;



public class PlayerAuthoring : MonoBehaviour
{
    [SerializeField] private float _playerSpeed;
    [SerializeField] private float _playerJumpSpeed;
    [SerializeField] private float _playerAttackCooldownSeconds;



    public class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Player { Speed = authoring._playerSpeed, JumpSpeed = authoring._playerJumpSpeed, AttackCooldownSeconds = authoring._playerAttackCooldownSeconds });
            AddComponent<FreezeRotationTag>(entity);
            AddComponent(entity, new IsPlayerGrounded { IsGrounded = true });
            AddComponent<Knockback>(entity);
            AddComponent<PlayerSetupRequired>(entity);
            AddComponent<PlayerSoulGroup>(entity);
        }
    }
}



public struct Player : IComponentData
{
    public float Speed;
    public float JumpSpeed;
    public float AttackCooldownSeconds;
}

public struct PlayerSoulGroup : IComponentData
{
    [GhostField] public Entity MySoulGroup;
}

public struct IsPlayerGrounded : IComponentData
{
    public bool IsGrounded;
}

public struct SoulWorldCollider : IComponentData
{
    public Entity ColliderEntity;
}

public struct ColliderRequiredTag : IComponentData { }
public struct FreezeRotationTag : IComponentData { }