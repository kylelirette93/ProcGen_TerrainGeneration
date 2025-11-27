using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraController : MonoBehaviour
{
    private Vector2 turn;
    float cameraSpeed = 40f;
    bool isLocked = false;
    private void Update()
    {
        float vertical = Input.GetAxis("Vertical") * cameraSpeed * Time.deltaTime;
        float horizontal = Input.GetAxis("Horizontal") * cameraSpeed * Time.deltaTime;
        float mouseScrollInput = Input.GetAxis("Mouse ScrollWheel") * cameraSpeed * 100f * Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isLocked = !isLocked;
            Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isLocked;
        }
        if (!isLocked)
        {
            transform.Translate(horizontal, vertical, mouseScrollInput);
            turn.x += Input.GetAxis("Mouse X");
            turn.y += Input.GetAxis("Mouse Y");
            transform.localRotation = Quaternion.Euler(-turn.y, turn.x, 0);
        }
    }
}
