using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

public partial class ParticleSimulationMainThreadSystem: SystemBase
{
    protected override void OnUpdate()
    {
        var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
        var particleRuleBuffer = SystemAPI.GetBuffer<ParticleRuleElement>(SystemAPI.GetSingletonEntity<ParticleSpawner>());

        var particlesQuery = EntityManager.CreateEntityQuery(typeof(ParticleTag), typeof(Velocity), typeof(WorldTransform));
        var particleTags = particlesQuery.ToComponentDataArray<ParticleTag>(Allocator.TempJob);
        var velocities = particlesQuery.ToComponentDataArray<Velocity>(Allocator.TempJob);
        var entities = particlesQuery.ToEntityArray(Allocator.TempJob);
        var transforms = particlesQuery.ToComponentDataArray<WorldTransform>(Allocator.TempJob);

        var force = new NativeArray<float3>(transforms.Length, Allocator.TempJob);

        for (var i = 0; i < transforms.Length; i++)
        {
            var aPos = transforms[i].Position;
            var aVel = velocities[i].value;
            var aColor = (int)particleTags[i].color;
            var entity = entities[i];

            for (var k = i+1; k < transforms.Length; k++)
            {
                var bColor = (int)particleTags[k].color;
                var bPos = transforms[k].Position;
                var delta = aPos - bPos;
                var edgeDeltaX = delta.x > 0 ? delta.x - spawner.simulationBounds.width : delta.x + spawner.simulationBounds.width;
                var edgeDeltaY = delta.y > 0 ? delta.y - spawner.simulationBounds.height : delta.y + spawner.simulationBounds.height;
                delta.x = math.abs(edgeDeltaX) < math.abs(delta.x) ? edgeDeltaX : delta.x;
                delta.y = math.abs(edgeDeltaY) < math.abs(delta.y) ? edgeDeltaY : delta.y;
                var distance = math.sqrt(delta.x * delta.x + delta.y * delta.y);
                
                if (distance < spawner.particleProperties.minRadius)
                {
                    var forceStrength = spawner.particleProperties.innerDetract * 1/distance * delta;
                    force[i] += forceStrength;
                    force[k] -= forceStrength;
                }
                else if (distance < spawner.particleProperties.maxRadius)
                {
                    var attractionA = particleRuleBuffer[aColor * 2 + bColor].attraction;
                    var forceStrength = attractionA * 1/distance * delta;
                    force[i] += forceStrength;
                    
                    var attractionB = particleRuleBuffer[bColor * 2 + aColor].attraction;
                    forceStrength = attractionB * 1/distance * delta;
                    force[k] -= forceStrength;
                }
            }

            aVel = math.lerp(aVel, force[i] * 0.4f, 0.5f);
            commandBuffer.SetComponent<Velocity>(entity, new Velocity { value = aVel });
        }

        particlesQuery.Dispose();
        particleTags.Dispose();
        velocities.Dispose();
        entities.Dispose();
        transforms.Dispose();
        force.Dispose();
    }
}