using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaShooter2D : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject upgradePanel;

    [Header("Disparo")]
    public Transform firePoint;
    [Tooltip("Prefab visual opcional; puede contener Projectile2D para la animación")]
    public GameObject projectilePrefab;
    public int poolSize = 20;
    public float maxStraightRange = 50f; // distancia máxima del raycast en disparo recto

    [Header("Homing")]
    public float homingRadius = 10f;
    public LayerMask enemyLayer = ~0;
    public string enemyTag = "enemigo";

    public Camera mainCamera;

    private float shootTimer = 0f;
    private List<GameObject> projectilePool;
    private Projectile2D cachedPrefabProjectileScript; // info del prefab (isExplosive, explosionRadius, effectSpawner)

    void Start()
    {
        mainCamera = Camera.main;
        InitPool();
    }

    void Update()
    {
        if (upgradePanel != null && upgradePanel.activeSelf) return;

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
        if (projectilePrefab == null) return;

        cachedPrefabProjectileScript = projectilePrefab.GetComponent<Projectile2D>();

        for (int i = 0; i < poolSize; i++)
        {
            GameObject proj = Instantiate(projectilePrefab);
            proj.SetActive(false);
            projectilePool.Add(proj);
        }
    }

    GameObject GetPooledProjectile()
    {
        if (projectilePrefab == null) return null;

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

    void ShootHoming(Collider2D targetCollider)
    {
        if (targetCollider == null || firePoint == null) return;

        Vector2 origin = firePoint.position;
        Vector2 targetCenter = (Vector2)targetCollider.bounds.center;

        // Si hay obstáculo entre origen y objetivo, raycast devolverá el hit (pero igualmente queremos dañar al objetivo si está en rango homing).
        RaycastHit2D checkHit = Physics2D.Raycast(origin, (targetCenter - origin).normalized, Vector2.Distance(origin, targetCenter), ~0);

        Vector2 impactPoint = targetCenter;
        Collider2D hitCollider = targetCollider;

        if (checkHit.collider != null)
        {
            // Si el raycast golpea algo distinto al propio objetivo, usamos el punto de colisión — excepto si es el mismo collider
            if (checkHit.collider != targetCollider)
            {
                impactPoint = checkHit.point;
                hitCollider = checkHit.collider;
            }
            else
            {
                impactPoint = checkHit.point;
                hitCollider = checkHit.collider;
            }
        }

        // Prefab info (cached)
        bool isExplosive = cachedPrefabProjectileScript != null && cachedPrefabProjectileScript.isExplosive;
        float explosionRadius = cachedPrefabProjectileScript != null ? cachedPrefabProjectileScript.explosionRadius : 0f;

        if (isExplosive)
        {
            ExplodeAt(impactPoint, explosionRadius);
        }
        else
        {
            // Preferir dañar el objetivo elegido si existe
            EnemyBase enemyToDamage = null;
            if (targetCollider != null)
                enemyToDamage = targetCollider.GetComponentInParent<EnemyBase>();

            // Si no encontramos EnemyBase en el targetCollider, intentar con el collider golpeado por raycast
            if (enemyToDamage == null && hitCollider != null)
                enemyToDamage = hitCollider.GetComponentInParent<EnemyBase>();

            if (enemyToDamage != null)
            {
                ApplyDamageToEnemy(enemyToDamage, impactPoint);
            }
        }

        // Spawn visual (si existe)
        SpawnVisual(origin, impactPoint);
    }

    void ShootStraight(Vector2 targetPos)
    {
        if (firePoint == null) return;

        Vector2 origin = firePoint.position;
        Vector2 dir = (targetPos - origin).normalized;
        float maxDist = maxStraightRange;

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, maxDist, ~0);
        Vector2 hitPoint = origin + dir * maxDist;
        Collider2D hitCollider = null;

        if (hit.collider != null)
        {
            hitPoint = hit.point;
            hitCollider = hit.collider;
        }

        // Prefab info
        bool isExplosive = cachedPrefabProjectileScript != null && cachedPrefabProjectileScript.isExplosive;
        float explosionRadius = cachedPrefabProjectileScript != null ? cachedPrefabProjectileScript.explosionRadius : 0f;

        if (isExplosive)
        {
            ExplodeAt(hitPoint, explosionRadius);
        }
        else
        {
            if (hitCollider != null)
            {
                EnemyBase enemy = hitCollider.GetComponentInParent<EnemyBase>();
                if (enemy != null)
                {
                    ApplyDamageToEnemy(enemy, hitPoint);
                }
            }
        }

        SpawnVisual(origin, hitPoint);
    }

    void ApplyDamageToEnemy(EnemyBase enemy, Vector2 hitPos)
    {
        if (enemy == null) return;

        float dmg = StatsCommunicator.Instance.CalculateDamage();
        enemy.TakeContactDamage(dmg);

        // Ejecutar efectos (replicando tu lógica previa)
        EffectSpawner effectSpawner = cachedPrefabProjectileScript != null ? cachedPrefabProjectileScript.effectSpawner : null;

        if (effectSpawner != null && RunEffectManager.Instance != null)
        {
            foreach (var activeEffect in RunEffectManager.Instance.GetActiveEffects())
            {
                if (effectSpawner.effects.Contains(activeEffect))
                {
                    if (activeEffect is IEffect ie)
                        ie.Execute(hitPos, gameObject);
                }
            }
        }
    }

    void ExplodeAt(Vector2 center, float radius)
    {
        float dmg = StatsCommunicator.Instance.CalculateDamage();
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyLayer);
        HashSet<EnemyBase> damaged = new HashSet<EnemyBase>();

        foreach (var c in hits)
        {
            if (c == null || !c.CompareTag(enemyTag)) continue;
            EnemyBase enemy = c.GetComponentInParent<EnemyBase>();
            if (enemy != null && !damaged.Contains(enemy))
            {
                enemy.TakeContactDamage(dmg);
                damaged.Add(enemy);
            }
        }

        EffectSpawner effectSpawner = cachedPrefabProjectileScript != null ? cachedPrefabProjectileScript.effectSpawner : null;

        if (effectSpawner != null && RunEffectManager.Instance != null)
        {
            foreach (var activeEffect in RunEffectManager.Instance.GetActiveEffects())
            {
                if (effectSpawner.effects.Contains(activeEffect))
                {
                    if (activeEffect is IEffect ie)
                        ie.Execute(center, gameObject);
                }
            }
        }
    }

    void SpawnVisual(Vector2 start, Vector2 end)
    {
        GameObject visual = GetPooledProjectile();
        if (visual == null) return;

        // Asegurarse de activar y resetear
        visual.SetActive(true);
        visual.transform.position = start;
        visual.transform.rotation = Quaternion.identity;

        // Desactivar físicas del visual si las tiene (para evitar que "se quede quieto" por dinámica)
        Rigidbody2D rb = visual.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        Projectile2D visualScript = visual.GetComponent<Projectile2D>();
        float speed = Mathf.Max(0.001f, StatsManager.Instance.RuntimeStats.projectileSpeed);
        if (visualScript != null)
        {
            visualScript.PlayVisual(start, end, speed);
        }
        else
        {
            // fallback: mover instantáneamente y desactivar pronto
            visual.transform.position = end;
            StartCoroutine(DeactivateAfter(visual, 0.12f));
        }
    }

    System.Collections.IEnumerator DeactivateAfter(GameObject go, float t)
    {
        yield return new WaitForSeconds(t);
        if (go != null) go.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(mouseWorldPos, homingRadius);
    }
}
