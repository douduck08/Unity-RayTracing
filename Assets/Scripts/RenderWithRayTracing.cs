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
    Vector4 cameraParameter;
    Matrix4x4 cameraCornerMat;

    void Start () {
        width = cameraCache.pixelWidth;
        height = cameraCache.pixelHeight;
        renderTarget = new RenderTexture (width, height, 0);
        renderTarget.enableRandomWrite = true;
        renderTarget.Create ();

        kernelID = rayTracingShader.FindKernel ("CSMain");
        rayTracingShader.SetTexture (kernelID, "result", renderTarget);

        cameraParameter = new Vector4 (width, height, 1f / width, 1f / height);
        rayTracingShader.SetVector ("cameraParameter", cameraParameter);

        CalculateFrustumCorners ();
        rayTracingShader.SetMatrix ("cameraCorner", cameraCornerMat);

        sphereBuffer = new ComputeBuffer (2, 16);
        rayTracingShader.SetBuffer (kernelID, "sphereBuffer", sphereBuffer);

        // tmp
        var data = new SphereData[2];
        data[0].position = new Vector3 (0, 0, 5);
        data[0].radius = 1;
        data[1].position = new Vector3 (0, -31, 5);
        data[1].radius = 30;
        sphereBuffer.SetData (data);
    }

    void OnPreRender () {
        rayTracingShader.Dispatch (kernelID, width / 8, height / 8, 1);
    }

    void OnRenderImage (RenderTexture source, RenderTexture dest) {
        Graphics.Blit (renderTarget, dest);
    }

    void OnDrawGizmos () {
        CalculateFrustumCorners ();
        for (int i = 0; i < 4; i++) {
            var worldSpaceCorner = transform.TransformVector (frustumCorners[i]);
            Debug.DrawRay (transform.position, worldSpaceCorner, Color.blue);
        }
    }

    void CalculateFrustumCorners () {
        cameraCache.CalculateFrustumCorners (new Rect (0, 0, 1, 1), cameraCache.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
        cameraCornerMat.SetRow (0, Vector3.Normalize (frustumCorners[0]));
        cameraCornerMat.SetRow (1, Vector3.Normalize (frustumCorners[2]) - Vector3.Normalize (frustumCorners[1]));
        cameraCornerMat.SetRow (2, Vector3.Normalize (frustumCorners[1]) - Vector3.Normalize (frustumCorners[0]));
    }

}