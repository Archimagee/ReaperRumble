using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class InitialiseSoulGroupServerSystem : SystemBase
{
    [BurstCompile]
    protected override void OnCreate()
    {
        RequireForUpdate<SoulGroupInitialise>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRO<SoulGroupInitialise> initialise, RefRO<LocalTransform> transform, Entity initialiseEntity) in SystemAPI.Query<RefRO<SoulGroupInitialise>, RefRO<LocalTransform>>().WithNone<SoulGroupTag>().WithEntityAccess())
        {
            Entity newSoulGroup = ecb.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().SoulGroupPrefabEntity);
            ecb.SetName(newSoulGroup, "Initialised Soul Group");
            ecb.SetComponent(newSoulGroup, new LocalTransform() { Position = transform.ValueRO.Position, Rotation = quaternion.identity, Scale = 1f });
            ecb.AddBuffer<SoulBufferElement>(newSoulGroup);
            ecb.AddComponent(newSoulGroup, new SoulGroupInitialise() { SoulAmount = initialise.ValueRO.SoulAmount, TimeLastsForSeconds = initialise.ValueRO.TimeLastsForSeconds });
            ecb.RemoveComponent<SoulGroupInitialise>(initialiseEntity);
        }

        foreach ((RefRO<SoulGroupInitialise> initialise, RefRO<LocalTransform> transform, Entity initialiseGroup) in SystemAPI.Query<RefRO<SoulGroupInitialise>, RefRO<LocalTransform>>().WithAll<SoulGroupTag>().WithEntityAccess())
        {
            Unity.Mathematics.Random random = new();
            random.InitState((uint)System.DateTime.Now.Millisecond * (uint)System.DateTime.Now.Second);

            for (int i = 0; i < initialise.ValueRO.SoulAmount; i++)
            {
                Entity newSoul = ecb.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().SoulPrefabEntity);
                ecb.SetName(newSoul, "Initialised Soul");
                ecb.SetComponent(newSoul, new LocalTransform()
                {
                    Position = transform.ValueRO.Position + random.NextFloat3(new float3(-1f, -1f, -1f), new float3(1f, 1f, 1f)),
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
                ecb.SetComponent(newSoul, new SoulGroupMember() { MyGroup = initialiseGroup });
                SystemAPI.GetBuffer<SoulBufferElement>(initialiseGroup).Add(new SoulBufferElement() { Soul = newSoul });

                ecb.RemoveComponent<SoulGroupInitialise>(initialiseGroup);
            }
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}