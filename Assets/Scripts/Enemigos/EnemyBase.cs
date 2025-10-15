using System.Buffers.Text;
using System.Collections;
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


    [Header("Attack (general)")]
    [Tooltip("Cooldown genérico que pueden usar las subclases")]
    [SerializeField] protected float attackCooldown = 1.2f;

    [Header("References")]
    [SerializeField] protected Animator animator;

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
        StopAllCoroutines(); // para que no se solapen varios flashes
        StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine()
    {
        for (int i = 0; i < flashCount; i++)
        {
            foreach (var sr in renderersToFlash)
                sr.enabled = false;
            yield return new WaitForSeconds(flashDuration);

            foreach (var sr in renderersToFlash)
                sr.enabled = true;
            yield return new WaitForSeconds(flashDuration);
        }
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


        Destroy(gameObject, 1.2f);
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
        float worldSpeed = parallaxController.baseSpeed * parallaxController.cameraMoveMultiplier;

        // Aplicar movimiento en X compensando el mundo
        Vector2 velocity = rb.linearVelocity;
        velocity.x = direction.x * moveSpeed - worldSpeed; // restamos para neutralizar movimiento del mundo
        rb.linearVelocity = velocity;
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
