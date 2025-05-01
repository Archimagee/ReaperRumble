using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;


public class TornadoDisasterAuthoring : MonoBehaviour
{
    [SerializeField] private double _spawnDelaySeconds;
    [SerializeField] private float _tornadoMoveSpeed = 1f;
    [SerializeField] private float _tornadoInnerRange;
    [SerializeField] private float _tornadoOuterRange;
    [SerializeField] private AABB _tornadoMovementBounds;
    [SerializeField] private float _rotationAmountRadians;
    [SerializeField] private double _minTargetChangeTimeSeconds;
    [SerializeField] private double _maxTargetChangeTimeSeconds;

    [SerializeField] private int _soulsOrphaned = 3;
    [SerializeField] private float _knockbackStrength = 45f;
    [SerializeField] private float _tickTimeSeconds = 0.1f;



    public class Baker : Baker<TornadoDisasterAuthoring>
    {
        public override void Bake(TornadoDisasterAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new TornadoDisasterData()
            {
                SpawnDelaySeconds = authoring._spawnDelaySeconds,
                TornadoMoveSpeed = authoring._tornadoMoveSpeed,
                TornadoInnerRange = authoring._tornadoInnerRange,
                TornadoOuterRange = authoring._tornadoOuterRange,
                MovementBounds = authoring._tornadoMovementBounds,
                RotationAmountRadians = authoring._rotationAmountRadians,
                MinTargetChangeTimeSeconds = authoring._minTargetChangeTimeSeconds,
                MaxTargetChangeTimeSeconds = authoring._maxTargetChangeTimeSeconds,
                SoulsOrphaned = authoring._soulsOrphaned,
                KnockbackStrength = authoring._knockbackStrength,
                TickTimeSeconds = authoring._tickTimeSeconds,
                StartTime = 0.0,
                SpawnTime = 0.0,
                LastTickedAt = 0.0,
                ChangeTargetAt = 0.0,
                CurrentTarget = float3.zero,
                CurrentDirection = float3.zero
            });
        }
    }
}



public partial struct TornadoDisasterData : IComponentData
{
    public double SpawnDelaySeconds;
    public float TornadoMoveSpeed;
    public float TornadoInnerRange;
    public float TornadoOuterRange;
    public AABB MovementBounds;
    public float RotationAmountRadians;
    public double MinTargetChangeTimeSeconds;
    public double MaxTargetChangeTimeSeconds;

    public int SoulsOrphaned;
    public float KnockbackStrength;
    public float TickTimeSeconds;

    public double StartTime;
    public double SpawnTime;
    public double LastTickedAt;
    public double ChangeTargetAt;
    public float3 CurrentTarget;
    public float3 CurrentDirection;
}