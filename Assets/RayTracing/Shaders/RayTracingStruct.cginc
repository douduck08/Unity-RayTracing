#ifndef RAY_TRACING_STRUCT_INCLUDED
#define RAY_TRACING_STRUCT_INCLUDED

#include "Common.cginc"

#define DIFFUSE_MATERIAL 1
#define GLOSSY_MATERIAL 2
#define TRANSLUCENT_MATERIAL 3

#define BOUNCE_RATIO _BounceRatio
float _BounceRatio;

struct SphereData {
    float3 position;
    float radius;
    float4 albedo;
    float4 specular;
    int material;
};

struct PlaneData {
    float3 position;
    float3 normal;
    float4 albedo;
    float4 specular;
    int material;
};

struct RayHit {
    float3 position;
    float t;
    float4 albedo;
    float4 specular;
    float3 normal;
    int material;
};

struct Ray {
    float3 origin;
    float3 direction;
    float3 color;
    float3 output;
    int count;

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
                hit.albedo = sphere.albedo;
                hit.specular = sphere.specular;
                hit.material = sphere.material;
                return true;
            }
        }
        return false; // hit is an inout parameter, will keep value when hit nothing.
    }

    bool HitPlane (PlaneData plane, float min_t, float max_t, inout RayHit hit) {
        float NDotD = dot(plane.normal, direction);
        if (NDotD < -1e-6) {
            float3 p0 = plane.position - origin;
            float t = dot(p0, plane.normal) / NDotD;
            if (t >= min_t && t < max_t) {
                hit.t = t;
                hit.position = GetHitPoint(t);
                hit.normal = plane.normal;
                hit.albedo = plane.albedo;
                hit.specular = plane.specular;
                hit.material = plane.material;
                return true;
            }
        }
        return false; // hit is an inout parameter, will keep value when hit nothing.
    }
};

Ray CreateRay (float3 origin, float3 direction) {
    Ray ray = (Ray)0;
    ray.origin = origin;
    ray.direction = normalize(direction);
    ray.color = 1;
    ray.output = 0;
    ray.count = 0;
    return ray;
}

Ray RedirectRay (float3 origin, float3 direction, float3 color, Ray old) {
    Ray ray = (Ray)0;
    ray.origin = origin;
    ray.direction = normalize(direction);
    ray.color = color;
    ray.output = old.output;
    ray.count = old.count;
    return ray;
}

bool ScatterLambertian (Ray ray, RayHit hit, out Ray scattered_ray) {
    scattered_ray = RedirectRay(
    hit.position + 0.001 * hit.normal,
    hit.normal + RandInUnitSphere (hit.normal + hit.position),
    ray.color * hit.albedo.rgb * BOUNCE_RATIO,
    ray
    );
    return true;
}

bool ScatterReflection (Ray ray, RayHit hit, out Ray scattered_ray) {
    float fuzz = hit.specular.a;
    float3 reflection = reflect(normalize(ray.direction), hit.normal);
    reflection = reflection + fuzz * RandInUnitSphere (hit.normal + hit.position);

    scattered_ray = RedirectRay(
    hit.position + 0.001 * hit.normal,
    reflection,
    ray.color * hit.specular.rgb * BOUNCE_RATIO,
    ray
    );
    return dot(reflection, hit.normal) > 0;
}

float Schlick(float cosine, float eta) {
    float r0 = (eta - 1) / (eta + 1);
    r0 = r0 * r0;
    return r0 + (1 - r0) * pow(1 - cosine, 5);
}

bool ScatterRefraction (Ray ray, RayHit hit, out Ray scattered_ray) {
    float eta = 1.5; // glass
    float3 normal = hit.normal;
    if (dot(ray.direction, normal) > 0) {
        normal = -normal; // refract outward
    }
    else {
        eta = 1 / eta; // refract inward
    }

    float cosine = -dot(ray.direction, normal) / length(ray.direction);
    float reflect_prob = Schlick(cosine, eta);

    float3 dir = normalize(ray.direction);
    float3 refraction = refract(-dir, normal, eta);
    float3 reflection = reflect(dir, hit.normal);
    if (!any(refraction)) {
        reflect_prob = 1;
    }

    if (abs(RandInUnitSphere (hit.normal + hit.position).x) > reflect_prob) {
        scattered_ray = RedirectRay(
        hit.position - 0.001 * normal,
        refraction,
        ray.color * hit.specular.rgb,
        ray
        );
    }
    else {
        scattered_ray = RedirectRay(
        hit.position + 0.001 * hit.normal,
        reflection,
        ray.color * hit.specular.rgb * BOUNCE_RATIO,
        ray
        );
    }
    return true;
}


bool Scatter (Ray ray, RayHit hit, out Ray scattered_ray) {
    if (hit.material == DIFFUSE_MATERIAL) {
        return ScatterLambertian(ray, hit, scattered_ray);
    }
    if (hit.material == GLOSSY_MATERIAL) {
        return ScatterReflection(ray, hit, scattered_ray);
    }
    if (hit.material == TRANSLUCENT_MATERIAL) {
        return ScatterRefraction(ray, hit, scattered_ray);
    }
    return false;
}

#endif // RAY_TRACING_STRUCT_INCLUDED