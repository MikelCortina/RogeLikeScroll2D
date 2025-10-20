using System.Buffers.Text;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBase : MonoBehaviour
{
    [Header("Stats (lvl 0")]
    [SerializeField] protected float maxHealth;
    [SerializeField] protected float contactDamage;

    [Header("Leveling")]
    [Tooltip("Si está activo, aplica el multiplicador de LevelManager.")] //Util para bosses por ejemplo
    [SerializeField] protected bool useLevelScaling = true;

    [Header("Detection")]
    [Tooltip("Radio para detectar al jugador")]
    [SerializeField] protected float detectRadius = 8f;
    [Tooltip("Layer del jugador (usa layer mask)")]
    [SerializeField] protected LayerMask playerLayer;
    [Tooltip("Tag del jugador (fallback si no quieres layer)")]
    [SerializeField] protected string playerTag = "Player";

    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected float stopDistance = 0.6f;
    [SerializeField] protected ParallaxController parallaxController;

    [Header("Terrain Adaptation")]
    public LayerMask Ground;
    public float rayLength = 1.0f;
    public float rotationSpeed = 10f;
    public float stepOffset = 0.3f;
    public float stepSmoothSpeed = 10f;




    [Header("Attack (general)")]
    [Tooltip("Cooldown genérico que pueden usar las subclases")]
    [SerializeField] protected float attackCooldown = 1.2f;

    [Header("References")]
    [SerializeField] protected Animator animator;

    private Coroutine flashRoutine;
    private Color[] originalColors;

    protected Rigidbody2D rb;
    protected Transform target;
    protected float currentHealth;
    protected bool isFacingRight = true;

    // valores ajustados por nivel
    protected float adjustedMaxHealth;
    protected float adjustedContactDamage;

    // Attack control
    protected bool canMove = true;
    protected float lastAttackTime = -999f;

    public int enemyLevel = 0; // nivel individual del enemigo (para XP)
    public EnemyLevelManager enemyLevelManager; // referencia opcional al LevelManager
    public float baseXP = 25f; // XP base que da este enemigo (se multiplica por nivel)

    [Header("Flash Settings")]
    [SerializeField] private SpriteRenderer[] renderersToFlash;
    [SerializeField] private float flashDuration = 0.1f; // duración de cada parpadeo
    [SerializeField] private int flashCount = 5; // cuántas veces parpadea


    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Calcula ajustes por nivel
        float multiplier = 1f;
        if (useLevelScaling && EnemyLevelManager.Instance != null)
            multiplier = EnemyLevelManager.Instance.GetEnemyMultiplier();

        adjustedMaxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * multiplier));
        adjustedContactDamage = Mathf.Max(1, Mathf.CeilToInt(contactDamage * multiplier));

        currentHealth = adjustedMaxHealth;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    protected virtual void Start()
    {
        GameObject playerObj = FindPlayerByLayerOrTag();
        if (playerObj != null)
            target = playerObj.transform;
    }

    private void OnValidate()
    {
        if (renderersToFlash == null || renderersToFlash.Length == 0)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                renderersToFlash = new SpriteRenderer[] { sr };
        }
    }

    /// <summary>
    /// Llama a este método cuando el enemigo reciba daño para que parpadee.
    /// </summary>
    public void Flash()
    {
        if (renderersToFlash == null || renderersToFlash.Length == 0) return;

        // Guardar colores originales solo la primera vez
        if (originalColors == null || originalColors.Length != renderersToFlash.Length)
        {
            originalColors = new Color[renderersToFlash.Length];
            for (int i = 0; i < renderersToFlash.Length; i++)
                originalColors[i] = renderersToFlash[i].color;
        }

        // Si ya hay un flash en curso, reiniciarlo
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine()
    {
        Color flashColor = new Color(1f, 1f, 1f, 0.7f);

        for (int i = 0; i < flashCount; i++)
        {
            for (int j = 0; j < renderersToFlash.Length; j++)
                renderersToFlash[j].color = flashColor;

            yield return new WaitForSeconds(flashDuration);

            for (int j = 0; j < renderersToFlash.Length; j++)
                renderersToFlash[j].color = originalColors[j];

            yield return new WaitForSeconds(flashDuration);
        }

        // Asegurar restauración final
        for (int j = 0; j < renderersToFlash.Length; j++)
            renderersToFlash[j].color = originalColors[j];

        flashRoutine = null;
    }

    #region Detection

    protected GameObject FindPlayerByLayerOrTag()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectRadius, playerLayer);
        if (hit != null) return hit.gameObject;

        GameObject t = GameObject.FindGameObjectWithTag(playerTag);
        return t;
    }

    public bool IsPlayerInRange()
    {
        if (target == null) return false;
        return Vector2.Distance(transform.position, target.position) <= detectRadius;
    }

    #endregion

    #region Health & Damage

    public void TakeContactDamage(float amount)
    {
        currentHealth -= amount;
        //Debug.Log($"{name} took {amount} damage, current health: {currentHealth}/{adjustedMaxHealth}");
        Flash(); // parpadea al recibir daño
        if (currentHealth <= 0) Die();
    }

    protected virtual void Die()
    {
        if (animator != null) animator.SetTrigger("Dead");
        canMove = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        Collider2D[] cols = GetComponents<Collider2D>();
        foreach (var c in cols) c.enabled = false;

        if (WaveManager.Instance != null)
            WaveManager.Instance.NotifyEnemyKilled(gameObject);

        float xpGained = StatsManager.Instance.GetXPForEnemy(enemyLevel, baseXP);
        Debug.Log($"Enemy Level: {enemyLevel}, Base XP: {baseXP}, XP Gained: {xpGained}");
        StatsManager.Instance.GainXP(xpGained);
        ScoreManager.Instance.EnemyDied();
        HealthDecay.Instance.GetBackHP();

        if(gameObject.GetComponent<PickupEffectItem>() != null)
        {
            PickupEffectItem pickup = gameObject.GetComponent<PickupEffectItem>();
            if (pickup != null)
            {

                // Ejecutar la coroutine desde este MonoBehaviour activo
                StartCoroutine(ShowUIFromPickup(pickup));
            }

        }
        Destroy(gameObject,0.05f);
    }
    private IEnumerator ShowUIFromPickup(PickupEffectItem pickup)
    {
        yield return null; // Esperar un frame para que Unity renderice el objeto
        Time.timeScale = 0f; // Pausar el juego
        StartCoroutine(pickup.ShowEffectNamesAndActivate());
    }

    public float GetContactDamage() => adjustedContactDamage;
    public float GetMaxHealth() => adjustedMaxHealth;

    #endregion

    #region Movement / Attack Utilities

    /// <summary>
    /// Movimiento genérico hacia el objetivo (usa moveSpeed y rb).
    /// </summary>
    protected void MoveTowardsPlayer()
    {
        if (target == null || !canMove)
            return;

        // Dirección hacia el jugador
        Vector2 direction = (target.position - transform.position);
        float distance = direction.magnitude;

        if (distance < 0.1f)
        {
            StopMovement();
            return;
        }

        direction.Normalize();
        FlipIfNeeded(direction.x);

        // Compensar la velocidad del mundo
        float worldSpeed = parallaxController != null ? parallaxController.baseSpeed * parallaxController.cameraMoveMultiplier : 0f;

        // Aplicar movimiento en X compensando el mundo
        Vector2 velocity = rb.linearVelocity;
        velocity.x = direction.x * moveSpeed - worldSpeed; // restamos para neutralizar movimiento del mundo
        rb.linearVelocity = velocity;

        // --- APLICAR INCLINACIÓN Y AJUSTE DE ESCALONES ---
        ApplyInclinationAndStepSmoothing();
    }

    /// <summary>
    /// Aplica el ajuste vertical (step smoothing) y rotación según la pendiente
    /// usando un raycast hacia abajo desde la posición del enemigo.
    /// </summary>
    protected void ApplyInclinationAndStepSmoothing()
    {
        Vector2 origin = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, rayLength, Ground);
        if (hit.collider != null)
        {
            // Ajuste vertical para escalones
            // No tenemos groundCheck transform aquí, así que usamos un offset de 0
            float targetY = hit.point.y;
            float deltaY = targetY - transform.position.y;

            if (deltaY > 0f && deltaY <= stepOffset)
            {
                float newY = Mathf.Lerp(transform.position.y, targetY, stepSmoothSpeed * Time.fixedDeltaTime);
                rb.MovePosition(new Vector2(rb.position.x, newY));
            }

            // Inclinación suave según pendiente
            float slopeAngle = Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg - 90f;
            float newRotation = Mathf.LerpAngle(rb.rotation, slopeAngle, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRotation);
        }
        else
        {
            // Si no hay suelo directamente debajo, suavemente dirigimos la rotación a 0
            float newRotation = Mathf.LerpAngle(rb.rotation, 0f, rotationSpeed * Time.fixedDeltaTime * 0.5f);
            rb.MoveRotation(newRotation);
        }
    }


    /// <summary>
    /// Para el movimiento horizontal.
    /// </summary>
    protected void StopMovement()
    {
        Vector2 vel = rb.linearVelocity;
        vel.x = 0f;
        rb.linearVelocity = vel;
    }

    /// <summary>
    /// Control de cooldown y trigger de animación. Las subclases deben implementar PerformAttack().
    /// </summary>
    protected void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;
        if (animator != null) animator.SetTrigger("Attack");
        PerformAttack();
    }

    /// <summary>
    /// Implementación por defecto vacía; las subclases hacen el daño real.
    /// </summary>
    protected virtual void PerformAttack()
    {
        // override en subclase
    }

    #endregion

    #region Utils & Editor

    protected void FlipIfNeeded(float direction)
    {
        if (direction > 0 && !isFacingRight) Flip();
        else if (direction < 0 && isFacingRight) Flip();
    }

    protected void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 s = transform.localScale;
        s.x *= -1f;
        transform.localScale = s;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }

    #endregion
}
