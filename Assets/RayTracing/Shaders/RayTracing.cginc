#ifndef RAY_TRACING_INCLUDED
#define RAY_TRACING_INCLUDED

#include "Common.cginc"
#include "RayTracingStruct.cginc"

#define DIFFUSE_MATERIAL 1
#define GLOSSY_MATERIAL 2
#define TRANSLUCENT_MATERIAL 3
#define VOLUME_MATERIAL 9
#define LIGHT_MATERIAL 10

#define BOUNCE_RATIO _BounceRatio
float _BounceRatio;

bool SphereData::Raycast (Ray ray, float min_t, float max_t, inout RayHit hit) {
    float3 oc = ray.origin - position;
    float a = dot(ray.direction, ray.direction);
    float b = dot(ray.direction, oc) * 2;
    float c = dot(oc, oc) - this.radius * this.radius;
    float dis = b * b - 4 * a * c;

    if (dis > 0) {
        float t = (-b - sqrt(dis)) / (2.0 * a);
        if (t > min_t && t < max_t) {
            hit.t = t;
            hit.position = ray.GetHitPoint(t);
            hit.normal = normalize(hit.position - position);
            hit.albedo = albedo;
            hit.specular = specular;
            hit.material = material;
            return true;
        }
    }
    return false; // hit is an inout parameter, will keep value when hit nothing.
}

bool PlaneData::Raycast (Ray ray, float min_t, float max_t, inout RayHit hit) {
    float NDotD = dot(normal, ray.direction);
    if (NDotD < -1e-6) {
        float3 p0 = position - ray.origin;
        float t = dot(p0, normal) / NDotD;
        if (t >= min_t && t < max_t) {
            hit.t = t;
            hit.position = ray.GetHitPoint(t);
            hit.normal = normal;
            hit.albedo = albedo;
            hit.specular = specular;
            hit.material = material;
            return true;
        }
    }
    return false; // hit is an inout parameter, will keep value when hit nothing.
}

bool BoxData::Raycast (Ray ray, float min_t, float max_t, inout RayHit hit, TransformData transformData) {
    // ref: https://www.iquilezles.org/www/articles/boxfunctions/boxfunctions.htm
    // float4x4 xRotationMatrix = rotationMatrix(float3(1, 0, 0), radians(rotation.x));
    // float4x4 yRotationMatrix = rotationMatrix(float3(0, 1, 0), radians(rotation.y));
    // float4x4 zRotationMatrix = rotationMatrix(float3(0, 0, 1), radians(rotation.z));
    // float4x4 rotMatrix = mul(yRotationMatrix, mul(zRotationMatrix, xRotationMatrix)); // objectToWorld

    // float4x4 translation = {
        //     scale.x, 0, 0, position.x,
        //     0, scale.y, 0, position.y,
        //     0, 0, scale.z, position.z,
        //     0, 0, 0, 1
    // };

    // float4x4 objectToWorld = mul(translation, rotMatrix);
    // float4x4 worldToObject = inverse(objectToWorld);

    float4x4 objectToWorld = transformData.ObjectToWorld();
    float4x4 worldToObject = transformData.WorldToObject();
    float3 o = mul(worldToObject, float4(ray.origin, 1.0)).xyz; // world to object space
    float3 d = mul(worldToObject, float4(ray.direction, 0.0)).xyz;

    float3 m = 1.0 / d;
    float3 n = m * o;
    float3 k = abs(m) / 2.0;
    float3 t1 = -n - k;
    float3 t2 = -n + k;

    float tN = max(max(t1.x, t1.y), t1.z);
    float tF = min(min(t2.x, t2.y), t2.z);

    if (tN > tF || tF < min_t || tN > max_t) {
        return false;
    }

    float3 out_normal = -sign(d) * step(t1.yzx, t1.xyz) * step(t1.zxy, t1.xyz);
    out_normal = mul(objectToWorld, float4(out_normal, 0.0)).xyz;

    if(material == VOLUME_MATERIAL) {
        // TODO: check if outside box
        float density = 0.1;
        float dt = -(1.0 / density) * log(Rand01(out_normal + d));
        if (dt < 1) {
            tN += dt * (tF - tN);
        }
        else {
            return false;
        }
    }

    hit.t = tN;
    hit.position = ray.GetHitPoint(tN); // world space
    hit.normal = out_normal;
    hit.albedo = albedo;
    hit.specular = specular;
    hit.material = material;
    return true;
}

bool ScatterLambertian (Ray ray, RayHit hit, out Ray scattered_ray) {
    scattered_ray = RedirectRay(
    hit.position + 0.001 * hit.normal,
    hit.normal + RandInUnitSphere(hit.normal + hit.position),
    ray.color * hit.albedo.rgb * BOUNCE_RATIO,
    ray
    );
    return true;
}

bool ScatterReflection (Ray ray, RayHit hit, out Ray scattered_ray) {
    float fuzz = hit.specular.a;
    float3 reflection = reflect(normalize(ray.direction), hit.normal);
    reflection = reflection + fuzz * RandInUnitSphere(hit.normal + hit.position);

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
        scattered_ray = RedirectRay(
        hit.position - 0.001 * normal,
        refraction,
        ray.color * hit.albedo.rgb,
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

bool HitLight (Ray ray, RayHit hit, out Ray scattered_ray) {
    // ray.emission = ray.color * hit.albedo.rgb;
    ray.emission += ray.color * hit.albedo.rgb;
    // TODO
    scattered_ray = RedirectRay(
    0,
    0,
    ray.color * BOUNCE_RATIO,
    ray
    );
    return true;
}

bool ScatterVolume (Ray ray, RayHit hit, out Ray scattered_ray) {
    scattered_ray = RedirectRay(
    hit.position,
    RandInUnitSphere(hit.normal + hit.position),
    ray.color * hit.albedo.rgb,
    ray
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
