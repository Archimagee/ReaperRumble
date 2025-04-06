using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class MeteorShowerDisasterSystem : SystemBase
{
    private Entity _meteorPrefab = Entity.Null;
    private NativeHashMap<double, float3> _upcomingMeteorSpawns = new(500, Allocator.Persistent);

    private readonly float3 _meteorAngle = new(15f, 35f, 0f);
    private readonly float _meteorSpeed = 2.5f;
    private readonly double _firstMeteorDelaySeconds = 1.0;
    private readonly double _minTimeBetweenMeteorsSeconds = 1.0;
    private readonly double _maxTimeBetweenMeteorsSeconds = 1.5;
    private readonly float _spawnDistance = 25f;
    private readonly float _impactRadius = 6f;
    private readonly float _impactKnockbackStrength = 55f;

    private readonly AABB _impactBounds = new()
    {
        Center = float3.zero,
        Extents = new float3(48f, 15f, 39f)
    };



    protected override void OnCreate()
    {
        RequireForUpdate<DisasterPrefabs>();
    }



    [BurstCompile]
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);



        if (_meteorPrefab == Entity.Null)
        {
            _meteorPrefab = SystemAPI.GetSingleton<DisasterPrefabs>().MeteorPrefabEntity;
        }



        foreach ((RefRO<DisasterData> disasterData, RefRO<EventSeed> seed, Entity disasterEntity) in SystemAPI.Query<RefRO<DisasterData>, RefRO<EventSeed>>().WithEntityAccess())
        {
            if (disasterData.ValueRO.MyDisasterType != DisasterType.MeteorShower) break;

            Unity.Mathematics.Random random = new Unity.Mathematics.Random();
            random.InitState(seed.ValueRO.Seed);

            double endTime = SystemAPI.Time.ElapsedTime + disasterData.ValueRO.TimeLastsForSeconds;
            double time = SystemAPI.Time.ElapsedTime + _firstMeteorDelaySeconds;


            while (time <= endTime)
            {
                float3 randomStrikePos = random.NextFloat3(_impactBounds.Min, _impactBounds.Max);
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
                    _upcomingMeteorSpawns.Add(time, hit.Position);
                }

                time += random.NextDouble(_minTimeBetweenMeteorsSeconds, _maxTimeBetweenMeteorsSeconds);
            }

            ecb.DestroyEntity(disasterEntity);
        }



        NativeList<double> spawnedMeteors = new(Allocator.Temp);

        foreach (KVPair<double, float3> meteorData in _upcomingMeteorSpawns)
        {
            if (SystemAPI.Time.ElapsedTime >= meteorData.Key)
            {
                Entity newMeteor = ecb.Instantiate(_meteorPrefab);
                ecb.AddComponent(newMeteor, new MeteorData() {
                    MovementDirection = -_meteorAngle,
                    MovementSpeed = _meteorSpeed,
                    KnockbackStrength = _impactKnockbackStrength,
                    ImpactRadius = _impactRadius,
                    SoulsOrphanedOnImpact = 0 });

                float3 positionToSpawnAt = meteorData.Value + _meteorAngle * _spawnDistance;

                ecb.SetComponent(newMeteor, new LocalTransform() { Position = positionToSpawnAt, Scale = 1f, Rotation = quaternion.identity });
                spawnedMeteors.Add(meteorData.Key);
            }
        }

        foreach (double meteor in spawnedMeteors) _upcomingMeteorSpawns.Remove(meteor);
        spawnedMeteors.Dispose();



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}



public struct MeteorData : IComponentData
{
    public float3 MovementDirection;
    public float MovementSpeed;
    public float KnockbackStrength;
    public int SoulsOrphanedOnImpact;
    public float ImpactRadius;
}