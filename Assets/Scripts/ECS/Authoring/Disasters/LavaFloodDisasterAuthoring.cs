using UnityEngine;
using Unity.Entities;



public class LavaFloodDisasterAuthoring : MonoBehaviour
{
    [SerializeField] private float _lavaStartHeight = 0f;
    [SerializeField] private float _floodRiseTimeSeconds = 5f;
    [SerializeField] private float _lavaEndHeight = 0f;
    [SerializeField] private float _floodDelaySeconds = 4f;
    [SerializeField] private int _lavaSoulsOrphaned = 4;
    [SerializeField] private float _lavaKnockbackStrength = 45f;
    [SerializeField] private float _lavaTickTimeSeconds = 1f;



    public class Baker : Baker<LavaFloodDisasterAuthoring>
    {
        public override void Bake(LavaFloodDisasterAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new LavaFloodDisasterData()
            {
                FloodRiseTimeSeconds = authoring._floodRiseTimeSeconds,
                LavaStartHeight = authoring._lavaStartHeight,
                LavaEndHeight = authoring._lavaEndHeight,
                FloodDelaySeconds = authoring._floodDelaySeconds,
                LavaSoulsOrphaned = authoring._lavaSoulsOrphaned,
                LavaKnockbackStrength = authoring._lavaKnockbackStrength,
                LavaTickTimeSeconds = authoring._lavaTickTimeSeconds,
                StartTime = 0.0,
                LastTickedAt = 0.0
            });
        }
    }
}



public partial struct LavaFloodDisasterData : IComponentData
{
    public float FloodRiseTimeSeconds;
    public float LavaStartHeight;
    public float LavaEndHeight;

    public float FloodDelaySeconds;
    public int LavaSoulsOrphaned;
    public float LavaKnockbackStrength;
    public float LavaTickTimeSeconds;

    public double StartTime;
    public double LastTickedAt;
}