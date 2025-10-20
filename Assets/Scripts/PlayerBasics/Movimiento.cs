using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Comprobaciones")]
    public Transform groundCheck;
    public float groundRadius = 0.2f;
    public LayerMask Ground;

    private Rigidbody2D rb;
    private float moveInput;
    private bool jumpPressed;
    public bool isGrounded;
    public bool dobleSalto;

    [Header("Referencias")]
    public GameObject riderPrefab;
    public RiderController rider1;
    public PlataformaChecker platCheck;

    [Header("Step Smoothing")]
    public float stepOffset = 0.3f;
    public float stepSmoothSpeed = 10f;

    [Header("Inclinación")]
    public float rayLength = 1.0f;
    public float rotationSpeed = 10f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // Salto (NO MODIFICADO)
        if (Input.GetButtonDown("Jump"))
        {
            if (IsGrounded())
            {
                jumpPressed = true;
            }
            else if (rider1 != null)
            {
                rider1.RequestJump();
            }
        }

        isGrounded = IsGrounded();
        if (isGrounded)
            dobleSalto = false;
    }

    void FixedUpdate()
    {
        float jumpForce = StatsManager.Instance.RuntimeStats.jumpForce;
        float moveForce = StatsManager.Instance.RuntimeStats.moveForce;
        float maxSpeed = StatsManager.Instance.RuntimeStats.maxSpeed;
        float friction = StatsManager.Instance.RuntimeStats.friction;

        // Movimiento horizontal
        if (!rider1.canMove)
        {
            if (moveInput != 0f)
            {
                rb.AddForce(Vector2.right * moveInput * moveForce, ForceMode2D.Force);
            }
            else
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x * friction, rb.linearVelocity.y);
            }
        }
        else
        {
            Vector3 p = transform.position;
            p.x = riderPrefab.transform.position.x;
            transform.position = p;
        }

        // Ajuste de escalones e inclinación (UNIFICADO)
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, rayLength, Ground);
        if (hit.collider != null)
        {
            // Ajuste vertical para escalones
            float targetY = hit.point.y + groundCheck.localPosition.y;
            float deltaY = targetY - transform.position.y;

            if (deltaY > 0f && deltaY <= stepOffset)
            {
                float newY = Mathf.Lerp(transform.position.y, targetY, stepSmoothSpeed * Time.fixedDeltaTime);
                rb.MovePosition(new Vector2(rb.position.x, newY));
            }

            // Inclinación suave según pendiente
            float slopeAngle = Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg - 90f;
            float newRotation = Mathf.LerpAngle(rb.rotation, slopeAngle, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRotation);
        }

        // Limitar velocidad horizontal
        rb.linearVelocity = new Vector2(Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed), rb.linearVelocity.y);

        // Salto (NO MODIFICADO)
        if (jumpPressed && rider1.isAttached)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpPressed = false;
        }
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundRadius, Ground);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * rayLength);
    }
}