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

    [Header("Disasters")]
    [SerializeField] private DisasterAuthoring _lightningStormDisasterPrefab;
    [SerializeField] private DisasterAuthoring _meteorShowerDisasterPrefab;
    [SerializeField] private GameObject _meteorPrefab;

    [Header("VFX")]
    [SerializeField] private VFXAuthoring _scytheSlashVFX;
    [SerializeField] private VFXAuthoring _lightningStrikeIncomingVFX;
    [SerializeField] private VFXAuthoring _lightningStrikeVFX;
    [SerializeField] private VFXAuthoring _meteorImpactVFX;
    [SerializeField] private VFXAuthoring _sixShooterTracerVFX;



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
                ScytheSlashVFXPrefabEntity = GetEntity(authoring._scytheSlashVFX, TransformUsageFlags.Dynamic),
                LightningStrikeIncomingVFXPrefabEntity = GetEntity(authoring._lightningStrikeIncomingVFX, TransformUsageFlags.Dynamic),
                LightningStrikeVFXPrefabEntity = GetEntity(authoring._lightningStrikeVFX, TransformUsageFlags.Dynamic),
                MeteorImpactVFXPrefabEntity = GetEntity(authoring._meteorImpactVFX, TransformUsageFlags.Dynamic),
                SixShooterTracerVFXPrefabEntity = GetEntity(authoring._sixShooterTracerVFX, TransformUsageFlags.Dynamic)
            });

            AddComponent(entity, new DisasterPrefabs
            {
                LightningStormDisasterPrefabEntity = GetEntity(authoring._lightningStormDisasterPrefab, TransformUsageFlags.None),
                MeteorShowerDisasterPrefabEntity = GetEntity(authoring._meteorShowerDisasterPrefab, TransformUsageFlags.None),
                MeteorPrefabEntity = GetEntity(authoring._meteorPrefab, TransformUsageFlags.Dynamic)
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

    public Entity LightningStrikeIncomingVFXPrefabEntity;
    public Entity LightningStrikeVFXPrefabEntity;

    public Entity MeteorImpactVFXPrefabEntity;

    public Entity SixShooterTracerVFXPrefabEntity;
}
public struct DisasterPrefabs : IComponentData
{
    public Entity LightningStormDisasterPrefabEntity;
    public Entity MeteorShowerDisasterPrefabEntity;
    public Entity MeteorPrefabEntity;
}