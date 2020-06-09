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

float Rand01 (float3 seed) {
    return abs(RandInUnitSphere (seed).x);
}

float3 GammaToLinearSpace (float3 sRGB) {
    // Approximate version from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
    return sRGB * (sRGB * (sRGB * 0.305306011h + 0.682171111h) + 0.012522878h);
}

// ------------------
// matrix functions
// ------------------
float4x4 rotationMatrix(float3 axis, float angle) {
    axis = normalize(axis);
    float s = sin(angle);
    float c = cos(angle);
    float oc = 1.0 - c;

    return float4x4(
    oc * axis.x * axis.x + c, oc * axis.x * axis.y - axis.z * s, oc * axis.z * axis.x + axis.y * s, 0.0,
    oc * axis.x * axis.y + axis.z * s, oc * axis.y * axis.y + c, oc * axis.y * axis.z - axis.x * s, 0.0,
    oc * axis.z * axis.x - axis.y * s, oc * axis.y * axis.z + axis.x * s, oc * axis.z * axis.z + c, 0.0,
    0, 0, 0, 1.0);
}

// https://forum.unity.com/threads/incorrect-normals-on-after-rotating-instances-graphics-drawmeshinstancedindirect.503232/#post-3277479
float4x4 inverse(float4x4 input) {
    #define minor(a,b,c) determinant(float3x3(input.a, input.b, input.c))

    float4x4 cofactors = float4x4(
    minor(_22_23_24, _32_33_34, _42_43_44),
    -minor(_21_23_24, _31_33_34, _41_43_44),
    minor(_21_22_24, _31_32_34, _41_42_44),
    -minor(_21_22_23, _31_32_33, _41_42_43),

    -minor(_12_13_14, _32_33_34, _42_43_44),
    minor(_11_13_14, _31_33_34, _41_43_44),
    -minor(_11_12_14, _31_32_34, _41_42_44),
    minor(_11_12_13, _31_32_33, _41_42_43),

    minor(_12_13_14, _22_23_24, _42_43_44),
    -minor(_11_13_14, _21_23_24, _41_43_44),
    minor(_11_12_14, _21_22_24, _41_42_44),
    -minor(_11_12_13, _21_22_23, _41_42_43),

    -minor(_12_13_14, _22_23_24, _32_33_34),
    minor(_11_13_14, _21_23_24, _31_33_34),
    -minor(_11_12_14, _21_22_24, _31_32_34),
    minor(_11_12_13, _21_22_23, _31_32_33)
    );
    #undef minor
    return transpose(cofactors) / determinant(input);
}

#endif // RAY_TRACING_COMMON_INCLUDED
