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
    [SerializeField] private float minFireRange = 1f;
    [SerializeField] private float maxFireRange = 4f;
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
        }
        baseY = transform.position.y; // Inicializamos baseY
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
            if (distToTarget > fireRange || !IsVisibleFrom(Camera.main, gameObject))
            {
                // Si estamos fuera del rango de disparo, movernos hacia el target
                canMove = true;
            }

            else if (IsVisibleFrom(Camera.main, gameObject))
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

        // Si estamos en knockback, no ejecutar la IA de movimiento
        if (IsCurrentlyKnockedBack()) return;

        // --- LÓGICA DE HOVER MODIFICADA ---

        if (canMove)
        {
            // 1. Calcular hover solo si se está moviendo/reposicionando
            hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
            float targetY = baseY + hoverOffset;

            FlyTowardsTarget(targetY);
        }
        else // Está parado para disparar
        {
            // 2. Detenemos el hover y fijamos la altura actual
            baseY = transform.position.y; // <--- FIJAMOS LA POSICIÓN BASE ACTUAL
            hoverOffset = 0f;

            // Solo hover (mantenemos la Y sin la oscilación, y preservamos la X si hay alguna inercia)
            Vector2 currentVel = rb.linearVelocity;

            // Calculamos la velocidad deseada en Y para mantener la altura actual (baseY)
            float desiredYVel = (baseY - transform.position.y) * 5f;
            desiredYVel = Mathf.Clamp(desiredYVel, -flyingSpeed * 2f, flyingSpeed * 2f);

            // Asignamos la componente vertical mientras preservamos la horizontal física (si hay inercia o knockback residual)
            rb.linearVelocity = new Vector2(currentVel.x, desiredYVel);
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
            prb.linearVelocity = aimDir * projectileSpeed;
        }

        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        proj.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void StopMovementPhysics()
    {
        if (rb != null)
        {
            // Solo detenemos la velocidad horizontal para que el hover vertical siga funcionando
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, fireRange);
        if (firePoint != null) Gizmos.DrawSphere(firePoint.position, 0.05f);
    }

    // Helper para verificar el estado de knockback heredado 
    // (Asume que EnemyBase tiene una propiedad 'isKnockedBack' o similar)
    private bool IsCurrentlyKnockedBack()
    {
        // En un escenario de código real, es mejor tener una propiedad 'protected' o 'public'
        // en EnemyBase para acceder a 'isKnockedBack'. Mantenemos la lógica de reflexión
        // que tenías como fallback, pero se recomienda cambiar EnemyBase.
        var baseType = typeof(EnemyBase);
        var field = baseType.GetField("isKnockedBack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            object val = field.GetValue(this);
            if (val is bool b) return b;
        }
        return false;
    }
}