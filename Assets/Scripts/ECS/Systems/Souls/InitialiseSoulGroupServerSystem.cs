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
        NativeArray<Entity> groupsToInitialise = SystemAPI.QueryBuilder().WithAll<SoulGroupInitialise>().WithNone<SoulGroupTag>().Build().ToEntityArray(Allocator.Temp);
        if (groupsToInitialise.Length > 0)
        {
            Entity initialiseGroup = groupsToInitialise[0];
            Entity newSoulGroup = EntityManager.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().SoulGroupPrefabEntity);
            EntityManager.SetName(newSoulGroup, "Initialised Soul Group");
            EntityManager.SetComponentData(newSoulGroup, new LocalTransform() { Position = SystemAPI.GetComponent<LocalToWorld>(initialiseGroup).Position, Rotation = quaternion.identity, Scale = 1f });
            EntityManager.AddBuffer<SoulBufferElement>(newSoulGroup);
            EntityManager.AddComponentData(newSoulGroup, new DestroySoulGroup() { TimeToDestroyAt = SystemAPI.Time.ElapsedTime + SystemAPI.GetComponent<SoulGroupInitialise>(initialiseGroup).TimeLastsForSeconds });



            Unity.Mathematics.Random random = new();
            random.InitState((uint)System.DateTime.Now.Millisecond * (uint)System.DateTime.Now.Second);

            for (int i = 0; i < SystemAPI.GetComponent<SoulGroupInitialise>(initialiseGroup).SoulAmount; i++)
            {
                Entity newSoul = EntityManager.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().SoulPrefabEntity);
                EntityManager.SetName(newSoul, "Initialised Soul");
                EntityManager.SetComponentData(newSoul, new LocalTransform()
                {
                    Position = SystemAPI.GetComponent<LocalTransform>(newSoulGroup).Position + random.NextFloat3(new float3(-1f, -1f, -1f), new float3(1f, 1f, 1f)),
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
                EntityManager.SetComponentData(newSoul, new SoulGroupMember() { MyGroup = newSoulGroup });
                SystemAPI.GetBuffer<SoulBufferElement>(newSoulGroup).Add(new SoulBufferElement() { Soul = newSoul });

                EntityManager.RemoveComponent<SoulGroupInitialise>(newSoulGroup);
            }



            EntityManager.RemoveComponent<SoulGroupInitialise>(initialiseGroup);
        }
    }
}