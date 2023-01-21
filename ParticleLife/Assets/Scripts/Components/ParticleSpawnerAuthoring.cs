using System;
using UnityEngine;
using Unity.Entities;

[Serializable]
public struct SimulationBounds
{
    public float height, width;
    [HideInInspector]
    public float heightRadius, widthRadius;
}

[Serializable]
public struct ParticleProperties
{
    public float innerDetract;
    public int count;
    public float minRadius, maxRadius;
}

[Serializable]
public struct ParticleMatrix
{
    public float redToRed, redToGreen;
    public float greenToRed, greenToGreen;
}

public struct ParticleSpawner: IComponentData
{
    public SimulationBounds simulationBounds;
    public ParticleProperties particleProperties;
    public int colorCount;
}

public class ParticleSpawnerAuthoring: MonoBehaviour
{ 
    public GameObject redParticlePrefab, greenParticlePrefab;
    public SimulationBounds simulationBounds;
    public ParticleProperties particleProperties;
    public ParticleMatrix particleMatrix;
}

public struct ParticleRuleElement: IBufferElementData { public float attraction; }
public struct ParticleEntityElement: IBufferElementData { public Entity prefab; }

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
            simulationBounds = bounds,
            particleProperties = authoring.particleProperties,
            colorCount = (int)Math.Sqrt(ruleBuffer.Length),
        });
    }
}