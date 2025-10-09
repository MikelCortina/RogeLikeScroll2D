using UnityEngine;

public class RiderController : MonoBehaviour
{
    private Rigidbody2D rb;
    private bool falling = false;
    public Transform horseTransform;
    private bool jumpRequested = false;
    public float jumpForce = 7f; // puedes sacarlo del StatsManager si quieres

    public void Init(Transform horse)
    {
        horseTransform = horse;
        transform.position = horse.position;
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // no queremos que caiga automáticamente
        falling = true;
    }

    void Update()
    {
        if (!falling || horseTransform == null)
            return;

        // Mantener X del caballo
        transform.position = new Vector2(horseTransform.position.x, transform.position.y);

        // Salto manual en Y si se pulsa espacio
        if (Input.GetButtonDown("Jump") && !jumpRequested)
            jumpRequested = true;
    }

    void FixedUpdate()
    {
        if (!falling) return;

        // Seguir X del caballo mientras no salta
        if (rb.gravityScale == 0)
            transform.position = new Vector2(horseTransform.position.x, horseTransform.position.y);

        // Salto solicitado
        if (jumpRequested)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            rb.gravityScale = 1f;
            jumpRequested = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!falling)
            return;

        // Solo ejecutar si el objeto tocado tiene la etiqueta "Player"
        if (other.CompareTag("Player"))
        {
            falling = false;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f; // enganchar al jugador/caballo
            transform.parent = other.transform;

            // Ajustar posición exacta para que "caiga" sobre el caballo
            transform.position = new Vector2(other.transform.position.x, other.transform.position.y);
        }
    }
}

