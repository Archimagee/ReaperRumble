using UnityEngine;
using Unity.Entities;



public class PlayerColliderAuthoring : MonoBehaviour
{



    public class Baker : Baker<PlayerColliderAuthoring>
    {
        public override void Bake(PlayerColliderAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        }
    }
}



public struct PlayerCollider : IComponentData
{
    public Entity FollowTarget;
}