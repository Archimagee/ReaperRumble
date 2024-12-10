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
        }
    }
}



public struct ClientPlayerInput : IInputComponentData
{
    public float2 ClientInput;
    public Quaternion ClientCameraRotation;
}