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

    // El punto de disparo activo que se usará en el método Shoot
    private Transform activeFirePoint;

    public GameObject projectilePrefab;
    public int poolSize = 20;

    // --- RAYCAST Y LAYERS (El resto de variables se mantienen igual) ---
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

    [SerializeField] private AudioSource shootAudioSource; // AudioSource desde inspector
    [SerializeField] private AudioClip shootClip;          // Clip de disparo desde inspector

    // --- NUEVA VARIABLE PARA ALTERNAR LOS PUNTOS DE DISPARO ---
    private bool useLeftFirePoint = true; // Empieza usando el izquierdo

    public CartridgeEjector2D cartridgeEjector2D; // Referencia al eyectador de cartuchos

    void Start()
    {
        mainCamera = Camera.main;
        projectileSpeed = StatsManager.Instance.RuntimeStats.projectileSpeed;

        // Inicializar el punto de disparo activo al inicio
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

            ToggleFirePoint();
            Shoot(mouseWorldPos);

            if (cartridgeEjector2D != null)
            {
                cartridgeEjector2D.Eject();
            }
            float fireRate = Mathf.Max(0.0001f, StatsManager.Instance.RuntimeStats.fireRate);
            shootTimer = 1f / fireRate;
        }
    }

    /// <summary>
    /// Alterna el punto de disparo activo entre el izquierdo y el derecho.
    /// </summary>
    private void ToggleFirePoint()
    {
        // Cambiamos el estado
        useLeftFirePoint = !useLeftFirePoint;

        // Asignamos el punto activo
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
            // Si el derecho es nulo, aseguramos usar el izquierdo
            activeFirePoint = firePointLeft;
            useLeftFirePoint = true;
        }
        else
        {
            activeFirePoint = null; // Ambos nulos, no hay punto de disparo
        }
    }

    void InitPool()
    {
        // ... (Se mantiene igual)
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
        // ... (Se mantiene igual)
        foreach (var proj in projectilePool)
            if (!proj.activeInHierarchy) return proj;

        GameObject newProj = Instantiate(projectilePrefab);
        newProj.SetActive(false);
        projectilePool.Add(newProj);
        return newProj;
    }

    void Shoot(Vector2 targetPos)
    {
        // Ahora comprueba el punto de disparo activo
        if (activeFirePoint == null) return;

        // --- SONIDO ---
        if (shootAudioSource != null && shootClip != null)
        {
            shootAudioSource.PlayOneShot(shootClip);
        }

        // Calcular dirección del disparo usando el activeFirePoint
        Vector2 dir = (targetPos - (Vector2)activeFirePoint.position).normalized;

        // --- RAYCAST con radio ---
        float fireRange = maxRange;
        float rayRadius = 0.1f;
        RaycastHit2D hit = Physics2D.CircleCast(activeFirePoint.position, rayRadius, dir, fireRange, enemyLayer);

        // ... (Lógica de impacto y daño se mantiene igual)

        // Datos para el visual: por defecto no hay impacto
        bool hitSomething = false;
        Vector2 hitPoint = (Vector2)activeFirePoint.position + dir * fireRange; // punto por defecto (max range)
        Transform hitTransform = null;

        if (hit.collider != null)
        {
            // Lógica de impacto y daño...
            if (hit.collider.CompareTag(enemyTag))
            {
                // ... (Daño a enemigo)
                hitSomething = true;
                hitPoint = hit.point;
                EnemyBase enemy = hit.collider.GetComponentInParent<EnemyBase>();
                if (enemy != null)
                {
                    hitTransform = enemy.transform;
                    float dmg = StatsCommunicator.Instance.CalculateGunDamage();
                    enemy.TakeContactDamage(dmg);
                }
            }
            // Lógica de impacto y destrucción de proyectil enemigo...
            else if (hit.collider.CompareTag(enemyProjectileTag))
            {
                hitSomething = true;
                hitPoint = hit.point;
                Projectile2D enemyProjComponent = hit.collider.GetComponentInParent<Projectile2D>();
                if (enemyProjComponent != null)
                {
                    enemyProjComponent.gameObject.SetActive(false);
                }
                else
                {
                    Destroy(hit.collider.gameObject);
                }
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
        // Usa el activeFirePoint para la posición de inicio
        visual.transform.position = activeFirePoint.position;
        visual.SetActive(true);

        Projectile2D p = visual.GetComponent<Projectile2D>();
        if (p != null)
        {
            p.InitializeVisual(dir, projectileSpeed, hitPoint, hitTransform);
        }

#if UNITY_EDITOR
        // Usa el activeFirePoint para el Debug.DrawRay
        Debug.DrawRay(activeFirePoint.position, dir * fireRange, Color.yellow, 0.15f);
#endif
    }
}