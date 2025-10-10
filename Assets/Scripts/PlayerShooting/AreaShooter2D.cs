using UnityEngine;

public class AreaShooter2D : MonoBehaviour
{
    [Header("Disparo")]
    public Transform firePoint;
    public GameObject projectilePrefab;

    [Header("Detección")]
    public LayerMask enemyLayer = ~0;
    public string enemyTag = "enemigo";
    public bool drawLinesToTargets = true;

    private float cooldown = 0f;


    void Reset()
    {
        if (firePoint == null)
            firePoint = transform;
    }

    void FixedUpdate()
    {
        CirculoMostrar();
        cooldown -= Time.deltaTime;
        float radius = StatsManager.Instance.RuntimeStats.radius;
        Collider2D[] hits = Physics2D.OverlapCircleAll(firePoint.position, radius, enemyLayer);

        foreach (Collider2D c in hits)
        {
            if (c == null || !c.CompareTag(enemyTag)) continue;

            if (cooldown <= 0f)
            {
                ShootAt(c);
                cooldown = 1f / Mathf.Max(0.0001f, StatsManager.Instance.RuntimeStats.fireRate);
            }
        }
    }

    void ShootAt(Collider2D target)
    {
        if (projectilePrefab == null || firePoint == null) return;

        Vector2 dir = ((Vector2)target.bounds.center - (Vector2)firePoint.position).normalized;

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile2D p = proj.GetComponent<Projectile2D>();
        if (p != null)
        {
            float projectileSpeed = StatsManager.Instance.RuntimeStats.projectileSpeed;
            p.Initialize(dir, projectileSpeed, gameObject);
        }
        else
        {
            Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float projectileSpeed = StatsManager.Instance.RuntimeStats.projectileSpeed;
                rb.linearVelocity = dir * projectileSpeed;
            }
        }
    }

    void CirculoMostrar()
    {
        if (firePoint == null)
            firePoint = transform;

        float radius = StatsManager.Instance.RuntimeStats.radius;

        // --- Dibujar círculo ---
        int segments = 32;
        float angleStep = 360f / segments;
        Vector3 prevPoint = firePoint.position + new Vector3(Mathf.Cos(0), Mathf.Sin(0)) * radius;

        for (int i = 1; i <= segments; i++)
        {
            float rad = Mathf.Deg2Rad * (i * angleStep);
            Vector3 newPoint = firePoint.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
            Debug.DrawLine(prevPoint, newPoint, Color.yellow);
            prevPoint = newPoint;
        }

        // --- Dibujar líneas a enemigos ---
        if (drawLinesToTargets)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(firePoint.position, radius, enemyLayer);
            foreach (Collider2D c in hits)
            {
                if (c != null && c.CompareTag(enemyTag))
                    Debug.DrawLine(firePoint.position, c.bounds.center, Color.red);
            }
        }

        // --- Punto central ---
        Debug.DrawRay(firePoint.position, Vector3.up * 0.1f, Color.yellow);
        Debug.DrawRay(firePoint.position, Vector3.right * 0.1f, Color.yellow);
    }
}
