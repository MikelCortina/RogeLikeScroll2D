using UnityEngine;

public class AreaShooter2D : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject upgradePanel; // 👈 arrastra aquí el panel de la UI en el inspector

    [Header("Disparo")]
    public Transform firePoint;
    public GameObject projectilePrefab;

    [Header("Homing")]
    public float homingRadius = 3f;
    public LayerMask enemyLayer = ~0;
    public string enemyTag = "enemigo";

    private float cooldown = 0f;
    private Camera mainCamera;

    private CursorLockMode savedLockState;
    private bool savedVisible;
    private float nextShootTime = 0f;

    void Start()
        {
            savedLockState = Cursor.lockState;
            savedVisible = Cursor.visible;
            mainCamera = Camera.main;
        }


    void Update()
    {
        // Bloquear disparo si hay panel abierto
        if (upgradePanel != null && upgradePanel.activeSelf)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        // Restaurar cursor
        if (Cursor.visible != savedVisible)
            Cursor.visible = savedVisible;
        if (Cursor.lockState != savedLockState)
            Cursor.lockState = savedLockState;

        // Tiempo actual
        float currentTime = Time.time;

        // Solo disparar si ha pasado el tiempo suficiente
        if (currentTime >= nextShootTime)
        {
            Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            TryShoot(mouseWorldPos);

            // Actualizar próximo disparo
            float fireRate = Mathf.Max(0.0001f, StatsManager.Instance.RuntimeStats.fireRate);
            nextShootTime = currentTime + (1f / fireRate);
        }
    }
    void TryShoot(Vector2 mouseWorldPos)
    {
        Collider2D target = FindClosestEnemyInCircle(mouseWorldPos);

        if (target != null)
            ShootWithHoming(target);
        else
            ShootStraight(mouseWorldPos);

        cooldown = 1f / Mathf.Max(0.0001f, StatsManager.Instance.RuntimeStats.fireRate);
    }

    Collider2D FindClosestEnemyInCircle(Vector2 searchCenter)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(searchCenter, homingRadius, enemyLayer);
        Collider2D closest = null;
        float minDistSqr = Mathf.Infinity;

        foreach (Collider2D c in hits)
        {
            if (c == null || !c.CompareTag(enemyTag)) continue;
            float distSqr = ((Vector2)c.bounds.center - searchCenter).sqrMagnitude;
            if (distSqr < minDistSqr)
            {
                minDistSqr = distSqr;
                closest = c;
            }
        }

        return closest;
    }

    void ShootWithHoming(Collider2D target)
    {
        if (projectilePrefab == null || firePoint == null || target == null) return;

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile2D p = proj.GetComponent<Projectile2D>();

        if (p != null)
        {
            float projectileSpeed = StatsManager.Instance.RuntimeStats.projectileSpeed;
            p.Initialize(target.transform, projectileSpeed, gameObject);
        }
        else
        {
            ShootStraight(target.bounds.center);
        }
    }

    void ShootStraight(Vector2 targetPosition)
    {
        if (projectilePrefab == null || firePoint == null) return;

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Vector2 dir = (targetPosition - (Vector2)firePoint.position).normalized;
        float projectileSpeed = StatsManager.Instance.RuntimeStats.projectileSpeed;

        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = dir * projectileSpeed;
    }
}
