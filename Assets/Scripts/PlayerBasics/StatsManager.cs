using UnityEngine;
using System;

[System.Serializable]
public class StatsData
{
    [Header("HP")]
    public float maxHP; public float currentHP;
    [Header("Projectile")]
    public float projectileDamage;public float projectileSpeed;
    [Header("Movimiento")]
    public float moveForce;public float jumpForce;public float maxSpeed;public float friction;
    [Header("FireArm")]
    public float fireRate;
    public float radius;
    [Header("Damage")]
    public float damagePercentage;
    [Header("Armor")]
    public float armorPercentage;
    public float armorAmount;
    [Header("XP")]
    public float xpMultiplier;


    public StatsData Clone()
    {
        return new StatsData
        {
            maxHP = this.maxHP,
            currentHP = this.currentHP,
            projectileDamage = this.projectileDamage,
            projectileSpeed = this.projectileSpeed,
            moveForce = this.moveForce,
            jumpForce = this.jumpForce,
            maxSpeed = this.maxSpeed,
            friction = this.friction,
            fireRate = this.fireRate,
            radius = this.radius,
            damagePercentage = this.damagePercentage,
            armorPercentage = this.armorPercentage,
            armorAmount = this.armorAmount,
            xpMultiplier = this.xpMultiplier
        };
    }
}

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance { get; private set; }

    [Header("Template Stats")] //Estadisticas base de esta run, siempre las mismas, se reinician
    [SerializeField] private StatsData templateStats = new StatsData();
    public StatsData RuntimeStats { get; private set; }

    [Header("Leveling System")]
    public int playerLevel = 1;
    public float currentXP = 0;
    float baseXPToLevel = 100f; // XP necesaria para subir del nivel 1 al 2
    float xpMultiplierPerLevel = 1.15f; // crecimiento moderado de XP necesaria por nivel


    [Header("Player Invulnerability")] // Parpadeo e invulnerabilidad tras recibir daño
    [SerializeField] public float iFrameDuration = 0.8f;
    [SerializeField] public float flashfloaterval = 0.08f;

    [Header("Player Renderers")] // Renderers que parpadean al recibir daño
    [SerializeField] public SpriteRenderer[] renderersToFlash;

    //NOTIFICAN A OBSERVERS
    public event Action<float, float> OnHealthChanged; // Notifica el HP actual y maximo
    public event Action<int> OnLevelUp; // Notifica el nivel alcanzado
    public event Action OnPlayerDied; // Notifica la muerte del jugador

    private bool isInvulnerable = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        RuntimeStats = templateStats.Clone();
        OnHealthChanged?.Invoke(RuntimeStats.currentHP, RuntimeStats.maxHP);

        if (renderersToFlash == null || renderersToFlash.Length == 0)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                renderersToFlash = new SpriteRenderer[] { sr };
        }
    }

    // --- Este es el metodo que finalmente otorga al jugador la experiencia total que ganara por eliminar el enemigo---
    public void GainXP(float xp)
    {
        // Mostramos cuánta XP se gana
        Debug.Log($"Gained {xp} XP");

        currentXP += xp;

        // Mostramos XP actual y lo que falta para subir de nivel
        float xpToLevel = baseXPToLevel * Mathf.Pow(xpMultiplierPerLevel, playerLevel - 1);
        Debug.Log($"Current XP: {currentXP}/{xpToLevel}");

        while (currentXP >= xpToLevel)
        {
            currentXP -= xpToLevel;
            LevelUp();
            xpToLevel = baseXPToLevel * Mathf.Pow(xpMultiplierPerLevel, playerLevel - 1);
            Debug.Log($"Leveled up! New Level: {playerLevel}, XP remaining: {currentXP}/{xpToLevel}");
        }
    }

    // Este metodo maneja el proceso de subir de nivel
    private void LevelUp()
    {
        playerLevel++;
     //   Debug.Log($"¡Subiste al nivel {playerLevel}!");
        OnLevelUp?.Invoke(playerLevel);
        // Aquí puedes abrir UI de selección de 3 upgrades
        UpgradeManager.Instance.ShowUpgradeOptions(3);
    }

    //Este metodo calcula la experiencia que se otorga por eliminar un enemigo segun su nivel, la base de XP que da cada enemigo y el multiplicador de XP actual del jugador
    public float GetXPForEnemy(int enemyLevel, float baseXP)
    {
        // Escalamos de forma exponencial o lineal suave según el nivel del enemigo
        float enemyXP = baseXP * Mathf.Pow(1, enemyLevel - 1);
        return enemyXP * RuntimeStats.xpMultiplier;
    }


    // --- Player Health Methods ---

             // Aplica daño al jugador, considerando la invulnerabilidad temporal
    public void DamagePlayer(float amount)
    {
        if (amount <= 0 || isInvulnerable) return;

        RuntimeStats.currentHP = Mathf.Max(0, RuntimeStats.currentHP - amount);
        Debug.Log($"Player took {amount} damage. Current HP: {RuntimeStats.currentHP}/{RuntimeStats.maxHP}");
        OnHealthChanged?.Invoke(RuntimeStats.currentHP, RuntimeStats.maxHP);

        if (iFrameDuration > 0f) StartCoroutine(InvulnerabilityCoroutine());

        if (RuntimeStats.currentHP <= 0)
            PlayerDeath();
    }

    // --- Player Escalado Methods ---
    public void HealPlayer(float amount)
    {
        if (amount <= 0) return;
        RuntimeStats.currentHP = Mathf.Min(RuntimeStats.maxHP, RuntimeStats.currentHP + amount);
        OnHealthChanged?.Invoke(RuntimeStats.currentHP, RuntimeStats.maxHP);
    }

    public void AddMaxHP(float delta)
    {
        RuntimeStats.maxHP = Mathf.Max(1, RuntimeStats.maxHP + delta);
        RuntimeStats.currentHP = Mathf.Min(RuntimeStats.currentHP, RuntimeStats.maxHP);
        OnHealthChanged?.Invoke(RuntimeStats.currentHP, RuntimeStats.maxHP);
    }

    public void AddProjectileDamage(float delta)
    {
        RuntimeStats.projectileDamage = Mathf.Max(0, RuntimeStats.projectileDamage + delta);
    }

    public void AddDamagePercentage(float delta)
    {
        RuntimeStats.damagePercentage = Mathf.Max(0, RuntimeStats.damagePercentage + delta);
    }

    public void AddProjectileSpeed(float delta)
    {
        RuntimeStats.projectileSpeed = Mathf.Max(0, RuntimeStats.projectileSpeed + delta);
    }
    
    public void AddRadiusToGun(float delta)
    {
        RuntimeStats.radius = Mathf.Max(0, RuntimeStats.radius + delta);
    }
    public void AddArmor(float delta)
    {
        RuntimeStats.armorAmount = Mathf.Max(0, RuntimeStats.armorAmount + delta);
    }


    // --- Invulnerability Coroutine ---
    private System.Collections.IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;
        if (renderersToFlash != null && renderersToFlash.Length > 0)
        {
            float elapsed = 0f;
            bool visible = true;
            while (elapsed < iFrameDuration)
            {
                foreach (var r in renderersToFlash)
                    if (r != null) r.enabled = visible;

                visible = !visible;
                yield return new WaitForSeconds(flashfloaterval);
                elapsed += flashfloaterval;
            }
            foreach (var r in renderersToFlash)
                if (r != null) r.enabled = true;
        }
        else
            yield return new WaitForSeconds(iFrameDuration);

        isInvulnerable = false;
    }

    private void PlayerDeath()
    {
        OnPlayerDied?.Invoke();
        Debug.Log("Jugador murió");
    }

    // --- Reset para nueva run ---
    public void ResetStats()
    {
        RuntimeStats = templateStats.Clone();
        OnHealthChanged?.Invoke(RuntimeStats.currentHP, RuntimeStats.maxHP);
    }
}
