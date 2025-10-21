using System.Collections.Generic;
using UnityEngine;

public class AreaShooter2D : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject upgradePanel;

    [Header("Disparo")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public int poolSize = 20;

    [Header("Homing")]
    public float homingRadius = 10f;
    public LayerMask enemyLayer = ~0;
    public string enemyTag = "enemigo";

    public Camera mainCamera;

    private float shootTimer = 0f;
    private List<GameObject> projectilePool;

    void Start()
    {
        mainCamera = Camera.main;
        InitPool();
    }

    void Update()
    {
        if (upgradePanel != null && upgradePanel.activeSelf)
            return;

        shootTimer -= Time.deltaTime;

        if (shootTimer <= 0f && Input.GetMouseButton(0))
        {
            Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Shoot(mouseWorldPos);

            float fireRate = Mathf.Max(0.0001f, StatsManager.Instance.RuntimeStats.fireRate);
            shootTimer = 1f / fireRate;
        }
    }

    void InitPool()
    {
        projectilePool = new List<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject proj = Instantiate(projectilePrefab);
            proj.SetActive(false);
            projectilePool.Add(proj);
        }
    }

    GameObject GetPooledProjectile()
    {
        foreach (var proj in projectilePool)
        {
            if (!proj.activeInHierarchy)
                return proj;
        }
        GameObject newProj = Instantiate(projectilePrefab);
        newProj.SetActive(false);
        projectilePool.Add(newProj);
        return newProj;
    }

    void Shoot(Vector2 targetPos)
    {
        Collider2D target = FindClosestEnemy(targetPos);
        if (target != null)
            ShootHoming(target);
        else
            ShootStraight(targetPos);
    }

    Collider2D FindClosestEnemy(Vector2 position)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, homingRadius, enemyLayer);
        Collider2D closest = null;
        float minDist = Mathf.Infinity;

        foreach (Collider2D c in hits)
        {
            if (c == null || !c.CompareTag(enemyTag)) continue;
            float dist = ((Vector2)c.bounds.center - position).sqrMagnitude;
            if (dist < minDist)
            {
                minDist = dist;
                closest = c;
            }
        }

        return closest;
    }

    void ShootHoming(Collider2D target)
    {
        if (target == null || firePoint == null) return;

        GameObject proj = GetPooledProjectile();
        proj.transform.position = firePoint.position;
        proj.SetActive(true);

        Projectile2D p = proj.GetComponent<Projectile2D>();
        if (p != null)
        {
            float speed = StatsManager.Instance.RuntimeStats.projectileSpeed;
            p.Initialize(target.transform, speed, gameObject); // Aseg�rate de que Projectile2D haga homing
        }
    }

    void ShootStraight(Vector2 targetPos)
    {
        if (firePoint == null) return;

        GameObject proj = GetPooledProjectile();
        proj.transform.position = firePoint.position;
        proj.SetActive(true);

        Vector2 dir = (targetPos - (Vector2)firePoint.position).normalized;
        float speed = StatsManager.Instance.RuntimeStats.projectileSpeed;

        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = dir * speed;
        }
    }

    // Visualizar el radio de homing en la escena
    void OnDrawGizmosSelected()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(mouseWorldPos, homingRadius);
    }
}
