using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI.ManagedAPI;



[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[RequireMatchingQueriesForUpdate]
[UpdateAfter(typeof(TransformSystemGroup))]
public partial struct PresentationGameObjectSyncSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRO<LocalToWorld> localToWorld, UnityEngineComponent<Transform> transform) in SystemAPI.Query<RefRO<LocalToWorld>, SystemAPI.ManagedAPI.UnityEngineComponent<Transform>>())
        {
            transform.Value.position = localToWorld.ValueRO.Position;
            transform.Value.rotation = localToWorld.ValueRO.Rotation;
        }
    }
}