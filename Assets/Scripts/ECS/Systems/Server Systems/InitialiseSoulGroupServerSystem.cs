using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;
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



        foreach ((RefRO<SoulGroupInitialise> initialise, RefRO<LocalTransform> transform, Entity groupEntity) in SystemAPI.Query<RefRO<SoulGroupInitialise>, RefRO<LocalTransform>>().WithEntityAccess().WithAll<SoulGroupTag>())
        {
            Entity sendRpcEntity = ecb.CreateEntity();
            ecb.AddComponent(sendRpcEntity, new SpawnSoulsRequestRPC
            {
                GroupID = SystemAPI.GetComponent<GhostInstance>(groupEntity).ghostId,
                Amount = initialise.ValueRO.SoulAmount,
                Position = transform.ValueRO.Position
            });
            ecb.AddComponent<SendRpcCommandRequest>(sendRpcEntity);

            Debug.Log(initialise.ValueRO.TimeLastsForSeconds);
            if (initialise.ValueRO.TimeLastsForSeconds != 0) ecb.AddComponent(groupEntity, new DestroySoulGroup() { TimeToDestroyAt = SystemAPI.Time.ElapsedTime + initialise.ValueRO.TimeLastsForSeconds });

            ecb.RemoveComponent<SoulGroupInitialise>(groupEntity);
        }



        foreach ((RefRO<SoulGroupInitialise> initialise, Entity entity) in SystemAPI.Query<RefRO<SoulGroupInitialise>>().WithEntityAccess().WithNone<SoulGroupTag>())
        {
            float3 position = SystemAPI.GetComponent<LocalToWorld>(entity).Position;

            Entity newSoulGroup = ecb.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().SoulGroupPrefabEntity);
            ecb.SetComponent(newSoulGroup, new LocalTransform() { Position = position, Scale = 1f, Rotation = quaternion.identity });
            ecb.AddComponent(newSoulGroup, new SoulGroupInitialise() { SoulAmount = initialise.ValueRO.SoulAmount, TimeLastsForSeconds = initialise.ValueRO.TimeLastsForSeconds });

            ecb.RemoveComponent<SoulGroupInitialise>(entity);
        }




        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}