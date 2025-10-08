using UnityEngine;

public class AreaShooter2D : MonoBehaviour
{
    [Header("Disparo")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    [Tooltip("Velocidad que recibirá el proyectil.")]
    public float projectileSpeed = 8f;
    [Tooltip("Veces por segundo que se dispara a cada enemigo.")]
    public float fireRate = 1f;

    [Header("Detección")]
    [Tooltip("Radio de detección (Gizmos).")]
    public float radius = 5f;
    [Tooltip("Capa de enemigos a detectar.")]
    public LayerMask enemyLayer = ~0;

    [Header("Opciones")]
    [Tooltip("Tag que deben tener los enemigos.")]
    public string enemyTag = "enemigo";
    [Tooltip("Dibuja líneas hacia los objetivos en el editor.")]
    public bool drawLinesToTargets = true;

    private float cooldown = 0f;

    void Reset()
    {
        if (firePoint == null)
            firePoint = transform;
    }

    void Update()
    {
        cooldown -= Time.deltaTime;

        // Detectamos todos los enemigos dentro del radio
        Collider2D[] hits = Physics2D.OverlapCircleAll(firePoint.position, radius, enemyLayer);

        foreach (Collider2D c in hits)
        {
            if (c == null || !c.CompareTag(enemyTag))
                continue;

            // Solo disparar si ha pasado el cooldown
            if (cooldown <= 0f)
            {
                ShootAt(c);
                cooldown = 1f / Mathf.Max(0.0001f, fireRate);
            }
        }
    }

    void ShootAt(Collider2D target)
    {
        if (projectilePrefab == null || firePoint == null)
            return;

        Vector2 dir = ((Vector2)target.bounds.center - (Vector2)firePoint.position).normalized;

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile2D p = proj.GetComponent<Projectile2D>();
        if (p != null)
        {
            p.Initialize(dir, projectileSpeed, gameObject);
        }
        else
        {
            Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = dir * projectileSpeed;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (firePoint == null)
            firePoint = transform;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(firePoint.position, 0.1f);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
        Gizmos.DrawWireSphere(firePoint.position, radius);

#if UNITY_EDITOR
        if (drawLinesToTargets)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(firePoint.position, radius, enemyLayer);
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            foreach (Collider2D c in hits)
            {
                if (c != null && c.CompareTag(enemyTag))
                    Gizmos.DrawLine(firePoint.position, c.bounds.center);
            }
        }
#endif
    }
}
