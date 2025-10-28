using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBase : MonoBehaviour
{
    [Header("Stats (lvl 0")]
    public float waveSpace;
    [SerializeField] protected float maxHealth;
    [SerializeField] protected float contactDamage;

    [Header("Leveling")]
    [Tooltip("Si está activo, aplica el multiplicador de LevelManager.")]
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

    [Header("Grouping / Collision Avoidance")]
    [SerializeField] protected float followSpacing = 0.4f; // separación mínima entre enemigos
    [SerializeField] protected float groupSpeedMultiplier = 0.8f; // ralentiza si hay enemigos delante

    [Header("Terrain Adaptation")]
    public LayerMask Ground;
    public float rayLength = 1.0f;
    public float rotationSpeed = 10f;
    public float stepOffset = 0.3f;
    public float stepSmoothSpeed = 10f;

    [Header("Attack (general)")]
    [SerializeField] protected float attackCooldown = 1.2f;

    [Header("References")]
    [SerializeField] protected Animator animator;

    [Header("Knockback")]
    [SerializeField] private float knockbackRecoveryTime = 0.25f; // tiempo que dura el knockback (puedes ajustar)
    private bool isKnockedBack = false;
    private Coroutine knockbackRoutine = null;

    private Coroutine flashRoutine;
    private Color[] originalColors;

    protected Rigidbody2D rb;
    protected Transform target;
    protected float currentHealth;
    protected bool isFacingRight = true;
    protected float adjustedMaxHealth;
    protected float adjustedContactDamage;
    protected bool canMove = true;

    public float lastAttackTime = -999f;
    public int enemyLevel = 0;
    public EnemyLevelManager enemyLevelManager;
    public float baseXP = 25f;

    [Header("Flash Settings")]
    [SerializeField] private SpriteRenderer[] renderersToFlash;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private int flashCount = 5;

    public Transform emergencySpawn;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        float multiplier = 1f;
        if (useLevelScaling && EnemyLevelManager.Instance != null) multiplier = EnemyLevelManager.Instance.GetEnemyMultiplier();
        adjustedMaxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * multiplier));
        adjustedContactDamage = Mathf.Max(1, Mathf.CeilToInt(contactDamage * multiplier));
        currentHealth = adjustedMaxHealth;
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    protected virtual void Start()
    {
        GameObject playerObj = FindPlayerByLayerOrTag();
        if (playerObj != null) target = playerObj.transform;
    }

    private void OnValidate()
    {
        if (renderersToFlash == null || renderersToFlash.Length == 0)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) renderersToFlash = new SpriteRenderer[] { sr };
        }
    }

    private void Update()
    {
        if (transform.position.y < -4)
        {
            if (emergencySpawn != null) transform.position = emergencySpawn.position;
        }
    }

    #region Flash
    public void Flash()
    {
        if (renderersToFlash == null || renderersToFlash.Length == 0) return;
        if (originalColors == null || originalColors.Length != renderersToFlash.Length)
        {
            originalColors = new Color[renderersToFlash.Length];
            for (int i = 0; i < renderersToFlash.Length; i++) originalColors[i] = renderersToFlash[i].color;
        }
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine()
    {
        Color flashColor = new Color(1f, 1f, 1f, 0.7f);
        for (int i = 0; i < flashCount; i++)
        {
            for (int j = 0; j < renderersToFlash.Length; j++) renderersToFlash[j].color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            for (int j = 0; j < renderersToFlash.Length; j++) renderersToFlash[j].color = originalColors[j];
            yield return new WaitForSeconds(flashDuration);
        }
        for (int j = 0; j < renderersToFlash.Length; j++) renderersToFlash[j].color = originalColors[j];
        flashRoutine = null;
    }
    #endregion

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
        Flash();
        if (currentHealth <= 0) Die();
    }

    protected virtual void Die()
    {
        if (animator != null) animator.SetTrigger("Dead");
        canMove = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        Collider2D[] cols = GetComponents<Collider2D>();
        foreach (var c in cols) c.enabled = false;
        if (WaveManager.Instance != null) WaveManager.Instance.NotifyEnemyKilled(gameObject);
        float xpGained = StatsManager.Instance.GetXPForEnemy(enemyLevel, baseXP);
        Debug.Log($"Enemy Level: {enemyLevel}, Base XP: {baseXP}, XP Gained: {xpGained}");
        StatsManager.Instance.GainXP(xpGained);
        ScoreManager.Instance.EnemyDied();
        HealthDecay.Instance.GetBackHP();
     
        DestroyChildrenWithTag("MeleAtack");
        Destroy(gameObject, 0.05f);
    }


    public float GetContactDamage() => adjustedContactDamage;
    public float GetMaxHealth() => adjustedMaxHealth;
    #endregion

    #region Movement / Attack Utilities
    protected bool IsBlockedByAlly(Vector2 direction)
    {
        if (direction.magnitude < 0.01f) return false;
        Collider2D[] hit = Physics2D.OverlapCircleAll(rb.position + direction.normalized * followSpacing, followSpacing);
        foreach (var col in hit)
        {
            if (col != null && col.gameObject != this.gameObject && col.GetComponent<EnemyBase>() != null) return true;
        }
        return false;
    }

    protected void MoveTowardsPlayer()
    {
        if (target == null || !canMove || isKnockedBack) return;
        Vector2 direction = (target.position - transform.position);
        direction.Normalize();
        FlipIfNeeded(direction.x);
        float speedMultiplier = IsBlockedByAlly(direction) ? groupSpeedMultiplier : 1f;
        float worldSpeed = parallaxController != null ? parallaxController.baseSpeed * parallaxController.cameraMoveMultiplier : 1f;
        Vector2 velocity = rb.linearVelocity;
        velocity.x = direction.x * moveSpeed * speedMultiplier - worldSpeed;
        rb.linearVelocity = velocity;
        ApplyInclinationAndStepSmoothing();
    }

    void DestroyChildrenWithTag(string tag)
    {
        foreach (Transform child in transform.Cast<Transform>().ToArray())
        {
            if (child.CompareTag(tag)) Destroy(child.gameObject);
        }
    }

    protected void ApplyInclinationAndStepSmoothing()
    {
        Vector2 origin = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, rayLength, Ground);
        if (hit.collider != null)
        {
            float targetY = hit.point.y;
            float deltaY = targetY - transform.position.y;
            if (deltaY > 0f && deltaY <= stepOffset)
            {
                float newY = Mathf.Lerp(transform.position.y, targetY, stepSmoothSpeed * Time.fixedDeltaTime);
                rb.MovePosition(new Vector2(rb.position.x, newY));
            }
            float slopeAngle = Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg - 90f;
            float newRotation = Mathf.LerpAngle(rb.rotation, slopeAngle, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRotation);
        }
        else
        {
            float newRotation = Mathf.LerpAngle(rb.rotation, 0f, rotationSpeed * Time.fixedDeltaTime * 0.5f);
            rb.MoveRotation(newRotation);
        }
    }

    protected void StopMovement()
    {
        Vector2 vel = rb.linearVelocity;
        vel.x = 0f;
        rb.linearVelocity = vel;
    }

    protected void TryAttack()
    {
        float timeSinceLast = Time.time - lastAttackTime;
        if (timeSinceLast < attackCooldown)
        {
            return;
        }
        lastAttackTime = Time.time;
        if (animator != null) animator.SetTrigger("Attack");
        PerformAttack();
    }

    protected virtual void PerformAttack()
    {
    }
    #endregion

    #region Knockback Handling
    public void ApplyKnockback(Vector2 force, float recoveryTime = -1f)
    {
        if (rb == null) return;
        if (recoveryTime > 0f) knockbackRecoveryTime = recoveryTime;

        // Aplicar la fuerza (impulso) al rigidbody
        rb.AddForce(force, ForceMode2D.Impulse);

        // Gestionar estado para que la IA no sobrescriba la velocidad
        if (knockbackRoutine != null) StopCoroutine(knockbackRoutine);
        knockbackRoutine = StartCoroutine(KnockbackCoroutine(knockbackRecoveryTime));
    }

    private IEnumerator KnockbackCoroutine(float duration)
    {
        isKnockedBack = true;
        canMove = false;
        // opcional: parar animación de movimiento si usas una trigger/param
        if (animator != null)
        {
            // ejemplo: animator.ResetTrigger("Walk"); // ajusta según tus animaciones
        }

        yield return new WaitForSeconds(duration);

        isKnockedBack = false;
        canMove = true;
        knockbackRoutine = null;
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
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.right * followSpacing, followSpacing);
    }
    #endregion
}
