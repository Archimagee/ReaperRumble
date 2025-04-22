using UnityEngine;
using Unity.Entities;



public class PoisonFieldAuthoring : MonoBehaviour
{
    [SerializeField] private double _durationSeconds;
    [SerializeField] private double _tickTimeSeconds;
    [SerializeField] private int _soulsOrphaned;



    public class Baker : Baker<PoisonFieldAuthoring>
    {
        public override void Bake(PoisonFieldAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PoisonFieldData()
            {
                DurationSeconds = authoring._durationSeconds,
                TickTimeSeconds = authoring._tickTimeSeconds,
                SoulsOrphaned = authoring._soulsOrphaned,
                StartTime = 0.0,
                LastTickedAt = 0.0
            });
        }
    }
}



public partial struct PoisonFieldData : IComponentData
{
    public double DurationSeconds;
    public double TickTimeSeconds;
    public int SoulsOrphaned;

    public double StartTime;
    public double LastTickedAt;
}