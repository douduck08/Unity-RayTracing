#ifndef RAY_TRACING_INCLUDED
#define RAY_TRACING_INCLUDED

#include "Common.cginc"
#include "RayTracingStruct.cginc"

#define SPHERE_SHAPE 1
#define BOX_SHAPE 2
#define PLANE_SHAPE 3

#define DIFFUSE_MATERIAL 1
#define GLOSSY_MATERIAL 2
#define TRANSLUCENT_MATERIAL 3
#define VOLUME_MATERIAL 9
#define LIGHT_MATERIAL 10

#define BOUNCE_RATIO _BounceRatio
float _BounceRatio;

bool SphereIntersection (float3 origin, float3 direction, float min_t, float max_t, float radius, out float out_t, out float far_t, out float3 out_normal) {
    float3 oc = origin; // oc = o - center, center = 0
    float a = dot(direction, direction);
    float b = dot(direction, oc) * 2;
    float c = dot(oc, oc) - radius * radius;
    float dis = b * b - 4 * a * c;

    if (dis <= 0) {
        out_t = 0;
        out_normal = 0;
        return false;
    }

    dis = sqrt(dis);
    float t = (-b - dis) / (2.0 * a);
    if (t < min_t || t > max_t) {
        out_t = 0;
        out_normal = 0;
        return false;
    }

    out_t = t;
    far_t = (-b + dis) / (2.0 * a);
    out_normal = normalize(origin + direction * t);
    return true;
}

bool BoxIntersection (float3 origin, float3 direction, float min_t, float max_t, float3 half_size, out float out_t, out float far_t, out float3 out_normal) {
    // ref: https://www.iquilezles.org/www/articles/boxfunctions/boxfunctions.htm
    float3 m = 1.0 / direction;
    float3 n = m * origin;
    float3 k = abs(m) * half_size;
    float3 t1 = -n - k;
    float3 t2 = -n + k;

    float tN = max(max(t1.x, t1.y), t1.z);
    float tF = min(min(t2.x, t2.y), t2.z);

    if (tN > tF || tF < min_t || tN > max_t) {
        out_t = 0;
        out_normal = 0;
        return false;
    }

    out_t = tN;
    far_t = tF;
    out_normal = -sign(direction) * step(t1.yzx, t1.xyz) * step(t1.zxy, t1.xyz);
    return true;
}

bool PlaneIntersection (float3 origin, float3 direction, float min_t, float max_t, float3 normal, out float out_t, out float3 out_normal) {
    float NDotD = dot(normal, direction);
    if (NDotD < -1e-6) {
        float3 p0 = -origin;
        float t = dot(p0, normal) / NDotD;
        if (t >= min_t && t < max_t) {
            out_t = t;
            out_normal = normal;
            return true;
        }
    }

    out_t = 0;
    out_normal = 0;
    return false;
}

bool ShapeData::Raycast (Ray ray, float min_t, float max_t, inout RayHit hitInfo, TransformData transformData) {
    float4x4 objectToWorld = transformData.ObjectToWorld();
    float4x4 worldToObject = transformData.WorldToObject();

    float3 o = mul(worldToObject, float4(ray.origin, 1.0)).xyz; // world to object space
    float3 d = mul((float3x3)worldToObject, ray.direction);

    float out_t = 0, far_t = 0;
    float3 out_normal = 0;
    bool hit = false;

    if (type == SPHERE_SHAPE) {
        hit = SphereIntersection(o, d, min_t, max_t, 0.5, out_t, far_t, out_normal);
    }
    else if (type == BOX_SHAPE) {
        hit = BoxIntersection(o, d, min_t, max_t, 0.5, out_t, far_t, out_normal);
    }
    else if (type == PLANE_SHAPE) {
        // hit = BoxIntersection(o, d, min_t, max_t, float3(5, 0.001, 5), out_t, far_t, out_normal);
        hit = PlaneIntersection(o, d, min_t, max_t, float3(0, 1, 0), out_t, out_normal);
        far_t = out_t;
    }

    if(hit && material.type == VOLUME_MATERIAL) {
        float density = material.albedo.w;
        float dt = -(1.0 / density) * log(Rand01(out_normal + d));
        if (dt < far_t - out_t) {
            out_t += dt;
        }
        else {
            hit = false;
        }
    }

    if (hit) {
        out_normal = mul((float3x3)objectToWorld, out_normal);
        out_normal = normalize(out_normal);

        hitInfo.t = out_t;
        hitInfo.position = ray.GetHitPoint(out_t); // world space
        hitInfo.normal = out_normal;
        hitInfo.albedo = material.albedo;
        hitInfo.specular = material.specular;
        hitInfo.material = material.type;
    }
    return hit;
}

bool ScatterLambertian (Ray ray, RayHit hit, out Ray scattered_ray) {
    scattered_ray = CreateRay(
    hit.position + 0.001 * hit.normal,
    hit.normal + RandInUnitSphere(hit.normal + hit.position),
    ray.color * hit.albedo.rgb * BOUNCE_RATIO,
    ray.emission
    );
    return true;
}

bool ScatterReflection (Ray ray, RayHit hit, out Ray scattered_ray) {
    float fuzz = hit.specular.a;
    float3 reflection = reflect(normalize(ray.direction), hit.normal);
    reflection = reflection + fuzz * RandInUnitSphere(hit.normal + hit.position);

    scattered_ray = CreateRay(
    hit.position + 0.001 * hit.normal,
    reflection,
    ray.color * hit.specular.rgb * BOUNCE_RATIO,
    ray.emission
    );
    return dot(reflection, hit.normal) > 0;
}

float Schlick(float cosine, float eta) {
    float r0 = (eta - 1) / (eta + 1);
    r0 = r0 * r0;
    return r0 + (1 - r0) * pow(1 - cosine, 5);
}

bool ScatterRefraction (Ray ray, RayHit hit, out Ray scattered_ray) {
    float eta = hit.albedo.w;
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

    if (Rand01(hit.normal + hit.position) > reflect_prob) {
        scattered_ray = CreateRay(
        hit.position - 0.001 * normal,
        refraction,
        ray.color * hit.albedo.rgb,
        ray.emission
        );
    }
    else {
        scattered_ray = CreateRay(
        hit.position + 0.001 * hit.normal,
        reflection,
        ray.color * hit.specular.rgb * BOUNCE_RATIO,
        ray.emission
        );
    }
    return true;
}

bool HitLight (Ray ray, RayHit hit, out Ray scattered_ray) {
    scattered_ray = CreateRay(
    0,
    0,
    ray.color * hit.albedo.rgb * BOUNCE_RATIO,
    ray.emission + ray.color * hit.albedo.rgb * hit.albedo.a
    );
    return true;
}

bool ScatterVolume (Ray ray, RayHit hit, out Ray scattered_ray) {
    scattered_ray = CreateRay(
    hit.position,
    RandInUnitSphere(hit.normal + hit.position),
    ray.color * hit.albedo.rgb,
    ray.emission
    );
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
    if (hit.material == VOLUME_MATERIAL) {
        return ScatterVolume(ray, hit, scattered_ray);
    }
    if (hit.material == LIGHT_MATERIAL) {
        return HitLight(ray, hit, scattered_ray);
    }
    return false;
}

#endif // RAY_TRACING_INCLUDED
