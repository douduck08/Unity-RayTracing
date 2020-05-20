#ifndef RAY_TRACING_STRUCT_INCLUDED
#define RAY_TRACING_STRUCT_INCLUDED

// struct TransformData {
    //     float4 objectToWorldRow0;
    //     float4 objectToWorldRow1;
    //     float4 objectToWorldRow2;
    //     float4 worldToObjectRow0;
    //     float4 worldToObjectRow1;
    //     float4 worldToObjectRow2;

    //     float4x4 ObjectToWorldMatix() {
        //         return float4x4(objectToWorldRow0, objectToWorldRow1, objectToWorldRow2, float4(0, 0, 0, 1));
    //     }

    //     float4x4 WorldToObjectMatix() {
        //         return float4x4(worldToObjectRow0, worldToObjectRow1, worldToObjectRow2, float4(0, 0, 0, 1));
    //     }
// };

struct TransformData {
    float4 row0;
    float4 row1;
    float4 row2;
    float4 row3;
    float4 row4;
    float4 row5;

    float4x4 ObjectToWorld() {
        return float4x4(row0, row1, row2, float4(0, 0, 0, 1));
    }

    float4x4 WorldToObject() {
        return float4x4(row3, row4, row5, float4(0, 0, 0, 1));
    }
};

struct Ray {
    float3 origin;
    float3 direction;
    float3 color;
    float3 emission;
    float3 output;
    int count;

    float3 GetHitPoint (float t) {
        return origin + direction * t;
    }

    // bool AABB(float3 min_pos, float3 max_pos, float min_t, float max_t) {
        //     for (int i = 0; i < 3; i++) {
            //         float t0 = min((min_pos[i] - origin[i]) / direction[i], (max_pos[i] - origin[i]) / direction[i]);
            //         float t1 = max((min_pos[i] - origin[i]) / direction[i], (max_pos[i] - origin[i]) / direction[i]);
            //         min_t = max(min_t, t0);
            //         max_t = min(max_t, t0);
            //         if (max_t < min_t) return false;
        //     }
        //     return true;
    // }
};

Ray CreateRay (float3 origin, float3 direction) {
    Ray ray = (Ray)0;
    ray.origin = origin;
    ray.direction = normalize(direction);
    ray.color = 1;
    ray.emission = 0;
    ray.output = 0;
    ray.count = 0;
    return ray;
}

Ray RedirectRay (float3 origin, float3 direction, float3 color, Ray old) {
    Ray ray = (Ray)0;
    ray.origin = origin;
    ray.direction = normalize(direction);
    ray.color = color;
    ray.emission = old.emission;
    ray.output = old.output;
    ray.count = old.count;
    return ray;
}

struct RayHit {
    float3 position;
    float t;
    float4 albedo;
    float4 specular;
    float3 normal;
    int material;
};

struct SphereData {
    float3 position;
    float radius;
    float4 albedo;
    float4 specular;
    int material;

    bool Raycast (Ray ray, float min_t, float max_t, inout RayHit hit);
};

struct PlaneData {
    float3 position;
    float3 normal;
    float4 albedo;
    float4 specular;
    int material;

    bool Raycast (Ray ray, float min_t, float max_t, inout RayHit hit);
};

struct BoxData {
    float3 position;
    float3 rotation;
    float3 scale;
    float4 albedo;
    float4 specular;
    int material;

    bool Raycast (Ray ray, float min_t, float max_t, inout RayHit hit, TransformData transformData);
};

#endif // RAY_TRACING_STRUCT_INCLUDED