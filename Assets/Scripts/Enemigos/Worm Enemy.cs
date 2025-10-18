using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class WormEnemy : EnemyBase
{
    [Header("Worm Settings")]
    [Tooltip("Cuántos niveles por encima del nivel global debe aparecer este gusano.")]
    [SerializeField] private int levelsAboveGlobal = 10;

    protected override void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // --- Determinar nivel usando el LevelManager ---
        if (EnemyLevelManager.Instance != null)
        {
            // Siempre 10 niveles por encima del nivel global
            enemyLevel = Mathf.RoundToInt(EnemyLevelManager.Instance.enemyLevel + levelsAboveGlobal);

            // Calcular multiplicador para este gusano
            float globalMultiplier = EnemyLevelManager.Instance.GetEnemyMultiplier();
            float wormMultiplier = Mathf.Pow(EnemyLevelManager.Instance.multiplicadoPorNivel, levelsAboveGlobal);

            float totalMultiplier = globalMultiplier * wormMultiplier;

            adjustedMaxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * totalMultiplier));
            adjustedContactDamage = Mathf.Max(1, Mathf.CeilToInt(contactDamage * totalMultiplier));
        }
        else
        {
            // Si no existe LevelManager, usar valores base
            enemyLevel = levelsAboveGlobal;
            adjustedMaxHealth = maxHealth;
            adjustedContactDamage = contactDamage;
        }

        currentHealth = adjustedMaxHealth;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    protected override void Start()
    {
        // Ignora completamente al jugador (no lo busca)
        target = null;
    }

    private void Update()
    {
        if (canMove)
            MoveLeft();
    }

    /// <summary>
    /// El gusano simplemente se mueve hacia la izquierda constantemente.
    /// </summary>
    private void MoveLeft()
    {
        if (rb == null) return;

        // Compensar velocidad del parallax si existe
        float worldSpeed = 0f;
        if (parallaxController != null)
            worldSpeed = parallaxController.baseSpeed * parallaxController.cameraMoveMultiplier;

        Vector2 velocity = rb.linearVelocity;
        velocity.x = -moveSpeed - worldSpeed; // Siempre hacia la izquierda
        rb.linearVelocity = velocity;

        // Asegurar que esté mirando a la izquierda visualmente
        if (isFacingRight)
            Flip();
    }

    protected override void PerformAttack()
    {
        // Este gusano no ataca activamente
    }

    protected override void Die()
    {
        base.Die();
        // Puedes añadir animación o efecto especial de "gusano explotando" aquí
    }
}
