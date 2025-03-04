using UnityEngine;
using Unity.Entities;



public class ChallengeAuthoring : MonoBehaviour
{
    [SerializeField] private double _timeLastsForSeconds;
    [SerializeField] private ChallengeType _challengeType;



    public class Baker : Baker<ChallengeAuthoring>
    {
        public override void Bake(ChallengeAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ChallengeData() {
                TimeLastsForSeconds = authoring._timeLastsForSeconds,
                MyChallengeType = authoring._challengeType });
        }
    }
}



public struct ChallengeData : IComponentData
{
    public double TimeLastsForSeconds;
    public ChallengeType MyChallengeType;
}

public struct EventDestroyAt : IComponentData
{
    public double TimeToDestroyAt;
}