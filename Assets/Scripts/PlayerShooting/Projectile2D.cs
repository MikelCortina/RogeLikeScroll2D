using System.Collections.Generic;
using UnityEngine;

public class Projectile2D : MonoBehaviour
{
    Rigidbody2D rb;
    public float speed = 8f;
    Vector2 direction = Vector2.right;
    public float lifeTime = 5f;
    public GameObject owner;

    // Homing
    Transform target;
    public bool isHoming = true;
    public float turningSpeed = 720f;

    public EffectSpawner effectSpawner;

    [Header("Colisión")]
    [Tooltip("Capas que el proyectil debe ignorar al colisionar.")]
    [SerializeField] private List<string> ignoreLayerNames = new List<string>();

    private HashSet<EnemyBase> damagedEnemies = new HashSet<EnemyBase>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.freezeRotation = true;
    }

    public void Initialize(Transform targetTransform, float spd, GameObject ownerObj = null)
    {
        target = targetTransform;
        speed = spd;
        owner = ownerObj;

        if (target != null)
            direction = ((Vector2)target.position - (Vector2)transform.position).normalized;

        if (rb != null)
            rb.linearVelocity = direction * speed;

        float ang = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang);
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (isHoming && target != null)
        {
            Vector2 toTarget = ((Vector2)target.position - rb.position).normalized;

            // Aplicar fuerza proporcional al turningSpeed y al deltaTime
            float rotationStep = turningSpeed * Time.fixedDeltaTime;
            float angle = Vector2.SignedAngle(direction, toTarget);
            float clampedAngle = Mathf.Clamp(angle, -rotationStep, rotationStep);

            direction = Quaternion.Euler(0, 0, clampedAngle) * direction;
            direction.Normalize();

            rb.linearVelocity = direction * speed;

            // Rotar el proyectil visualmente
            float ang = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }

        // --- RAYCAST CONTINUO ---
        float rayDist = speed * Time.fixedDeltaTime + 0.05f;
        if (direction.sqrMagnitude > 0f)
        {
            RaycastHit2D hit = Physics2D.Raycast(rb.position, direction, rayDist);
            if (hit.collider != null)
            {
                Collider2D other = hit.collider;

                if (owner != null && other.gameObject == owner) return;

                string otherLayerName = LayerMask.LayerToName(other.gameObject.layer);
                if (ignoreLayerNames.Contains(otherLayerName)) return;

                EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
                if (enemy != null && !damagedEnemies.Contains(enemy))
                {
                    float dmg = StatsCommunicator.Instance.CalculateDamage();
                    enemy.TakeContactDamage(dmg);
                    damagedEnemies.Add(enemy);

                    if (effectSpawner != null && RunEffectManager.Instance != null)
                    {
                        foreach (var activeEffect in RunEffectManager.Instance.GetActiveEffects())
                        {
                            if (effectSpawner.effects.Contains(activeEffect))
                            {
                                if (activeEffect is IEffect ie)
                                    ie.Execute(transform.position, owner);
                            }
                        }
                    }

                    ReturnToPool();
                    return;
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.gameObject == owner) return;

        string otherLayerName = LayerMask.LayerToName(other.gameObject.layer);
        if (ignoreLayerNames.Contains(otherLayerName)) return;

        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();

        if (enemy != null && !damagedEnemies.Contains(enemy))
        {
            float dmg = StatsCommunicator.Instance.CalculateDamage();
            enemy.TakeContactDamage(dmg);
            damagedEnemies.Add(enemy);
        }

        // Ejecutar efectos
        if (effectSpawner != null && RunEffectManager.Instance != null)
        {
            foreach (var activeEffect in RunEffectManager.Instance.GetActiveEffects())
            {
                if (effectSpawner.effects.Contains(activeEffect))
                {
                    if (activeEffect is IEffect ie)
                        ie.Execute(transform.position, owner);
                }
            }
        }

        ReturnToPool(); // ✅ se llama **al final**, después de daño y efectos
    }

    public void ReturnToPool()
    {
        target = null;
        isHoming = false;
        damagedEnemies.Clear();
        rb.linearVelocity = Vector2.zero;
        gameObject.SetActive(false);  // ⚡ reemplaza Destroy
    }
}
