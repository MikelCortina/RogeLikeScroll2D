using System.Collections;
using System.Collections.Generic;
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
    [Tooltip("Objeto o punto en la jerarquía al que se pegará el jinete (Ej: La posición de la montura).")]
    public Transform targetTransform; // CAMBIO: Ahora es el objeto asignable
    public PlataformaChecker platCheck;  // referencia al script de comprobación de suelo alto

    public bool canMove = false; // se activa tras tocar suelo
    private float moveInput;
    private bool hasTouchedGround = false;

    [Header("Opciones de reattach")]
    [Tooltip("Si false, NO se reenganchara automáticamente al objeto tras colisiones.")]
    public bool allowAutoReattach = true;
    [Tooltip("Tag (etiqueta) que debe tener el objeto para permitir el reenganche (ej: 'Horse' o 'Mount').")]
    public string mountTag = "Horse"; // CAMBIO: Tag asignable para reenganche

    [Header("layer / Step Smoothing")]
    public LayerMask Ground;
    public Transform groundCheck; // opcional, para calcular targetY (si está null usa transform)
    public float rayLength = 1.0f;
    public float rotationSpeed = 10f;
    public float stepOffset = 0.3f;
    public float stepSmoothSpeed = 10f;

    [Header("Cable (auto-move)")]
    public float cableSpeed = 3f;           // velocidad en el cable
    public int cableDirection = 1;          // 1 hacia la derecha, -1 hacia la izquierda
    private bool onCable = false;           // true mientras esté en contacto con plataforma tag "cable"
    public string cableTag = "cable";      // tag a comprobar (case-sensitive)

    [Header("Drop (pasar hacia abajo)")]
    public Collider2D playerCollider;                   // asignar desde inspector o se obtiene en Awake
    public float dropIgnoreTime = 0.25f;               // tiempo que ignoramos la colisión
    public float dropDownImpulse = 2f;                 // impulso hacia abajo al soltarse
    private Coroutine dropCoroutine = null;

    private readonly List<Collider2D> currentPlatformColliders = new List<Collider2D>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();

        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider2D>();
        }
    }

    // CAMBIO: Ahora usa targetTransform
    public void Init(Transform target)
    {
        targetTransform = target;
        isAttached = true;
        jumpRequested = false;
        if (targetTransform != null) transform.position = targetTransform.position;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        rb.rotation = 0f;
    }

    public void RequestJump()
    {
        if (!isAttached) return;
        jumpRequested = true;
    }

    void Update()
    {
        float rawHorizontal = Input.GetAxisRaw("Horizontal");
        moveInput = rawHorizontal;

        if (Input.GetButtonDown("Jump") && platCheck != null && platCheck.isGrounded)
        {
            jumpPressed = true;
        }

        if (onCable && !isAttached)
        {
            soloMove = true;
            moveInput = Mathf.Sign(cableDirection);

            if (Input.GetKeyDown(KeyCode.S))
            {
                TryDropFromPlatform();
            }
        }
        else
        {
            soloMove = false;
        }

        if (isAttached)
        {
            // CAMBIO: Usar targetTransform en lugar de horseTransform
            if (targetTransform != null) transform.position = targetTransform.position;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
            soloMove = false;

            rb.MoveRotation(Mathf.LerpAngle(rb.rotation, 0f, rotationSpeed * Time.deltaTime));
        }
        else if (!hasTouchedGround && !isAttached)
        {
            Vector3 p = transform.position;
            // CAMBIO: Usar targetTransform para mantener la posición X al soltarse
            if (targetTransform != null) p.x = targetTransform.position.x;
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
        float jumpForce = StatsManager.Instance.RuntimeStats.jumpForce;
        float moveForce = StatsManager.Instance.RuntimeStats.moveForce;
        float maxSpeed = StatsManager.Instance.RuntimeStats.maxSpeed;
        float friction = StatsManager.Instance.RuntimeStats.friction;

        bool grounded = platCheck != null && platCheck.isGrounded;

        if (grounded)
            hasTouchedGround = true;

        canMove = (((hasTouchedGround && !isAttached)));

        if (onCable && !isAttached)
        {
            float targetVelX = cableDirection * cableSpeed;
            float newVelX = Mathf.MoveTowards(rb.linearVelocity.x, targetVelX, moveForce * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newVelX, rb.linearVelocity.y);
        }
        if (!onCable && !isAttached)
        {
            if (canMove && Mathf.Abs(moveInput) > 0f)
            {
                rb.AddForce(Vector2.right * moveInput * moveForce, ForceMode2D.Force);
                float clampedX = Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed);
                rb.linearVelocity = new Vector2(clampedX, rb.linearVelocity.y);
            }
            else if (canMove && Mathf.Approximately(moveInput, 0f))
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x * friction, rb.linearVelocity.y);
            }
        }

        if (jumpPressed && grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce * 1.5f, ForceMode2D.Impulse);
            jumpPressed = false;
        }

        if (jumpRequested && isAttached)
        {
            jumpRequested = false;
            ExecuteRiderJump_ApplyComputedImpulse();
        }

        if (isAttached)
        {
            hasTouchedGround = false;
        }

        if (!isAttached)
        {
            ApplyInclinationAndStepSmoothing();
        }
    }

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
            float impulseNeeded = mass * deltaV;
            rb.AddForce(Vector2.up * impulseNeeded, ForceMode2D.Impulse);
        }
    }

    private void ApplyInclinationAndStepSmoothing()
    {
        Vector2 origin = (groundCheck != null) ? (Vector2)groundCheck.position : (Vector2)transform.position;

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, rayLength, Ground);
        if (hit.collider != null)
        {
            float groundCheckLocalY = (groundCheck != null) ? groundCheck.localPosition.y : 0f;
            float targetY = hit.point.y + groundCheckLocalY;
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
        else
        {
            float newRotation = Mathf.LerpAngle(rb.rotation, 0f, rotationSpeed * Time.fixedDeltaTime * 0.5f);
            rb.MoveRotation(newRotation);
        }
    }

    // --- Gestión de contacto con plataformas "cable" y reenganche ---
    void OnTriggerEnter2D(Collider2D other)
    {
        // CAMBIO: Ahora usa mountTag para el reenganche
        if (allowAutoReattach && !isAttached && other.CompareTag(mountTag))
        {
            ReattachToTarget(other.transform);
            return;
        }

        // Detectar contacto con plataformas "cable" por trigger (sin cambios)
        if (!isAttached && other.CompareTag(cableTag))
        {
            if (other.usedByEffector || other.GetComponent<PlatformEffector2D>() != null)
            {
                AddPlatformCollider(other);
                rb.gravityScale = 0.1f;
            }
            else
            {
                AddPlatformCollider(other);
                rb.gravityScale = 0.1f;
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(cableTag))
        {
            RemovePlatformCollider(other);
            rb.gravityScale = 0.9f;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // CAMBIO: Ahora usa mountTag para el reenganche
        if (allowAutoReattach && !isAttached && collision.collider.CompareTag(mountTag))
        {
            ReattachToTarget(collision.transform);
            return;
        }

        if (!isAttached && collision.collider.CompareTag(cableTag))
        {
            Collider2D c = collision.collider;
            AddPlatformCollider(c);
            rb.gravityScale = 0.1f;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag(cableTag))
        {
            RemovePlatformCollider(collision.collider);
            rb.gravityScale = 0.9f;
        }
    }

    private void AddPlatformCollider(Collider2D col)
    {
        if (!currentPlatformColliders.Contains(col))
        {
            currentPlatformColliders.Add(col);
            onCable = true;
        }
    }

    private void RemovePlatformCollider(Collider2D col)
    {
        if (currentPlatformColliders.Contains(col))
            currentPlatformColliders.Remove(col);

        if (currentPlatformColliders.Count == 0)
            onCable = false;
    }

    private void TryDropFromPlatform()
    {
        if (!onCable || currentPlatformColliders.Count == 0 || playerCollider == null)
            return;

        if (dropCoroutine != null) StopCoroutine(dropCoroutine);
        dropCoroutine = StartCoroutine(TemporarilyIgnorePlatformCollisions(currentPlatformColliders.ToArray(), dropIgnoreTime));
    }

    private IEnumerator TemporarilyIgnorePlatformCollisions(Collider2D[] platformCols, float duration)
    {
        foreach (var pc in platformCols)
        {
            if (pc != null)
                Physics2D.IgnoreCollision(playerCollider, pc, true);
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.down * dropDownImpulse, ForceMode2D.Impulse);

        yield return new WaitForSeconds(duration);

        foreach (var pc in platformCols)
        {
            if (pc != null)
                Physics2D.IgnoreCollision(playerCollider, pc, false);
        }

        dropCoroutine = null;
    }

    // CAMBIO: Renombrado a ReattachToTarget y usa targetTransform
    private void ReattachToTarget(Transform target)
    {
        if (target == null) return;

        targetTransform = target; // Asigna la nueva posición de la montura si es diferente
        isAttached = true;
        jumpRequested = false;
        transform.position = targetTransform.position;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        rb.MoveRotation(0f);
        currentPlatformColliders.Clear();
        onCable = false;
    }
}