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

    // Ahora solo devuelve el proyectil a la pool (sin instanciar partículas)
    private void ImpactAtPoint(Vector2 point)
    {
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
