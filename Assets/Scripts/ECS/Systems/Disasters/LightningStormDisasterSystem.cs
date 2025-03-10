using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.NetCode;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class LightningStormDisasterSystem : SystemBase
{
    private Entity _lightningStrikeVFXPrefab = Entity.Null;
    private Entity _incomingLightningStrikeVFXPrefab = Entity.Null;
    private NativeHashMap<double, float3> _upcomingLightningStrikes = new(500, Allocator.Persistent);

    private readonly double _firstStrikeDelaySeconds = 2.0;
    private readonly double _minTimeBetweenStrikesSeconds = 0.1;
    private readonly double _maxTimeBetweenStrikesSeconds = 0.3;
    private readonly double _incomingTime = 1.0;
    private readonly float _strikeRadius = 8f;
    private readonly float _strikeKnockbackStrength = 100f;

    private readonly AABB _strikeBounds = new() {
        Center = float3.zero,
        Extents = new float3(48f, 15f, 39f) };



    protected override void OnCreate()
    {
        RequireForUpdate<VFXPrefabs>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        if (_lightningStrikeVFXPrefab == Entity.Null || _lightningStrikeVFXPrefab == Entity.Null)
        {
            _incomingLightningStrikeVFXPrefab = SystemAPI.GetSingleton<VFXPrefabs>().LightningStrikeIncomingVFXPrefabEntity;
            _lightningStrikeVFXPrefab = SystemAPI.GetSingleton<VFXPrefabs>().LightningStrikeVFXPrefabEntity;
        }



        foreach ((RefRO<DisasterData> disasterData, RefRO<EventSeed> seed, Entity disasterEntity) in SystemAPI.Query<RefRO<DisasterData>, RefRO<EventSeed>>().WithEntityAccess())
        {
            if (disasterData.ValueRO.MyDisasterType != DisasterType.LightningStorm) break;

            Unity.Mathematics.Random random = new Unity.Mathematics.Random();
            random.InitState(seed.ValueRO.Seed);

            double endTime = SystemAPI.Time.ElapsedTime + disasterData.ValueRO.TimeLastsForSeconds;
            double time = SystemAPI.Time.ElapsedTime + _firstStrikeDelaySeconds;


            while (time <= endTime)
            {
                float3 randomStrikePos = random.NextFloat3(_strikeBounds.Min, _strikeBounds.Max);
                randomStrikePos.y = 15f;



                RaycastInput raycastInput = new RaycastInput()
                {
                    Start = randomStrikePos,
                    End = randomStrikePos + new float3(0f, -30f, 0f),
                    Filter = new CollisionFilter { BelongsTo = ~0u, CollidesWith = 1u << 2 }
                };

                Unity.Physics.RaycastHit hit = new();
                bool hasHit = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CastRay(raycastInput, out hit);



                if (hasHit)
                {
                    //Entity newIncomingVFX = ecb.Instantiate(_incomingLightningStrikeVFXPrefab);
                    //ecb.SetComponent(newIncomingVFX, new LocalTransform() { Position = hit.Position, Scale = 1f, Rotation = quaternion.identity });

                    _upcomingLightningStrikes.Add(time + _incomingTime, hit.Position);
                }

                time += random.NextDouble(_minTimeBetweenStrikesSeconds, _maxTimeBetweenStrikesSeconds);
            }

            ecb.DestroyEntity(disasterEntity);
        }



        NativeList<double> completedStrikes = new(Allocator.Temp);

        foreach (KVPair<double, float3> strikeData in _upcomingLightningStrikes)
        {
            if (SystemAPI.Time.ElapsedTime >= strikeData.Key)
            {
                float3 strikePosition = strikeData.Value;

                Entity newLightningStrike = ecb.Instantiate(_lightningStrikeVFXPrefab);
                ecb.SetComponent(newLightningStrike, new LocalTransform() { Position = strikePosition, Scale = 1f, Rotation = quaternion.identity });



                //float3 maxPos = strikePosition + new float3(_strikeDiameter, _strikeDiameter, _strikeDiameter);
                //float3 minPos = strikePosition - new float3(_strikeDiameter, 0f, _strikeDiameter);

                //Unity.Physics.OverlapAabbInput overlapInput = new Unity.Physics.OverlapAabbInput() {
                //    Aabb = new Aabb() { Max = maxPos, Min = minPos },
                //    Filter = new CollisionFilter() { BelongsTo = ~0u, CollidesWith = 1u << 0 }
                //};

                NativeList<DistanceHit> hits = new(Allocator.Temp);
                SystemAPI.GetSingleton<PhysicsWorldSingleton>().OverlapSphere(strikePosition, _strikeRadius, ref hits, new CollisionFilter() { BelongsTo = ~0u, CollidesWith = 1u << 0 });

                foreach (DistanceHit hit in hits)
                {
                    Entity rpcEntity = ecb.CreateEntity();
                    ecb.AddComponent(rpcEntity, new ApplyKnockbackToPlayerRequestRPC() {
                        KnockbackDirection = math.normalizesafe(SystemAPI.GetComponent<LocalTransform>(hit.Entity).Position - strikePosition),
                        Strength = _strikeKnockbackStrength,
                        PlayerGhostID = SystemAPI.GetComponent<GhostInstance>(hit.Entity).ghostId });
                    ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
                }



                hits.Dispose();
                completedStrikes.Add(strikeData.Key);
            }
        }

        foreach (double strike in completedStrikes) _upcomingLightningStrikes.Remove(strike);
        completedStrikes.Dispose();


        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}