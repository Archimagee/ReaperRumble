using UnityEngine;
using Unity.Entities;



public class DisasterAuthoring : MonoBehaviour
{
    [SerializeField] private double _timeLastsForSeconds;
    [SerializeField] private DisasterType _disasterType;



    public class Baker : Baker<DisasterAuthoring>
    {
        public override void Bake(DisasterAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new DisasterData()
            {
                TimeLastsForSeconds = authoring._timeLastsForSeconds,
                MyDisasterType = authoring._disasterType
            });
        }
    }
}



public struct DisasterData : IComponentData
{
    public double TimeLastsForSeconds;
    public DisasterType MyDisasterType;
}