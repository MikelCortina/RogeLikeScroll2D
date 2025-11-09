using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ProjectileReturn : MonoBehaviour
{
    private SpawnTwoProjectilesReturningEffect parentEffect;
    private int index = -1;
    private float returnSpeed = 10f;
    private float collectDistance = 0.5f;
    private float maxDistance = 8f;
    [HideInInspector] public GameObject owner;
    private Rigidbody2D rb;
    private bool isReturning = false;
    private bool hasCollected = false;
    private bool returnRequested = false; // evitar múltiples solicitudes de retorno

    // --- Para detectar y voltear según dirección ---
    private bool facingRight = true;
    private Vector2 lastPosition;
    private float flipThreshold = 0.05f; // umbral para evitar jitter

    // Inicializar desde el effect
    public void Initialize(SpawnTwoProjectilesReturningEffect parent, int projectileIndex, float returnSpeed, float collectDistance, float maxDistance)
    {
        this.parentEffect = parent;
        this.index = projectileIndex;
        this.returnSpeed = returnSpeed;
        this.collectDistance = collectDistance;
        this.maxDistance = maxDistance;
        rb = GetComponent<Rigidbody2D>();

        // establecer estado inicial de facing según la escala local
        facingRight = transform.localScale.x >= 0f;
        lastPosition = transform.position;
    }

    // Resetear estado cada vez que se relanza
    public void ResetState(GameObject owner)
    {
        this.owner = owner;
        isReturning = false;
        hasCollected = false;
        returnRequested = false;

        if (rb == null) rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.simulated = true;
            // intentar compatibilidad con 'linearVelocity' si tu proyecto la usa
            try
            {
                var prop = rb.GetType().GetProperty("linearVelocity");
                if (prop != null) prop.SetValue(rb, Vector2.zero, null);
            }
            catch { }

            rb.linearVelocity = Vector2.zero;
        }

        // activar collider en caso de que estuviera desactivado
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        // actualizar lastPosition para la detección de movimiento
        lastPosition = transform.position;

        // asegurar facingRight acorde a la escala actual (por si lo cambiaste en prefab)
        facingRight = transform.localScale.x >= 0f;

        gameObject.name = $"ReturningProj_{index}_active";
        Debug.Log($"[ProjectileReturn] ResetState idx={index}, owner={(owner != null ? owner.name : "NULL")}");
    }

    // Inicia la fase de retorno hacia el owner
    public void StartReturn(GameObject owner)
    {
        if (hasCollected) return;
        if (returnRequested) return; // ya solicitado
        this.owner = owner;
        returnRequested = true;
        isReturning = true;
        Debug.Log($"[ProjectileReturn] StartReturn idx={index}");
    }

    private void Update()
    {
        if (hasCollected) return;

        // Auto-detectar distancia máxima respecto al owner y forzar retorno si la supera
        if (!isReturning && owner != null && maxDistance > 0f)
        {
            float distToOwner = Vector2.Distance(transform.position, owner.transform.position);
            if (distToOwner > maxDistance)
            {
                Debug.Log($"[ProjectileReturn] idx={index} superó maxDistance ({distToOwner} > {maxDistance}), forzando retorno");
                StartReturn(owner);
            }
        }

        if (!isReturning || owner == null)
        {
            // aunque no esté retornando, actualizamos facing para que mire según movimiento
            HandleFacing();
            return;
        }

        Vector2 targetPos = (Vector2)owner.transform.position + (Vector2)(owner.transform.rotation * new Vector2(0,0.1f));
        Vector2 toOwner = targetPos - (Vector2)transform.position;

        float dist = toOwner.magnitude;

        if (rb != null)
        {
            Vector2 vel = toOwner.normalized * returnSpeed;
            // intentar compatibilidad con 'linearVelocity' si existe
            try
            {
                var prop = rb.GetType().GetProperty("linearVelocity");
                if (prop != null) prop.SetValue(rb, vel, null);
            }
            catch { }

            rb.linearVelocity = vel;
        }
        else
        {
            transform.position = Vector2.MoveTowards(transform.position, owner.transform.position, returnSpeed * Time.deltaTime);
        }

        // chequear facing mientras vuelve (apunta hacia la dirección de movimiento)
        HandleFacing();

        if (dist <= collectDistance)
        {
            Collect();
        }
    }

    private void HandleFacing()
    {
        float moveX = 0f;

        if (rb != null)
        {
            moveX = rb.linearVelocity.x;
            // si velocity es prácticamente cero, intentar leer linearVelocity por reflexión (compatibilidad)
            if (Mathf.Abs(moveX) < 0.0001f)
            {
                try
                {
                    var prop = rb.GetType().GetProperty("linearVelocity");
                    if (prop != null)
                    {
                        var val = prop.GetValue(rb, null);
                        if (val is Vector2 lv) moveX = lv.x;
                    }
                }
                catch { }
            }
        }
        else
        {
            // fallback por delta posición
            Vector2 delta = (Vector2)transform.position - lastPosition;
            moveX = delta.x / Mathf.Max(Time.deltaTime, 1e-6f);
        }

        // aplicamos umbral para evitar jitter
        if (Mathf.Abs(moveX) > flipThreshold)
        {
            if (moveX > 0f && !facingRight)
            {
                Flip();
            }
            else if (moveX < 0f && facingRight)
            {
                Flip();
            }
        }

        lastPosition = transform.position;
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    private void Collect()
    {
        if (hasCollected) return;
        hasCollected = true;
        isReturning = false;
        returnRequested = false;
        if (rb != null)
        {
            try
            {
                var prop = rb.GetType().GetProperty("linearVelocity");
                if (prop != null) prop.SetValue(rb, Vector2.zero, null);
            }
            catch { }

            rb.linearVelocity = Vector2.zero;
        }
        Debug.Log($"[ProjectileReturn] idx={index} collected");
        parentEffect?.NotifyCollected(index);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasCollected) return;
        if (other == null) return;

        // Solo reaccionamos si es Enemy o Ground
        if (other.CompareTag("Ground"))
        {
            // Si choca con owner, marcar recogido inmediatamente
            if (owner != null && other.gameObject == owner)
            {
                Collect();
                return;
            }

            // Si choca con suelo/enemigo, pedimos al effect que haga que vuelva
            Debug.Log($"[ProjectileReturn] idx={index} OnTrigger with {other.name} ({other.tag}). Requesting return.");
            parentEffect?.RequestReturn(index);
        }
        else if (other.CompareTag("enemigo"))
        {
            EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
            if (enemy != null)
            {
                Debug.Log("KnifeProjectile2D: Impacto con enemigo, aplicando daño y destruyendo proyectil.");
                float dmg = StatsCommunicator.Instance.CalculateGunDamage();
                enemy.TakeContactDamage(dmg);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        OnTriggerEnter2D(collision.collider);
    }
}
