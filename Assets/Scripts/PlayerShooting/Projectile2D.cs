using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile2D : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D col;
    private float timer;
    public float lifeTime = 2f;

    [Header("Visual Effects")]
    public TrailRenderer trailRenderer;         // assign in prefab (child of projectile)
    public ParticleSystem hitParticlesPrefab;   // assign a SEPARATE prefab (NO child of projectile)
    [Tooltip("Offset del orden en capa del sistema de partículas respecto al trail (por defecto = +1)")]
    public int hitParticleSortingOrderOffset = 1;

    [Header("Hit Particle Sorting (override)")]
    [Tooltip("Si true, se usará hitParticleSortingLayer/hitParticleSortingOrder en lugar de trailSortingLayer + offset")]
    public bool useAbsoluteHitParticleSorting = false;
    public string hitParticleSortingLayer = "Default";
    public int hitParticleSortingOrder = 100;
    [Tooltip("Ajuste fino para desempates de orden (float)")]
    public float hitParticleSortingFudge = 0f;
    [Tooltip("Si true, creamos un material instanciado y forzamos su renderQueue a 4000 para asegurarlo encima de todo")]
    public bool forceRenderQueueToAlwaysOnTop = false;

    [Header("Homing / Arrival")]
    public float arriveThreshold = 0.12f;    // distancia a la que consideramos "llegado"
    public float homingRotationSpeed = 720f; // grados/s para rotar hacia objetivo (visual)

    [Header("Sorting Config")]
    public string trailSortingLayer = "Default";
    public int trailSortingOrder = 10;

    // Internals
    private Coroutine disableCoroutine;
    private bool isActiveVisual = false;

    // Movement mode state
    private bool isHoming = false;
    private Transform homingTarget = null;   // si está presente hacemos homing hacia este transform
    private Vector2 fixedTargetPoint;        // si homingTarget == null pero se pasó punto, nos movemos a este punto
    private float moveSpeed = 10f;           // velocidad actual (set desde Initialize)
    private bool usePhysicsVelocity = true;  // true => usamos rb.velocity (no homing)

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (col != null) col.isTrigger = true;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        if (trailRenderer != null)
        {
            trailRenderer.sortingLayerName = trailSortingLayer;
            trailRenderer.sortingOrder = trailSortingOrder;
            trailRenderer.Clear();
            trailRenderer.emitting = false;
        }

        // Forzar que las partículas del prefab no se reproduzcan al instanciar el proyectil
        if (hitParticlesPrefab != null)
        {
            var main = hitParticlesPrefab.main;
            main.playOnAwake = false;
        }
    }

    public void InitializeVisual(Vector2 direction, float speed, Vector2 targetPoint, Transform targetTransform = null)
    {
        if (disableCoroutine != null)
        {
            StopCoroutine(disableCoroutine);
            disableCoroutine = null;
        }

        timer = lifeTime;
        isActiveVisual = true;

        moveSpeed = Mathf.Max(0.001f, speed);

        homingTarget = targetTransform;
        fixedTargetPoint = targetPoint;
        isHoming = (homingTarget != null);

        if (isHoming)
        {
            usePhysicsVelocity = false;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
        else
        {
            usePhysicsVelocity = true;
            if (rb != null)
                rb.linearVelocity = direction.normalized * moveSpeed;
        }

        float ang = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang);

        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            trailRenderer.emitting = true;
        }
    }

    void Update()
    {
        if (!isActiveVisual) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            ReturnToPoolWithTrailFade();
            return;
        }

        if (!usePhysicsVelocity)
        {
            Vector2 currentPos = transform.position;
            Vector2 targetPos = homingTarget != null ? (Vector2)homingTarget.position : fixedTargetPoint;

            Vector2 newPos = Vector2.MoveTowards(currentPos, targetPos, moveSpeed * Time.deltaTime);
            transform.position = newPos;

            Vector2 toTarget = targetPos - newPos;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                float targetAng = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
                Quaternion desired = Quaternion.Euler(0f, 0f, targetAng);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, homingRotationSpeed * Time.deltaTime);
            }

            if (Vector2.Distance(newPos, targetPos) <= arriveThreshold)
            {
                ImpactAtPoint(targetPos);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        ImpactAtPoint(transform.position);
    }

    private void ImpactAtPoint(Vector2 point)
    {
        if (hitParticlesPrefab != null)
        {
            GameObject psGO = Instantiate(hitParticlesPrefab.gameObject, point, Quaternion.identity);
            ParticleSystem ps = psGO.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;

                ParticleSystemRenderer psr = psGO.GetComponent<ParticleSystemRenderer>();
                if (psr != null)
                {
                    // elegir layer/order: absoluto o relativo al trail
                    if (useAbsoluteHitParticleSorting)
                    {
                        psr.sortingLayerName = hitParticleSortingLayer;
                        psr.sortingOrder = hitParticleSortingOrder;
                    }
                    else
                    {
                        psr.sortingLayerName = trailSortingLayer;
                        psr.sortingOrder = trailSortingOrder + hitParticleSortingOrderOffset;
                    }

                    // sortingFudge es un ajuste fino (float) que ayuda a desempatar en la ordenación
                    psr.sortingFudge = hitParticleSortingFudge;

                    // Si queremos forzar visualmente que quede por encima, instanciamos el material y subimos su renderQueue
                    if (forceRenderQueueToAlwaysOnTop && psr.material != null)
                    {
                        // crear instancia del material (para no modificar sharedMaterial)
                        Material matInstance = new Material(psr.material);
                        // 4000 es la cola más alta por defecto (Overlay), asegurará que se dibuje encima
                        matInstance.renderQueue = 4000;
                        psr.material = matInstance;
                    }
                }

                ps.transform.position = point;
                ps.Play();

                // Calcular tiempo de vida: duration + posible startLifetime máximo
                float duration = main.duration;
                float startLifetimeMax = 0f;
                var startLifetime = main.startLifetime;

                if (startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
                    startLifetimeMax = startLifetime.constantMax;
                else if (startLifetime.mode == ParticleSystemCurveMode.Constant)
                    startLifetimeMax = startLifetime.constant;
                else
                {
                    try { startLifetimeMax = main.startLifetime.constantMax; } catch { startLifetimeMax = 0.5f; }
                }

                float destroyAfter = duration + startLifetimeMax + 0.1f;
                Destroy(psGO, destroyAfter);
            }
            else
            {
                Destroy(psGO);
            }
        }

        ReturnToPoolWithTrailFade();
    }

    private void ReturnToPoolWithTrailFade()
    {
        if (!isActiveVisual) return;
        isActiveVisual = false;

        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
            float wait = Mathf.Max(0.01f, trailRenderer.time);
            disableCoroutine = StartCoroutine(DisableAfterSeconds(wait));
        }
        else
        {
            DisableNow();
        }

        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator DisableAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        DisableNow();
    }

    private void DisableNow()
    {
        if (trailRenderer != null)
            trailRenderer.Clear();

        homingTarget = null;
        isHoming = false;
        isActiveVisual = false;

        gameObject.SetActive(false);
    }

    public void ForceReturnToPoolImmediate()
    {
        if (disableCoroutine != null)
        {
            StopCoroutine(disableCoroutine);
            disableCoroutine = null;
        }
        DisableNow();
    }
}
