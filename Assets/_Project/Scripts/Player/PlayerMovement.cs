using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speedMultiplier = 1f;
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private float walkMultiplier = 0.125f;

    public float SpeedMultiplier
    {
        get => speedMultiplier;
        set => speedMultiplier = value;
    }

    [Header("Jump")]
    [SerializeField] private float jumpLimit = 35f;
    [SerializeField] private float externalImpulseLimit = 80f;
    [SerializeField] private float coyoteTime = 0.2f;

    [Header("References")]
    [SerializeField] private Animator animator;

    private Rigidbody rb;
    private float coyoteTimer;
    private float externalImpulseLimitTimer;
    private bool jumpConsumedUntilLanding;
    private bool hasLeftGroundSinceJump;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void Update()
    {
        UpdateCoyoteTimer();
        HandleJump();
        UpdateExternalImpulseLimitTimer();
        LimitJumpVelocity();
    }

    private void HandleMovement()
    {
        Vector3 movement = new Vector3(0f, rb.velocity.y - 0.5f, 0f);
        bool isMoving = false;

        float currentSpeed = Settings.speedPlayer * speedMultiplier;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            if (Input.GetKey(KeyCode.LeftShift))
                movement += transform.forward * currentSpeed * sprintMultiplier;
            else if (Input.GetKey(KeyCode.LeftControl))
                movement += transform.forward * currentSpeed * walkMultiplier;
            else
                movement += transform.forward * currentSpeed;

            isMoving = true;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            if (Input.GetKey(KeyCode.LeftShift))
                movement -= transform.forward * currentSpeed * sprintMultiplier;
            else if (Input.GetKey(KeyCode.LeftControl))
                movement -= transform.forward * currentSpeed * walkMultiplier;
            else
                movement -= transform.forward * currentSpeed;

            isMoving = true;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            if (Input.GetKey(KeyCode.LeftShift))
                movement -= transform.right * currentSpeed * sprintMultiplier;
            else if (Input.GetKey(KeyCode.LeftControl))
                movement -= transform.right * currentSpeed * walkMultiplier;
            else
                movement -= transform.right * currentSpeed;

            isMoving = true;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            if (Input.GetKey(KeyCode.LeftShift))
                movement += transform.right * currentSpeed * sprintMultiplier;
            else if (Input.GetKey(KeyCode.LeftControl))
                movement += transform.right * currentSpeed * walkMultiplier;
            else
                movement += transform.right * currentSpeed;

            isMoving = true;
        }

        rb.velocity = movement;

        if (animator != null)
            animator.SetBool("IsRunning", isMoving);
    }

    private void UpdateCoyoteTimer()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);
        bool hasGroundContact = false;

        if (Physics.Raycast(ray, out RaycastHit hit, 1.2f))
        {
            float distance = Vector3.Distance(transform.position, hit.point);

            if (distance <= 1.1f)
            {
                hasGroundContact = true;

                if (distance < 1.05f && rb.velocity.y <= 0.1f)
                {
                    if (!jumpConsumedUntilLanding || hasLeftGroundSinceJump)
                    {
                        jumpConsumedUntilLanding = false;
                        hasLeftGroundSinceJump = false;
                        coyoteTimer = coyoteTime;
                    }
                }
                else if (!jumpConsumedUntilLanding)
                {
                    coyoteTimer = coyoteTime;
                }
            }

            if (distance < 1.05f && animator != null && animator.GetBool("IsJumping"))
                animator.SetBool("IsJumping", false);
        }

        if (!hasGroundContact)
        {
            if (jumpConsumedUntilLanding)
                hasLeftGroundSinceJump = true;

            coyoteTimer = Mathf.Max(0f, coyoteTimer - Time.deltaTime);
        }
    }

    private void HandleJump()
    {
        if (!Input.GetKeyDown(KeyCode.Space) || coyoteTimer <= 0f || jumpConsumedUntilLanding)
            return;

        if (animator != null)
        {
            animator.SetBool("IsJumping", true);
            animator.CrossFadeInFixedTime("Jump", 0.1f);
        }

        rb.AddForce(transform.up * (Settings.jumpForcePlayer * 500f));
        coyoteTimer = 0f;
        jumpConsumedUntilLanding = true;
        hasLeftGroundSinceJump = false;
    }

    private void LimitJumpVelocity()
    {
        // Keep regular jump and external impulses independent:
        // while external impulse window is active, clamp only by externalImpulseLimit.
        float activeLimit = externalImpulseLimitTimer > 0f ? externalImpulseLimit : jumpLimit;
        if (rb.velocity.y > activeLimit)
            rb.velocity = new Vector3(rb.velocity.x, activeLimit, rb.velocity.z);
    }

    public bool IsGrounded() => coyoteTimer > 0f;

    public void AllowExternalVerticalImpulse(float duration)
    {
        externalImpulseLimitTimer = Mathf.Max(externalImpulseLimitTimer, duration);
    }

    public void ApplyExternalImpulse(Vector3 impulse, ForceMode forceMode, float limitDuration = 0.5f, bool resetVelocity = true)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb == null)
            return;

        AllowExternalVerticalImpulse(limitDuration);

        if (resetVelocity)
            rb.velocity = Vector3.zero;

        rb.AddForce(impulse, forceMode);
    }

    private void UpdateExternalImpulseLimitTimer()
    {
        if (externalImpulseLimitTimer > 0f)
            externalImpulseLimitTimer = Mathf.Max(0f, externalImpulseLimitTimer - Time.deltaTime);
    }
}
