using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Camera))]
public class RenderWithRayTracing : MonoBehaviour {

    Camera mainCamera;
    RenderTexture renderTarget;
    Vector3[] frustumCorners = new Vector3[4];

    [SerializeField] ComputeShader rayTracingShader;
    int kernelID = 0;
    ComputeBuffer sphereBuffer;

    void OnEnable () {
        mainCamera = GetComponent<Camera> ();
        renderTarget = CreateRenderTarget (mainCamera.pixelWidth, mainCamera.pixelHeight);
        rayTracingShader.SetTexture (kernelID, "result", renderTarget);

        sphereBuffer = new ComputeBuffer (1000, RayTracingObjectDataSize.Sphere);
        rayTracingShader.SetBuffer (kernelID, "sphereBuffer", sphereBuffer);
    }

    void OnDisable () {
        if (sphereBuffer != null) {
            sphereBuffer.Release ();
        }
    }

    void OnPreRender () {
        rayTracingShader.SetVector ("cameraParameter", GetCameraParameter (mainCamera.pixelWidth, mainCamera.pixelHeight));
        rayTracingShader.SetMatrix ("cameraCorner", GetCameraCorner ());

        rayTracingShader.SetInt ("sphereNumber", RayTracingObjectManager.instance.sphereNumber);
        sphereBuffer.SetData (RayTracingObjectManager.instance.sphereDataArray);

        rayTracingShader.Dispatch (kernelID, Mathf.CeilToInt (mainCamera.pixelWidth / 8.0f), Mathf.CeilToInt (mainCamera.pixelHeight / 8.0f), 1);
    }

    void OnRenderImage (RenderTexture source, RenderTexture dest) {
        Graphics.Blit (renderTarget, dest);
    }

    void OnDrawGizmos () {
        for (int i = 0; i < 4; i++) {
            var worldSpaceCorner = transform.TransformVector (frustumCorners[i]);
            Debug.DrawRay (transform.position, worldSpaceCorner, Color.blue);
        }
    }

    RenderTexture CreateRenderTarget (int width, int height) {
        var rt = new RenderTexture (width, height, 0);
        rt.enableRandomWrite = true;
        rt.Create ();
        return rt;
    }

    Vector4 GetCameraParameter (int width, int height) {
        return new Vector4 (width, height, 1f / width, 1f / height);
    }

    Matrix4x4 GetCameraCorner () {
        mainCamera.CalculateFrustumCorners (new Rect (0, 0, 1, 1), mainCamera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);

        Matrix4x4 cameraCorner = new Matrix4x4 ();
        cameraCorner.SetRow (0, transform.position);
        cameraCorner.SetRow (1, Vector3.Normalize (frustumCorners[0]));
        cameraCorner.SetRow (2, Vector3.Normalize (frustumCorners[2]) - Vector3.Normalize (frustumCorners[1]));
        cameraCorner.SetRow (3, Vector3.Normalize (frustumCorners[1]) - Vector3.Normalize (frustumCorners[0]));

        return cameraCorner;
    }
}