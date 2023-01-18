
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public struct Velocity: IComponentData { public float3 value; }
public class VelocityAuthoring: MonoBehaviour { public float3 value; }
public class VelocityBaker: Baker<VelocityAuthoring>
{
    public override void Bake(VelocityAuthoring authoring)
    {
        AddComponent(new Velocity { value = authoring.value });
    }
}