using UnityEngine;
using Unity.Entities;


public class TornadoDisasterAuthoring : MonoBehaviour
{
    [SerializeField] private float _tornadoMoveSpeed = 1f;
    [SerializeField] private float _tornadoInnerRange;
    [SerializeField] private float _tornadoOuterRange;

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
                TornadoMoveSpeed = authoring._tornadoMoveSpeed,
                TornadoInnerRange = authoring._tornadoInnerRange,
                TornadoOuterRange = authoring._tornadoOuterRange,
                SoulsOrphaned = authoring._soulsOrphaned,
                KnockbackStrength = authoring._knockbackStrength,
                TickTimeSeconds = authoring._tickTimeSeconds,
                StartTime = 0.0,
                LastTickedAt = 0.0
            });
        }
    }
}



public partial struct TornadoDisasterData : IComponentData
{
    public float TornadoMoveSpeed;
    public float TornadoInnerRange;
    public float TornadoOuterRange;

    public int SoulsOrphaned;
    public float KnockbackStrength;
    public float TickTimeSeconds;

    public double StartTime;
    public double LastTickedAt;
}