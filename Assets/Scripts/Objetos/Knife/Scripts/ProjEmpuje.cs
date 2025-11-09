using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ProjectileEmpuje : MonoBehaviour
{
    private SpawnTwoProjectilesEmpujeEffect parentEffect;
    private int index = -1;
    private float returnSpeed = 10f;
    private float collectDistance = 0.5f;
    private float maxDistance = 8f;
    [HideInInspector] public GameObject owner;
    private Rigidbody2D rb;
    private bool isReturning = false;
    private bool hasCollected = false;
    private bool returnRequested = false; // evitar múltiples solicitudes de retorno

    // Push configuration (aún guardado por si lo quieres usar)
    private float pushForce = 6f;
    private float continuousPushMultiplier = 0.5f;

    // TRACKEO de enemigos "adjuntados" al proyectil
    private HashSet<EnemyBase> attachedEnemies = new HashSet<EnemyBase>();

    // --- Para detectar y voltear según dirección ---
    private bool facingRight = true;
    private Vector2 lastPosition;
    private float flipThreshold = 0.05f; // umbral para evitar jitter

    // Inicializar desde el effect
    public void Initialize(SpawnTwoProjectilesEmpujeEffect parent, int projectileIndex, float returnSpeed, float collectDistance, float maxDistance, float pushForce)
    {
        this.parentEffect = parent;
        this.index = projectileIndex;
        this.returnSpeed = returnSpeed;
        this.collectDistance = collectDistance;
        this.maxDistance = maxDistance;
        this.pushForce = pushForce;
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
        attachedEnemies.Clear();

        if (rb == null) rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.simulated = true;
            // Si tu proyecto usa "linearVelocity" explícitamente, mantenlo. Aquí intentamos también asegurar velocity por si acaso:
            try
            {
                // intentar asignar linearVelocity si existe (si no, se lanzará y caerá al siguiente)
                rb.GetType().GetProperty("linearVelocity")?.SetValue(rb, Vector2.zero, null);
            }
            catch { /* si no existe, lo ignoramos */ }

            rb.linearVelocity = Vector2.zero;
        }

        // activar collider en caso de que estuviera desactivado
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        // actualizar lastPosition para la detección de movimiento
        lastPosition = transform.position;

        // asegurar facingRight acorde a la escala actual (por si lo cambiaste en prefab)
        facingRight = transform.localScale.x >= 0f;

        gameObject.name = $"EmpujeProj_{index}_active";
        Debug.Log($"[ProjectileEmpuje] ResetState idx={index}, owner={(owner != null ? owner.name : "NULL")}");
    }

    // Inicia la fase de retorno hacia el owner
    public void StartReturn(GameObject owner)
    {
        if (hasCollected) return;
        if (returnRequested) return; // ya solicitado
        this.owner = owner;
        returnRequested = true;
        isReturning = true;
        Debug.Log($"[ProjectileEmpuje] StartReturn idx={index}");

        // cuando empieza a volver, soltamos a todos los enemigos que estuvieran "adjuntos"
        if (attachedEnemies.Count > 0)
        {
            foreach (var e in attachedEnemies)
            {
                if (e != null) e.StopBeingCarried();
            }
            attachedEnemies.Clear();
        }
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
                Debug.Log($"[ProjectileEmpuje] idx={index} superó maxDistance ({distToOwner} > {maxDistance}), forzando retorno");
                StartReturn(owner);
            }
        }

        if (!isReturning || owner == null)
        {
            // Aun cuando no esté retornando, queremos detectar la dirección de movimiento para voltear sprite
            HandleFacing();
            return;
        }

        Vector2 targetPos = (Vector2)owner.transform.position + (Vector2)(owner.transform.rotation * new Vector2(0, 0.1f));
        Vector2 toOwner = targetPos - (Vector2)transform.position;
        float dist = toOwner.magnitude;

        if (rb != null)
        {
            Vector2 vel = toOwner.normalized * returnSpeed;
            // intentar mantener compatibilidad con 'linearVelocity' si tu proyecto la usa
            try
            {
                rb.GetType().GetProperty("linearVelocity")?.SetValue(rb, vel, null);
            }
            catch { /* ignore */ }

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
        bool haveMotion = false;

        if (rb != null)
        {
            moveX = rb.linearVelocity.x;
            // si velocity es prácticamente cero, puede que el proyectil esté usando una propiedad 'linearVelocity' custom: intentar leerla
            if (Mathf.Abs(moveX) < 0.0001f)
            {
                // intento reflect para linearVelocity (si existe)
                var prop = rb.GetType().GetProperty("linearVelocity");
                if (prop != null)
                {
                    var val = prop.GetValue(rb, null);
                    if (val is Vector2 lv) moveX = lv.x;
                }
            }

            haveMotion = Mathf.Abs(moveX) > 0f;
        }
        else
        {
            // fallback: calcular por delta posición
            Vector2 delta = (Vector2)transform.position - lastPosition;
            moveX = delta.x / Mathf.Max(Time.deltaTime, 1e-6f);
            haveMotion = delta.sqrMagnitude > 0f;
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
                rb.GetType().GetProperty("linearVelocity")?.SetValue(rb, Vector2.zero, null);
            }
            catch { }
            rb.linearVelocity = Vector2.zero;
        }
        Debug.Log($"[ProjectileEmpuje] idx={index} collected");
        parentEffect?.NotifyCollected(index);

        // asegurarnos de soltar cualquier enemigo restante
        if (attachedEnemies.Count > 0)
        {
            foreach (var e in attachedEnemies)
            {
                if (e != null) e.StopBeingCarried();
            }
            attachedEnemies.Clear();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasCollected) return;
        if (other == null) return;

        // Si choca con owner (por collider), marcar recogido inmediatamente
        if (owner != null && other.gameObject == owner)
        {
            Collect();
            return;
        }

        // Si colisiona con suelo, pedimos retorno (se mantiene comportamiento)
        if (other.CompareTag("Ground"))
        {
            Debug.Log($"[ProjectileEmpuje] idx={index} OnTrigger with Ground {other.name}. Requesting return.");
            parentEffect?.RequestReturn(index);
            return;
        }

        // Enemigo: aplicar daño y "adjuntarlo" mientras el proyectil no esté retornando
        if (other.CompareTag("enemigo"))
        {
            EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
            if (enemy != null)
            {
                Debug.Log("ProjectileEmpuje: Impacto con enemigo, aplicando daño.");
                float dmg = StatsCommunicator.Instance.CalculateGunDamage();
                enemy.TakeContactDamage(dmg);

                // Si NO está en fase de retorno, lo "adjuntamos" para que se mueva con el proyectil
                if (!isReturning)
                {
                    // CALCULAR offset relativo al proyectil (carrier)
                    Vector3 localOffset = transform.InverseTransformPoint(enemy.transform.position);
                    // llamamos al API de EnemyBase que empieza el estado "carried"
                    enemy.StartBeingCarried(this.transform, localOffset);
                    attachedEnemies.Add(enemy);
                }

                // **NO** solicitamos retorno al impactar con enemigos (así los arrastra hasta pared o maxDistance)
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null) return;

        // Si un enemigo sale del trigger antes de que el proyectil empiece a volver, lo soltamos
        if (other.CompareTag("enemigo"))
        {
            EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
            if (enemy != null)
            {
                if (attachedEnemies.Contains(enemy))
                {
                    enemy.StopBeingCarried();
                    attachedEnemies.Remove(enemy);
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        OnTriggerEnter2D(collision.collider);
    }
}
