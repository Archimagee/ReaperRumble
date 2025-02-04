using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;



public class ClientPlayerInputAuthoring : MonoBehaviour
{
    public class Baker : Baker<ClientPlayerInputAuthoring>
    {
        public override void Bake(ClientPlayerInputAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent<ClientPlayerInput>(entity);
            AddComponent(entity, new ClientPlayerInputSettings { LookSensitivity = 1.3f });
        }
    }
}



public struct ClientPlayerInput : IInputComponentData
{
    public float2 ClientInput;
    public quaternion ClientPlayerRotation;
    public Quaternion ClientCameraRotation;
}

public struct ClientPlayerInputSettings : IInputComponentData
{
    public float LookSensitivity;
}