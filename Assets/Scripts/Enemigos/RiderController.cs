using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RiderController : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Estado")]
    public bool isAttached = true;
    private bool jumpRequested = false;
    private int requestToken = -1;

    [Header("Referencias")]
    public Transform horseTransform;    // asignar o con Init()

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
        requestToken = -1;
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
        if (horseTransform == null) return;

        if (isAttached)
        {
            // forzar misma coordenada exacta mientras está enganchado
            transform.position = horseTransform.position;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }
        else
        {
            // en aire: forzar X, dejar Y a física
            Vector3 p = transform.position;
            p.x = horseTransform.position.x;
            transform.position = p;
            rb.gravityScale = 1f;
        }
    }

    void FixedUpdate()
    {
        float jumpImpulse = StatsManager.Instance.RuntimeStats.jumpForce;
        if (jumpRequested && isAttached)
        {
            jumpRequested = false;
            ExecuteRiderJump_ApplyComputedImpulse();
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
        rb.gravityScale = 1f;

        float mass = rb.mass;
        float vObjetivo = mass > 0f ? jumpImpulse / mass : 0f;
        float vActual = rb.linearVelocity.y;

        float deltaV = vObjetivo - vActual;

        if (deltaV > 0f)
        {
            float impulseNeeded = mass * deltaV; // N·s
            rb.AddForce(Vector2.up * impulseNeeded, ForceMode2D.Impulse);
            Debug.Log("[Rider] Salto ejecutado (impulso aplicado = " + impulseNeeded + ").");
        }
        else
        {
            // Si deltaV <= 0 significa que ya tenemos una velocidad vertical igual o superior al objetivo.
            // No aplicamos nada (si quieres sobrescribir la velocidad, usa la opción 1).
            Debug.Log("[Rider] No se aplicó impulso (deltaV <= 0).");
        }
    }

    // Reattach
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isAttached && (other.CompareTag("Horse") || other.CompareTag("Player")))
        {
            ReattachToHorse(other.transform);
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
        Debug.Log("[Rider] Re-enganchado al caballo.");
    }
}
