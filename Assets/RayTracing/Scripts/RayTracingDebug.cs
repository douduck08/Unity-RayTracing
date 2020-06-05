using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracingDebug : MonoBehaviour {

    [SerializeField] bool drawBounds;

    void Update () {

    }

    void OnDrawGizmos () {
        if (drawBounds) {
            RayTracingShape.DrawGizmos ();
        }
    }
}
