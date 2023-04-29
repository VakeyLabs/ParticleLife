using System;
using UnityEngine;
using Unity.Entities;

public enum ParticleSystemType
{
    MainThreadNaive,
    MainThreadOptimized,
    MainThreadSpatialPartitioning,
    JobNaive,
    JobSpatialPartitioning,
    Boid,
    Boid2,
}

public class ParticleSystemManagement: MonoBehaviour
{ 
    public ParticleSystemType currentSystem;

    private Action<bool> currentSystemHandle;

    private Action<bool> GetSystem(ParticleSystemType systemType, World world)
    {
        if (systemType == ParticleSystemType.MainThreadNaive)
        {
            return (bool enabled) =>  world.Unmanaged.GetExistingSystemState<ParticleSimulationMainThreadNaiveSystem>().Enabled = enabled;
        }
        else if (systemType == ParticleSystemType.MainThreadOptimized)
        {
            return (bool enabled) =>  world.Unmanaged.GetExistingSystemState<ParticleSimulationMainThreadOptimizedSystem>().Enabled = enabled;
        }
        else if (systemType == ParticleSystemType.JobNaive)
        {
            return (bool enabled) =>  world.Unmanaged.GetExistingSystemState<ParticleSimulationJobNaiveSystem>().Enabled = enabled;
        }
        else if (systemType == ParticleSystemType.MainThreadSpatialPartitioning)
        {
            return (bool enabled) =>  world.Unmanaged.GetExistingSystemState<SpatialPartitioningMainThreadSystem>().Enabled = enabled;
        }
        else if (systemType == ParticleSystemType.JobSpatialPartitioning)
        {
            // return (bool enabled) => world.GetExistingSystemManaged<SpatialPartitioningJobSystem>().Enabled = enabled;
            return (bool enabled) =>  world.Unmanaged.GetExistingSystemState<SpatialPartitioningJobSystem>().Enabled = enabled;
        }
        else if (systemType == ParticleSystemType.Boid)
        {
            return (bool enabled) =>  world.Unmanaged.GetExistingSystemState<BoidJobSystem>().Enabled = enabled;
        }
        else if (systemType == ParticleSystemType.Boid2)
        {
            return (bool enabled) =>  world.Unmanaged.GetExistingSystemState<BoidJobSystem2>().Enabled = enabled;
        }


        return (bool enabled) => { };
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) return;

        var world = World.DefaultGameObjectInjectionWorld;

        if (currentSystemHandle == null)
        {
            foreach (ParticleSystemType systemType in Enum.GetValues(typeof(ParticleSystemType)))
            {
                var systemHandle = GetSystem(systemType, world);

                if (currentSystem == systemType)
                {
                    currentSystemHandle = systemHandle;
                }
                else
                {
                    systemHandle(false);
                }
            }
        }
        else
        {
            currentSystemHandle(false);
            currentSystemHandle = GetSystem(currentSystem, world);
            currentSystemHandle(true);
        }
    }
}