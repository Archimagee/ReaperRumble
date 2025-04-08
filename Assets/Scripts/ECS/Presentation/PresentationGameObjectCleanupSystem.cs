using Unity.Entities;
using UnityEngine;



[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct PresentationGameObjectCleanupSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecbbi = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        foreach ((PresentationGameObjectCleanup cleanup, Entity entity)in
            SystemAPI.Query<PresentationGameObjectCleanup>()
            .WithNone<PresentationGameObject>().WithEntityAccess())
        {
            if (cleanup.Instance == null) continue;

            PresentationGameObjectDestructionManager destructionManager;
            cleanup.Instance.TryGetComponent<PresentationGameObjectDestructionManager>(out destructionManager);

            if (destructionManager != null) destructionManager.Destroy(cleanup.Instance);
            else Object.Destroy(cleanup.Instance.gameObject);

            ecbbi.RemoveComponent<PresentationGameObjectCleanup>(entity);
        }
    }
}