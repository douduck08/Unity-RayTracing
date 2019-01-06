using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracingSphere : MonoBehaviour {

    public Color color = Color.red;
    public float radius = 1;

    void OnDrawGizmos () {
        Gizmos.color = color;
        Gizmos.DrawSphere (transform.position, radius);
    }

    void OnEnable () {
        RayTracingObjectManager.instance.AddObject (this);
    }

    void OnDisable () {
        RayTracingObjectManager.instance.RemoveObject (this);
    }
}