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

    [Header("Raycast daño")]
    public float maxRange = 20f;
    public LayerMask enemyLayer;
    public string enemyTag = "enemigo";

    [Header("Visual")]
    public float projectileSpeed;
    public Camera mainCamera;

    private float shootTimer = 0f;
    private List<GameObject> projectilePool;

    [SerializeField] private AudioSource shootAudioSource; // AudioSource desde inspector
    [SerializeField] private AudioClip shootClip;          // Clip de disparo desde inspector

    void Start()
    {
        mainCamera = Camera.main;
        projectileSpeed = StatsManager.Instance.RuntimeStats.projectileSpeed;   
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
            if (!proj.activeInHierarchy) return proj;

        GameObject newProj = Instantiate(projectilePrefab);
        newProj.SetActive(false);
        projectilePool.Add(newProj);
        return newProj;
    }

    void Shoot(Vector2 targetPos)
    {
        if (firePoint == null) return;

        // --- SONIDO ---
        if (shootAudioSource != null && shootClip != null)
        {
            shootAudioSource.PlayOneShot(shootClip);
        }

        // Calcular dirección del disparo
        Vector2 dir = (targetPos - (Vector2)firePoint.position).normalized;

        // --- RAYCAST con radio ---
        float fireRange = maxRange;
        float rayRadius = 0.1f;
        RaycastHit2D hit = Physics2D.CircleCast(firePoint.position, rayRadius, dir, fireRange, enemyLayer);

        // Datos para el visual: por defecto no hay impacto
        bool hitSomething = false;
        Vector2 hitPoint = (Vector2)firePoint.position + dir * fireRange; // punto por defecto (max range)
        Transform hitTransform = null;

        if (hit.collider != null && hit.collider.CompareTag(enemyTag))
        {
            hitSomething = true;
            hitPoint = hit.point;

           EnemyBase enemy = hit.collider.GetComponentInParent<EnemyBase>();

            if (enemy != null)
            {
                hitTransform = enemy.transform;
                float dmg = StatsCommunicator.Instance.CalculateDamage();
                enemy.TakeContactDamage(dmg);
            }
        }

        // --- EJECUTAR EFECTOS ACTIVOS ---
        EffectSpawner effectSpawner = GetComponent<EffectSpawner>();
        if (effectSpawner != null)
        {
            foreach (var activeEffect in RunEffectManager.Instance.GetActiveEffects())
            {
                effectSpawner.TriggerEffect(activeEffect, hitPoint, gameObject);
            }
        }

        // --- EFECTO VISUAL DEL PROYECTIL ---
        GameObject visual = GetPooledProjectile();
        visual.transform.position = firePoint.position;
        visual.SetActive(true);

        Projectile2D p = visual.GetComponent<Projectile2D>();
        if (p != null)
        {
            // Inicializa visual con dirección, velocidad y punto de impacto
            p.InitializeVisual(dir, projectileSpeed, hitPoint, hitTransform);
        }

#if UNITY_EDITOR
        Debug.DrawRay(firePoint.position, dir * fireRange, Color.yellow, 0.15f);
#endif
    }

}
