using UnityEngine;
using UnityEngine.EventSystems;  // Add this for detecting UI interaction

public class CameraController : MonoBehaviour
{
    float rotationSpeed = 100f;
    float translationSpeed = 10f;
    float zoomSpeed = 10f;
    float minFOV = 15f;
    float maxFOV = 90f;
    float smoothTime = 0.4f;
    float rotationX;
    float rotationY;

    Vector3 originalPosition;
    Quaternion originalRotation;
    Camera cam;
    Vector3 targetPosition;
    Vector3 velocity = Vector3.zero;
    Quaternion targetRotation;


    void Start()
    {
        cam = GetComponent<Camera>();

        targetPosition = transform.position;
        targetRotation = transform.rotation;

        originalPosition = transform.position;
        originalRotation = transform.rotation;

        rotationX = transform.eulerAngles.y;
        rotationY = transform.eulerAngles.x;
    }

    void Update()
    {

        if (Application.isMobilePlatform)
            TouchIn();
        else
            MouseIn();

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime / smoothTime * 10f);
    }

    void MouseIn()
    {
        if (Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

            rotationX += mouseX;
            rotationY -= mouseY;
            rotationY = Mathf.Clamp(rotationY, -90f, 90f);

            targetRotation = Quaternion.Euler(rotationY, rotationX, 0f);

            Vector3 direction = Vector3.zero;

            if (Input.GetKey(KeyCode.W)) direction += transform.forward;
            if (Input.GetKey(KeyCode.S)) direction -= transform.forward;
            if (Input.GetKey(KeyCode.A)) direction -= transform.right;
            if (Input.GetKey(KeyCode.D)) direction += transform.right;
            if (Input.GetKey(KeyCode.Q)) direction -= transform.up;
            if (Input.GetKey(KeyCode.E)) direction += transform.up;

            if (direction != Vector3.zero)
            {
                direction.Normalize();
                targetPosition += direction * translationSpeed * Time.deltaTime;
            }
        }

        if (Input.GetKeyDown(KeyCode.F)) ResetCameraPosition();

        float scroll = Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - scroll, minFOV, maxFOV);
    }

    void ResetCameraPosition()
    {
        targetPosition = originalPosition;
        targetRotation = originalRotation;
    }

    void TouchIn()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                float touchX = touch.deltaPosition.x * rotationSpeed * 0.01f;
                float touchY = touch.deltaPosition.y * rotationSpeed * 0.01f;

                rotationX += touchX;
                rotationY -= touchY;
                rotationY = Mathf.Clamp(rotationY, -90f, 90f);

                targetRotation = Quaternion.Euler(rotationY, rotationX, 0f);
            }
        }

        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            float prevTouchDeltaMag = (touch1.position - touch1.deltaPosition - (touch2.position - touch2.deltaPosition)).magnitude;
            float touchDeltaMag = (touch1.position - touch2.position).magnitude;

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView + deltaMagnitudeDiff * zoomSpeed * 0.01f, minFOV, maxFOV);
        }
    }
}