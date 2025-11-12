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

    [Tooltip("Grado máximo de imprecisión en grados.")]
    [Range(0f, 45f)]
    public float maxSpreadAngle = 5f;

    [Tooltip("Radio de dispersión alrededor del cursor para disparos aleatorios.")]
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

        if (shootTimer <= 0f && Input.GetMouseButton(0))
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

    GameObject GetPooledProjectile()
    {
        foreach (var proj in projectilePool)
            if (!proj.activeInHierarchy) return proj;

        GameObject newProj = Instantiate(projectilePrefab);
        newProj.SetActive(false);
        projectilePool.Add(newProj);
        return newProj;
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

        float fireRange = maxRange;
        float rayRadius = 0.1f;

        // 🔹 En lugar de un solo hit, obtenemos todos los impactos
        RaycastHit2D[] hits = Physics2D.CircleCastAll(activeFirePoint.position, rayRadius, dir, fireRange, enemyLayer);

        bool hitSomething = false;
        Vector2 hitPoint = (Vector2)activeFirePoint.position + dir * fireRange;
        Transform hitTransform = null;

        float knockback = StatsManager.Instance.RuntimeStats.knockback;

        // 🔹 Recorremos todos los impactos en orden de distancia
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null) continue;

            // Si es proyectil enemigo → destruirlo y seguir
            if (hit.collider.CompareTag(enemyProjectileTag))
            {
                Projectile2D enemyProjComponent = hit.collider.GetComponentInParent<Projectile2D>();
                if (enemyProjComponent != null)
                {
                    enemyProjComponent.gameObject.SetActive(false);
                }
                else
                {
                    Destroy(hit.collider.gameObject);
                }
                // No rompemos → seguimos buscando enemigos detrás
                continue;
            }

            // Si es enemigo → aplicar daño y detener el rayo ahí
            if (hit.collider.CompareTag(enemyTag))
            {
                hitSomething = true;
                hitPoint = hit.point;
                EnemyBase enemy = hit.collider.GetComponentInParent<EnemyBase>();

                if (enemy != null)
                {
                    hitTransform = enemy.transform;
                    float dmg = StatsCommunicator.Instance.CalculateGunDamage();
                    enemy.TakeContactDamage(dmg, true);

                    Vector2 knockbackDir = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
                    enemy.ApplyKnockback(knockbackDir * knockback / 7.5f);
                }

                // 💥 Solo el primer enemigo recibe daño → rompemos aquí
                break;
            }
        }

        // 🔹 Efectos visuales, igual que antes
        EffectSpawner effectSpawner = GetComponent<EffectSpawner>();
        if (effectSpawner != null)
        {
            foreach (var activeEffect in RunEffectManager.Instance.GetActiveEffects())
            {
                effectSpawner.TriggerEffect(activeEffect, hitPoint, gameObject);
            }
        }

        GameObject visual = GetPooledProjectile();
        visual.transform.position = activeFirePoint.position;
        visual.SetActive(true);

        Projectile2D p = visual.GetComponent<Projectile2D>();
        if (p != null)
        {
            p.InitializeVisual(dir, projectileSpeed, hitPoint, hitTransform);
        }

#if UNITY_EDITOR
        Debug.DrawRay(activeFirePoint.position, dir * fireRange, Color.yellow, 0.15f);
#endif
    }

}