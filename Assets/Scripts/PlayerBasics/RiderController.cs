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
        if (Input.GetButtonDown("Jump")&&platCheck.isGrounded)
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
        }
        else if (!hasTouchedGround && !isAttached)
        {
            // en aire: forzar X, dejar Y a física
            Vector3 p = transform.position;
            p.x = horseTransform.position.x;
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

        bool grounded = platCheck.isGrounded;

        // --- DETECCIÓN DE TOQUE DE SUELO ---
        if (grounded)
            hasTouchedGround = true;

        // --- LÓGICA DE CONTROL ---
        // Si ya tocó el suelo al menos una vez, puede moverse mientras no esté attached
        canMove = (((hasTouchedGround && !isAttached)));

        // --- MOVIMIENTO HORIZONTAL ---
        if (canMove && moveInput != 0f)
        {
            rb.AddForce(Vector2.right * moveInput * moveForce, ForceMode2D.Force);
        }
        else if (canMove && moveInput == 0f)
        {
            // aplicar fricción cuando no hay input horizontal
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
            // resetear el estado: no puede moverse solo hasta que vuelva a tocar suelo
            hasTouchedGround = false;
        }
    }

    // === Opción 1 (recomendada): fijar la velocidad vertical objetivo ===
    private void ExecuteRiderJump_SetVelocity()
    {
        float jumpImpulse = StatsManager.Instance.RuntimeStats.jumpForce;

        isAttached = false;
        transform.SetParent(null);

        // activar física vertical
        rb.gravityScale = 1f;

        // velocidad vertical objetivo equivalente al impulso aplicado en apex:
        // si en tu código usabas rb.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
        // entonces deltaV_apex = jumpImpulse / masa
        float mass = rb.mass;
        float vObjetivo = 0f;
        if (mass > 0f) vObjetivo = jumpImpulse / mass;

        // Seteamos la velocidad vertical exactamente a vObjetivo (esto reproduce
        // lo que habría pasado si hubiéramos aplicado el impulso en el apex).
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, vObjetivo);

        Debug.Log("[Rider] Salto ejecutado (velocidad fijada a vObjetivo = " + vObjetivo + ").");
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
            //Debug.Log("[Rider] Salto ejecutado (impulso aplicado = " + impulseNeeded + ").");
        }
        else
        {
            // Si deltaV <= 0 significa que ya tenemos una velocidad vertical igual o superior al objetivo.
            // No aplicamos nada (si quieres sobrescribir la velocidad, usa la opción 1).
            // Debug.Log("[Rider] No se aplicó impulso (deltaV <= 0).");
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
            // Comprobamos si el rider está más abajo que el caballo
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
        // Debug.Log("[Rider] Re-enganchado al caballo.");
    }
}
