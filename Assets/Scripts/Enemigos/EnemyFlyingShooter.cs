using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyFlyingShooter : EnemyBase
{
    [Header("Flying Movement")]
    [SerializeField] private float flyingSpeed = 3.5f;
    [SerializeField] private float hoverAmplitude = 0.25f;
    [SerializeField] private float hoverFrequency = 1.2f;
    [SerializeField] private float wanderRadius = 0.5f; // radio de movimiento alrededor del homePoint
    [SerializeField] private float maxHomePointDistance = 1f; // distancia máxima desde homePoint actual
    [SerializeField] private float arrivalRadius = 0.15f; // radio donde el enemigo deja de vibrar

    [Header("Shooting")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float minFireRange = 1f;
    [SerializeField] private float maxFireRange = 4f;
    private float fireRange;
    [SerializeField] private float projectileSpeed = 9f;
    [SerializeField] private float burstDelay = 0.12f;
    [SerializeField] private int burstCount = 1;

    [Header("Behavior Options")]
    [SerializeField] private bool canShootWhileMoving = true;

    [Header("Attack Cooldown Override")]
    [SerializeField] private float attackRate = 1f;
    private float attackCooldownTimer = 0f;

    private TargetZone targetZone; // Guardamos la referencia
    private Vector2 homeViewportPoint; // Punto en viewport relativo a la cámara
    private Vector2 velocity = Vector2.zero; // para SmoothDamp

    private float changeHomePointTimer = 0f;
    [SerializeField] private float changeHomePointInterval = 5f; // cada X segundos cambiar el homePoint
   
    public Animator anim;

    public string animationName = "EnemyShoot";

    protected override void Awake()
    {
        base.Awake();
        rb.gravityScale = 0f;
    }

    protected override void Start()
    {
        base.Start();

        fireRange = Random.Range(minFireRange, maxFireRange);

        if (firePoint == null)
        {
            var fp = transform.Find("FirePoint");
            if (fp != null) firePoint = fp;
        }

        if (firePoint == null) Debug.LogWarning($"{name}: firePoint no asignado!");
        if (projectilePrefab == null) Debug.LogWarning($"{name}: projectilePrefab no asignado!");

        targetZone = FindObjectOfType<TargetZone>();
        if (targetZone != null)
        {
            homeViewportPoint = new Vector2(
                Random.Range(targetZone.minViewport.x, targetZone.maxViewport.x),
                Random.Range(targetZone.minViewport.y, targetZone.maxViewport.y)
            );
        }
        else
        {
            Vector3 viewport = Camera.main.WorldToViewportPoint(transform.position);
            homeViewportPoint = new Vector2(viewport.x, viewport.y);
        }
    }

    private void Update()
    {
        if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;
        TryShoot();
    }

    private void FixedUpdate()
    {
        if (IsCurrentlyKnockedBack()) return;

        MoveAroundHomePoint();
    }

    private void MoveAroundHomePoint()
    {
        if (targetZone != null)
        {
            changeHomePointTimer -= Time.fixedDeltaTime;
            if (changeHomePointTimer <= 0f)
            {
                homeViewportPoint = new Vector2(
                    Random.Range(targetZone.minViewport.x, targetZone.maxViewport.x),
                    Random.Range(targetZone.minViewport.y, targetZone.maxViewport.y)
                );

                changeHomePointTimer = changeHomePointInterval;
            }
        }

        // Convertir viewport a world point
        Vector2 homePoint = Camera.main.ViewportToWorldPoint(homeViewportPoint);

        Vector2 toTarget = homePoint - rb.position;

        if (toTarget.magnitude < arrivalRadius)
        {
            rb.linearVelocity = Vector2.zero; // ya llegó
        }
        else
        {
            Vector2 newPos = Vector2.SmoothDamp(rb.position, homePoint, ref velocity, 0.2f);
            Vector2 desiredVelocity = (newPos - rb.position) / Time.fixedDeltaTime;
            rb.linearVelocity = Vector2.ClampMagnitude(desiredVelocity, flyingSpeed);
        }
    }
    private void TryShoot()
    {
        if (attackCooldownTimer > 0f) return;
        attackCooldownTimer = 1f / attackRate;
        PerformAttack();
    }

    protected override void PerformAttack()
    {
        if (projectilePrefab == null || firePoint == null || target == null) return;

        if (burstCount <= 1) ShootOne();
        else StartCoroutine(ShootBurst());
    }

    private IEnumerator ShootBurst()
    {
        for (int i = 0; i < burstCount; i++)
        {
            ShootOne();
            yield return new WaitForSeconds(burstDelay);
        }
    }

    private void ShootOne()
    {
        anim.Play(animationName);
        Vector2 aimDir = ((Vector2)target.position - (Vector2)firePoint.position).normalized;
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile2DEnemy projScript = proj.GetComponent<Projectile2DEnemy>();
        if (projScript != null) projScript.owner = gameObject;

        Rigidbody2D prb = proj.GetComponent<Rigidbody2D>();
        if (prb != null)
            prb.linearVelocity = aimDir * projectileSpeed;

        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        proj.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        
    }

    private bool IsCurrentlyKnockedBack() => isKnockedBack;

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.cyan;

        if (Application.isPlaying)
        {
            Vector2 homePoint = Camera.main.ViewportToWorldPoint(homeViewportPoint);
            Gizmos.DrawWireSphere(homePoint, wanderRadius);
        }

        if (firePoint != null) Gizmos.DrawSphere(firePoint.position, 0.05f);
    }
}
