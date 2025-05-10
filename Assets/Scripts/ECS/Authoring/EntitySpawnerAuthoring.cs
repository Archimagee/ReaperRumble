using UnityEngine;
using Unity.Entities;



public class EntitySpawnerAuthoring : MonoBehaviour
{
    [Header("Entities")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameObject _playerColliderPrefab;
    [SerializeField] private GameObject _playerCameraPrefab;
    [SerializeField] private GameObject _soulGroupPrefab;
    [SerializeField] private GameObject _soulPrefab;

    [Header("Challenges")]
    [SerializeField] private GameObject _parkourChallengePrefab;

    [Header("Disasters")]
    [SerializeField] private GameObject _lightningStormDisasterPrefab;
    [SerializeField] private GameObject _meteorShowerDisasterPrefab;
    [SerializeField] private GameObject _meteorPrefab;
    [SerializeField] private GameObject _lavaFloodDisasterPrefab;
    [SerializeField] private GameObject _tornadoDisasterPrefab;

    [Header("Abilities")]
    [SerializeField] private GameObject _poisonVialAbilityPrefab;
    [SerializeField] private GameObject _poisonFieldPrefab;

    [Header("VFX")]
    [SerializeField] private GameObject _scytheSlashVFX;
    [SerializeField] private GameObject _lightningStrikeIncomingVFX;
    [SerializeField] private GameObject _lightningStrikeVFX;
    [SerializeField] private GameObject _meteorImpactVFX;
    [SerializeField] private GameObject _HitVFX;
    [SerializeField] private GameObject _sixShooterTracerVFX;



    public class Baker : Baker<EntitySpawnerAuthoring>
    {
        public override void Bake(EntitySpawnerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new EntitySpawnerPrefabs
            {
                PlayerPrefabEntity = GetEntity(authoring._playerPrefab, TransformUsageFlags.Dynamic),
                PlayerColliderPrefabEntity = GetEntity(authoring._playerColliderPrefab, TransformUsageFlags.Dynamic),
                PlayerCameraPrefabEntity = GetEntity(authoring._playerCameraPrefab, TransformUsageFlags.Dynamic),
                SoulGroupPrefabEntity = GetEntity(authoring._soulGroupPrefab, TransformUsageFlags.Dynamic),
                SoulPrefabEntity = GetEntity(authoring._soulPrefab, TransformUsageFlags.Dynamic),
                ParkourChallengePrefabEntity = GetEntity(authoring._parkourChallengePrefab, TransformUsageFlags.Dynamic),
            });

            AddComponent(entity, new VFXPrefabs
            {
                ScytheSlashVFXPrefabEntity = GetEntity(authoring._scytheSlashVFX, TransformUsageFlags.Dynamic),
                LightningStrikeIncomingVFXPrefabEntity = GetEntity(authoring._lightningStrikeIncomingVFX, TransformUsageFlags.Dynamic),
                LightningStrikeVFXPrefabEntity = GetEntity(authoring._lightningStrikeVFX, TransformUsageFlags.Dynamic),
                MeteorImpactVFXPrefabEntity = GetEntity(authoring._meteorImpactVFX, TransformUsageFlags.Dynamic),
                HitVFXPrefabEntity = GetEntity(authoring._HitVFX, TransformUsageFlags.Dynamic),
                SixShooterTracerVFXPrefabEntity = GetEntity(authoring._sixShooterTracerVFX, TransformUsageFlags.Dynamic)
            });

            AddComponent(entity, new AbilityPrefabs
            {
                PoisonVialPrefabEntity = GetEntity(authoring._poisonVialAbilityPrefab, TransformUsageFlags.Dynamic),
                PoisonFieldPrefabEntity = GetEntity(authoring._poisonFieldPrefab, TransformUsageFlags.Dynamic)
            });

            AddComponent(entity, new DisasterPrefabs
            {
                LightningStormDisasterPrefabEntity = GetEntity(authoring._lightningStormDisasterPrefab, TransformUsageFlags.None),
                MeteorShowerDisasterPrefabEntity = GetEntity(authoring._meteorShowerDisasterPrefab, TransformUsageFlags.None),
                MeteorPrefabEntity = GetEntity(authoring._meteorPrefab, TransformUsageFlags.Dynamic),
                LavaFloodDisasterPrefabEntity = GetEntity(authoring._lavaFloodDisasterPrefab, TransformUsageFlags.Dynamic),
                TornadoDisasterPrefabEntity = GetEntity(authoring._tornadoDisasterPrefab, TransformUsageFlags.Dynamic)
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

public struct DisasterPrefabs : IComponentData
{
    public Entity LightningStormDisasterPrefabEntity;
    public Entity MeteorShowerDisasterPrefabEntity;
    public Entity MeteorPrefabEntity;
    public Entity LavaFloodDisasterPrefabEntity;
    public Entity TornadoDisasterPrefabEntity;
}

public struct AbilityPrefabs : IComponentData
{
    public Entity PoisonVialPrefabEntity;
    public Entity PoisonFieldPrefabEntity;
}

public struct VFXPrefabs : IComponentData
{
    public Entity ScytheSlashVFXPrefabEntity;

    public Entity LightningStrikeIncomingVFXPrefabEntity;
    public Entity LightningStrikeVFXPrefabEntity;

    public Entity MeteorImpactVFXPrefabEntity;

    public Entity HitVFXPrefabEntity;

    public Entity SixShooterTracerVFXPrefabEntity;
}