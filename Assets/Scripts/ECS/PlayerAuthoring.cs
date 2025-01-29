using UnityEngine;
using Unity.Entities;
using Unity.NetCode;



public class PlayerAuthoring : MonoBehaviour
{
    [SerializeField] private float _playerSpeed;



    public class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Player { Speed = authoring._playerSpeed });
            AddComponent(entity, new SoulGroup { });
        }
    }
}



public struct Player : IComponentData
{
    public float Speed;
}

[GhostComponent]
public struct SoulGroup : IComponentData
{
    [GhostField] public Entity MySoulGroup;
}