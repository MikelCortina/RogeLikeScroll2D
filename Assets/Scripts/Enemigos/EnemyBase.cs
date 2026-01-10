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
    [SerializeField] protected float followSpacing = 0.4f;
    [SerializeField] protected float groupSpeedMultiplier = 0.8f;

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
    [SerializeField] private float knockbackRecoveryTime = 0.25f;
    public bool isKnockedBack = false;
    private Coroutine knockbackRoutine = null;

    protected Coroutine flashRoutine;
    protected Color[] originalColors;

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
    [SerializeField] protected SpriteRenderer[] renderersToFlash;
    [SerializeField] protected float flashDuration = 0.05f;
    [SerializeField] protected int flashCount = 5;

    public Transform emergencySpawn;
    public GameObject gorePrefab;

    private bool isCarried = false;
    private Transform carrierTransform = null;
    private Vector3 carrierLocalOffset = Vector3.zero;
    private bool prevKinematic = false;
    private bool prevCanMove = true;

    [Header("Death VFX")]
    public ParticleSystem deathParticlesPrefab;
    public bool detachDeathParticles = true;
    public float fallbackDeathParticlesLifetime = 3f;

    [Header("Audio")]
    public AudioClip impactSound;
    public AudioClip killSound; // 🔊 Sonido de muerte del enemigo
    private AudioSource audioSource;

    private bool alreadyNotified = false;

    private void OnEnable() => EnemyUpdateManager.Register(this);
    private void OnDisable() => EnemyUpdateManager.Unregister(this);

    public bool flyer;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        float multiplier = 1f;
        if (useLevelScaling && EnemyLevelManager.Instance != null)
            multiplier = EnemyLevelManager.Instance.GetEnemyMultiplier();

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

        if (isCarried && carrierTransform != null)
        {
            Vector3 desired = carrierTransform.TransformPoint(carrierLocalOffset);
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

    public void Tick() { }

    #region Flash
    public void Flash()
    {
        if (renderersToFlash == null || renderersToFlash.Length == 0) return;

        if (originalColors == null || originalColors.Length != renderersToFlash.Length)
        {
            originalColors = new Color[renderersToFlash.Length];
            for (int i = 0; i < renderersToFlash.Length; i++)
            {
                originalColors[i] = renderersToFlash[i].material.GetColor("_Color");
            }
        }

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashCoroutine());
    }

    protected virtual IEnumerator FlashCoroutine()
    {
        Color flashColor = new Color(0.5f,0,0,1); // Fixed the incorrect usage of 'new Color.red'

        for (int i = 0; i < flashCount; i++)
        {
            for (int j = 0; j < renderersToFlash.Length; j++)
                renderersToFlash[j].material.SetColor("_Color", flashColor);

            yield return new WaitForSeconds(flashDuration);

            for (int j = 0; j < renderersToFlash.Length; j++)
                renderersToFlash[j].material.SetColor("_Color", originalColors[j]);

            yield return new WaitForSeconds(flashDuration);
        }

        for (int j = 0; j < renderersToFlash.Length; j++)
            renderersToFlash[j].material.SetColor("_Color", originalColors[j]);

        flashRoutine = null;
    }

    #endregion

    private void OnDestroy()
    {
        WaveManager.Instance?.NotifyEnemyKilled(gameObject);
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
    public void TakeContactDamage(float amount, bool sound)
    {
        currentHealth -= amount;
        Flash();

        // 🔊 Sonido de impacto
      

        if (currentHealth > 0)
        {
            ParticleSystem ps = Instantiate(deathParticlesPrefab, transform.position, Quaternion.identity);
        }

        if (currentHealth <= 0)
            Die();
        else if (impactSound != null && sound)
        {
            AudioManager.Instance.PlayImpactSound(impactSound, transform.position);
        }

        // StartCoroutine(HitPause(0.02f));
        //ConsoleManager.Instance.Log($"You made {amount} damage");
    }

    IEnumerator HitPause(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    public virtual void Die()
    {


        // 🔊 Sonido de muerte (no se corta al destruir el enemigo)
        if (killSound != null)
        {
            AudioManager.Instance.PlayDeathSound(killSound, transform.position);
        }

        canMove = false;
        StopAllCoroutines();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
#if UNITY_2020_1_OR_NEWER
            rb.simulated = false;
#else
            rb.isKinematic = true;
#endif
        }

        Collider2D[] cols = GetComponents<Collider2D>();
        foreach (var c in cols) c.enabled = false;
       
        // Efecto de partículas de muerte
        if (deathParticlesPrefab != null)
        {
            ParticleSystem ps = Instantiate(deathParticlesPrefab, transform.position, Quaternion.identity);
            if (detachDeathParticles) ps.transform.SetParent(null);
            else ps.transform.SetParent(transform);

            try
            {
                var main = ps.main;
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

        // Ocultar sprites y detener animador
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        var childRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in childRenderers) r.enabled = false;

        if (animator != null) animator.enabled = false;

     
        float xpGained = StatsManager.Instance.GetXPForEnemy(enemyLevel, baseXP);
        StatsManager.Instance.GainXP(xpGained);
        ScoreManager.Instance.EnemyDied();
        EnemyEvents.OnEnemyDied?.Invoke(this);



        DestroyChildrenWithTag("MeleAtack");

        // Gore opcional
        if (gorePrefab != null) Instantiate(gorePrefab, transform.position, Quaternion.identity);

        var heli = GetComponent<EnemyHelicopter>();
        if (heli != null)
        {
            if (gorePrefab != null) Instantiate(gorePrefab, transform.position, Quaternion.identity);
            heli.HandleDeathCleanup(0.05f, false);
            return;
        }

        Destroy(gameObject, 0.05f);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth playerHealth = other.gameObject.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeMeleDamage(contactDamage);
            ConsoleManager.Instance.Log($"Player damage received: {contactDamage}");
        }
    }

    public float GetContactDamage() => adjustedContactDamage;

    public float GetMaxHealth() => adjustedMaxHealth;
    #endregion

    #region Movement / Attack Utilities
   /* protected void MoveTowardsPlayer()
    {
        if (target == null || !canMove || isKnockedBack) return;
        Vector2 direction = (target.position - transform.position).normalized;
        direction.Normalize();
        FlipIfNeeded(direction.x);
        float worldSpeed = parallaxController != null ? parallaxController.baseSpeed * parallaxController.cameraMoveMultiplier : 1f;
        Vector2 velocity = rb.linearVelocity;
        velocity.x = direction.x * moveSpeed;
        if (direction.x < 0f)
        {
            velocity.x = direction.x * moveSpeed * 3.1f;
        }

        rb.linearVelocity = velocity;
        ApplyInclinationAndStepSmoothing();
    }*/

    protected void ApplyInclinationAndStepSmoothing()
    {
        if(flyer) return;

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
        if (timeSinceLast < attackCooldown) return;
        lastAttackTime = Time.time;
        if (animator != null) animator.SetTrigger("Attack");
        PerformAttack();
    }

    protected virtual void PerformAttack() { }
    #endregion

    #region Knockback Handling
    public void ApplyKnockback(Vector2 force, float recoveryTime = -1f)
    {
        if (rb == null) return;
        if (recoveryTime > 0f) knockbackRecoveryTime = recoveryTime;

        rb.AddForce(force, ForceMode2D.Impulse);

        if (knockbackRoutine != null) StopCoroutine(knockbackRoutine);
        knockbackRoutine = StartCoroutine(KnockbackCoroutine(knockbackRecoveryTime));
    }

    private IEnumerator KnockbackCoroutine(float duration)
    {
        isKnockedBack = true;
        canMove = false;
        yield return new WaitForSeconds(duration);
        isKnockedBack = false;
        canMove = true;
        knockbackRoutine = null;
    }
    #endregion

    #region Carried API
    public void StartBeingCarried(Transform carrier, Vector3 optionalLocalOffset)
    {
        if (carrier == null) return;
        if (isCarried && carrierTransform == carrier) return;

        prevKinematic = rb != null ? rb.isKinematic : false;
        prevCanMove = canMove;

        carrierTransform = carrier;
        carrierLocalOffset = optionalLocalOffset;
        isCarried = true;

        canMove = false;

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
        transform.SetParent(null);
        if (rb != null) rb.isKinematic = prevKinematic;
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

    public static bool IsVisibleFrom(Camera cam, GameObject obj)
    {
        if (cam == null || obj == null) return false;
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null) return false;
        return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
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
