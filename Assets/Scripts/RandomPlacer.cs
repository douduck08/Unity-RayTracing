using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomPlacer : MonoBehaviour {

    [SerializeField] RayTracingSphere prefab;
    [SerializeField] int spawnNumber;
    [SerializeField] Vector3 minPos;
    [SerializeField] Vector3 maxPos;

    void Start () {
        Random.InitState (System.DateTime.Now.Millisecond);

        for (int i = 0; i < spawnNumber; i++) {
            var sphere = Instantiate (prefab, this.transform);
            var pos = new Vector3 ();
            pos.x = Random.Range (minPos.x, maxPos.x);
            pos.y = Random.Range (minPos.y, maxPos.y);
            pos.z = Random.Range (minPos.z, maxPos.z);
            sphere.transform.localPosition = pos;
            sphere.SetMaterial ((MaterialType)Random.Range (1, 3), Random.ColorHSV (0f, 1f, 0.9f, 1f, 0.5f, 1f), Random.Range (0f, 1f), Random.Range (0f, 1f));
        }
    }
}
