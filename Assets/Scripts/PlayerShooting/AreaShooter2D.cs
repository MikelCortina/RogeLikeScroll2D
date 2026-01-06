using System.Collections.Generic;
using UnityEngine;

public class AreaShooter2D : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject upgradePanel;

    [Header("Disparo")]
    [Tooltip("Punto de disparo para el hueso del lado izquierdo.")]
    public Transform firePointLeft;
    [Tooltip("Punto de disparo para el hueso del lado derecho.")]
    public Transform firePointRight;

    [Range(0f, 45f)]
    public float maxSpreadAngle = 5f;

    [Range(0f, 3f)]
    public float aimSpreadRadius = 0.5f;

    private Transform activeFirePoint;

    public GameObject projectilePrefab;
    public int poolSize = 20;

    [Header("Raycast daño")]
    public float maxRange = 20f;
    public LayerMask enemyLayer;
    public string enemyTag = "enemigo";

    [Header("Proyectiles enemigos")]
    public string enemyProjectileTag = "enemyprojectile";

    [Header("Visual")]
    public float projectileSpeed;
    public Camera mainCamera;

    private float shootTimer = 0f;
    private List<GameObject> projectilePool;

    [SerializeField] private AudioSource shootAudioSource;
    [SerializeField] private AudioClip shootClip;

    private bool useLeftFirePoint = true;

    public CartridgeEjector2D cartridgeEjector2D;
    public ParticleSystem fireSpriteRight;
    public ParticleSystem fireSpriteLeft;

    void Start()
    {
        mainCamera = Camera.main;
        projectileSpeed = StatsManager.Instance.RuntimeStats.projectileSpeed;
        activeFirePoint = firePointLeft;
        InitPool();
    }

    void Update()
    {
        if (upgradePanel != null && upgradePanel.activeSelf)
            return;

        shootTimer -= Time.deltaTime;

        if (shootTimer <= 0f)
        {
            Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 randomOffset = Random.insideUnitCircle * aimSpreadRadius;
            Vector2 randomizedTargetPos = mouseWorldPos + randomOffset;

            ToggleFirePoint();
            Shoot(randomizedTargetPos);

            if (cartridgeEjector2D != null)
            {
                cartridgeEjector2D.Eject();
            }

            float fireRate = Mathf.Max(0.0001f, StatsManager.Instance.RuntimeStats.fireRate);
            shootTimer = 1f / fireRate;
        }
    }

    private void ToggleFirePoint()
    {
        useLeftFirePoint = !useLeftFirePoint;
        if (useLeftFirePoint && firePointLeft != null)
        {
            activeFirePoint = firePointLeft;
        }
        else if (firePointRight != null)
        {
            activeFirePoint = firePointRight;
        }
        else if (firePointLeft != null)
        {
            activeFirePoint = firePointLeft;
            useLeftFirePoint = true;
        }
        else
        {
            activeFirePoint = null;
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

    public GameObject GetPooledProjectile()
    {
        foreach (var proj in projectilePool)
            if (!proj.activeInHierarchy) return proj;

        GameObject newProj = Instantiate(projectilePrefab);
        newProj.SetActive(false);
        projectilePool.Add(newProj);
        return newProj;
    }

    private int GetPenetrationCount()
    {
        int total = 0;
        foreach (var effect in RunEffectManager.Instance.GetActiveEffects())
        {
            if (effect is PenetrationEffect p)
                total += p.penetrationCount;
        }
        return total;
    }

    void Shoot(Vector2 targetPos)
    {
        fireSpriteLeft.Play();
        fireSpriteRight.Play();

        if (activeFirePoint == null) return;

        if (shootAudioSource != null && shootClip != null)
        {
            shootAudioSource.PlayOneShot(shootClip);
        }

        Vector2 dir = (targetPos - (Vector2)activeFirePoint.position).normalized;
        float spread = Random.Range(-maxSpreadAngle, maxSpreadAngle);
        dir = Quaternion.Euler(0, 0, spread) * dir;

        // ========= NUEVO: Ejecutar efectos de disparo como SpreadShot =========
        foreach (var activeEffect in RunEffectManager.Instance.GetActiveEffects())
        {
            if (activeEffect is SpreadShotEffect spreadEffect)
            {
                spreadEffect.ExecuteOnShoot(
                    activeFirePoint.position,
                    dir,
                    gameObject
                );
            }
        }
        // =====================================================================

        float fireRange = maxRange;
        float rayRadius = 0.1f;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(activeFirePoint.position, rayRadius, dir, fireRange, enemyLayer);

        Vector2 finalHitPoint = (Vector2)activeFirePoint.position + dir * fireRange;
        int penetrationRemaining = GetPenetrationCount();

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null) continue;

            if (hit.collider.CompareTag(enemyProjectileTag))
            {
                Projectile2D enemyProjComponent = hit.collider.GetComponentInParent<Projectile2D>();
                if (enemyProjComponent != null)
                    enemyProjComponent.gameObject.SetActive(false);
                else
                    Destroy(hit.collider.gameObject);
                continue;
            }

            if (hit.collider.CompareTag(enemyTag))
            {
                EnemyBase enemy = hit.collider.GetComponentInParent<EnemyBase>();
                if (enemy != null)
                {
                    float dmg = StatsCommunicator.Instance.CalculateGunDamage();
                    enemy.TakeContactDamage(dmg, true);
                    ConsoleManager.Instance.Log($"Projectile damage dealed: {dmg}");

                    Vector2 knockbackDir = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
                    enemy.ApplyKnockback(knockbackDir * StatsManager.Instance.RuntimeStats.knockback / 7.5f);
                }

                // Ejecutar efectos de impacto (los que usan Execute normal)
                EffectSpawner effectSpawner1 = GetComponent<EffectSpawner>();
                if (effectSpawner1 != null)
                {
                    foreach (var activeEffect in RunEffectManager.Instance.GetActiveEffects())
                    {
                        if (activeEffect is ProjectileEffect effect && !(effect is SpreadShotEffect))
                            effect.Execute(hit.point, gameObject);
                    }
                }

                penetrationRemaining--;
                if (penetrationRemaining <= 0)
                {
                    finalHitPoint = hit.point;
                    break;
                }
            }
        }

        // Visual del proyectil principal
        GameObject visual = GetPooledProjectile();
        visual.transform.position = activeFirePoint.position;
        visual.SetActive(true);
        Projectile2D p = visual.GetComponent<Projectile2D>();
        if (p != null)
        {
            p.InitializeVisual(dir, projectileSpeed, finalHitPoint, null);
        }
    }

    public Vector2 CurrentFirePointPosition
    {
        get
        {
            return activeFirePoint != null ? (Vector2)activeFirePoint.position : (Vector2)transform.position;
        }
    }
}