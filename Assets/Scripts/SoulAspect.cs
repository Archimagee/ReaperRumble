using Unity.Entities;
using Unity.Transforms;



public readonly partial struct SoulAspect : IAspect
{
    public readonly RefRO<SoulTag> Soul;
    public readonly RefRW<LocalTransform> Transform;
    public readonly RefRO<MoveSpeedComponent> MoveSpeed;
}