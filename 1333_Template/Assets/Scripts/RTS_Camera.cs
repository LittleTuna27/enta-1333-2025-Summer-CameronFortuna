using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTS_Camera : MonoBehaviour
{
    public float moveSpeed = 10f; // Speed of camera movement
    public float rotateSpeed = 100f; // Speed of camera rotation
    public float zoomSpeed = 2f; // Speed of zooming in and out
    public float minZoom = 5f; // Minimum zoom limit
    public float maxZoom = 30f; // Maximum zoom limit
    public float verticalSpeed = 5f; // Speed of camera vertical movement

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        // Move the camera with WASD or arrow keys
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);

        // Move the camera up and down with Q and E keys
        if (Input.GetKey(KeyCode.LeftControl))
        {
            transform.Translate(Vector3.down * verticalSpeed * Time.deltaTime, Space.World);
        }
        if (Input.GetKey(KeyCode.Space))
        {
            transform.Translate(Vector3.up * verticalSpeed * Time.deltaTime, Space.World);
        }

        // Zoom in and out using the mouse scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - scroll * zoomSpeed, minZoom, maxZoom);

        // Rotate the camera with right mouse button drag
        if (Input.GetMouseButton(1)) // Right mouse button
        {
            float rotation = Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
            transform.Rotate(0, rotation, 0, Space.World);
        }
    }
}

