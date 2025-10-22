using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile2D : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D col;
    private float timer;
    public float lifeTime = 2f;

    [Header("Visual Effects")]
    public ParticleSystem hitParticles; // Partículas al colisionar
    public TrailRenderer trailRenderer;  // Trail del proyectil

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (col != null) col.isTrigger = true;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        if (trailRenderer != null)
            trailRenderer.emitting = false;
    }

    public void InitializeVisual(Vector2 direction, float speed)
    {
        if (rb == null) return;

        timer = lifeTime;

        rb.linearVelocity = direction.normalized * speed;

        float ang = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang);

        // Activar trail
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            trailRenderer.emitting = true;
        }

        // Activar partículas iniciales si quieres (opcional)
        if (hitParticles != null && !hitParticles.isPlaying)
            hitParticles.Play();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
            ReturnToPool();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Partículas de impacto
        if (hitParticles != null)
            hitParticles.Play();

        ReturnToPool();
    }

    public void ReturnToPool()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // Desactivar trail
        if (trailRenderer != null)
            trailRenderer.emitting = false;

        gameObject.SetActive(false);
    }
}
