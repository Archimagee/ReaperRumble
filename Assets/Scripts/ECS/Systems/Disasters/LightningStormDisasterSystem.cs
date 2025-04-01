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
    private NativeHashMap<double, float3> _upcomingIncomingVFX = new(500, Allocator.Persistent);

    private readonly double _firstStrikeDelaySeconds = 4.0;
    private readonly double _minTimeBetweenStrikesSeconds = 0.1;
    private readonly double _maxTimeBetweenStrikesSeconds = 0.4;
    private readonly double _incomingTime = 1.6;
    private readonly float _strikeRadius = 4f;
    private readonly float _strikeKnockbackStrength = 30f;

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
                    _upcomingIncomingVFX.Add(time, hit.Position);
                    _upcomingLightningStrikes.Add(time + _incomingTime, hit.Position);
                }

                time += random.NextDouble(_minTimeBetweenStrikesSeconds, _maxTimeBetweenStrikesSeconds);
            }

            ecb.DestroyEntity(disasterEntity);
        }



        NativeList<double> completedStrikes = new(Allocator.Temp);
        NativeList<double> completedIncomingVFX = new(Allocator.Temp);

        foreach (KVPair<double, float3> strikeData in _upcomingIncomingVFX)
        {
            if (SystemAPI.Time.ElapsedTime >= strikeData.Key)
            {
                Entity newIncomingVFX = ecb.Instantiate(_incomingLightningStrikeVFXPrefab);
                ecb.SetComponent(newIncomingVFX, new LocalTransform() { Position = strikeData.Value, Scale = 1f, Rotation = quaternion.identity });
                completedIncomingVFX.Add(strikeData.Key);
            }
        }

        foreach (KVPair<double, float3> strikeData in _upcomingLightningStrikes)
        {
            if (SystemAPI.Time.ElapsedTime >= strikeData.Key)
            {
                float3 strikePosition = strikeData.Value;

                Entity newLightningStrike = ecb.Instantiate(_lightningStrikeVFXPrefab);
                ecb.SetComponent(newLightningStrike, new LocalTransform() { Position = strikePosition, Scale = 1f, Rotation = quaternion.identity });

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

                    rpcEntity = ecb.CreateEntity();
                    ecb.AddComponent(rpcEntity, new OrphanSoulsRequestRPC
                    {
                        GroupID = SystemAPI.GetComponent<GhostInstance>(SystemAPI.GetComponent<PlayerSoulGroup>(hit.Entity).MySoulGroup).ghostId,
                        Amount = 2,
                        Position = SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetComponent<PlayerSoulGroup>(hit.Entity).MySoulGroup).Position
                    });
                    ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
                }



                hits.Dispose();
                completedStrikes.Add(strikeData.Key);
            }
        }

        foreach (double strike in completedIncomingVFX) _upcomingIncomingVFX.Remove(strike);
        foreach (double strike in completedStrikes) _upcomingLightningStrikes.Remove(strike);
        completedIncomingVFX.Dispose();
        completedStrikes.Dispose();


        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}