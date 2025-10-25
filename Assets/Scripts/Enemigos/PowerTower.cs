using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PowerTower : EnemyBase
{
    [Header("tower Settings")]
    [Tooltip("Cuántos niveles por encima del nivel global debe aparecer este tower.")]
    [SerializeField] private int levelsAboveGlobal = 10;
    public float speed = -2f;
    [SerializeField] private Transform player; // Arrastra el jugador desde el inspector
    [SerializeField] private float destroyOffset = -10f; // Distancia relativa para destruirla
    protected override void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // --- Determinar nivel usando el LevelManager ---
        if (EnemyLevelManager.Instance != null)
        {
            // Siempre 10 niveles por encima del nivel global
            enemyLevel = Mathf.RoundToInt(EnemyLevelManager.Instance.enemyLevel + levelsAboveGlobal);

            // Calcular multiplicador para este tower
            float globalMultiplier = EnemyLevelManager.Instance.GetEnemyMultiplier();
            float towerMultiplier = Mathf.Pow(EnemyLevelManager.Instance.multiplicadoPorNivel, levelsAboveGlobal);
            float totalMultiplier = globalMultiplier * towerMultiplier;

            adjustedMaxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * totalMultiplier));
            adjustedContactDamage = Mathf.Max(1, Mathf.CeilToInt(contactDamage * totalMultiplier));

            Debug.Log($"[PowerTower] Nivel global: {EnemyLevelManager.Instance.enemyLevel}, Nivel tower: {enemyLevel}, Multiplicador total: {totalMultiplier:F2}, Vida ajustada: {adjustedMaxHealth}, Daño ajustado: {adjustedContactDamage}");
        }
        else
        {
            // Si no existe LevelManager, usar valores base
            enemyLevel = levelsAboveGlobal;
            adjustedMaxHealth = maxHealth;
            adjustedContactDamage = contactDamage;
        }

        currentHealth = adjustedMaxHealth;
    }


    private void Update()
    {
        float dx = speed * Time.deltaTime;
        transform.position += new Vector3(dx, 0f, 0f);

        // Destruir si ha llegado al punto relativo al jugador
        if (player != null && transform.position.x <= player.position.x + destroyOffset)
        {
            Destroy(gameObject);
        }
    }

    protected override void PerformAttack()
    {
        // Este tower no ataca activamente
    }

    protected override void Die()
    {
        StatsManager.Instance.ResetMaxHP();
        base.Die();
        // Puedes añadir animación o efecto especial de "tower explotando" aquí
    }
}
