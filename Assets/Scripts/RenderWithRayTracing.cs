using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Camera))]
public class RenderWithRayTracing : MonoBehaviour {

    public ComputeShader rayTracingShader;
    int kernelID;

    ComputeBuffer sphereBuffer;

    RenderTexture renderTarget;
    int width;
    int height;

    new Camera camera;
    Camera cameraCache {
        get {
            if (camera == null) {
                camera = GetComponent<Camera> ();
            }
            return camera;
        }
    }
    Vector3[] frustumCorners = new Vector3[4];
    Matrix4x4 cameraFrustumData;
    Vector4 cameraParameter;

    void Start () {
        width = cameraCache.pixelWidth;
        height = cameraCache.pixelHeight;
        renderTarget = new RenderTexture (width, height, 0);
        renderTarget.enableRandomWrite = true;
        renderTarget.Create ();

        kernelID = 0;
        rayTracingShader.SetTexture (kernelID, "result", renderTarget);

        CalculateCameraParameter ();
        rayTracingShader.SetVector ("cameraParameter", cameraParameter);

        int sphereNumber = 4; // tmp
        sphereBuffer = new ComputeBuffer (sphereNumber, 32);
        rayTracingShader.SetInt ("sphereNumber", sphereNumber);
        rayTracingShader.SetBuffer (kernelID, "sphereBuffer", sphereBuffer);

        // tmp
        var data = new SphereData[sphereNumber];
        data[0].position = new Vector3 (0, 0, 5);
        data[0].radius = 1;
        data[0].color = Color.red;
        data[1].position = new Vector3 (0, -51, 5);
        data[1].radius = 50;
        data[1].color = Color.white;
        data[2].position = new Vector3 (-2, 0, 5);
        data[2].radius = 1;
        data[2].color = Color.green;
        data[3].position = new Vector3 (2, 0, 5);
        data[3].radius = 1;
        data[3].color = Color.blue;
        sphereBuffer.SetData (data);
    }

    void OnDestroy () {
        if (sphereBuffer != null) {
            sphereBuffer.Release ();
        }
    }

    void OnPreRender () {
        CalculateFrustumData ();
        rayTracingShader.SetMatrix ("cameraCorner", cameraFrustumData);
        rayTracingShader.Dispatch (kernelID, Mathf.CeilToInt (width / 8.0f), Mathf.CeilToInt (height / 8.0f), 1);
    }

    void OnRenderImage (RenderTexture source, RenderTexture dest) {
        Graphics.Blit (renderTarget, dest);
    }

    void OnDrawGizmos () {
        CalculateFrustumData ();
        for (int i = 0; i < 4; i++) {
            var worldSpaceCorner = transform.TransformVector (frustumCorners[i]);
            Debug.DrawRay (transform.position, worldSpaceCorner, Color.blue);
        }
    }

    void CalculateCameraParameter () {
        cameraParameter = new Vector4 (width, height, 1f / width, 1f / height);
    }

    void CalculateFrustumData () {
        cameraCache.CalculateFrustumCorners (new Rect (0, 0, 1, 1), cameraCache.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        cameraFrustumData.SetRow (0, transform.position);
        cameraFrustumData.SetRow (1, Vector3.Normalize (frustumCorners[0]));
        cameraFrustumData.SetRow (2, Vector3.Normalize (frustumCorners[2]) - Vector3.Normalize (frustumCorners[1]));
        cameraFrustumData.SetRow (3, Vector3.Normalize (frustumCorners[1]) - Vector3.Normalize (frustumCorners[0]));
    }

}