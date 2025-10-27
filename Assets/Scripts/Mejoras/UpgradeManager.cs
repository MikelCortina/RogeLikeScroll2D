using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public enum StatCategory
{
    HP,
    Projectile,
    Movimiento,
    FireArm,
    Damage,
    Armor,
    XP,
    Dodge,
    Suerte,
    Knockback
}

[System.Serializable]
public class UpgradeGroup
{
    public StatCategory category;
    [Tooltip("Arrastra aquí los ScriptableObjects de tipo Upgrade que pertenezcan a esta categoría.")]
    public List<Upgrade> upgrades = new List<Upgrade>();
}

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    // Grupos visibles en el inspector para organizar los upgrades por tipo de estadística.
    public List<UpgradeGroup> upgradeGroups = new List<UpgradeGroup>();

    // Lista global usada en runtime. Oculta en inspector porque se rellena desde los grupos.
    [HideInInspector]
    public List<Upgrade> allUpgrades = new List<Upgrade>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        SyncAllUpgradesFromGroups();
    }

    /// <summary>
    /// Rellena la lista allUpgrades a partir de los grupos visibles en el inspector.
    /// Llamar si editas manualmente los grupos desde el inspector (o pulsas el botón "Sync" en el custom editor).
    /// </summary>
    public void SyncAllUpgradesFromGroups()
    {
        allUpgrades.Clear();
        foreach (var g in upgradeGroups)
        {
            if (g == null || g.upgrades == null) continue;
            // Evitamos duplicados; si quieres permitir duplicados quita Distinct()
            allUpgrades.AddRange(g.upgrades);
        }

        // opcional: eliminar nulos y duplicados
        allUpgrades = allUpgrades.Where(u => u != null).Distinct().ToList();
    }

    public void ShowUpgradeOptions(int count)
    {
        List<Upgrade> options = new List<Upgrade>();
        int attempts = 0;

        while (options.Count < count && attempts < 100)
        {
            attempts++;
            if (allUpgrades == null || allUpgrades.Count == 0) break;

            int index = Random.Range(0, allUpgrades.Count);
            Upgrade candidate = allUpgrades[index];

            if (RollSpawnByQuality(candidate.quality))
            {
                options.Add(candidate); // agregamos el ScriptableObject original, no lo modificamos
            }
        }

        // Aquí abres la UI para que el jugador seleccione uno
    }

    public void ApplyUpgrade(Upgrade upgrade)
    {
        upgrade.Apply(StatsManager.Instance);
    }

    private bool RollSpawnByQuality(UpgradeQuality quality)
    {
        float roll = Random.value; // entre 0 y 1

        switch (quality)
        {
            case UpgradeQuality.Rare:
                return roll < 0.6f; // 60% de spawn
            case UpgradeQuality.Epic:
                return roll < 0.3f; // 30% de spawn
            case UpgradeQuality.Legendary:
                return roll < 0.1f; // 10% de spawn
            default:
                return false;
        }
    }

    public List<Upgrade> GetRandomUpgrades(int count)
    {
        List<Upgrade> options = new List<Upgrade>();
        int attempts = 0;

        while (options.Count < count && attempts < 200)
        {
            attempts++;
            if (allUpgrades == null || allUpgrades.Count == 0) break;
            int index = Random.Range(0, allUpgrades.Count);
            Upgrade candidate = allUpgrades[index];

            if (RollSpawnByQuality(candidate.quality))
            {
                options.Add(candidate);
            }
        }

        return options;
    }
}
