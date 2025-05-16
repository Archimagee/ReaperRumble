using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;



[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct PresentationGameObjectSpawnSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (Entity entity in SystemAPI.QueryBuilder().WithAll<LocalToWorld>()
            .WithAny<PresentationGameObjectPrefab, LocalPresentationGameObjectPrefab>()
            .WithNone<PresentationGameObjectCleanup>()
            .Build().ToEntityArray(Allocator.Temp))
        {
            GameObject newGameObject;

            if (SystemAPI.HasComponent<GhostOwnerIsLocal>(entity) && SystemAPI.ManagedAPI.HasComponent<LocalPresentationGameObjectPrefab>(entity) && SystemAPI.IsComponentEnabled<GhostOwnerIsLocal>(entity))
            {
                LocalPresentationGameObjectPrefab gameObjectPrefab = SystemAPI.ManagedAPI.GetComponent<LocalPresentationGameObjectPrefab>(entity);
                newGameObject = Object.Instantiate(gameObjectPrefab.Prefab);
            }
            else
            {
                PresentationGameObjectPrefab gameObjectPrefab = SystemAPI.ManagedAPI.GetComponent<PresentationGameObjectPrefab>(entity);
                newGameObject = Object.Instantiate(gameObjectPrefab.Prefab);
            }



            foreach (Component component in newGameObject.GetComponents(typeof(Component)))
            {
                if (component != null) state.EntityManager.AddComponentObject(entity, component);
            }

            newGameObject.AddComponent<PresentationGameObject>().Assign(entity, state.World);
            state.EntityManager.AddComponentData(entity, new PresentationGameObjectCleanup() { Instance = newGameObject });

            LocalToWorld localToWorld = SystemAPI.GetComponent<LocalToWorld>(entity);
            newGameObject.transform.position = localToWorld.Position;
            newGameObject.transform.rotation = localToWorld.Rotation;
        }
    }
}