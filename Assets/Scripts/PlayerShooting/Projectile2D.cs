using System.Collections.Generic;
using UnityEngine;

public class Projectile2D : MonoBehaviour
{
    Rigidbody2D rb;
    float speed = 8f;
    Vector2 direction = Vector2.right;
    public float lifeTime = 5f;
    public GameObject owner;

    // Homing
    Transform target;
    public bool isHoming = true;
    // Si quieres que gire m�s lento en lugar de apuntar instant�neamente, usa turningSpeed > 0 (grados por segundo)
    public float turningSpeed = 720f; // 0 = giro instant�neo, mayor = giro m�s r�pido

    public EffectSpawner effectSpawner;

    [Header("Colisión")]
    [Tooltip("Capas que el proyectil debe ignorar al colisionar.")]
    [SerializeField] private List<string> ignoreLayerNames = new List<string>();


    private HashSet<EnemyBase> damagedEnemies = new HashSet<EnemyBase>();



    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.freezeRotation = true;
        Destroy(gameObject, lifeTime);
    }

    /// <summary>
    /// Inicializa el proyectil con objetivo (homing).
    /// </summary>
    public void Initialize(Transform targetTransform, float spd, GameObject ownerObj = null)
    {
        target = targetTransform;
        speed = spd;
        owner = ownerObj;

        // direcci�n inicial hacia donde est� el target en este instante
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
            // Si el objetivo ha sido destruido, target ser� null y continuar� en la �ltima direcci�n
            Vector2 toTarget = (Vector2)target.position - rb.position;
            if (toTarget.sqrMagnitude < 0.01f)
            {
                // Estamos pr�cticamente encima: aseguramos impacto movi�ndonos directamente al centro
                rb.linearVelocity = toTarget.normalized * speed;
            }
            else
            {
                Vector2 desiredDir = toTarget.normalized;

                if (turningSpeed <= 0f)
                {
                    // Giro instant�neo: m�xima precisi�n
                    direction = desiredDir;
                }
                else
                {
                    // Giro con l�mite angular: interpolamos la rotaci�n
                    float currentAng = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    float desiredAng = Mathf.Atan2(desiredDir.y, desiredDir.x) * Mathf.Rad2Deg;
                    float maxDelta = turningSpeed * Time.fixedDeltaTime;
                    float newAng = Mathf.MoveTowardsAngle(currentAng, desiredAng, maxDelta);
                    float rad = newAng * Mathf.Deg2Rad;
                    direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
                }

                rb.linearVelocity = direction * speed;
            }

            // orientaci�n visual
            float ang = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }
        else
        {
            // No homing: mantenemos velocidad constante
            // rb.velocity ya fue fijado en Initialize; opcionalmente podr�amos mantener la rotaci�n
        }
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.gameObject == owner) return;

        string otherLayerName = LayerMask.LayerToName(other.gameObject.layer);
        if (ignoreLayerNames.Contains(otherLayerName))
            return;

        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
        if (enemy != null && !damagedEnemies.Contains(enemy))
        {
            float dmg = StatsCommunicator.Instance.CalculateDamage();
            enemy.TakeContactDamage(dmg);
            damagedEnemies.Add(enemy);
        }

        // --- AQUI VIENE LA CLAVE ---
        if (effectSpawner != null && RunEffectManager.Instance != null)
        {
            foreach (var activeEffect in RunEffectManager.Instance.GetActiveEffects())
            {
                if (effectSpawner.effects.Contains(activeEffect))
                {
                    if (activeEffect is IEffect ie)
                    {
                        ie.Execute(transform.position, owner);
                    }
                    else
                    {
                        Debug.LogWarning($"El ScriptableObject {activeEffect.name} no implementa IEffect.");
                    }
                }
            }
        }

        Destroy(gameObject);
    }

}

