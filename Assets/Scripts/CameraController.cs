using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraController : MonoBehaviour
{
    private Vector2 turn;
    float cameraSpeed = 40f;
    bool shouldLockInput = false;

    private void Start()
    {
        ToggleCursor();
    }

    private void Update()
    {
        float vertical = Input.GetAxis("Vertical") * cameraSpeed * Time.deltaTime;
        float horizontal = Input.GetAxis("Horizontal") * cameraSpeed * Time.deltaTime;
        float mouseScrollInput = Input.GetAxis("Mouse ScrollWheel") * cameraSpeed * 100f * Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            shouldLockInput = !shouldLockInput;
            ToggleCursor();
        }
        if (!shouldLockInput)
        {
            transform.Translate(horizontal, vertical, mouseScrollInput);
            turn.x += Input.GetAxis("Mouse X");
            turn.y += Input.GetAxis("Mouse Y");
            transform.localRotation = Quaternion.Euler(-turn.y, turn.x, 0);
        }
    }

    private void ToggleCursor()
    {
        Cursor.lockState = shouldLockInput ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = shouldLockInput;
    }
}
