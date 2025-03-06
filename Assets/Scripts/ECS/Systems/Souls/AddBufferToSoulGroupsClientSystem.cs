using Unity.Burst;
using Unity.Collections;
using Unity.Entities;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class AddBufferToSoulGroupsClientSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<SoulGroupTag>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRO<SoulGroupTag> soulGroup, Entity soulGroupEntity) in SystemAPI.Query<RefRO<SoulGroupTag>>().WithNone<HasBufferTag>().WithEntityAccess())
        {
            ecb.AddBuffer<SoulBufferElement>(soulGroupEntity);
            ecb.AddComponent<HasBufferTag>(soulGroupEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

public partial struct HasBufferTag : IComponentData { }