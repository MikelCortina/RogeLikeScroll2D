using UnityEngine;

public class StatsCommunicator : MonoBehaviour
{
    public static StatsCommunicator Instance { get; private set; }

    private StatsManager statsManager => StatsManager.Instance;

    // Buffs temporales, multiplicadores, etc.
    private float projectileDamageMultiplier = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    // --- Métodos de acceso ---
    public float GetCurrentHP() => statsManager.RuntimeStats.currentHP;
    public float GetMaxHP() => statsManager.RuntimeStats.maxHP;
    public float GetProjectileDamage() => statsManager.RuntimeStats.gunDamage * projectileDamageMultiplier;



    // --- Métodos para modificar estadísticas ---

    //Calcular el daino final recibido por el jugador, teniendo en cuenta armadura y dodge
    public float CalculateMeleTakenDamage(float incomingDamage)
    {
        if (incomingDamage <= 0f) return 0f;

        StatsData stats = statsManager.RuntimeStats;
        if (stats == null)
        {
            Debug.LogWarning("Stats missing in CalculateMeleTakenDamage, applying full damage.");
            return incomingDamage;
        }

        // Asegúrate si stats.meleDodgeChance está en 0..1 o en 0..100.
        // Si lo guardas como porcentaje (ej. 20 -> 20%), conviértelo a probabilidad:
        float dodgeChance = stats.meleDodgeChance;
        if (dodgeChance > 1f) dodgeChance = Mathf.Clamp01(dodgeChance / 100f);
        else dodgeChance = Mathf.Clamp01(dodgeChance);

        // 1️⃣ Verificar dodge
        float dodgeRoll = Random.value; // entre [0,1)
        if (dodgeRoll < dodgeChance)
        {
            Debug.Log($"Player dodged the attack! roll={dodgeRoll:F3}, needed<{dodgeChance:F3}");
            return 0f;
        }

        // 2️⃣ Aplicar reducción de daño por armadura
        float damageAfterArmor = incomingDamage;
        if (stats.armorPercentage > 0f)
        {
            float armorPct = Mathf.Clamp(stats.armorPercentage, 0f, 100f);
            damageAfterArmor = incomingDamage * (1f - armorPct / 100f);
            damageAfterArmor = Mathf.Max(0f, damageAfterArmor);
        }

        Debug.Log($"Incoming {incomingDamage} -> final {damageAfterArmor} (armor {stats.armorPercentage}%)");
        return damageAfterArmor;
    }
    public float CalculateRangeTakenDamage(float incomingDamage)
    {
        if (incomingDamage <= 0f) return 0f;

        StatsData stats = statsManager.RuntimeStats;
        if (stats == null)
        {
            Debug.LogWarning("Stats missing in CalculateRangeTakenDamage, applying full damage.");
            return incomingDamage;
        }

        // 1️⃣ Calcular chance de dodge (admite tanto 0–1 como 0–100)
        float dodgeChance = stats.rangeDodgeChance;
        if (dodgeChance > 1f)
            dodgeChance = Mathf.Clamp01(dodgeChance / 100f);
        else
            dodgeChance = Mathf.Clamp01(dodgeChance);

        float dodgeRoll = Random.value; // entre [0,1)
        if (dodgeRoll < dodgeChance)
        {
            Debug.Log($"Player DODGED ranged attack! roll={dodgeRoll:F3}, needed<{dodgeChance:F3}");
            return 0f;
        }

        // 2️⃣ Aplicar reducción por armadura
        float damageAfterArmor = incomingDamage;
        if (stats.armorPercentage > 0f)
        {
            float armorPct = Mathf.Clamp(stats.armorPercentage, 0f, 100f);
            damageAfterArmor = incomingDamage * (1f - armorPct / 100f);
            damageAfterArmor = Mathf.Max(0f, damageAfterArmor);
        }

        Debug.Log($"Ranged hit: {incomingDamage} -> {damageAfterArmor} (armor {stats.armorPercentage}%)");
        return damageAfterArmor;
    }

    //Calcular el daino final que hace el jugador, teniendo en cuenta crítico y porcentaje de daino
    // Helper genérico
    public float CalculateDamageWithCrit(float baseDamage, float critChanceRaw, float critMultiplier = 2f)
    {
        if (baseDamage <= 0f) return 0f;

        // Normalizar crit chance
        float critChance = critChanceRaw > 1f ? Mathf.Clamp01(critChanceRaw / 100f) : Mathf.Clamp01(critChanceRaw);

        float roll = Random.value;
        bool isCrit = roll < critChance;

        float damage = baseDamage * (isCrit ? critMultiplier : 1f);

        if (isCrit)
            Debug.Log($"CRIT! roll={roll:F3} < critChance={critChance:F3} -> damage={damage:F2}");
        else
            Debug.Log($"No crit. roll={roll:F3} >= critChance={critChance:F3} -> damage={damage:F2}");

        return damage;
    }

    // Uso desde los métodos concretos
    public float CalculateGunDamage()
    {
        StatsData stats = statsManager.RuntimeStats;
        if (stats == null) { Debug.LogWarning("Stats missing in CalculateGunDamage"); return 0f; }
        float critMult = 2f;
        return CalculateDamageWithCrit(stats.gunDamage, stats.criticalChance, critMult);
    }

    public float CalculateExplosionDamage()
    {
        StatsData stats = statsManager.RuntimeStats;
        if (stats == null) { Debug.LogWarning("Stats missing in CalculateExplosionDamage"); return 0f; }
        float critMult =2f;
        return CalculateDamageWithCrit(stats.explosionDamage, stats.criticalChance, critMult);
    }



    public void HealPlayer(float amount) => statsManager.HealPlayer(amount);
    public void DamagePlayer(float amount) => statsManager.DamagePlayer(amount);
}
