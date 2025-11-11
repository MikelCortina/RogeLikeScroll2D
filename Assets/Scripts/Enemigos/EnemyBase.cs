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
    // NOTA: canMove sigue siendo protected para que tus derivados lo usen
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

    public GameObject gorePrefab; // asigna tu prefab en el inspector

    // --- NUEVO: estado de "estar llevado" ---
    private bool isCarried = false;
    private Transform carrierTransform = null;
    private Vector3 carrierLocalOffset = Vector3.zero;
    private bool prevKinematic = false;
    private bool prevCanMove = true;

    [Header("Death VFX")]
    public ParticleSystem deathParticlesPrefab;      // asigna en el inspector (prefab de ParticleSystem)
    public bool detachDeathParticles = true;         // true = desvincula partículas del enemigo (para que no se destruyan con él)
    public float fallbackDeathParticlesLifetime = 3f; // si no se puede calcular la duración, se usa este valor

    public AudioClip impactSound; // 🎵 Asigna aquí el sonido de impacto
    private AudioSource audioSource;

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

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
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

        // Si estamos siendo llevados, sincronizamos la posición aquí (evita tocar la IA)
        if (isCarried && carrierTransform != null)
        {
            // Mantener offset en world space
            Vector3 desired = carrierTransform.TransformPoint(carrierLocalOffset);
            // Usar MovePosition para respetar física lo más posible
            if (rb != null)
            {
                rb.MovePosition(new Vector2(desired.x, desired.y));
            }
            else
            {
                transform.position = desired;
            }
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
    public void TakeContactDamage(float amount, bool sound)
    {

        currentHealth -= amount;
        Flash();


        // 🔊 Reproducir sonido de impacto
        if (impactSound != null && sound)
        {
            audioSource.PlayOneShot(impactSound);
        }

        if (currentHealth > 0)
        {
            ParticleSystem ps = Instantiate(deathParticlesPrefab, transform.position, Quaternion.identity);
        }

        if (currentHealth <= 0)
            Die();
        HitPause(0.02f);

    }
    private IEnumerator HitPause(float duration)
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f; // Pausa el juego
        yield return new WaitForSecondsRealtime(duration); // espera tiempo real
        Time.timeScale = originalTimeScale; // reanuda el juego
    }

    protected virtual void Die()
    {
        // Trigger animación de muerte si existe
        if (animator != null) animator.SetTrigger("Dead");

        // Intento de limpieza inmediata
        canMove = false;

        // Parar corrutinas propias (esto solo afecta corrutinas iniciadas en este componente)
        StopAllCoroutines();

        // Detener Rigidbody2D si existe
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
#if UNITY_2020_1_OR_NEWER
            rb.simulated = false;
#else
        rb.isKinematic = true;
#endif
        }

        // Desactivar colisiones
        Collider2D[] cols = GetComponents<Collider2D>();
        foreach (var c in cols) c.enabled = false;

        // --- INSTANTIAR PARTICULAS ANTES DE DESACTIVAR RENDERERS ----
        if (deathParticlesPrefab != null)
        {
            ParticleSystem ps = Instantiate(deathParticlesPrefab, transform.position, Quaternion.identity);
            if (detachDeathParticles)
            {
                ps.transform.SetParent(null);
            }
            else
            {
                // si no las desvinculas, parentéalas al enemigo (no recomendado si el enemigo se destruye rápido)
                ps.transform.SetParent(transform);
            }

            // intentar calcular duración real de las partículas para destruir el objeto instanciado
            try
            {
                var main = ps.main;
                // startLifetime puede ser MinMaxCurve; usamos constantMax como aproximación
                float startLifetimeMax = 0f;
                try { startLifetimeMax = main.startLifetime.constantMax; } catch { startLifetimeMax = 0f; }
                float duration = main.duration + startLifetimeMax;
                if (duration <= 0f) duration = fallbackDeathParticlesLifetime;
                Destroy(ps.gameObject, duration + 0.1f);
            }
            catch
            {
                Destroy(ps.gameObject, fallbackDeathParticlesLifetime);
            }
        }

        // Desactivar renderers (por si la animación o child renderers mantienen visible el sprite)
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        var childRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in childRenderers) r.enabled = false;

        // Desactivar animador para que no re-enable renderers/propiedades
        if (animator != null) animator.enabled = false;

        // Notificar sistema de oleadas / puntaje / xp
        if (WaveManager.Instance != null) WaveManager.Instance.NotifyEnemyKilled(gameObject);
        float xpGained = StatsManager.Instance.GetXPForEnemy(enemyLevel, baseXP);
        Debug.Log($"Enemy Level: {enemyLevel}, Base XP: {baseXP}, XP Gained: {xpGained}");
        StatsManager.Instance.GainXP(xpGained);
        ScoreManager.Instance.EnemyDied();
        HealthDecay.Instance.GetBackHP();

        // Limpieza de hijos con tag
        DestroyChildrenWithTag("MeleAtack");

        // Instanciar gore si procede
        if (gorePrefab != null) Instantiate(gorePrefab, transform.position, Quaternion.identity);

        // Si el objeto tiene un cleanup específico (como HandleDeathCleanup en EnemyHelicopter), llamarlo.
        var heli = GetComponent<EnemyHelicopter>();
        if (heli != null)
        {
            // Le decimos al helicóptero que gestione su limpieza/pooling; le damos 0.05s para mantener tu timing original.
            if (gorePrefab != null) Instantiate(gorePrefab, transform.position, Quaternion.identity);
            heli.HandleDeathCleanup(0.05f, false);

            return;
        }

        // Si no hay cleanup específico, destruimos el objeto rápido
        Destroy(gameObject, 0.05f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (rb.linearVelocity.y > 0.2&& collision.gameObject.CompareTag("Ground"))
        {
            Die();
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verifica si el objeto que entra en el trigger tiene el componente PlayerHealth
        PlayerHealth playerHealth = other.gameObject.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {

            playerHealth.TakeMeleDamage(contactDamage);
        }
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
        Vector2 direction = (target.position - transform.position).normalized;
        direction.Normalize();
        FlipIfNeeded(direction.x);
        float speedMultiplier = IsBlockedByAlly(direction) ? groupSpeedMultiplier : 1f;
        float worldSpeed = parallaxController != null ? parallaxController.baseSpeed * parallaxController.cameraMoveMultiplier : 1f;
        Vector2 velocity = rb.linearVelocity;
        velocity.x = direction.x * moveSpeed * speedMultiplier - worldSpeed;

        rb.linearVelocity = velocity;
        ApplyInclinationAndStepSmoothing();
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

    void DestroyChildrenWithTag(string tag)
    {
        foreach (Transform child in transform.Cast<Transform>().ToArray())
        {
            if (child.CompareTag(tag)) Destroy(child.gameObject);
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

    #region Carried API (nuevo)
    /// <summary>
    /// Llamar para hacer que el enemigo "se mueva con" el carrier (ej. proyectil).
    /// Mientras está carried:
    ///  - se desactiva la IA (canMove = false)
    ///  - se parenta al carrier (para seguir rot/pos) y se pone rb.isKinematic = true
    ///  - cuando se suelta, se restaura el estado previo
    /// </summary>
    public void StartBeingCarried(Transform carrier, Vector3 optionalLocalOffset)
    {
        if (carrier == null) return;
        if (isCarried && carrierTransform == carrier) return;

        // guardar estado previo
        prevKinematic = rb != null ? rb.isKinematic : false;
        prevCanMove = canMove;

        carrierTransform = carrier;
        carrierLocalOffset = optionalLocalOffset;
        isCarried = true;

        // desactivar IA / movimiento para que no sobreescriba la posición
        canMove = false;

        // parentar y poner kinematic para evitar forces conflictivas
        transform.SetParent(carrierTransform);
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = true;
        }
    }

    public void StopBeingCarried()
    {
        if (!isCarried) return;

        isCarried = false;

        // desparentar y restaurar física/IA
        transform.SetParent(null);

        if (rb != null)
        {
            rb.isKinematic = prevKinematic;
        }

        canMove = prevCanMove;
        carrierTransform = null;
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
