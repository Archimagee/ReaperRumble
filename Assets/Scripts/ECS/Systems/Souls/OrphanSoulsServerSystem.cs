using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class OrphanSoulsServerSystem : SystemBase
{
    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRO<OrphanSoulsRequestRPC> orphan, Entity rpcEntity) in SystemAPI.Query<RefRO<OrphanSoulsRequestRPC>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
        {
            if (orphan.ValueRO.Amount > 0)
            {
                foreach ((RefRO<GhostInstance> ghost, Entity groupEntity) in SystemAPI.Query<RefRO<GhostInstance>>().WithEntityAccess())
                    if (ghost.ValueRO.ghostId == orphan.ValueRO.GroupID)
                    {
                        ecb.AddComponent(groupEntity, new OrphanSouls() { Amount = orphan.ValueRO.Amount, Position = orphan.ValueRO.Position });

                        ecb.DestroyEntity(rpcEntity);

                        break;
                    }
            }
            else
            {
                Debug.LogWarning("Server received orphan request for 0 souls");
                ecb.DestroyEntity(rpcEntity);
            }
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
        ecb = new EntityCommandBuffer(Allocator.Temp);



        foreach ((RefRO<OrphanSouls> orphan, Entity groupEntity) in SystemAPI.Query<RefRO<OrphanSouls>>().WithEntityAccess())
        {
            if (orphan.ValueRO.Amount > 0)
            {
                float3 position;
                if (!float3.Equals(orphan.ValueRO.Position, float3.zero)) position = orphan.ValueRO.Position;
                else position = SystemAPI.GetComponent<LocalTransform>(groupEntity).Position;



                Entity newGroup = ecb.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().SoulGroupPrefabEntity);
                ecb.SetComponent(newGroup, new LocalTransform() { Scale = 1f, Rotation = quaternion.identity, Position = position });
                ecb.AddBuffer<SoulBufferElement>(newGroup);
                ecb.SetName(newGroup, "Orphan group");



                NativeArray<SoulBufferElement> soulElementArray = SystemAPI.GetBuffer<SoulBufferElement>(groupEntity).ToNativeArray(Allocator.Temp);
                int amountToOrphan = math.min(orphan.ValueRO.Amount, soulElementArray.Length);

                int i = 0;
                while (i < amountToOrphan)
                {
                    ecb.AddComponent(soulElementArray[i].Soul, new ChangeSoulGroup() { SoulGroupToMoveTo = newGroup });
                    i++;
                }

                soulElementArray.Dispose();

                break;
            }
            else Debug.LogWarning("Server tried to orphan 0 souls from group " + groupEntity);
        }



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

public partial struct OrphanSouls : IComponentData
{
    public int Amount;
    public float3 Position;
}