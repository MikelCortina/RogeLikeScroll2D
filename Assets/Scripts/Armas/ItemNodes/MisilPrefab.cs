using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class MisilProjectile : MonoBehaviour
{
    [Header("Movimiento (opcional, si no lo controla el looper)")]
    public float speed = 5f; // Solo se usa si no está controlado externamente

    [Header("Explosion Settings")]
    public GameObject explosionPrefab;           // VFX de explosión
                // Tiempo máximo en pantalla
    public float explosionRadius = 1.8f;
    public float explosionForce = 300f;
    public ExplosionEffect explosionEffect;      // Tu ScriptableObject de daño en área

    [Header("Daño")]
    public float damage = 25f;                   // Daño base (si lo usas directamente)

    private bool exploded = false;

    // Evento para notificar al pool (importante para el looper)
    public event System.Action<MisilProjectile> OnExplode;

    private void OnEnable()
    {
        exploded = false;
        // Reiniciar lifetime cada vez que se activa
        CancelInvoke(nameof(LifetimeExpired));
     
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(LifetimeExpired));
    }

    // Método opcional: si quieres que el misil se mueva solo (por si lo usas fuera del looper)
    // El looper ya mueve el transform, así que esto es redundante pero inofensivo
    private void FixedUpdate()
    {
        // Si el movimiento lo controla el ScreenLooper, no hacemos nada aquí
        // Pero lo dejamos por compatibilidad si se usa en otro contexto
        // transform.position += Vector3.right * speed * Time.fixedDeltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Colisión con suelo
        if (collision.CompareTag("Ground"))
        {
            Explode();
            return;
        }

        // Colisión con jugador (o cualquier enemigo/destructible)
        if (collision.CompareTag("Player") || collision.CompareTag("enemigo"))
        {
            Explode();
        }
    }

    private void LifetimeExpired()
    {
        Explode();
    }

    public void Explode()
    {
        if (exploded) return;
        exploded = true;

        // 1. VFX de explosión
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // 2. Efecto de daño en área (tu ExplosionEffect ScriptableObject)
        explosionEffect?.Execute(transform.position, gameObject);

        // 3. Física de empuje (opcional, para ragdolls o objetos cercanos)
        ApplyExplosionForce();

        // 4. Notificar al sistema de pooling (muy importante para el looper)
        OnExplode?.Invoke(this);

        // 5. Desactivar el misil (no destruirlo, para reutilizarlo)
        gameObject.SetActive(false);
    }

    private void ApplyExplosionForce()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D col in colliders)
        {
            Rigidbody2D rb = col.GetComponent<Rigidbody2D>();
            if (rb != null && rb != GetComponent<Rigidbody2D>())
            {
                Vector2 direction = (col.transform.position - transform.position);
                float distance = direction.magnitude;
                if (distance < 0.1f) distance = 0.1f;

                float force = explosionForce * (1f - distance / explosionRadius);
                rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
            }
        }
    }

    // Método público para resetear el misil (llamado por el pool o looper si es necesario)
    public void ResetMissile()
    {
        exploded = false;
        transform.rotation = Quaternion.identity;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.35f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}