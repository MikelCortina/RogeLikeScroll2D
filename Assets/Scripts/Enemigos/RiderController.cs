using UnityEngine;

public class RiderController : MonoBehaviour
{
    private Rigidbody2D rb;
    private bool falling = true; // empieza "enganchado" fuera de caballo hasta Init
    public Transform horseTransform; // puedes asignar en inspector o via Init
    private bool jumpRequested = false;
    public float jumpForce = 7f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogWarning("[Rider] No Rigidbody2D encontrado. Se añade uno automáticamente.");
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        Debug.Log("[Rider] Awake: bodyType=" + rb.bodyType + " gravityScale=" + rb.gravityScale + " constraints=" + rb.constraints);
    }

    public void Init(Transform horse)
    {
        horseTransform = horse;
        transform.position = new Vector2(horse.position.x, horse.position.y);
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        rb.isKinematic = false; // dejamos Dynamic por defecto
        // solo congelamos la rotacion para evitar que gire:
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        falling = true;
        Debug.Log("[Rider] Init called. Horse asignado: " + (horseTransform != null));
    }

    void Update()
    {
        // leer input en Update
        if (Input.GetButtonDown("Jump") && !jumpRequested)
        {
            jumpRequested = true;
            Debug.Log("[Rider] Jump requested");
        }
    }

    void LateUpdate()
    {
        // usar LateUpdate para seguir el transform del caballo si el caballo mueve su transform en Update
        if (!falling || horseTransform == null) return;

        // si está "enganchado" (gravityScale == 0) sigue SOLO la X del caballo
        if (Mathf.Approximately(rb.gravityScale, 0f))
        {
            Vector3 pos = transform.position;
            pos.x = horseTransform.position.x;
            transform.position = pos;
        }
    }

    void FixedUpdate()
    {
        if (!falling) return;

        if (jumpRequested)
        {
            // Ejecutar salto
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            rb.gravityScale = 1f;
            jumpRequested = false;
            Debug.Log("[Rider] Saltando: gravityScale ahora " + rb.gravityScale);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("[Rider] OnTriggerEnter2D con: " + other.name + " tag:" + other.tag);
        if (!falling) return;

        if (other.CompareTag("Horse") || other.CompareTag("Player"))
        {
            falling = false;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // no congelamos posiciones
            // Parentear al root del caballo (evita parentear a un collider hijo)
            Transform parentTransform = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform.root;
            transform.SetParent(parentTransform, worldPositionStays: true);
            transform.position = new Vector2(parentTransform.position.x, parentTransform.position.y);
            Debug.Log("[Rider] Enganchado a " + parentTransform.name);
        }
    }
}
