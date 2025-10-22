using System.Collections;
using UnityEngine;
using UnityEngine.ProBuilder;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyFlyingShooter : EnemyBase
{
    [Header("Flying Movement")]
    [SerializeField] private float flyingSpeed = 3.5f;
    [SerializeField] private float hoverAmplitude = 0.25f;
    [SerializeField] private float hoverFrequency = 1.2f;

    [Header("Shooting")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float minFireRange = 3f;
    [SerializeField] private float maxFireRange = 7f;
    private float fireRange;
    [SerializeField] private float projectileSpeed = 9f;
    [SerializeField] private float burstDelay = 0.12f;
    [SerializeField] private int burstCount = 1;



    // Internal
    private float hoverOffset = 0f;
    private float baseY = 0f;

    protected override void Awake()
    {
        base.Awake();
        if (rb != null) rb.gravityScale = 0f;
        baseY = transform.position.y;
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

        if (firePoint == null) Debug.LogWarning($"{name}: firePoint no asignado (asigna en inspector o crea un hijo llamado 'FirePoint').");
        if (projectilePrefab == null) Debug.LogWarning($"{name}: projectilePrefab no asignado.");
    }

    private void Update()
    {
        if (target == null) return;

        float distToTarget = Vector2.Distance(transform.position, target.position);

        if (distToTarget <= detectRadius)
        {
            if (distToTarget > fireRange)
            {
                canMove = true;
            }
            else
            {
                canMove = false;
                StopMovementPhysics();
                TryAttack();
            }
        }
        else
        {
            canMove = false;
        }
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        // Hover
        hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        float targetY = baseY + hoverOffset;

        if (canMove)
        {
            FlyTowardsTarget(targetY);
        }
        else
        {
            // Solo hover
            rb.MovePosition(new Vector2(transform.position.x, targetY));
        }
    }

    private void FlyTowardsTarget(float targetY)
    {
        Vector2 direction = (Vector2)target.position - (Vector2)transform.position;
        float distance = direction.magnitude;
        if (distance < 0.05f)
        {
            StopMovementPhysics();
            return;
        }

        direction.Normalize();
        FlipIfNeeded(direction.x);

        float worldSpeed = parallaxController != null ? parallaxController.baseSpeed * parallaxController.cameraMoveMultiplier : 0f;
        Vector2 desiredVelocity = direction * flyingSpeed;
        desiredVelocity.x -= worldSpeed;

        // Mantener hover suavemente
        desiredVelocity.y = (targetY - transform.position.y) / Time.fixedDeltaTime;
        rb.linearVelocity = desiredVelocity;
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
        if (projectilePrefab == null || firePoint == null || target == null) return;

        Vector2 aimDir = ((Vector2)target.position - (Vector2)firePoint.position).normalized;
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile2DEnemy projScript = proj.GetComponent<Projectile2DEnemy>();
        if (projScript != null)
        {
            projScript.owner = this.gameObject;
        }

        Rigidbody2D prb = proj.GetComponent<Rigidbody2D>();
        if (prb != null)
        {
            prb.linearVelocity = aimDir * projectileSpeed;
        }

        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        proj.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void StopMovementPhysics()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, fireRange);
        if (firePoint != null) Gizmos.DrawSphere(firePoint.position, 0.05f);
    }
}


