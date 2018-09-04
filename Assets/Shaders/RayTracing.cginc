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

float3 getHitPoint (Ray ray, float t) {
    return ray.origin + ray.direction * t;
}

bool hitSphere (Ray ray, SphereData sphere, out RayHit hit) {
    float3 oc = ray.origin - sphere.position;
    float a = dot(ray.direction, ray.direction);
    float b = dot(ray.direction, oc) * 2;
    float c = dot(oc, oc) - sphere.radius * sphere.radius;
    float dis = b * b - 4 * a * c;

    if (dis < 0) {
        return false;
    } else {
        hit.t = (-b - sqrt(dis)) / (2.0 * a);
        hit.position = getHitPoint(ray, hit.t);
        hit.normal = normalize(hit.position - sphere.position);
        return true;
    }
}

#endif // RAY_TRACING_INCLUDED