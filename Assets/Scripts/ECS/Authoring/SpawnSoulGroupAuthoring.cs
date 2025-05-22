using UnityEngine;
using Unity.Entities;



public class SpawnSoulGroupAuthoring : MonoBehaviour
{
    [SerializeField] private int _initialSoulAmount;
    [SerializeField] private double _timeLastsForSeconds;

    public class Baker : Baker<SpawnSoulGroupAuthoring>
    {
        public override void Bake(SpawnSoulGroupAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SoulGroupInitialise { SoulAmount = authoring._initialSoulAmount, TimeLastsForSeconds = authoring._timeLastsForSeconds });
        }
    }
}

public partial struct SoulGroupInitialise : IComponentData
{
    public int SoulAmount;
    public double TimeLastsForSeconds;
}