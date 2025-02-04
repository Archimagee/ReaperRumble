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
            AddComponent<PlayerSoulGroup>(entity);
            AddComponent(entity, new CameraRequired { Complete = false });
        }
    }
}



public struct Player : IComponentData
{
    public float Speed;
}

[GhostComponent]
public struct PlayerSoulGroup : IComponentData
{
    [GhostField] public Entity MySoulGroup;
}

public struct CameraRequired : IComponentData
{
    public bool Complete;
}