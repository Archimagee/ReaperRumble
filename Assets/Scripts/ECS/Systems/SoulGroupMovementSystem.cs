using Unity.Entities;



[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class SoulGroupMovementSystem : SystemBase
{
    protected override void OnCreate()
    {

    }



    protected override void OnUpdate()
    {
        foreach (RefRO<SoulGroupTag> element in SystemAPI.Query<RefRO<SoulGroupTag>>())
        {

        }
    }
}



//public void FixedUpdate()
//{
//    Vector3 targetPos = _target.transform.position;
//    Vector3 followerPos = this.transform.position;
//    Vector3 direction = (targetPos - followerPos).normalized;
//    float distance = Vector3.Distance(followerPos, targetPos);
//    distance = Mathf.Max(distance - 8f, 0f);
//    this.transform.position += direction * distance * 0.05f;
//}