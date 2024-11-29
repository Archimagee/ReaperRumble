using Unity.Entities;
using Unity.Mathematics;



public struct SoulBoidComponent : IComponentData
{
    public float3 followerPos;
    public float3 targetPos;
}