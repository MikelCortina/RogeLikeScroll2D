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
        if (rb != null)
        {
            rb.gravityScale = 0f; // aseguramos que no caiga
            // revisa en Inspector que Body Type sea Dynamic para que AddForce/velocity funcione
        }
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
                // Si estamos fuera del rango de disparo, movernos hacia el target
                canMove = true;
            }
            else if (distToTarget < fireRange * 0.8f)
            {
                // Si estamos demasiado cerca, movernos ligeramente hacia atrás para mantener variedad
                canMove = true;
            }
            else
            {
                // Dentro del rango ideal, no moverse horizontalmente
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

        // Si estamos en knockback, no ejecutar la IA de movimiento (dejar que la f�sica aplique la fuerza)
        if (IsCurrentlyKnockedBack()) return;

        // Hover
        hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        float targetY = baseY + hoverOffset;

        if (canMove)
        {
            FlyTowardsTarget(targetY);
        }
        else
        {
            // Solo hover: mover suavemente la posici�n vertical sin anular la f�sica horizontal
            Vector2 currentVel = rb.linearVelocity;
            float desiredYVel = (targetY - transform.position.y) * 5f; // 5f = suavizado; ajusta si necesitas m�s/menos seguimiento
            // limitamos para evitar valores enormes
            desiredYVel = Mathf.Clamp(desiredYVel, -flyingSpeed * 2f, flyingSpeed * 2f);

            // asignamos la componente vertical mientras preservamos la horizontal f�sica
            rb.linearVelocity = new Vector2(currentVel.x, desiredYVel);
            // NOTA: no uso MovePosition aqu� para no interferir con fuerzas/knockback
        }
    }

    private void FlyTowardsTarget(float targetY)
    {
        if (IsCurrentlyKnockedBack()) return;

        Vector2 direction = (Vector2)target.position - (Vector2)transform.position;
        float distance = direction.magnitude;
        if (distance < 0.05f)
        {
            StopMovementPhysics();
            return;
        }

        // Normalizamos solo horizontalmente si queremos que ajuste la distancia
        direction.Normalize();

        // Ajuste para que mantenga el rango: si está demasiado cerca, invertimos la dirección
        float distToTarget = Vector2.Distance(transform.position, target.position);
        if (distToTarget < fireRange * 0.8f) direction.x *= -1f;

        FlipIfNeeded(direction.x);

        float worldSpeed = parallaxController != null ? parallaxController.baseSpeed * parallaxController.cameraMoveMultiplier : 0f;
        Vector2 desiredVelocity = new Vector2(direction.x * flyingSpeed - worldSpeed, 0f);

        // Hover suavizado
        float currentY = rb.linearVelocity.y;
        float targetYVel = (targetY - transform.position.y) * 5f;
        targetYVel = Mathf.Clamp(targetYVel, -flyingSpeed * 2f, flyingSpeed * 2f);
        desiredVelocity.y = Mathf.Lerp(currentY, targetYVel, 0.2f);

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
            // Usa la propiedad correcta y no forces que puedan ser anuladas
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

    // Helper para verificar el estado de knockback heredado (isKnockedBack es privado en la base)
    // Si en el futuro quieres exponerlo mejor, convierte isKnockedBack en protected en EnemyBase.
    private bool IsCurrentlyKnockedBack()
    {
        // Intentamos obtener el componente y comprobar su estado a trav�s de reflexi�n segura.
        // Mejor: si prefieres, cambia `isKnockedBack` en EnemyBase a `protected` o a�ade un getter p�blico.
        var baseType = typeof(EnemyBase);
        var field = baseType.GetField("isKnockedBack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            object val = field.GetValue(this);
            if (val is bool b) return b;
        }
        // fallback: si no podemos leerlo, asumimos false para no bloquear movimiento
        return false;
    }
}

