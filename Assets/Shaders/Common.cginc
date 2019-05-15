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

float3 GammaToLinearSpace (float3 sRGB) {
    // Approximate version from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
    return sRGB * (sRGB * (sRGB * 0.305306011h + 0.682171111h) + 0.012522878h);
}

#endif // RAY_TRACING_COMMON_INCLUDED
