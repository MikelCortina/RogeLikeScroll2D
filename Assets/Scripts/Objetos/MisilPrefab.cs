// BombProjectile.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class MisilProjectile : MonoBehaviour
{
    [Tooltip("Prefab de la explosión que se instanciará al impactar.")]
    public GameObject explosionPrefab;
    [Tooltip("Tiempo máximo antes de auto-destruirse (segundos).")]
    public float lifetime = 10f;
    [Tooltip("Radio del daño (si implementas daño).")]
    public float explosionRadius = 1.5f;
    [Tooltip("Fuerza del empuje al explotar.")]
    public float explosionForce = 200f;

    private Vector3 target;
    private bool targetSet = false;
    private bool exploded = false;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void SetTarget(Vector3 worldTarget)
    {
        target = worldTarget;
        targetSet = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (exploded) return;
        Explode();
    }

    private void Explode()
    {
        exploded = true;
        // Efecto visual
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        // Si quieres aplicar daño o fuerza a rigidbodies dentro del radio:
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            Rigidbody2D rb = hit.attachedRigidbody;
            if (rb != null)
            {
                Vector2 dir = rb.position - (Vector2)transform.position;
                float dist = Mathf.Max(dir.magnitude, 0.01f);
                Vector2 force = dir.normalized * (explosionForce / dist);
                rb.AddForce(force);
            }
            // Aquí podrías invocar una interfaz IDamageable, etc.
        }

        // Destruir la bomba
        Destroy(gameObject);
    }

    // Solo para depuración / editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
