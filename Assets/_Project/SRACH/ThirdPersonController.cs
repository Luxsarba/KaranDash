using UnityEngine;

public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Jump & Gravity")]
    public float jumpForce = 2.5f;
    public float gravity = -9.81f;

    [Header("Climb")]
    public float climbSpeed = 3f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 3f;
    public Transform cameraPivot;

    private CharacterController controller;
    private float cameraPitch;
    private float verticalVelocity;

    private bool isClimbing;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cameraPitch = cameraPivot.localEulerAngles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMovement();
        Look();
    }

    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 velocity;

        if (isClimbing)
        {
            // движение по лестнице: влево/вправо + вверх/вниз
            Vector3 climbMove =
                transform.right * x * climbSpeed +
                Vector3.up * z * climbSpeed;

            velocity = climbMove;
            verticalVelocity = 0f; // гравитация выключена
        }
        else
        {
            Vector3 move = (transform.right * x + transform.forward * z) * moveSpeed;

            if (controller.isGrounded)
            {
                if (verticalVelocity < 0)
                    verticalVelocity = -2f;

                if (Input.GetButtonDown("Jump"))
                {
                    verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
                }
            }

            verticalVelocity += gravity * Time.deltaTime;

            velocity = move;
            velocity.y = verticalVelocity;
        }

        controller.Move(velocity * Time.deltaTime);
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 100f * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 100f * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -40f, 70f);

        cameraPivot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ladder"))
        {
            isClimbing = true;
            verticalVelocity = 0f;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ladder"))
        {
            isClimbing = false;
        }
    }
}
