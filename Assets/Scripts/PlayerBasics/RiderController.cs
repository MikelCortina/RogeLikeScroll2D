using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RiderController : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Estado")]
    public bool isAttached = true;
    private bool jumpRequested = false;
    public bool soloMove;
    public bool jumpPressed;

    [Header("Referencias")]
    public Transform horseTransform; // asignar o con Init()
    public PlataformaChecker platCheck;   // referencia al script de comprobación de suelo alto

    public bool canMove = false; // se activa tras tocar suelo
    private float moveInput;
    private bool hasTouchedGround = false;

    [Header("Inclinación / Step Smoothing")]
    public LayerMask Ground;
    public Transform groundCheck; // opcional, para calcular targetY (si está null usa transform)
    public float rayLength = 1.0f;
    public float rotationSpeed = 10f;
    public float stepOffset = 0.3f;
    public float stepSmoothSpeed = 10f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
    }

    public void Init(Transform horse)
    {
        horseTransform = horse;
        isAttached = true;
        jumpRequested = false;
        if (horseTransform != null) transform.position = horseTransform.position;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        rb.SetRotation(0f);
    }

    // Llamada externa desde PlayerMovement
    public void RequestJump()
    {
        if (!isAttached) return;
        jumpRequested = true;
    }

    void Update()
    {
        // leer input en Update
        moveInput = Input.GetAxisRaw("Horizontal");

        // Guardamos la intención de salto
        if (Input.GetButtonDown("Jump") && platCheck != null && platCheck.isGrounded)
        {
            jumpPressed = true;
        }

        if (isAttached)
        {
            // forzar misma coordenada exacta mientras está enganchado
            transform.position = horseTransform.position;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
            soloMove = false;

            // cuando está attached queremos mantener rotación a 0
            rb.MoveRotation(Mathf.LerpAngle(rb.rotation, 0f, rotationSpeed * Time.deltaTime));
        }
        else if (!hasTouchedGround && !isAttached)
        {
            // en aire: forzar X, dejar Y a física
            Vector3 p = transform.position;
            if (horseTransform != null) p.x = horseTransform.position.x;
            transform.position = p;
            rb.gravityScale = 0.9f;
        }

        if (hasTouchedGround)
        {
            rb.gravityScale = 0.9f;
        }
    }

    void FixedUpdate()
    {
        // Stats
        float jumpForce = StatsManager.Instance.RuntimeStats.jumpForce;
        float moveForce = StatsManager.Instance.RuntimeStats.moveForce;
        float maxSpeed = StatsManager.Instance.RuntimeStats.maxSpeed;
        float friction = StatsManager.Instance.RuntimeStats.friction;

        bool grounded = platCheck != null && platCheck.isGrounded;

        // --- DETECCIÓN DE TOQUE DE SUELO ---
        if (grounded)
            hasTouchedGround = true;

        // --- LÓGICA DE CONTROL ---
        canMove = (((hasTouchedGround && !isAttached)));

        // --- MOVIMIENTO HORIZONTAL ---
        if (canMove && moveInput != 0f)
        {
            rb.AddForce(Vector2.right * moveInput * moveForce, ForceMode2D.Force);
        }
        else if (canMove && moveInput == 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * friction, rb.linearVelocity.y);
        }

        // --- SALTO NORMAL ---
        if (jumpPressed && grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpPressed = false;
        }

        // --- SALTO DEL RIDER ---
        if (jumpRequested && isAttached)
        {
            jumpRequested = false;
            ExecuteRiderJump_ApplyComputedImpulse();
        }

        // --- SI VUELVE A ESTAR ATTACHED ---
        if (isAttached)
        {
            hasTouchedGround = false;
        }

        // --- APLICAR INCLINACIÓN Y AJUSTE DE ESCALONES (solo cuando NO está attached) ---
        if (!isAttached)
        {
            ApplyInclinationAndStepSmoothing();
        }
    }

    // === Opción 2 (alternativa): aplicar impulso calculado ===
    private void ExecuteRiderJump_ApplyComputedImpulse()
    {
        float jumpImpulse = StatsManager.Instance.RuntimeStats.jumpForce;

        isAttached = false;
        transform.SetParent(null);

        rb.gravityScale = 0.9f;

        float mass = rb.mass;
        float vObjetivo = mass > 0f ? jumpImpulse / mass : 0f;
        float vActual = rb.linearVelocity.y;
        float deltaV = vObjetivo - vActual;

        if (deltaV > 0f)
        {
            float impulseNeeded = mass * deltaV; // N·s
            rb.AddForce(Vector2.up * impulseNeeded, ForceMode2D.Impulse);
        }
    }

    private void ApplyInclinationAndStepSmoothing()
    {
        // Origin del raycast (usar groundCheck si está asignado para calcular targetY)
        Vector2 origin = (groundCheck != null) ? (Vector2)groundCheck.position : (Vector2)transform.position;

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, rayLength, Ground);
        if (hit.collider != null)
        {
            // Ajuste vertical para escalones (usar groundCheck.localPosition.y si hay)
            float groundCheckLocalY = (groundCheck != null) ? groundCheck.localPosition.y : 0f;
            float targetY = hit.point.y + groundCheckLocalY;
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
        else
        {
            // Si no hay suelo detectado, rotamos hacia 0 gradualmente (opcional)
            float newRotation = Mathf.LerpAngle(rb.rotation, 0f, rotationSpeed * Time.fixedDeltaTime * 0.5f);
            rb.MoveRotation(newRotation);
        }
    }

    // Reattach
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isAttached && (other.CompareTag("Horse") || other.CompareTag("Player")))
        {
            ReattachToHorse(other.transform);
        }

        if (!isAttached && (other.CompareTag("Horse") || other.CompareTag("Player")))
        {
            if (transform.position.y < other.transform.position.y)
            {
                ReattachToHorse(other.transform);
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isAttached && (collision.collider.CompareTag("Horse") || collision.collider.CompareTag("Player")))
        {
            ReattachToHorse(collision.transform);
        }
    }

    private void ReattachToHorse(Transform horse)
    {
        horseTransform = horse;
        isAttached = true;
        jumpRequested = false;
        transform.position = horseTransform.position;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        rb.MoveRotation(0f);
    }
}
