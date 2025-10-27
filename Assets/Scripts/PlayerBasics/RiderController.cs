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
    public Transform horseTransform; // asignar o con Init()
    public PlataformaChecker platCheck;   // referencia al script de comprobación de suelo alto

    public bool canMove = false; // se activa tras tocar suelo
    private float moveInput;
    private bool hasTouchedGround = false;

    [Header("layer / Step Smoothing")]
    public LayerMask Ground;
    public Transform groundCheck; // opcional, para calcular targetY (si está null usa transform)
    public float rayLength = 1.0f;
    public float rotationSpeed = 10f;
    public float stepOffset = 0.3f;
    public float stepSmoothSpeed = 10f;

    [Header("Cable (auto-move)")]
    public float cableSpeed = 3f;          // velocidad en el cable
    public int cableDirection = 1;         // 1 hacia la derecha, -1 hacia la izquierda
    private bool onCable = false;          // true mientras esté en contacto con plataforma tag "cable"
    public string cableTag = "cable";      // tag a comprobar (case-sensitive)

    [Header("Drop (pasar hacia abajo)")]
    public Collider2D playerCollider;                  // asignar desde inspector o se obtiene en Awake
    public float dropIgnoreTime = 0.25f;               // tiempo que ignoramos la colisión
    public float dropDownImpulse = 2f;                 // impulso hacia abajo al soltarse
    private Coroutine dropCoroutine = null;

    // guardamos todos los colliders de la/s plataforma/s cable con las que estamos en contacto
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

    public void Init(Transform horse)
    {
        horseTransform = horse;
        isAttached = true;
        jumpRequested = false;
        if (horseTransform != null) transform.position = horseTransform.position;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        rb.rotation = 0f;
    }

    // Llamada externa desde PlayerMovement
    public void RequestJump()
    {
        if (!isAttached) return;
        jumpRequested = true;
    }

    void Update()
    {
        // leer input en Update (pero lo sobrescribiremos si está en cable)
        float rawHorizontal = Input.GetAxisRaw("Horizontal");
        moveInput = rawHorizontal;

        // Guardamos la intención de salto
        if (Input.GetButtonDown("Jump") && platCheck != null && platCheck.isGrounded)
        {
            jumpPressed = true;
        }

        // Si está en contacto con cable y no attached -> movimiento automático
        if (onCable && !isAttached)
        {
            soloMove = true;
            // forzamos movimiento hacia adelante según cableDirection
            moveInput = Mathf.Sign(cableDirection);

            // si el jugador pulsa S (minúscula o mayúscula) se intenta 'bajar' del cable
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
            // forzar misma coordenada exacta mientras está enganchado
            if (horseTransform != null) transform.position = horseTransform.position;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
            soloMove = false;

            // cuando está attached queremos mantener rotación a 0
            rb.MoveRotation(Mathf.LerpAngle(rb.rotation, 0f, rotationSpeed * Time.deltaTime));
        }
        else if (!hasTouchedGround && !isAttached)
        {
            // en aire: forzar X si hay horseTransform, dejar Y a física
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
        // Stats (deja igual que tienes)
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
        if (onCable && !isAttached)
        {
            // Movimiento automático en cable: fijamos velocidad hacia adelante suavemente
            float targetVelX = cableDirection * cableSpeed;
            float newVelX = Mathf.MoveTowards(rb.linearVelocity.x, targetVelX, moveForce * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newVelX, rb.linearVelocity.y);
            // No aplicamos fricción ni input horizontal mientras esté en cable
        }
         if (!onCable && !isAttached)
        {
            if (canMove && Mathf.Abs(moveInput) > 0f)
            {
                rb.AddForce(Vector2.right * moveInput * moveForce, ForceMode2D.Force);
                // clamp horizontal speed
                float clampedX = Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed);
                rb.linearVelocity = new Vector2(clampedX, rb.linearVelocity.y);
            }
            else if (canMove && Mathf.Approximately(moveInput, 0f))
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x * friction, rb.linearVelocity.y);
            }
        }

        // --- SALTO NORMAL ---
        if (jumpPressed && grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpPressed = false;
        }

        // --- SALTO DEL RIDER (desde caballo) ---
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

        // --- APLICAR layer Y AJUSTE DE ESCALONES (solo cuando NO está attached) ---
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

            // layer suave según pendiente
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

    // --- Gestión de contacto con plataformas "cable" ---
    void OnTriggerEnter2D(Collider2D other)
    {
        // Reattach habitual
        if (!isAttached && (other.CompareTag("Horse") || other.CompareTag("Player")))
        {
            ReattachToHorse(other.transform);
            return;
        }

        // Detectar contacto con plataformas "cable" por trigger
        if (!isAttached && other.CompareTag(cableTag))
        {
            // Comprobamos si el collider está "usedByEffector" o tiene PlatformEffector2D
            if (other.usedByEffector || other.GetComponent<PlatformEffector2D>() != null)
            {
                AddPlatformCollider(other);
                rb.gravityScale = 0.1f;
            }
            else
            {
                // si no es effector pero sigue siendo cable, lo añadimos para soporte general
                AddPlatformCollider(other);
                rb.gravityScale = 0.1f;
            }
        }
        if(!isAttached && other.CompareTag("Ground"))
        {
            ReattachToHorse(horseTransform.transform);
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
        if (!isAttached && (collision.collider.CompareTag("Horse") || collision.collider.CompareTag("Player")))
        {
            ReattachToHorse(collision.transform);
            return;
        }

        // Si la plataforma cable usa colisiones normales
        if (!isAttached && collision.collider.CompareTag(cableTag))
        {
            Collider2D c = collision.collider;
            if (c.usedByEffector || c.GetComponent<PlatformEffector2D>() != null)
            {
                AddPlatformCollider(c);
                rb.gravityScale = 0.1f;
            }
            else
            {
                AddPlatformCollider(c);
                rb.gravityScale = 0.1f;
            }
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

    // --- Intento de bajar desde la plataforma (S) ---
    private void TryDropFromPlatform()
    {
        if (!onCable || currentPlatformColliders.Count == 0 || playerCollider == null)
            return;

        // evita llamadas repetidas
        if (dropCoroutine != null) StopCoroutine(dropCoroutine);
        dropCoroutine = StartCoroutine(TemporarilyIgnorePlatformCollisions(currentPlatformColliders.ToArray(), dropIgnoreTime));
    }

    private IEnumerator TemporarilyIgnorePlatformCollisions(Collider2D[] platformCols, float duration)
    {
        // Ignoramos colisión entre rider y todos los colliders de la plataforma
        foreach (var pc in platformCols)
        {
            if (pc != null)
                Physics2D.IgnoreCollision(playerCollider, pc, true);
        }

        // aseguramos que cae: cancelamos su velocidad vertical y aplicamos impulso hacia abajo
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.down * dropDownImpulse, ForceMode2D.Impulse);

        yield return new WaitForSeconds(duration);

        // Restablecemos la colisión (si los colliders aún existen)
        foreach (var pc in platformCols)
        {
            if (pc != null)
                Physics2D.IgnoreCollision(playerCollider, pc, false);
        }

        dropCoroutine = null;
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
        currentPlatformColliders.Clear();
        onCable = false;
    }
}
