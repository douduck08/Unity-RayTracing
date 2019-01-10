#ifndef RAY_TRACING_STRUCT_INCLUDED
#define RAY_TRACING_STRUCT_INCLUDED

#include "RayTracingCommon.cginc"

#define BOUNCE_RATIO 0.9

struct SphereData {
    float3 position;
    float radius;
    float4 color;
};

struct RayHit {
    float3 position;
    float t;
    float3 normal;
    float3 albedo;
};

struct Ray {
    float3 origin;
    float3 direction;
    float3 color;

    float3 GetHitPoint (float t) {
        return origin + direction * t;
    }

    bool HitSphere (SphereData sphere, float min_t, float max_t, inout RayHit hit) {
        float3 oc = origin - sphere.position;
        float a = dot(direction, direction);
        float b = dot(direction, oc) * 2;
        float c = dot(oc, oc) - sphere.radius * sphere.radius;
        float dis = b * b - 4 * a * c;

        if (dis > 0) {
            float t = (-b - sqrt(dis)) / (2.0 * a);
            if (t > min_t && t < max_t) {
                hit.t = t;
                hit.position = GetHitPoint(t);
                hit.normal = normalize(hit.position - sphere.position);
                hit.albedo = sphere.color;
                return true;
            }
        }
        return false;
        // hit is an inout parameter, will keep value when hit no thing.
    }
};

Ray CreateRay (float3 origin, float3 direction) {
    Ray ray = (Ray)0;
    ray.origin = origin;
    ray.direction = direction;
    ray.color = 1;
    return ray;
}

Ray CreateRay (float3 origin, float3 direction, float3 color) {
    Ray ray = (Ray)0;
    ray.origin = origin;
    ray.direction = direction;
    ray.color = color;
    return ray;
}

bool ScatterLambertian (Ray ray, RayHit hit, out Ray scattered_ray) {
    scattered_ray = CreateRay(
        hit.position + 0.001 * hit.normal,
        hit.normal + RandInUnitSphere (hit.normal),
        ray.color * hit.albedo * BOUNCE_RATIO
    );
    return true;
}

bool ScatterReflection (Ray ray, RayHit hit, out Ray scattered_ray) {
    // Fuzz should be a material parameter.
    const float fuzz = 0.01;

    float3 reflection = reflect(normalize(ray.direction), hit.normal);
    reflection = reflection + fuzz * RandInUnitSphere (hit.normal);

    bool outside = dot(reflection, hit.normal) > 0;
    if (outside) {
        scattered_ray = CreateRay(
            hit.position + 0.001 * hit.normal,
            reflection,
            ray.color * hit.albedo * BOUNCE_RATIO
        );
    } else {
        scattered_ray = (Ray)0;
    }
    return outside;
}

#endif // RAY_TRACING_STRUCT_INCLUDED