using UnityEngine;
using Unity.Entities;



public class PlayerCameraAuthoring : MonoBehaviour
{
    public class Baker : Baker<PlayerCameraAuthoring>
    {
        public override void Bake(PlayerCameraAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<PlayerCameraTag>(entity);
        }
    }
}



public struct PlayerCameraTag : IComponentData { }

public struct PlayerCameraFollowTarget : IComponentData
{
    public Entity Target;
}