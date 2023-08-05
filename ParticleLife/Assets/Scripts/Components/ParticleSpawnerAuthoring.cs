using System;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct SpawnProperties
{
    public int total;
    public int spawnRadius;
    public int sizeFrom, sizeTo;
}

[Serializable]
public struct SimulationBounds
{
    public float radiusAttraction;
    public float height, width;
    [HideInInspector]
    public float heightRadius, widthRadius;
}

[Serializable]
public struct ParticleProperties
{
    public float innerDetract;
    public float minRadius, maxRadius;
    public float lerpTime;
}

[Serializable]
public struct ParticleMatrix
{
    public float redToRed, redToGreen;
    public float greenToRed, greenToGreen;
}

public struct ParticleRuleElement: IBufferElementData { public float attraction; }
public struct ParticleEntityElement: IBufferElementData { public Entity prefab; }

public struct ParticleSpawner: IComponentData
{
    public SpawnProperties spawnProperties;
    public SimulationBounds simulationBounds;
    public ParticleProperties particleProperties;
    public int colorCount;

    public float3 GetDelta(float3 aPos, float3 bPos)
    {
        var delta = aPos - bPos;
        // var edgeDeltaX = delta.x > 0 ? delta.x - simulationBounds.width : delta.x + simulationBounds.width;
        // var edgeDeltaY = delta.y > 0 ? delta.y - simulationBounds.height : delta.y + simulationBounds.height;
        // delta.x = math.abs(edgeDeltaX) < math.abs(delta.x) ? edgeDeltaX : delta.x;
        // delta.y = math.abs(edgeDeltaY) < math.abs(delta.y) ? edgeDeltaY : delta.y;

        return delta;
    }

    public float3 GetForce(float attraction, float distance, float3 delta)
    {
        return attraction / distance * delta;
    }
}

public class ParticleSpawnerAuthoring: MonoBehaviour
{ 
    public GameObject redParticlePrefab, greenParticlePrefab;
    public SimulationBounds simulationBounds;
    public ParticleProperties particleProperties;
    public ParticleMatrix particleMatrix;
    public SpawnProperties spawnProperties;
}

public class ParticleSpawnerBaker: Baker<ParticleSpawnerAuthoring>
{
    public override void Bake(ParticleSpawnerAuthoring authoring)
    {
        var bounds = authoring.simulationBounds;
        bounds.heightRadius = bounds.height / 2;
        bounds.widthRadius = bounds.width / 2;

        var ruleBuffer = AddBuffer<ParticleRuleElement>();
        ruleBuffer.Add(new ParticleRuleElement { attraction = authoring.particleMatrix.redToRed });
        ruleBuffer.Add(new ParticleRuleElement { attraction = authoring.particleMatrix.redToGreen });
        ruleBuffer.Add(new ParticleRuleElement { attraction = authoring.particleMatrix.greenToRed });
        ruleBuffer.Add(new ParticleRuleElement { attraction = authoring.particleMatrix.greenToGreen });

        var entityBuffer = AddBuffer<ParticleEntityElement>();
        entityBuffer.Add(new ParticleEntityElement { prefab = GetEntity(authoring.redParticlePrefab) });
        entityBuffer.Add(new ParticleEntityElement { prefab = GetEntity(authoring.greenParticlePrefab) });

        AddComponent(new ParticleSpawner {
            spawnProperties = authoring.spawnProperties,
            simulationBounds = bounds,
            particleProperties = authoring.particleProperties,
            colorCount = (int)Math.Sqrt(ruleBuffer.Length),
        });
    }
}