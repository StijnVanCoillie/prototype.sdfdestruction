## Starting using Signed Distance Field Destruction
general information about the setup creation and how to use it.

## Destructible Object Component
The **DestructibleObject.cs** is automaticly assigned to the generate destruction prefab. This component is responsible for managing the destruction. Currently there is only one function you can call to apply damage `AddDamage( Vector3 hitPoint, Vector3 hitDir, float radius)`. In code you do have the option to choose between CPU or GPU based destruction.

## Shader Overview
In the DestructionShader.shader we divide our structure into three different parts.
1. **Vertex** : Converting the vertex position to the SDF position and calculating our ray.
2. **Fragment** : Sampling our SDF. In case the distance is smaller then 0, we call the raymarch function.
3. **Raymarching** : We use the sphere tracing technique to determine if we reached a surface, if we reach a surface we calculate the visuals.

For more information about raymarching:
* [Iquilezles.org](https://iquilezles.org)
* [The Art of Code](https://www.youtube.com/c/TheArtofCodeIsCool)
* [Shadertoy](https://www.shadertoy.com)

## Compute Shaders
You can find the compute shaders located at `Assets\Compute Shaders\Resources`. This folder contains three important files for the destruction method to work.
1. **CreateSDF.compute** : Generates an SDF of a mesh model. This file is executed when generating the destructiable asset.
2. **CreateEmptySDF.compute** : Generates an empty SDF, to apply our future destruction / damage on.
3. **ApplyDamage.compute** : Applies destruction on the SDF according to the minimum signed distance function of the destruction type.
