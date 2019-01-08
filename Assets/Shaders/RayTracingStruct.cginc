#ifndef RAY_TRACING_STRUCT_INCLUDED
#define RAY_TRACING_STRUCT_INCLUDED

#include "RayTracingCommon.cginc"

struct Ray {
    float3 origin;
    float pad0;
    float3 direction;
    float pad1;
    float3 color;
    float pad2;
};

struct RayHit {
    float3 position;
    float3 normal;
    float t;
};

struct SphereData {
    float3 position;
    float radius;
    float4 color;
};

Ray CreateRay (float3 origin, float3 direction) {
    Ray ray;
    ray.origin = origin;
    ray.direction = direction;
    ray.color = 1;
    ray.pad0 = 0;
    ray.pad1 = 0;
    ray.pad2 = 0;
    return ray;
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

#endif // RAY_TRACING_STRUCT_INCLUDED