using UnityEngine;
using Unity.Entities;



public class EntitySpawnerAuthoring : MonoBehaviour
{
    [Header("Entities")]
    [SerializeField] private PlayerAuthoring _playerPrefab;
    [SerializeField] private PlayerColliderAuthoring _playerColliderPrefab;
    [SerializeField] private PlayerCameraAuthoring _playerCameraPrefab;
    [SerializeField] private SoulGroupAuthoring _soulGroupPrefab;
    [SerializeField] private SoulAuthoring _soulPrefab;

    [Header("Challenges")]
    [SerializeField] private ChallengeAuthoring _parkourChallengePrefab;

    [Header("VFX")]
    [SerializeField] private VFXAuthoring _scytheSlashVFX;



    public class Baker : Baker<EntitySpawnerAuthoring>
    {
        public override void Bake(EntitySpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new EntitySpawnerPrefabs
            {
                PlayerPrefabEntity = GetEntity(authoring._playerPrefab, TransformUsageFlags.Renderable),
                PlayerColliderPrefabEntity = GetEntity(authoring._playerColliderPrefab, TransformUsageFlags.Dynamic),
                PlayerCameraPrefabEntity = GetEntity(authoring._playerCameraPrefab, TransformUsageFlags.Renderable),
                SoulGroupPrefabEntity = GetEntity(authoring._soulGroupPrefab, TransformUsageFlags.Dynamic),
                SoulPrefabEntity = GetEntity(authoring._soulPrefab, TransformUsageFlags.Renderable),
                ParkourChallengePrefabEntity = GetEntity(authoring._parkourChallengePrefab, TransformUsageFlags.Dynamic),
            });

            AddComponent(entity, new VFXPrefabs
            {
                ScytheSlashVFXPrefabEntity = GetEntity(authoring._scytheSlashVFX, TransformUsageFlags.Dynamic)
            });
        }
    }
}



public struct EntitySpawnerPrefabs : IComponentData
{
    public Entity PlayerPrefabEntity;
    public Entity PlayerColliderPrefabEntity;
    public Entity PlayerCameraPrefabEntity;
    public Entity SoulGroupPrefabEntity;
    public Entity SoulPrefabEntity;

    public Entity ParkourChallengePrefabEntity;
}

public struct VFXPrefabs : IComponentData
{
    public Entity ScytheSlashVFXPrefabEntity;
}