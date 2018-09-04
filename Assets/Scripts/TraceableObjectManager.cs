using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SphereData {
    public Vector3 position;
    public float radius;
}

public class TraceableObjectManager {

    static TraceableObjectManager _instance;
    public static TraceableObjectManager instance {
        get {
            if (_instance == null) {
                _instance = new TraceableObjectManager ();
            }
            return _instance;
        }
    }

    TraceableObjectManager () { }

    public static void AddObject () {

    }

    public static void RemoveObject () {

    }

}