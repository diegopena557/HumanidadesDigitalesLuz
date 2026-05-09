using UnityEngine;
using UnityEngine.InputSystem;

public class CameraOrbit : MonoBehaviour
{
    public Transform target;
    public float distance = 5f;
    public float rotationSpeed = 0.2f;

    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    private Vector2 lastMouseDelta;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        rotationX = angles.x;
        rotationY = angles.y;
    }

    void LateUpdate()
    {
        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            rotationY += mouseDelta.x * rotationSpeed;
            rotationX -= mouseDelta.y * rotationSpeed;

            rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);
        }

        Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0);
        Vector3 position = target.position - (rotation * Vector3.forward * distance);

        transform.position = position;
        transform.rotation = rotation;
    }
}