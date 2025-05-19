using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class EruptionDisasterSystem : SystemBase
{
    private Entity _rockPrefab = Entity.Null;
    private NativeHashMap<double, float3> _upcomingRockSpawns = new(500, Allocator.Persistent);

    private readonly float _rockSpeed = 90f;
    private readonly float _rotationSpeed = 0.01f;
    private readonly float _initialUpwardsRotationDegrees = 75f;
    private readonly double _firstRockDelaySeconds = 6.0;
    private readonly double _minTimeBetweenRockGroupsSeconds = 2.0;
    private readonly double _maxTimeBetweenRockGroupsSeconds = 6;
    private readonly double _timeBetweenRocks = 0.06;
    private readonly int _numberOfRocksInGroup = 3;
    private readonly float3 _spawnPosition = new float3(350f, 40f, 0f);
    private readonly float _impactRadius = 10f;
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
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);



        if (_rockPrefab == Entity.Null)
        {
            _rockPrefab = SystemAPI.GetSingleton<DisasterPrefabs>().EruptionRockPrefabEntity;
        }



        foreach ((RefRO<DisasterData> disasterData, RefRO<EventSeed> seed, Entity disasterEntity) in SystemAPI.Query<RefRO<DisasterData>, RefRO<EventSeed>>().WithEntityAccess())
        {
            if (disasterData.ValueRO.MyDisasterType != DisasterType.Eruption) break;

            Unity.Mathematics.Random random = new Unity.Mathematics.Random();
            random.InitState(seed.ValueRO.Seed);

            double endTime = SystemAPI.Time.ElapsedTime + disasterData.ValueRO.TimeLastsForSeconds;
            double time = SystemAPI.Time.ElapsedTime + _firstRockDelaySeconds;



            while (time <= endTime)
            {
                for (int i = 0; i < _numberOfRocksInGroup; i++)
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
                        _upcomingRockSpawns.Add(time, hit.Position);
                    }

                    time += _timeBetweenRocks;
                }

                time += random.NextDouble(_minTimeBetweenRockGroupsSeconds, _maxTimeBetweenRockGroupsSeconds);
            }

            ecb.DestroyEntity(disasterEntity);
        }



        NativeList<double> spawnedRocks = new(Allocator.Temp);

        foreach (KVPair<double, float3> rockData in _upcomingRockSpawns)
        {
            if (SystemAPI.Time.ElapsedTime >= rockData.Key)
            {
                Entity newRock = ecb.Instantiate(_rockPrefab);
                ecb.AddComponent(newRock, new EruptionRockData() {
                    ImpactPoint = rockData.Value,
                    RotationSpeedRadians = _rotationSpeed,
                    MovementSpeed = _rockSpeed,
                    KnockbackStrength = _impactKnockbackStrength,
                    ImpactRadius = _impactRadius,
                    SoulsOrphanedOnImpact = 0
                });



                ecb.SetComponent(newRock, new LocalTransform() {
                    Position = _spawnPosition,
                    Scale = 1f,
                    Rotation = math.mul(quaternion.LookRotation(math.normalize(rockData.Value - _spawnPosition), new float3(0f, 1f, 0f)), quaternion.RotateX((-_initialUpwardsRotationDegrees / 360) * (2 * math.PI)))
                });

                spawnedRocks.Add(rockData.Key);
            }
        }

        foreach (double meteor in spawnedRocks) _upcomingRockSpawns.Remove(meteor);
        spawnedRocks.Dispose();



        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

public partial struct EruptionRockData : IComponentData
{
    public float3 ImpactPoint;
    public float RotationSpeedRadians;
    public float MovementSpeed;
    public float KnockbackStrength;
    public float ImpactRadius;
    public int SoulsOrphanedOnImpact;
}