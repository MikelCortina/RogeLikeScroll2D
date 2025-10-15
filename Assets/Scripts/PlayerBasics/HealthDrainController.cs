using UnityEngine;

public class HealthDecay : MonoBehaviour
{
    public static HealthDecay Instance { get; private set; }

    private StatsManager statsManager;

    [Header("Decay Settings")]
    public float baseDecayPerSecond = 1f;    // Vida que baja por segundo
    public float damageMultiplier = 2f;      // Aumento temporal del decay al recibir daño
    public float maxHPRecoverPerKill = 0.5f; // Vida máxima que se recupera al matar enemigos

    private float decaySpeed;
    private float accumulatedDecay = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Intentamos asignar StatsManager
        statsManager = StatsManager.Instance;
        decaySpeed = baseDecayPerSecond;
    }

    private void Start()
    {
        // Si por algún motivo statsManager aún es null
        if (statsManager == null)
            statsManager = StatsManager.Instance;

        // Suscribirse al evento de recibir daño
        if (statsManager != null)
            statsManager.OnTakeDamage += OnTakeDamage;
    }

    private void OnDestroy()
    {
        if (statsManager != null)
            statsManager.OnTakeDamage -= OnTakeDamage;
    }

    private void Update()
    {
        if (statsManager == null || statsManager.RuntimeStats.currentHP <= 0) return;

        // Decaimiento de vida continuo
        accumulatedDecay += decaySpeed * Time.deltaTime;
        if (accumulatedDecay >= 1f)
        {
            int decayInt = Mathf.FloorToInt(accumulatedDecay);
            accumulatedDecay -= decayInt;

            statsManager.DamagePlayerDecay(decayInt);
            statsManager.AddMaxHP(-decayInt); // También resta de maxHP
            statsManager.NotifyHealthChanged();
        }

        // DecaySpeed vuelve gradualmente a baseDecayPerSecond
        if (decaySpeed > baseDecayPerSecond)
            decaySpeed -= damageMultiplier * Time.deltaTime; // ajuste suave
        decaySpeed = Mathf.Max(baseDecayPerSecond, decaySpeed);
    }

    private void OnTakeDamage(float damage)
    {
        // Aumentar temporalmente la velocidad de decay
        decaySpeed += damageMultiplier;
    }

    // Llamar desde EnemyBase.Die()
    public void EnemyDie()
    {
        if (statsManager == null) return;

        float baseMaxHP = statsManager.RuntimeStats.maxHP;
        float currentMaxHP = statsManager.RuntimeStats.maxHP;

        if (currentMaxHP < baseMaxHP)
        {
            float newMaxHP = Mathf.Min(currentMaxHP + maxHPRecoverPerKill, baseMaxHP);
            statsManager.RuntimeStats.maxHP = newMaxHP;

            // También recuperamos algo de vida actual
            statsManager.RuntimeStats.currentHP = Mathf.Min(
                statsManager.RuntimeStats.currentHP + maxHPRecoverPerKill, newMaxHP
            );

            statsManager.NotifyHealthChanged();
        }
    }
}
