using UnityEngine;

/// <summary>
/// Basic player controllable camera.
/// </summary>
public class CameraController : MonoBehaviour
{
    private Vector2 turn;
    private float cameraSpeed = 60f;
    private bool shouldLockInput = false;
    private float verticalInput;
    private float horizontalInput;
    private float mouseScrollInput;

    private void Start()
    {
        // Locks cursor at start.
        ToggleCursor();
    }

    private void Update()
    {
        GetCameraInput();
        HandleCameraMovement();
    }

    /// <summary>
    /// Gets camera input from player.
    /// </summary>
    private void GetCameraInput()
    {
        verticalInput = Input.GetAxis("Vertical") * cameraSpeed * Time.deltaTime;
        horizontalInput = Input.GetAxis("Horizontal") * cameraSpeed * Time.deltaTime;
        mouseScrollInput = Input.GetAxis("Mouse ScrollWheel") * cameraSpeed * 100f * Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleLock();
        }
    }

    /// <summary>
    /// Handles camera movement based on input.
    /// </summary>
    private void HandleCameraMovement()
    {
        // If the cursor isn't locked, the player can move camera.
        if (!shouldLockInput)
        {
            transform.Translate(horizontalInput, verticalInput, mouseScrollInput);
            turn.x += Input.GetAxis("Mouse X");
            turn.y += Input.GetAxis("Mouse Y");
            transform.localRotation = Quaternion.Euler(-turn.y, turn.x, 0);
        }
    }

    /// <summary>
    /// Toggles lock state for input.
    /// </summary>
    private void ToggleLock()
    {
        shouldLockInput = !shouldLockInput;
        ToggleCursor();
    }

    /// <summary>
    /// Toggles cursor state.
    /// </summary>
    private void ToggleCursor()
    {
        Cursor.lockState = shouldLockInput ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = shouldLockInput;
    }
}
