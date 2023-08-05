using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
[BurstCompile]
public partial struct MovementSystem: ISystem
{
    public void OnCreate(ref SystemState state) { }
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var spawner = SystemAPI.GetSingleton<ParticleSpawner>();
        new MovementJob{
            deltaTime = SystemAPI.Time.DeltaTime,
            bounds = spawner.simulationBounds,
            lerpTime = spawner.particleProperties.lerpTime
        }.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct MovementJob: IJobEntity
{
    public float deltaTime, lerpTime;
    public SimulationBounds bounds;
    
    [BurstCompile]  
    public void Execute(TransformAspect transform, RefRO<Velocity> velocityRO)
    {
        var distance = math.distance(transform.LocalPosition, float3.zero);
        var velocity = math.lerp(velocityRO.ValueRO.value, -distance * transform.LocalPosition * bounds.radiusAttraction, lerpTime);

        transform.TranslateWorld(deltaTime * velocity);

        // if (transform.LocalPosition.x <= -bounds.widthRadius) { transform.TranslateWorld(new float3(bounds.width, 0, 0)); } 
        // else if (transform.LocalPosition.x >= bounds.widthRadius) { transform.TranslateWorld(new float3(-bounds.width, 0, 0)); }
        // else if (transform.LocalPosition.y <= -bounds.heightRadius) { transform.TranslateWorld(new float3(0, bounds.height, 0)); }
        // else if (transform.LocalPosition.y >= bounds.heightRadius) { transform.TranslateWorld(new float3(0, -bounds.height, 0)); }
    }
}