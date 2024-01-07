using UnityEngine;

public class PlayerCanvasLogic : MonoBehaviour
{
    // The camera the canvas should face
    private Transform cameraTransform;

    // Set the target camera
    public void SetTargetCamera(Transform newTargetCamera)
    {
        cameraTransform = newTargetCamera;
    }

    void Update()
    {
        // Check if a target camera is set
        if (cameraTransform != null)
        {
            // Make the canvas face the camera
            transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward,
                             cameraTransform.rotation * Vector3.up);
        }
        else
        {
            Debug.LogWarning("No target camera set for the BillboardCanvas script.");
        }
    }
}
