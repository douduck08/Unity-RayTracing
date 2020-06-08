using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracingBox : RayTracingShape {

    public override ShapeData GetShapeData () {
        ShapeData result;
        result.position = transform.position;
        result.scale = transform.lossyScale;
        result.rotation = transform.eulerAngles;
        result.type = (int)ShapeType.Box;
        material.GetStructData (out result.material);
        return result;
    }

    public override Aabb GetAabb () {
        var p0 = Abs (transform.TransformVector (new Vector3 (0.5f, 0.5f, 0.5f)));
        var p1 = Abs (transform.TransformVector (new Vector3 (-0.5f, 0.5f, 0.5f)));
        var p2 = Abs (transform.TransformVector (new Vector3 (0.5f, -0.5f, 0.5f)));
        var p3 = Abs (transform.TransformVector (new Vector3 (0.5f, 0.5f, -0.5f)));

        var pos = transform.position;
        var size = new Vector3 ();
        size.x = Mathf.Max (p0.x, p1.x, p2.x, p3.x);
        size.y = Mathf.Max (p0.y, p1.y, p2.y, p3.y);
        size.z = Mathf.Max (p0.z, p1.z, p2.z, p3.z);

        return new Aabb (pos - size, pos + size);
    }

    Vector3 Abs (Vector3 a) {
        a.x = Mathf.Abs (a.x);
        a.y = Mathf.Abs (a.y);
        a.z = Mathf.Abs (a.z);
        return a;
    }
}
