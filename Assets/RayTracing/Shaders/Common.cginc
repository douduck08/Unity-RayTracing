#ifndef RAY_TRACING_COMMON_INCLUDED
#define RAY_TRACING_COMMON_INCLUDED

#define USE_SPHERICAL_FIBONACCI
#ifdef USE_SPHERICAL_FIBONACCI
RWStructuredBuffer<float3> _SphericalSampleBuffer;
int _SphericalSampleOffset;
#endif

uint GetIndex (uint3 id, uint width, uint height) {
    return id.x + width * id.y + width * height * id.z;
}

float2 GetUV (uint3 id, uint width, uint height) {
    float2 texelSize = rcp(float2(width, height));
    float2 uv = float2(id.xy) + 0.5;
    uv *= texelSize;
    return uv;
}

float Hash (float2 seed) {
    return frac(sin(dot(seed, float2(12.9898, 78.233))) * 43758.5453);
}

float2 Hash2 (float2 seed) {
    float x = frac(sin(dot(seed, float2(127.1, 311.7))) * 43758.5453);
    float y = frac(sin(dot(seed, float2(269.5, 183.3))) * 43758.5453);
    return float2(x, y);
}

float3 RandInUnitSphere (float3 seed) {
#ifdef USE_SPHERICAL_FIBONACCI
    seed = _SphericalSampleBuffer[(Hash(seed.xy + seed.yz) * 392901 + _SphericalSampleOffset) % 4096];
#else
    seed = 2.0 * float3(Hash(seed.xy), Hash(seed.yz), Hash(seed.zx)) - 1;
    while (dot(seed, seed) > 1.0) {
        seed = 2.0 * float3(Hash(seed.xy), Hash(seed.yz), Hash(seed.zx)) - 1;
    }
#endif
    return seed;
}

float3 GammaToLinearSpace (float3 sRGB) {
    // Approximate version from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
    return sRGB * (sRGB * (sRGB * 0.305306011h + 0.682171111h) + 0.012522878h);
}

#endif // RAY_TRACING_COMMON_INCLUDED
