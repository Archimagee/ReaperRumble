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
            AddComponent<SoulGroup>(entity);
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
    public Entity MySoulGroup;
}