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
    public bool isGrounded;
    private float moveInput;
    public bool jumpPressed;

    // Ventana para permitir que el jinete haga su double jump
    public bool dobleSalto; // expuesto por si quieres ver en inspector

    [Header("Referencias")]
    public GameObject riderPrefab; // asignar prefab del jinete en el Inspector (si lo spawneas)
    public RiderController rider1; // referencia al jinete (mejor asignar en inspector)

    public PlataformaChecker platCheck;   // referencia al script de comprobaci�n de suelo alto
    [Header("Step Smoothing")]
    public float stepOffset = 0.3f;        // Altura máxima de “escalón” que se sube suavemente
    public float stepSmoothSpeed = 10f;    // Velocidad de suavizado



    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // leer input en Update
        moveInput = Input.GetAxisRaw("Horizontal");

        // salto: primer caso: suelo -> salto del jugador
        if (Input.GetButtonDown("Jump"))
        {
            if (IsGrounded())
            {
                jumpPressed = true;
            }

            if (!IsGrounded())
            {
                // Si el jugador pulsa salto en el aire pedimos al rider que encole su salto al apex
                if (rider1 != null)
                {
                    rider1.RequestJump();
                }
            }
        }

        // mantener flag en false cuando estamos en suelo
        isGrounded = IsGrounded();
        if (isGrounded)
        {
            dobleSalto = false;
        }
    }

    void FixedUpdate()
    {
        float jumpForce = StatsManager.Instance.RuntimeStats.jumpForce;
        float moveForce = StatsManager.Instance.RuntimeStats.moveForce;
        float maxSpeed = StatsManager.Instance.RuntimeStats.maxSpeed;
        float friction = StatsManager.Instance.RuntimeStats.friction;

        // Raycast para detectar suelo
        float rayLength = 1.0f;
        LayerMask groundLayer = LayerMask.GetMask("Ground");
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, rayLength, groundLayer);

        if (hit.collider != null)
        {
            Vector2 surfaceNormal = hit.normal;
            Vector2 slopeDir = new Vector2(surfaceNormal.y, -surfaceNormal.x).normalized;

            // --- Movimiento horizontal con AddForce ---
            if (!rider1.canMove)
            {
                if (moveInput != 0f)
                {
                    // Aplicar fuerza horizontal ignorando microvariaciones verticales
                    Vector2 horizontalDir = new Vector2(1f, 0f); // siempre horizontal
                    rb.AddForce(horizontalDir * moveInput * moveForce, ForceMode2D.Force);
                }
                else
                {
                    // Aplicar fricción cuando no hay input
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x * friction, rb.linearVelocity.y);
                }
            }
            else if (rider1.canMove)
            {
                Vector3 p = transform.position;
                p.x = riderPrefab.transform.position.x;
                transform.position = p;
            }

            // --- Suavizado de pequeños escalones ---
            float targetY = hit.point.y + groundCheck.localPosition.y;
            float deltaY = targetY - transform.position.y;

            if (deltaY > 0f && deltaY <= stepOffset)
            {
                float newY = Mathf.Lerp(transform.position.y, targetY, stepSmoothSpeed * Time.fixedDeltaTime);
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }
        }
        else
        {
            // Movimiento en aire
            if (!rider1.canMove && moveInput != 0f)
                rb.AddForce(Vector2.right * moveInput * moveForce, ForceMode2D.Force);
        }

        // Limitar velocidad horizontal
        rb.linearVelocity = new Vector2(Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed), rb.linearVelocity.y);

        // Salto
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
    }
}
