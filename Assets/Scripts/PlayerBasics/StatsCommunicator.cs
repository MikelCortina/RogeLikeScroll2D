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
        if (incomingDamage <= 0) return 0f;

        StatsData stats = statsManager.RuntimeStats;

        // 1️⃣ Verificar dodge
        float dodgeRoll = Random.value; // entre 0 y 1
        if (dodgeRoll < stats.meleDodgeChance)
        {
            Debug.Log("Player dodged the attack!");
            return 0f;
        }

        // 2️⃣ Aplicar reducción de daño por armadura
        if (stats.armorPercentage > 0)
        {
            float damageAfterArmor = incomingDamage - incomingDamage * (stats.armorPercentage / 100);
            //Debug.Log($"Incoming damage {incomingDamage} Player took {damageAfterArmor} damage after armor reduction of {stats.armor}%");
            damageAfterArmor = Mathf.Max(0f, damageAfterArmor); // No puede ser negativo
            return damageAfterArmor;
        }
        else
        {
            return incomingDamage;

        }
        
    }
    public float CalculateRangeTakenDamage(float incomingDamage)
    {
        if (incomingDamage <= 0) return 0f;

        StatsData stats = statsManager.RuntimeStats;

        // 1️⃣ Verificar dodge
        float dodgeRoll = Random.value; // entre 0 y 1
        if (dodgeRoll < stats.rangeDodgeChance)
        {
            Debug.Log("Player dodged the attack!");
            return 0f;
        }

        // 2️⃣ Aplicar reducción de daño por armadura
        if (stats.armorPercentage > 0)
        {
            float damageAfterArmor = incomingDamage - incomingDamage * (stats.armorPercentage / 100);
            //Debug.Log($"Incoming damage {incomingDamage} Player took {damageAfterArmor} damage after armor reduction of {stats.armor}%");
            damageAfterArmor = Mathf.Max(0f, damageAfterArmor); // No puede ser negativo
            return damageAfterArmor;
        }
        else
        {
            return incomingDamage;

        }

    }
    //Calcular el daino final que hace el jugador, teniendo en cuenta crítico y porcentaje de daino
    public float CalculateGunDamage()
    {
        StatsData stats = statsManager.RuntimeStats;
        float baseDamage = stats.gunDamage;
        if (baseDamage <= 0) return 0f;

        float damage = baseDamage;

        float roll = Random.value; // 0..1
        if (roll < stats.criticalChance/100)
        {
            damage *= 2f;
            Debug.Log($"Critical hit! roll={roll} critChance={stats.criticalChance}");
        }
        else
        {
            Debug.Log($"No crit. roll={roll} critChance={stats.criticalChance}");
        }

        return damage;
    }
    public float CalculateExplosionDamage()
    {
        StatsData stats = statsManager.RuntimeStats;
        float baseDamage = stats.explosionDamage;
        if (baseDamage <= 0) return 0f;

        float damage = baseDamage;

        float roll = Random.value; // 0..1
        if (roll < stats.criticalChance / 100)
        {
            damage *= 2f;
            Debug.Log($"Critical hit! roll={roll} critChance={stats.criticalChance}");
        }
        else
        {
            Debug.Log($"No crit. roll={roll} critChance={stats.criticalChance}");
        }

        return damage;
    }


    public void HealPlayer(float amount) => statsManager.HealPlayer(amount);
    public void DamagePlayer(float amount) => statsManager.DamagePlayer(amount);
}
