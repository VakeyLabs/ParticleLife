using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct MovementSystem: ISystem
{
    public void OnCreate(ref SystemState state) { }
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var spawner = SystemAPI.GetSingleton<ParticleSpawner>();
        new MovementJob{ deltaTime = SystemAPI.Time.DeltaTime, bounds = spawner.simulationBounds }.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct MovementJob: IJobEntity
{
    public float deltaTime;
    public SimulationBounds bounds;
    
    [BurstCompile]  
    public void Execute(TransformAspect transform, RefRO<Velocity> velocity)
    {
        transform.TranslateWorld(deltaTime * velocity.ValueRO.value);

        if (transform.LocalPosition.x <= -bounds.widthRadius) { transform.TranslateWorld(new float3(bounds.width, 0, 0)); } 
        if (transform.LocalPosition.x >= bounds.widthRadius) { transform.TranslateWorld(new float3(-bounds.width, 0, 0)); }
        if (transform.LocalPosition.y <= -bounds.heightRadius) { transform.TranslateWorld(new float3(0, bounds.height, 0)); }
        if (transform.LocalPosition.y >= bounds.heightRadius) { transform.TranslateWorld(new float3(0, -bounds.height, 0)); }
    }
}


// public partial class MovementSystem: SystemBase
// {
//     protected override void OnUpdate()
//     {
//         foreach ((TransformAspect transform, RefRO<Velocity> velocity) in SystemAPI.Query<TransformAspect, RefRO<Velocity>>()) {
//             transform.TranslateWorld(SystemAPI.Time.DeltaTime * velocity.ValueRO.value);
//         }
//     }
// }