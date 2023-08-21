# Particle Life

 A particle simulation that creates life-like structures based on [Jeffrey Ventrella's](https://en.wikipedia.org/wiki/Jeffrey_Ventrella) idea of "Clusters". This is where small simple parts are combined to create a larger more complex structure using simple rules of where particles are either attracted or repelled from each other.

For example, the simulation below has the following rules:
- Red Particles are attracted to Red Particles by 1x
- Red Particles are repelled by Green Particles by 2x
- Green Particles are attracted to Red Particles by 1x
- Green Particles are attracted to Green Particles by 0.5x
- Finally, there is a gravitational force that pushes toward the center. The further a particle is from the center, the more force is applied.

![Screenshot](sample.gif)

## Optimizations

Since all particles can affect each other, all particles will need to be processed to each other to determine the velocity of each particle. In Big-O terms, that is a whopping O(n²). The following optimizations have been applied that allow the simulation to run smoothly with up to 20,000 particles.
- Use of Spatial Partitioning Technique to dramatically reduce the number of operations needed to determine the velocity of each particle.
- Use of Data Oriented Design principles. Particle data is organized in contiguous cache-friendly data structures to allow for efficient data access and parallelization.