using UnityEngine;

public class RTS_Camera : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float rotateSpeed = 100f;
    public float zoomSpeed = 10f;
    public float minZoom = 10f;
    public float maxZoom = 80f;
    public float verticalSpeed = 10f;

    public Transform cameraHolder; // assign in inspector to the parent of your camera (to rotate/zoom)

    private void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleRotation();
    }

    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(h, 0f, v).normalized;
        transform.Translate(direction * moveSpeed * Time.deltaTime, Space.World);

        if (Input.GetKey(KeyCode.E))
            transform.Translate(Vector3.up * verticalSpeed * Time.deltaTime, Space.World);
        if (Input.GetKey(KeyCode.Q))
            transform.Translate(Vector3.down * verticalSpeed * Time.deltaTime, Space.World);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (cameraHolder != null)
        {
            float newY = Mathf.Clamp(cameraHolder.localPosition.y - scroll * zoomSpeed, minZoom, maxZoom);
            cameraHolder.localPosition = new Vector3(cameraHolder.localPosition.x, newY, cameraHolder.localPosition.z);
        }
    }

    void HandleRotation()
    {
        if (Input.GetMouseButton(1)) // Right Mouse Held
        {
            float mouseX = Input.GetAxis("Mouse X");
            transform.Rotate(0f, mouseX * rotateSpeed * Time.deltaTime, 0f, Space.World);
        }
    }
}