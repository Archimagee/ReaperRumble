using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;



public class ClientPlayerInputAuthoring : MonoBehaviour
{
    public class Baker : Baker<ClientPlayerInputAuthoring>
    {
        public override void Bake(ClientPlayerInputAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PlayerInput { ClientInput = float2.zero, IsJumping = false,  ClientPlayerRotationEuler = float3.zero, ClientCameraRotation = quaternion.identity, ClientCameraRotationEuler = float3.zero });
            AddComponent(entity, new PlayerInputSettings { LookSensitivity = 0.035f });
        }
    }
}