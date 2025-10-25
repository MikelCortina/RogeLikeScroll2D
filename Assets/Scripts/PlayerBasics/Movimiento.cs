using System.Collections;
using System.Collections.Generic;
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
    public Animator anim;

    private bool isJumping = false;

    [Header("Ajustes de movimiento")]
    public float leftSpeedMultiplier = 1.2f;

    [Header("Ajustes de salto horizontal")]
    public float jumpHorizontalImpulse = 4f;     // impulso total hacia la derecha
    public float jumpHorizontalDuration = 0.3f;  // duración del impulso progresivo

    // Control interno del impulso progresivo
    private float applyJumpHorizontalTimer = 0f;
    private float jumpHorizontalRemaining = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

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
    }

    void FixedUpdate()
    {
        float jumpForce = StatsManager.Instance.RuntimeStats.jumpForce;
        float moveForce = StatsManager.Instance.RuntimeStats.moveForce;
        float maxSpeed = StatsManager.Instance.RuntimeStats.maxSpeed;
        float friction = StatsManager.Instance.RuntimeStats.friction;

        // --- Calcular grounded ---
        isGrounded = IsGrounded();
        if (isGrounded)
            dobleSalto = false;

        // --- Movimiento horizontal ---
        if (!rider1.canMove)
        {
            if (moveInput != 0f)
            {
                float adjustedMoveForce = moveForce;
                if (moveInput < 0f)
                    adjustedMoveForce *= leftSpeedMultiplier;

                rb.AddForce(Vector2.right * moveInput * adjustedMoveForce, ForceMode2D.Force);
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

        // --- Ajuste de inclinación ---
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, rayLength, Ground);
        if (hit.collider != null)
        {
            float targetY = hit.point.y + groundCheck.localPosition.y;
            float deltaY = targetY - transform.position.y;

            if (deltaY > 0f && deltaY <= stepOffset)
            {
                float newY = Mathf.Lerp(transform.position.y, targetY, stepSmoothSpeed * Time.fixedDeltaTime);
                rb.MovePosition(new Vector2(rb.position.x, newY));
            }

            float slopeAngle = Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg - 90f;
            float newRotation = Mathf.LerpAngle(rb.rotation, slopeAngle, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRotation);
        }

        // --- Limitar velocidad horizontal ---
        rb.linearVelocity = new Vector2(
            Mathf.Clamp(rb.linearVelocity.x, -maxSpeed * leftSpeedMultiplier, maxSpeed),
            rb.linearVelocity.y
        );

        // --- SALTO ---
        if (jumpPressed && rider1.isAttached)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            // 🔹 SOLO aplicar impulso si se está pulsando hacia la derecha
            if (moveInput > 0.01f)
            {
                jumpHorizontalRemaining = jumpHorizontalImpulse;
                applyJumpHorizontalTimer = jumpHorizontalDuration;
            }
            else
            {
                jumpHorizontalRemaining = 0f;
                applyJumpHorizontalTimer = 0f;
            }

            isJumping = true;
            isGrounded = false;
            PlayAnimationOnce("HorseJump");
            jumpPressed = false;
        }

        // --- IMPULSO HORIZONTAL PROGRESIVO DURANTE EL SALTO ---
        if (applyJumpHorizontalTimer > 0f && jumpHorizontalRemaining > 0f)
        {
            // 🟡 Si el jugador pulsa izquierda en el aire → cancelar impulso
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                applyJumpHorizontalTimer = 0f;
                jumpHorizontalRemaining = 0f;
                return; // salir inmediatamente, no aplicar más impulso
            }

            float dt = Time.fixedDeltaTime;
            float duration = Mathf.Max(0.0001f, jumpHorizontalDuration);
            float impulseThisFrame = (jumpHorizontalImpulse * dt / duration);

            if (impulseThisFrame > jumpHorizontalRemaining)
                impulseThisFrame = jumpHorizontalRemaining;

            Vector2 horizImpulse = Vector2.right * impulseThisFrame;
            rb.AddForce(horizImpulse, ForceMode2D.Impulse);

            jumpHorizontalRemaining -= impulseThisFrame;
            applyJumpHorizontalTimer -= dt;

            if (applyJumpHorizontalTimer <= 0f || jumpHorizontalRemaining <= 0f)
            {
                applyJumpHorizontalTimer = 0f;
                jumpHorizontalRemaining = 0f;
            }
        }

        // --- ANIMACIONES ---
        float speedThreshold = 0.2f;

        if (isJumping)
        {
            if (IsGrounded() && rb.linearVelocity.y <= 0.01f)
            {
                isJumping = false;
                isGrounded = true;
            }
            else
            {
                return;
            }
        }

        if (isGrounded)
        {
            if (Mathf.Abs(rb.linearVelocity.x) < speedThreshold)
                PlayAnimationIfNotPlaying("Idle");
            else if (rb.linearVelocity.x > speedThreshold)
                PlayAnimationIfNotPlaying("HorseRunRight");
            else if (rb.linearVelocity.x < -speedThreshold)
                PlayAnimationIfNotPlaying("HorseRunLeft");
        }
    }

    void PlayAnimationOnce(string animName)
    {
        if (anim == null) return;
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName(animName))
            anim.Play(animName, 0, 0f);
    }

    void PlayAnimationIfNotPlaying(string animName)
    {
        if (anim == null) return;
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName(animName))
            anim.Play(animName);
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
