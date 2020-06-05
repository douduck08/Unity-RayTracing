using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Aabb {
    public static readonly int Stride = 6 * sizeof (float);

    public Vector3 min;
    public Vector3 max;

    public Vector3 center {
        get {
            return (min + max) * 0.5f;
        }
    }

    public Vector3 size {
        get {
            return (max - min);
        }
    }

    public Aabb (Vector3 min, Vector3 max) {
        this.min = min;
        this.max = max;
    }

    public static Aabb Union (Aabb a, Aabb b) {
        return new Aabb (Vector3.Min (a.min, b.min), Vector3.Max (a.max, b.max));
    }
}

public class AabbTree {

    public struct AabbNode {
        public Aabb aabb;
        public int parent;
        public int childA;
        public int childB;
        public int dataId;
    }

    List<AabbNode> nodes = new List<AabbNode> ();

    public void Build (List<RayTracingShape> objectList) {
        nodes.Clear ();
        for (int i = 0; i < objectList.Count; i++) {
            var node = new AabbNode ();
            node.aabb = objectList[i].GetAabb ();
            nodes.Add (node);
        }
    }

    public void Build (List<RayTracingBox> objectList) {
        // nodes.Clear ();
        // for (int i = 0; i < objectList.Count; i++) {
        //     var bounds = objectList[i].GetBounds ();
        //     var node = new AabbNode ();
        //     node.aabb = new Aabb (bounds.min, bounds.max);
        //     nodes.Add (node);
        // }
    }

    public void DrawGizmos () {
        Gizmos.color = new Color (1f, 0f, 0f, 0.5f);
        for (int i = 0; i < nodes.Count; i++) {
            Gizmos.DrawWireCube (nodes[i].aabb.center, nodes[i].aabb.size);
        }
    }

    // TODO:
    // void Insert (int id, Aabb aabb) { }
    // void Remove (int id) { }
    // void Balance () { }
}
