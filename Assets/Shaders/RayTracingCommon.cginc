#ifndef RAY_TRACING_COMMON_INCLUDED
#define RAY_TRACING_COMMON_INCLUDED

float Rand (float2 st) {
    return frac(sin(dot(st, float2(12.9898, 78.233))) * 43758.5453);
}

float3 RandInUnitSphere (float3 normal) {
    float3 seed = normal;
    seed = 2.0 * float3(Rand(seed.xy), Rand(seed.yz), Rand(seed.zx)) - 1;
    while (dot(seed, seed) > 1.0) {
        seed = 2.0 * float3(Rand(seed.xy), Rand(seed.yz), Rand(seed.zx)) - 1;
    }
    return seed;
}

#endif // RAY_TRACING_COMMON_INCLUDED
