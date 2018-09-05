#ifndef RAY_TRACING_INCLUDED
#define RAY_TRACING_INCLUDED

struct Ray {
    float3 origin;
    float3 direction;
};

struct RayHit {
    float3 position;
    float3 normal;
    float t;
};

struct SphereData {
    float3 position;
    float radius;
};

Ray CreateRay (float3 origin, float3 direction) {
    Ray ray;
    ray.origin = origin;
    ray.direction = direction;
    return ray;
}

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

float3 GetHitPoint (Ray ray, float t) {
    return ray.origin + ray.direction * t;
}

bool HitSphere (Ray ray, SphereData sphere, float min_t, float max_t, out RayHit hit) {
    float3 oc = ray.origin - sphere.position;
    float a = dot(ray.direction, ray.direction);
    float b = dot(ray.direction, oc) * 2;
    float c = dot(oc, oc) - sphere.radius * sphere.radius;
    float dis = b * b - 4 * a * c;

    if (dis > 0) {
        float t = (-b - sqrt(dis)) / (2.0 * a);
        if (t > min_t && t < max_t) {
            hit.t = t;
            hit.position = GetHitPoint(ray, hit.t);
            hit.normal = normalize(hit.position - sphere.position);
            return true;
        }
    }
    return false;
}

#endif // RAY_TRACING_INCLUDED