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



        NativeArray<Entity> rpcs = SystemAPI.QueryBuilder().WithAll<ReceiveRpcCommandRequest, OrphanSoulsRequestRPC>().Build().ToEntityArray(Allocator.Temp);

        foreach (Entity rpcEntity in rpcs)
        {
            OrphanSoulsRequestRPC orphan = SystemAPI.GetComponent<OrphanSoulsRequestRPC>(rpcEntity);

            if (orphan.Amount > 0)
            {
                foreach ((RefRO<GhostInstance> ghost, Entity groupEntity) in SystemAPI.Query<RefRO<GhostInstance>>().WithEntityAccess())
                    if (ghost.ValueRO.ghostId == orphan.GroupID)
                    {
                        float3 position;
                        if (!float3.Equals(orphan.Position, float3.zero)) position = orphan.Position;
                        else position = SystemAPI.GetComponent<LocalTransform>(groupEntity).Position;



                        Entity newGroup = ecb.Instantiate(SystemAPI.GetSingleton<EntitySpawnerPrefabs>().SoulGroupPrefabEntity);
                        ecb.SetComponent(newGroup, new LocalTransform() { Scale = 1f, Rotation = quaternion.identity, Position = position });
                        ecb.AddBuffer<SoulBufferElement>(newGroup);
                        ecb.SetName(newGroup, "Orphan group");



                        NativeArray<SoulBufferElement> soulElementArray = SystemAPI.GetBuffer<SoulBufferElement>(groupEntity).ToNativeArray(Allocator.Temp);
                        int amountToOrphan = math.min(orphan.Amount, soulElementArray.Length);

                        int i = 0;
                        while (i < amountToOrphan)
                        {
                            ecb.AddComponent(soulElementArray[i].Soul, new ChangeSoulGroup() { SoulGroupToMoveTo = newGroup });
                            i++;
                        }

                        soulElementArray.Dispose();
                        ecb.DestroyEntity(rpcEntity);

                        break;
                    }
            }
            else
            {
                Debug.Log("Server received orphan request for 0 souls");
                ecb.DestroyEntity(rpcEntity);
            }
        }

        rpcs.Dispose();



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}