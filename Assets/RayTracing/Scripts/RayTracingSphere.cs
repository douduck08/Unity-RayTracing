using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracingSphere : RayTracingShape {

    public override ShapeData GetShapeData () {
        ShapeData result;
        result.position = transform.position;
        result.scale = transform.lossyScale;
        result.rotation = transform.eulerAngles;
        result.type = (int)ShapeType.Sphere;
        material.GetStructData (out result.material);
        return result;
    }

    public override Aabb GetAabb () {
        // TODO: bounds with scale
        var pos = transform.position;
        var size = transform.lossyScale * 0.5f;
        return new Aabb (transform.position - size, transform.position + size);
    }
}