
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
    Knockback,
    Nodo
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
    [SerializeField] private float chanceToOfferSkillNodeUnlock = 0.25f; // 25%
    [SerializeField] private UpgradeQuality skillNodeUpgradeQuality = UpgradeQuality.Epic; // o Legendary
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
        FindObjectOfType<SkillTreeUI>()?.InitializeForRun();
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

        if (upgrade is Upgrade_SkillNode skillNodeUpgrade)
        {
            // ...eliminamos este upgrade del pool global
            allUpgrades.Remove(upgrade);

            // Por si estaba dentro de algún grupo también lo borramos
            foreach (var group in upgradeGroups)
            {
                group.upgrades.Remove(upgrade);
            }
        }
    }
    public void RemoveUpgradesRelatedToNode(ItemNode node)
    {
        if (node == null) return;

        // Busca upgrades de tipo Upgrade_SkillNode que apunten a este nodo
        var toRemove = allUpgrades
            .Where(u => u is Upgrade_SkillNode sn && sn.nodeToUnlock == node)
            .ToList();

        foreach (var up in toRemove)
        {
            allUpgrades.Remove(up);
            foreach (var g in upgradeGroups)
                g.upgrades.Remove(up);
        }
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

        // Primero decidimos si esta tanda incluye un desbloqueo de nodo
        bool includeSkillNode = Random.value < chanceToOfferSkillNodeUnlock;

        Upgrade skillNodeUpgrade = null;
        if (includeSkillNode)
        {
            skillNodeUpgrade = TryCreateSkillNodeUpgrade();
            if (skillNodeUpgrade != null)
            {
                options.Add(skillNodeUpgrade);
                count--; // ya hemos puesto una, pedimos una menos del pool normal
            }
        }

        // Rellenamos el resto con upgrades normales
        while (options.Count < Mathf.Max(1, count) && attempts < 200)
        {
            attempts++;
            if (allUpgrades == null || allUpgrades.Count == 0) break;

            int index = Random.Range(0, allUpgrades.Count);
            Upgrade candidate = allUpgrades[index];

            // Evitamos duplicados en esta selección
            if (options.Contains(candidate)) continue;

            if (RollSpawnByQuality(candidate.quality))
            {
                options.Add(candidate);
            }
        }

        // Si no conseguimos 3, rellenamos con cualquiera (para que siempre haya 3 opciones)
        while (options.Count < 3 && allUpgrades.Count > 0)
        {
            var fallback = allUpgrades[Random.Range(0, allUpgrades.Count)];
            if (!options.Contains(fallback))
                options.Add(fallback);
        }

        return options.Take(3).ToList();
    }
    private Upgrade TryCreateSkillNodeUpgrade()
    {
        var skillTree = FindObjectOfType<SkillTreeUI>();
        if (skillTree == null)
        {
            Debug.LogWarning("No se encontró SkillTreeUI en la escena.");
            return null;
        }

        var runEffectManager = RunEffectManager.Instance;
        if (runEffectManager == null)
        {
            Debug.LogWarning("No se encontró RunEffectManager.");
            return null;
        }

        var availableNodes = new List<ItemNode>();

        foreach (var family in skillTree.skillFamilies)
        {
            if (family?.nodes == null) continue;
            foreach (var node in family.nodes)
            {
                if (node == null) continue;

                // NUEVA CONDICIÓN:
                // Solo si el efecto NO está activo esta run
                if (node.effectToActivate != null &&
                    !runEffectManager.IsEffectActive(node.effectToActivate))
                {
                    availableNodes.Add(node);
                }
                // Opcional: también permitir nodos sin efecto (raros, pero por completitud)
                else if (node.effectToActivate == null)
                {
                    availableNodes.Add(node);
                }
            }
        }

        if (availableNodes.Count == 0)
        {
            // Debug.Log("No hay nodos disponibles cuyo efecto no esté ya activo esta run.");
            return null;
        }

        var chosenNode = availableNodes[Random.Range(0, availableNodes.Count)];

        var upgradeInstance = ScriptableObject.CreateInstance<Upgrade_SkillNode>();
        upgradeInstance.name = $"UNLOCK_{chosenNode.nodeId}";
        upgradeInstance.upgradeName = $"¡NUEVA HABILIDAD!\n{chosenNode.displayName}";
        upgradeInstance.description = $"Desbloquea:\n{chosenNode.description}";
        upgradeInstance.quality = skillNodeUpgradeQuality;
        upgradeInstance.nodeToUnlock = chosenNode;

        return upgradeInstance;
    }
}
