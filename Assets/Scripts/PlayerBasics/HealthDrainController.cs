using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class HealthDecay : MonoBehaviour
{
    public static HealthDecay Instance { get; private set; }

    private StatsManager statsManager;

    [Header("Decay Settings")]
    public float baseDecayPerSecond;    // Vida que baja por segundo
    private float damageMultiplier = 2f;      // Aumento temporal del decay al recibir daño
    public float maxHPRecoverPerKill; // Vida máxima que se recupera al matar enemigos
    public float velocidadDecayTrasDaino;

    private float decaySpeed;
    private float accumulatedDecay = 0f;
    public bool aceleracion = true;
    public bool halfDecay = false;

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
    }

    private void Update()
    {
        if (statsManager == null || statsManager.RuntimeStats.currentHP <= 0) return;
        if (halfDecay) decaySpeed = decaySpeed / 2f;
       
        // Decaimiento de vida continuo
        accumulatedDecay += decaySpeed * Time.deltaTime;
        if (accumulatedDecay >= 1f)
        {
            int decayInt = Mathf.FloorToInt(accumulatedDecay);
            accumulatedDecay -= decayInt;


            // statsManager.DamagePlayerDecay(decayInt);
            statsManager.AddCurrentMaxHP(-decayInt); // También resta de maxHP
            statsManager.NotifyHealthChanged();
        }

       
    }

    public void OnTakeDamage()
    {
        if (!aceleracion) return;
        decaySpeed += velocidadDecayTrasDaino;
    }
    public void ResetDecay()
    {
       decaySpeed = baseDecayPerSecond;
    }

    // Llamar desde EnemyBase.Die()
    public void GetBackHP()
    {
        Debug.Log(" recuperada");
        if (statsManager == null) return;

        float baseMaxHP = statsManager.RuntimeStats.maxHP;
        float currentMaxHP = statsManager.RuntimeStats.currentMaxHP;

        if (currentMaxHP < baseMaxHP)
        {
            float newMaxHP = Mathf.Min(currentMaxHP + maxHPRecoverPerKill, baseMaxHP);
            statsManager.RuntimeStats.currentMaxHP = newMaxHP;
            Debug.Log($"GetBackHP called. baseMaxHP: {baseMaxHP}, currentMaxHP: {currentMaxHP}, recover: {maxHPRecoverPerKill}");

            statsManager.NotifyHealthChanged();
        }
    }
}

