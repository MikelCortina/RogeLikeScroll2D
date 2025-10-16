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

        // Usar fixedDeltaTime porque estamos en FixedUpdate
        cooldown -= Time.fixedDeltaTime;

        float radius = StatsManager.Instance.RuntimeStats.radius;
        Collider2D[] hits = Physics2D.OverlapCircleAll(firePoint.position, radius, enemyLayer);

        // Buscar el objetivo más cercano
        Collider2D closest = null;
        float minDistSqr = Mathf.Infinity;
        Vector2 origin = firePoint.position;

        foreach (Collider2D c in hits)
        {
            if (c == null || !c.CompareTag(enemyTag)) continue;

            Vector2 toTarget = (Vector2)c.bounds.center - origin;
            float distSqr = toTarget.sqrMagnitude;
            if (distSqr < minDistSqr)
            {
                minDistSqr = distSqr;
                closest = c;
            }
        }

        if (closest != null && cooldown <= 0f)
        {
            ShootAt(closest);
            cooldown = 1f / Mathf.Max(0.0001f, StatsManager.Instance.RuntimeStats.fireRate);
        }
    }

    void ShootAt(Collider2D target)
    {
        if (projectilePrefab == null || firePoint == null || target == null) return;

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile2D p = proj.GetComponent<Projectile2D>();
        if (p != null)
        {
            float projectileSpeed = StatsManager.Instance.RuntimeStats.projectileSpeed;
            // Le pasamos la transform del objetivo para homing
            p.Initialize(target.transform, projectileSpeed, gameObject);
        }
        else
        {
            // Si el prefab no tiene Projectile2D, lo lanzamos a la posición actual del objetivo (fallback)
            Vector2 dir = ((Vector2)target.bounds.center - (Vector2)firePoint.position).normalized;
            Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float projectileSpeed = StatsManager.Instance.RuntimeStats.projectileSpeed;
                rb.linearVelocity = dir * projectileSpeed;
            }
        }
    }

    //Simplemente muestra el radio de alcance del arma en la escena
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
