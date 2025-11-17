using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class MisilProjectile : MonoBehaviour
{
    [Header("Explosion Settings")]
    public GameObject explosionPrefab;
    public float lifetime = 10f;
    public float explosionRadius = 1.5f;
    public float explosionForce = 200f;
    public ExplosionEffect explosionEffect; // Referencia al efecto de explosi�n

    private Vector3 target;
    private bool targetSet = false;
    private bool exploded = false;

    // Evento para pooling
    public event System.Action OnExplode;

    private void OnEnable()
    {
        // Iniciar conteo de vida solo si no usamos pooling externo
        Invoke(nameof(LifetimeExpired), lifetime);
    }

    private void OnDisable()
    {
        // Cancelar invocaciones al desactivar
        CancelInvoke();
        ResetMissile();
    }

    public void SetTarget(Vector3 worldTarget)
    {
        target = worldTarget;
        targetSet = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Explode();
    }

    private void LifetimeExpired()
    {
        Explode();
    }

    public void Explode()
    {
        if (exploded) return;
        exploded = true;

        // Instanciar VFX si existe
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        // Ejecutar efecto de explosi�n si existe
        explosionEffect?.Execute(transform.position, gameObject);

        // Notificar al pool
        OnExplode?.Invoke();

        // Si no hay pool, destruir el objeto
        if (OnExplode == null)
            Destroy(gameObject);
    }

    public void ResetMissile()
    {
        exploded = false;
        targetSet = false;

        // Reset Rigidbody
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = true;
        }

        // Reset transform
        transform.rotation = Quaternion.identity;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
