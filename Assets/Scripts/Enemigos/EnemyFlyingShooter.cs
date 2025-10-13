using System.Collections;
using UnityEngine;

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
    [SerializeField] private float fireRange = 6f;
    [SerializeField] private float projectileSpeed = 9f;
    [SerializeField] private float burstDelay = 0.12f;
    [SerializeField] private int burstCount = 1;

    // internal
    private float hoverOffset = 0f;
    private float baseY = 0f;

    protected override void Awake()
    {
        base.Awake();
        if (rb != null)
            rb.gravityScale = 0f;

        // Guardar la Y base para aplicar hover sin acumular errores
        baseY = transform.position.y;
    }

    protected override void Start()
    {
        base.Start();

        // si firePoint no está asignado, intenta buscar uno hijo llamado "FirePoint"
        if (firePoint == null)
        {
            var fp = transform.Find("FirePoint");
            if (fp != null) firePoint = fp;
        }

        if (firePoint == null)
            Debug.LogWarning($"{name}: firePoint no asignado (asigna en inspector o crea un hijo llamado 'FirePoint').");

        if (projectilePrefab == null)
            Debug.LogWarning($"{name}: projectilePrefab no asignado.");
    }

    private void Update()
    {
        if (target == null) return;

        float distToTarget = Vector2.Distance(transform.position, target.position);

        // Si fuera necesario, asegúrate de que detectRadius viene de la base y tiene valor lógico
        if (distToTarget <= detectRadius)
        {
            if (distToTarget > fireRange)
            {
                canMove = true;
            }
            else
            {
                canMove = false;
                // Aseguramos detener la física
                StopMovementPhysics();
                TryAttack();
            }
        }
        else
        {
            // fuera de detección: quizá patrullar o quedarse quieto
            canMove = false;
        }

        // Hover logic (visual). Usamos baseY + offset (no multiplicar por deltaTime).
        hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        var p = transform.position;
        transform.position = new Vector3(p.x, baseY + hoverOffset, p.z);
    }

    private void FixedUpdate()
    {
        if (target == null) return;
        if (canMove)
        {
            FlyTowardsTarget();
        }
    }

    private void FlyTowardsTarget()
    {
        Vector2 direction = ((Vector2)target.position - (Vector2)transform.position);
        float distance = direction.magnitude;
        if (distance < 0.05f)
        {
            StopMovementPhysics();
            return;
        }

        direction.Normalize();
        FlipIfNeeded(direction.x);

        float worldSpeed = 0f;
        if (parallaxController != null)
            worldSpeed = parallaxController.baseSpeed * parallaxController.cameraMoveMultiplier;

        Vector2 desired = direction * flyingSpeed;
        desired.x -= worldSpeed;

        // !!! CORRECCIÓN: usar rb.velocity (no existe linearVelocity en Rigidbody2D)
        rb.linearVelocity = desired;
    }

    protected override void PerformAttack()
    {
        if (projectilePrefab == null || firePoint == null || target == null) return;

        if (burstCount <= 1)
        {
            ShootOne();
        }
        else
        {
            StartCoroutine(ShootBurst());
        }
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

        Rigidbody2D prb = proj.GetComponent<Rigidbody2D>();
        if (prb != null)
        {
            // !!! CORRECCIÓN: usar velocity
            prb.linearVelocity = aimDir * projectileSpeed;
        }

        // Opcional: rotar el proyectil para que apunte a aimDir (2D)
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
        if (firePoint != null)
            Gizmos.DrawSphere(firePoint.position, 0.05f);
    }
}

