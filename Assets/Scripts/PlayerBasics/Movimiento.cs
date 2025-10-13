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

    public PlataformaChecker platCheck;   // referencia al script de comprobación de suelo alto

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
        // Valores desde StatsManager (sin cambiar)
        float jumpForce = StatsManager.Instance.RuntimeStats.jumpForce;
        float moveForce = StatsManager.Instance.RuntimeStats.moveForce;
        float maxSpeed = StatsManager.Instance.RuntimeStats.maxSpeed;
        float friction = StatsManager.Instance.RuntimeStats.friction;

        // movimiento horizontal
        if (moveInput != 0f && !rider1.canMove)
        {
            rb.AddForce(Vector2.right * moveInput * moveForce, ForceMode2D.Force);
        }
        else if (!rider1.canMove)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * friction, rb.linearVelocity.y);
        }
        else if (rider1.canMove)
        {
            // en aire: forzar X, dejar Y a física
            Vector3 p = transform.position;
            p.x = riderPrefab.transform.position.x;
            transform.position = p;
        }

            // limitar velocidad
            float clampedX = Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed);
        rb.linearVelocity = new Vector2(clampedX, rb.linearVelocity.y);

        // Salto: aplicar impulso una vez
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
